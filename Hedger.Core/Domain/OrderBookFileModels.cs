namespace Hedger.Core.Domain
{
    // Matches the JSON part AFTER the tab in each line.
    internal class OrderBookFileModel
    {
        public string AcqTime { get; set; } = string.Empty;
        public List<OrderEntryFileModel> Bids { get; set; } = new();
        public List<OrderEntryFileModel> Asks { get; set; } = new();
    }

    internal class OrderEntryFileModel
    {
        public OrderFileModel Order { get; set; } = new();
    }

    internal class OrderFileModel
    {
        public decimal Amount { get; set; }
        public decimal Price { get; set; }

        // other fields (Id, Time, Type, Kind) exist in file but we don't need them
    }
}
