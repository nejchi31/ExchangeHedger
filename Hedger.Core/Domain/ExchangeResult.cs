namespace Hedger.Core.Models
{
    public class ExchangeResult
    {
        /// <summary>
        /// List of concrete orders to place on particular exchanges.
        /// </summary>
        public List<ProcessedOrder> Orders { get; set; } = new List<ProcessedOrder>();

        /// <summary>
        /// Total BTC bought or sold.
        /// </summary>
        public decimal TotalBtc { get; set; }

        /// <summary>
        /// Total EUR spent (for buy) or received (for sell).
        /// </summary>
        public decimal TotalEur { get; set; }

        /// <summary>
        /// True if we managed to fully fill the requested BTC amount.
        /// </summary>
        public bool FullyFilled
        {
            get; set;
        }
    }
}
