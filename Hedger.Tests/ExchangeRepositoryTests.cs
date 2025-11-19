using Hedger.Core.Domain;
using Hedger.Core.Enum;
using Hedger.Core.Interfaces;
using Hedger.Core.Repositories.Interfaces;
using Hedger.Core.Repositories.RepositoryImpl;
using Hedger.Core.Services;

namespace Hedger.Tests
{
    public class ExchangeRepositoryTests
    {
        [Fact]
        public async Task FileExchangeRepository_WithSimpleFile_Buy_UsesCheapestAskAsync()
        {
            // Arrange: create temporary order_books file with 2 "exchanges" (2 lines)
            var tempFile = Path.GetTempFileName();

            try
            {
               
                var snapshot1Json = @"
{
  ""AcqTime"": ""2025-01-01T00:00:00Z"",
      ""Bids"": [
        { ""Order"": { ""Amount"": 1.0, ""Price"": 27000.0 } }, 
        { ""Order"": { ""Amount"": 0.5, ""Price"": 29500.0 } }
      ],
      ""Asks"": [
        { ""Order"": { ""Amount"": 0.5, ""Price"": 30000.0 } }, 
        { ""Order"": { ""Amount"": 1.0, ""Price"": 35000.0 } }
      ]
}";
                
                var snapshot2Json = @"
{
  ""AcqTime"": ""2025-11-17T00:00:01Z"",
      ""Bids"": [
        { ""Order"": { ""Amount"": 0.5, ""Price"": 29000.0 } }, 
        { ""Order"": { ""Amount"": 0.5, ""Price"": 28000.0 } }
      ],
      ""Asks"": [
        { ""Order"": { ""Amount"": 1.0, ""Price"": 31000.0 } }, 
        { ""Order"": { ""Amount"": 1.5, ""Price"": 36000.0 } }
      ]
}";

                // File format: timestamp \t JSON (single-line JSON)
                var line1 = "1\t" + snapshot1Json.Trim().Replace("\r", "").Replace("\n", "");
                var line2 = "2\t" + snapshot2Json.Trim().Replace("\r", "").Replace("\n", "");

                File.WriteAllLines(tempFile, new[]
                {
                    line1,
                    line2
                });

                // Optional sanity check for debugging:
                var lines = File.ReadAllLines(tempFile);
                Assert.Equal(2, lines.Length);          

                // Config: generate 2 exchanges from the file, with enough EUR/BTC balances
                var config = new ExchangeConfigModel
                {
                    ExchangeGenerateCount = 2,
                    EurBalanceMin = 50_000,
                    EurBalanceMax = 50_000,
                    BtcBalanceMin = 0,
                    BtcBalanceMax = 0
                };

                IExchangeRepository repository = new ExchangeRepository(tempFile);
                IExchangeService service = new ExchangeService();

                // Act
                var exchanges = repository.GetExchanges(config);

                // Sanity: we should have 2 exchanges
                Assert.Equal(2, exchanges.Count);

                var result = await service.ExecuteOrder(exchanges, OrderTypeEnum.Buy,1.5m);

                // Assert
                Assert.True(result.FullyFilled);
                Assert.Equal(1.5m, result.TotalBtc);

                // 0.5 * 30000 + 1.0 * 31000 = 15000 + 31000 = 46000
                Assert.Equal(46_000m, result.TotalEur);

                // We expect exactly 2 orders: However, because the repository reads order_book_data and randomly picks N exchanges, assigning them names like Ex1, Ex2, etc., the same snapshot may not always get the same exchange name between test runs. As a result, we can’t rely on a specific exchange name (e.g. “Ex1” vs “Ex2”) in this test.
                //   Ex1: Buy 0.5 BTC @ 30000
                //   Ex2: Buy 1.0 BTC @ 31000
                Assert.Equal(2, result.Orders.Count);

                Assert.Contains(result.Orders, o =>
                    o.OrderType == OrderTypeEnum.Buy &&
                    o.AmountBtc == 0.5m &&
                    o.Price == 30_000m);

                Assert.Contains(result.Orders, o =>
                    o.OrderType == OrderTypeEnum.Buy &&
                    o.AmountBtc == 1.0m &&
                    o.Price == 31_000m);
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
