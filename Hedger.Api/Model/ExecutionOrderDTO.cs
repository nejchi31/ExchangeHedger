namespace Hedger.Api.Model
{
    public class ExecutionOrderDTO
    {
        public string Exchange { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty; // "Buy" / "Sell"
        public decimal AmountBtc { get; set; }
        public decimal Price { get; set; }
    }
}
