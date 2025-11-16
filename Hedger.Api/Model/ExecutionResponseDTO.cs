namespace Hedger.Api.Model
{
    public class ExecutionResponseDTO
    {
        public bool FullyFilled { get; set; }
        public decimal TotalBtc { get; set; }
        public decimal TotalEur { get; set; }
        public List<ExecutionOrderDTO> Orders { get; set; } = new();
    }
}
