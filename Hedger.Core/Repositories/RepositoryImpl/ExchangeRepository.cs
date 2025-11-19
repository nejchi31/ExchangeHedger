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
        private readonly Random _random = new Random();

        public ExchangeRepository(string orderBooksFilePath)
        {
            _orderBooksFilePath = orderBooksFilePath ?? throw new ArgumentNullException(nameof(orderBooksFilePath));
        }


        public List<ExchangeState> GetExchanges(ExchangeConfigModel config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            var jsonSnapshots = LoadJsonSnapshotsFromFile(_orderBooksFilePath);

            var exchangeCount = NormalizeExchangeCount(config.ExchangeGenerateCount, jsonSnapshots.Count);

            var selectedIndices = PickRandomIndices(exchangeCount, jsonSnapshots.Count, _random);

            return BuildExchanges(jsonSnapshots, selectedIndices, config, _random);
        }


        #region HELPER METHODS
        // ---------- Top-level helpers ----------

        private static List<string> LoadJsonSnapshotsFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Order books file not found", filePath);

            var snapshots = new List<string>();

            foreach (var rawLine in File.ReadLines(filePath))
            {
                var line = rawLine?.Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.Length > 0 && line[0] == '\uFEFF')
                    line = line.TrimStart('\uFEFF');

                var tabIndex = line.IndexOf('\t');
                if (tabIndex < 0)
                    continue;

                var jsonPart = line[(tabIndex + 1)..].Trim();
                if (string.IsNullOrEmpty(jsonPart))
                    continue;

                snapshots.Add(jsonPart);
            }

            if (snapshots.Count == 0)
                throw new InvalidOperationException("No valid snapshots found in order books file.");

            return snapshots;
        }

        private static int NormalizeExchangeCount(int requestedCount, int availableCount)
        {
            if (requestedCount <= 0 || requestedCount > availableCount)
                return availableCount;

            return requestedCount;
        }

        private static List<int> PickRandomIndices(int count, int total, Random rnd)
        {
            return Enumerable
                .Range(0, total)
                .OrderBy(_ => rnd.Next())
                .Take(count)
                .ToList();
        }

        private List<ExchangeState> BuildExchanges(
            List<string> jsonSnapshots,
            List<int> selectedIndices,
            ExchangeConfigModel config,
            Random rnd)
        {
            var exchanges = new List<ExchangeState>();
            var exchangeNumber = 1;

            foreach (var idx in selectedIndices)
            {
                var snapshot = DeserializeSnapshot(jsonSnapshots[idx]);
                if (snapshot is null)
                    continue;

                var exchange = CreateExchangeFromSnapshot(snapshot, exchangeNumber, config, rnd);
                exchanges.Add(exchange);
                exchangeNumber++;
            }

            return exchanges;
        }

        // ---------- Snapshot & mapping ----------

        private static OrderBookFileModel? DeserializeSnapshot(string json)
        {
            return JsonConvert.DeserializeObject<OrderBookFileModel>(json);
        }

        private ExchangeState CreateExchangeFromSnapshot(
            OrderBookFileModel snapshot,
            int exchangeNumber,
            ExchangeConfigModel config,
            Random rnd)
        {
            return new ExchangeState
            {
                Exchange = $"Ex{exchangeNumber}",
                EurBalance = GenerateRandomEurBalance(config, rnd),
                BtcBalance = GenerateRandomBtcBalance(config, rnd),
                OrderBook = new OrderBook
                {
                    Bids = MapLevels(snapshot.Bids),
                    Asks = MapLevels(snapshot.Asks)
                }
            };
        }

        private static List<OrderBookEntry> MapLevels(List<OrderEntryFileModel>? source)
        {
            if (source == null) return new List<OrderBookEntry>();

            return source
                .Where(l => l?.Order != null && l.Order.Price > 0 && l.Order.Amount > 0)
                .Select(l => new OrderBookEntry
                {
                    Price = l.Order.Price,
                    Amount = l.Order.Amount
                })
                .ToList();
        }

        // ---------- Balance generators using config ----------

        private static decimal GenerateRandomEurBalance(ExchangeConfigModel config, Random rnd)
        {
            var min = Math.Max(0, config.EurBalanceMin);
            var max = Math.Max(min, config.EurBalanceMax);

            // whole EUR values between min and max inclusive
            var value = rnd.Next(min, max + 1);
            return value;
        }

        private static decimal GenerateRandomBtcBalance(ExchangeConfigModel config, Random rnd)
        {
            var min = Math.Max(0, config.BtcBalanceMin);
            var max = Math.Max(min, config.BtcBalanceMax);

            // whole BTC values between min and max inclusive (adjust if you want decimals)
            var value = rnd.Next(min, max + 1);
            return value;
        }
        #endregion
    }

}
