using Hedger.Core.Domain;
using Hedger.Core.Enum;
using Hedger.Core.Interfaces;
using Hedger.Core.Models;
using Hedger.Core.Repositories.Interfaces;
using Hedger.Core.Repositories.RepositoryImpl;
using Hedger.Core.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var filePath = @"order_books_data"; 

        // 1) Ask user for exchange config
        var config = ReadExchangeConfigFromConsole();

        // 2) Build repository & service
        IExchangeRepository repo = new ExchangeRepository(filePath);
        IExchangeService service = new ExchangeService();

        // 3) Load N exchanges based on config
        var exchanges = repo.GetExchanges(config);

        Console.WriteLine();
        Console.WriteLine($"Generated {exchanges.Count} exchanges:");
        foreach (var ex in exchanges)
        {
            Console.WriteLine($"  {ex.Exchange}: EUR={ex.EurBalance}, BTC={ex.BtcBalance}");
        }
        Console.WriteLine();

        // 4) Ask user for order
        var (orderType, amountBtc) = ReadUserOrderFromConsole();

        // 👇 NEW: print best 10 book orders per exchange
        PrintTopOfBookPerExchange(exchanges, orderType, topN: 10);

        // 5) Execute meta-order across all exchanges
        var result = await service.ExecuteOrder(exchanges, orderType, amountBtc);

        // 6) Print result
        PrintMetaExchangeResult(result);
    }

    private static ExchangeConfigModel ReadExchangeConfigFromConsole()
    {
        Console.Write("How many exchanges to generate? ");
        int.TryParse(Console.ReadLine(), out var exCount);

        Console.Write("EUR balance min (e.g. 0): ");
        int.TryParse(Console.ReadLine(), out var eurMin);

        Console.Write("EUR balance max (e.g. 40000): ");
        int.TryParse(Console.ReadLine(), out var eurMax);

        Console.Write("BTC balance min (e.g. 0): ");
        int.TryParse(Console.ReadLine(), out var btcMin);

        Console.Write("BTC balance max (e.g. 5): ");
        int.TryParse(Console.ReadLine(), out var btcMax);

        return new ExchangeConfigModel
        {
            ExchangeGenerateCount = exCount,
            EurBalanceMin = eurMin,
            EurBalanceMax = eurMax,
            BtcBalanceMin = btcMin,
            BtcBalanceMax = btcMax
        };
    }

    private static (OrderTypeEnum orderType, decimal amountBtc) ReadUserOrderFromConsole()
    {
        Console.Write("Order type (Buy/Sell): ");
        var typeStr = (Console.ReadLine() ?? "").Trim();

        Console.Write("Amount BTC: ");
        var amountStr = Console.ReadLine();
        var amount = decimal.TryParse(amountStr, out var a) ? a : 0m;

        var orderType = typeStr.Equals("sell", StringComparison.OrdinalIgnoreCase)
            ? OrderTypeEnum.Sell
            : OrderTypeEnum.Buy;

        return (orderType, amount);
    }

    private static void PrintMetaExchangeResult(ExchangeResult result)
    {
        Console.WriteLine();
        Console.WriteLine("=== Best Execution Plan ===");
        Console.WriteLine($"Fully filled: {result.FullyFilled}");
        Console.WriteLine($"Total BTC:    {result.TotalBtc}");
        Console.WriteLine($"Total EUR:    {result.TotalEur}");
        Console.WriteLine();

        if (result.Orders.Count == 0)
        {
            Console.WriteLine("No executable orders were found with given balances and books.");
            return;
        }

        Console.WriteLine("Orders to execute:");
        foreach (var o in result.Orders)
        {
            Console.WriteLine(
                $"  {o.Exchange}: {o.OrderType} {o.AmountBtc} BTC @ {o.Price} EUR");
        }
    }

    private static void PrintTopOfBookPerExchange(
        List<ExchangeState> exchanges,
        OrderTypeEnum orderType,
        int topN = 10)
    {
        Console.WriteLine();
        Console.WriteLine("=== Top of Order Books (per exchange) ===");

        foreach (var ex in exchanges)
        {
            Console.WriteLine();
            Console.WriteLine($"Exchange {ex.Exchange} (EUR={ex.EurBalance}, BTC={ex.BtcBalance})");

            var sideName = orderType == OrderTypeEnum.Buy ? "Asks (sell orders)" : "Bids (buy orders)";
            Console.WriteLine($"  Showing top {topN} {sideName}:");
            Console.WriteLine("    Price (EUR)     Amount (BTC)");

            if (orderType == OrderTypeEnum.Buy)
            {
                // We care about Asks: lowest price first
                var topAsks = ex.OrderBook.Asks
                    .Where(a => a.Price > 0 && a.Amount > 0)
                    .OrderBy(a => a.Price)
                    .Take(topN);

                foreach (var a in topAsks)
                {
                    Console.WriteLine($"    {a.Price,10:0.00}     {a.Amount,10:0.########}");
                }
            }
            else
            {
                // We care about Bids: highest price first
                var topBids = ex.OrderBook.Bids
                    .Where(b => b.Price > 0 && b.Amount > 0)
                    .OrderByDescending(b => b.Price)
                    .Take(topN);

                foreach (var b in topBids)
                {
                    Console.WriteLine($"    {b.Price,10:0.00}     {b.Amount,10:0.########}");
                }
            }
        }

        Console.WriteLine();
    }
}