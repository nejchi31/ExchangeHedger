using Hedger.Core.Domain;
using Hedger.Core.Models;
using Hedger.Core.Repositories.Interfaces;
using Newtonsoft.Json;

namespace Hedger.Core.Repositories.RepositoryImpl
{
    /// <summary>
    /// Repository that loads a single "dummy" exchange from a file
    /// like order_books_data, taking the last snapshot as current book.
    /// </summary>
    public class ExchangeRepository : IExchangeRepository
    {
        private readonly string _orderBooksFilePath;
        private readonly string _exchangeId;
        private readonly decimal _eurBalance;
        private readonly decimal _btcBalance;
        public ExchangeRepository
            (string orderBooksFilePath,
            string exchangeId = "DummyExchange",
            decimal eurBalance = 100_000m,
            decimal btcBalance = 10m)
        {
            _orderBooksFilePath = orderBooksFilePath ?? throw new ArgumentNullException(nameof(orderBooksFilePath));
            _exchangeId = exchangeId;
            _eurBalance = eurBalance;
            _btcBalance = btcBalance;
        }
        public List<ExchangeState> GetAllExchanges()
        {
            if (!File.Exists(_orderBooksFilePath))
            {
                throw new FileNotFoundException("Order books file not found", _orderBooksFilePath);
            }

            // We will use the LAST non-empty line as "current" snapshot
            string? lastJsonPart = null;

            foreach (var line in File.ReadLines(_orderBooksFilePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                int tabIndex = line.IndexOf('\t');
                if (tabIndex < 0)
                    continue; // malformed line

                var jsonPart = line[(tabIndex + 1)..].Trim();
                if (!string.IsNullOrEmpty(jsonPart))
                {
                    lastJsonPart = jsonPart;
                }
            }

            if (lastJsonPart == null)
            {
                throw new InvalidOperationException("No valid order book snapshots found in file.");
            }

            var snapshot = JsonConvert.DeserializeObject<OrderBookFileModel>(lastJsonPart);
            if (snapshot == null)
            {
                throw new InvalidOperationException("Failed to deserialize last order book snapshot.");
            }

            var exchange = new ExchangeState
            {
                Exchange = _exchangeId,
                EurBalance = _eurBalance,
                BtcBalance = _btcBalance,
                OrderBook = new OrderBook
                {
                    Bids = snapshot.Bids?
                        .Select(b => new OrderBookEntry
                        {
                            Price = b.Order.Price,
                            Amount = b.Order.Amount
                        })
                        .ToList() ?? new List<OrderBookEntry>(),

                    Asks = snapshot.Asks?
                        .Select(a => new OrderBookEntry
                        {
                            Price = a.Order.Price,
                            Amount = a.Order.Amount
                        })
                        .ToList() ?? new List<OrderBookEntry>()
                }
            };

            return new List<ExchangeState> { exchange };
        }
    }
    
}
