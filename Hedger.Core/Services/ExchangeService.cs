using Hedger.Core.Domain;
using Hedger.Core.Enum;
using Hedger.Core.Interfaces;
using Hedger.Core.Models;

namespace Hedger.Core.Services
{
    public class ExchangeService : IExchangeService
    {
        public async Task<ExchangeResult> ExecuteOrder(List<ExchangeState> exchanges, OrderTypeEnum orderType, decimal targetBtc)
        {
            if (exchanges == null) throw new ArgumentNullException(nameof(exchanges));
            if (targetBtc <= 0) throw new ArgumentOutOfRangeException(nameof(targetBtc), "Target BTC must be positive.");

            return orderType == OrderTypeEnum.Buy
                ? ExecuteBuy(exchanges, targetBtc)
                : ExecuteSell(exchanges, targetBtc);
        }

        #region BUY SIDE

        private ExchangeResult ExecuteBuy(List<ExchangeState> exchanges, decimal targetBtc)
        {
            var result = new ExchangeResult();
            var asks = BuildAskBook(exchanges);
            var sortedAsks = asks.OrderBy(a => a.Price).ToList();

            decimal remaining = targetBtc;

            foreach (var ask in sortedAsks)
            {
                if (remaining <= 0) break;

                var maxPossible = CalculateMaxBuyAtLevel(ask, remaining);
                if (maxPossible <= 0) continue;

                ApplyBuyAtLevel(result, ask, ref remaining, maxPossible);
            }

            result.FullyFilled = remaining <= 0;
            return result;
        }

        private List<BookEntry> BuildAskBook(List<ExchangeState> exchanges)
        {
            var asks = new List<BookEntry>();

            foreach (var exchange in exchanges)
            {
                foreach (var ask in exchange.OrderBook.Asks)
                {
                    if (ask.Amount <= 0 || ask.Price <= 0) continue;

                    asks.Add(new BookEntry
                    {
                        Exchange = exchange,
                        Side = BookSideEnum.Ask,
                        Price = ask.Price,
                        AvailableBtc = ask.Amount
                    });
                }
            }

            return asks;
        }

        private decimal CalculateMaxBuyAtLevel(BookEntry ask, decimal remaining)
        {
            var maxByLevel = ask.AvailableBtc;
            var maxByBalance = ask.Exchange.EurBalance > 0
                ? ask.Exchange.EurBalance / ask.Price
                : 0m;

            return Math.Min(remaining, Math.Min(maxByLevel, maxByBalance));
        }

        private void ApplyBuyAtLevel(
            ExchangeResult result,
            BookEntry ask,
            ref decimal remaining,
            decimal amountToBuy)
        {
            var eurCost = amountToBuy * ask.Price;

            ask.Exchange.EurBalance -= eurCost;
            remaining -= amountToBuy;

            result.TotalBtc += amountToBuy;
            result.TotalEur += eurCost;

            AddOrMergeExecutionOrder(
                result.Orders,
                ask.Exchange.Exchange,
                OrderTypeEnum.Buy,
                amountToBuy,
                ask.Price);
        }

        #endregion

        #region SELL SIDE

        private ExchangeResult ExecuteSell(List<ExchangeState> exchanges, decimal targetBtc)
        {
            var result = new ExchangeResult();
            var bids = BuildBidBook(exchanges);
            var sortedBids = bids.OrderByDescending(b => b.Price).ToList();

            decimal remaining = targetBtc;

            foreach (var bid in sortedBids)
            {
                if (remaining <= 0) break;

                var maxPossible = CalculateMaxSellAtLevel(bid, remaining);
                if (maxPossible <= 0) continue;

                ApplySellAtLevel(result, bid, ref remaining, maxPossible);
            }

            result.FullyFilled = remaining <= 0;
            return result;
        }

        private List<BookEntry> BuildBidBook(List<ExchangeState> exchanges)
        {
            var bids = new List<BookEntry>();

            foreach (var exchange in exchanges)
            {
                foreach (var bid in exchange.OrderBook.Bids)
                {
                    if (bid.Amount <= 0 || bid.Price <= 0) continue;

                    bids.Add(new BookEntry
                    {
                        Exchange = exchange,
                        Side = BookSideEnum.Bid,
                        Price = bid.Price,
                        AvailableBtc = bid.Amount
                    });
                }
            }

            return bids;
        }

        private decimal CalculateMaxSellAtLevel(BookEntry bid, decimal remaining)
        {
            var maxByLevel = bid.AvailableBtc;
            var maxByBalance = bid.Exchange.BtcBalance;

            return Math.Min(remaining, Math.Min(maxByLevel, maxByBalance));
        }

        private void ApplySellAtLevel(
            ExchangeResult result,
            BookEntry bid,
            ref decimal remaining,
            decimal amountToSell)
        {
            var eurReceived = amountToSell * bid.Price;

            bid.Exchange.BtcBalance -= amountToSell;
            remaining -= amountToSell;

            result.TotalBtc += amountToSell;
            result.TotalEur += eurReceived;

            AddOrMergeExecutionOrder(
                result.Orders,
                bid.Exchange.Exchange,
                OrderTypeEnum.Sell,
                amountToSell,
                bid.Price);
        }

        // ---------- Shared helpers ----------

        private static void AddOrMergeExecutionOrder(
            List<ProcessedOrder> orders,
            string exchange,
            OrderTypeEnum type,
            decimal amountBtc,
            decimal price)
        {
            var existing = orders.FirstOrDefault(o =>
                o.Exchange == exchange &&
                o.OrderType == type &&
                o.Price == price);

            if (existing != null)
            {
                existing.AmountBtc += amountBtc;
                return;
            }

            orders.Add(new ProcessedOrder
            {
                Exchange = exchange,
                OrderType = type,
                AmountBtc = amountBtc,
                Price = price
            });
        }


        #endregion
    }
}
