using Hedger.Core.Enum;
using Hedger.Core.Interfaces;
using Hedger.Core.Models;
using Hedger.Core.Services;


namespace Hedger.Tests
{
    public class ExchangeServiceTests
    {
        private readonly IExchangeService _service = new ExchangeService();
        [Fact]
        public async Task ExecuteMetaOrder_Buy_FullyFilled_UsesCheapestAsksAcrossExchangesAsync()
        {
            // Test: Buy fully filled, cheapest asks across exchanges
            var exchanges = new List<ExchangeState>
            {
                new ExchangeState
                {
                    Exchange = "Ex1",
                    EurBalance = 100_000m,
                    BtcBalance = 0m,
                    OrderBook = new OrderBook
                    {
                        Asks = new List<OrderBookEntry>
                        {
                            new() { Price = 31_000m, Amount = 2m } // more expensive
                        }
                    }
                },
                new ExchangeState
                {
                    Exchange = "Ex2",
                    EurBalance = 100_000m,
                    BtcBalance = 0m,
                    OrderBook = new OrderBook
                    {
                        Asks = new List<OrderBookEntry>
                        {
                            new() { Price = 30_000m, Amount = 1m },  // cheaper
                            new() { Price = 30_500m, Amount = 1m }   // middle
                        }
                    }
                }
            };

            var targetBtc = 2m;

            // Act
            var result = await _service.ExecuteMetaOrder(exchanges, OrderTypeEnum.Buy, targetBtc);

            // Assert
            Assert.True(result.FullyFilled);
            Assert.Equal(2m, result.TotalBtc);

            // Check order breakdown: should first use 1 BTC @ 30000 (Ex2),
            // then 1 BTC @ 30500 (Ex2), not the expensive Ex1.
            Assert.Equal(2, result.Orders.Count);

            Assert.Contains(result.Orders, o =>
                o.Exchange == "Ex2" &&
                o.OrderType == OrderTypeEnum.Buy &&
                o.Price == 30_000m &&
                o.AmountBtc == 1m);

            Assert.Contains(result.Orders, o =>
                o.Exchange == "Ex2" &&
                o.OrderType == OrderTypeEnum.Buy &&
                o.Price == 30_500m &&
                o.AmountBtc == 1m);

            Assert.DoesNotContain(result.Orders, o => o.Exchange == "Ex1");
        }


        [Fact]
        public async Task ExecuteMetaOrder_Buy_LimitedByEurBalance_PartialFillAsync()
        {
            // Test: cheap asks but not enough EUR
            var exchanges = new List<ExchangeState>
            {
                new ExchangeState
                {
                    Exchange = "Ex1",
                    EurBalance = 10_000m,
                    BtcBalance = 0m,
                    OrderBook = new OrderBook
                    {
                        Asks = new List<OrderBookEntry>
                        {
                            new() { Price = 20_000m, Amount = 2m }
                        }
                    }
                }
            };

            var targetBtc = 1m;

            // Act
            var result = await _service.ExecuteMetaOrder(exchanges, OrderTypeEnum.Buy, targetBtc);

            // Assert
            Assert.False(result.FullyFilled);      // cannot reach 1 BTC
            Assert.Equal(0.5m, result.TotalBtc);   // only half
            Assert.Equal(10_000m, result.TotalEur);  // exchange has only 10000 €

            Assert.Single(result.Orders); // single order
            var order = result.Orders[0];
            Assert.Equal("Ex1", order.Exchange); // order executed at Ex1
            Assert.Equal(OrderTypeEnum.Buy, order.OrderType); // order type BUY
            Assert.Equal(0.5m, order.AmountBtc); // order amount BTC 0.5
            Assert.Equal(20_000m, order.Price); // price at 20000 €
        }


        [Fact]
        public async Task ExecuteMetaOrder_Sell_UsesHighestBidsAcrossExchangesAsync()
        {
            // Test: Sell chooses highest bids
            var exchanges = new List<ExchangeState>
            {
                new ExchangeState
                {
                    Exchange = "Ex1",
                    EurBalance = 0m,
                    BtcBalance = 1m,
                    OrderBook = new OrderBook
                    {
                        Bids = new List<OrderBookEntry>
                        {
                            new() { Price = 30_000m, Amount = 1m } // lower
                        }
                    }
                },
                new ExchangeState
                {
                    Exchange = "Ex2",
                    EurBalance = 0m,
                    BtcBalance = 1m,
                    OrderBook = new OrderBook
                    {
                        Bids = new List<OrderBookEntry>
                        {
                            new() { Price = 31_000m, Amount = 0.5m } // higher
                        }
                    }
                }
            };

            var targetBtc = 1m;

            // Act
            var result = await _service.ExecuteMetaOrder(exchanges, OrderTypeEnum.Sell, targetBtc);

            // Assert
            Assert.True(result.FullyFilled);
            Assert.Equal(1m, result.TotalBtc);

            // We should sell 0.5 BTC on Ex2 at 31k, then 0.5 BTC on Ex1 at 30k
            Assert.Equal(2, result.Orders.Count);

            Assert.Contains(result.Orders, o =>
                o.Exchange == "Ex2" &&
                o.OrderType == OrderTypeEnum.Sell &&
                o.Price == 31_000m &&
                o.AmountBtc == 0.5m);

            Assert.Contains(result.Orders, o =>
                o.Exchange == "Ex1" &&
                o.OrderType == OrderTypeEnum.Sell &&
                o.Price == 30_000m &&
                o.AmountBtc == 0.5m);
        }


        [Fact]
        public async Task ExecuteMetaOrder_Buy_NoAsks_ProducesNoOrdersAsync()
        {
            //Test: No liquidity(no asks)
            var exchanges = new List<ExchangeState>
            {
                new ExchangeState
                {
                    Exchange = "Ex1",
                    EurBalance = 100_000m,
                    BtcBalance = 0m,
                    OrderBook = new OrderBook
                    {
                        Asks = new List<OrderBookEntry>() // empty
                    }
                }
            };

            var result = await _service.ExecuteMetaOrder(exchanges, OrderTypeEnum.Buy, 1m);

            Assert.False(result.FullyFilled);
            Assert.Equal(0m, result.TotalBtc);
            Assert.Empty(result.Orders);
        }

        [Fact]
        public async Task ExecuteMetaOrder_Sell_NoBids_ProducesNoOrders()
        {
            //Test: No liquidity(no bids)
            var exchanges = new List<ExchangeState>
            {
                new ExchangeState
                {
                    Exchange = "Ex1",
                    EurBalance = 0m,
                    BtcBalance = 1m,
                    OrderBook = new OrderBook
                    {
                        Bids = new List<OrderBookEntry>() // empty
                    }
                }
            };

            var result = await _service.ExecuteMetaOrder(exchanges, OrderTypeEnum.Sell, 1m);

            Assert.False(result.FullyFilled);
            Assert.Equal(0m, result.TotalBtc);
            Assert.Empty(result.Orders);
        }

        [Fact]
        public async Task ExecuteMetaOrder_InvalidAmount_Throws()
        {
            var exchanges = new List<ExchangeState>(); // doesn't matter

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                _service.ExecuteMetaOrder(exchanges, OrderTypeEnum.Buy, 0m));
        }
    }
}
