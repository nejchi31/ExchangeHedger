namespace Hedger.Api.Model
{
    public class ExecutionRequestDto
    {
        public string OrderType { get; set; } = string.Empty; // "Buy" / "Sell"
        public decimal AmountBtc { get; set; }
    }
}
