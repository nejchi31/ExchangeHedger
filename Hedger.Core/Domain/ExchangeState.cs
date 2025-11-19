namespace Hedger.Core.Models
{
    public class ExchangeState
    {
        /// <summary>
        /// Exchange name
        /// </summary>
        public string Exchange { get; set; } = string.Empty;

        /// <summary>
        /// Current order book snapshot for this exchange.
        /// </summary>
        public OrderBook OrderBook { get; set; } = new OrderBook();

        /// <summary>
        /// Available EUR on this exchange.
        /// </summary>
        public decimal EurBalance { get; set; }

        /// <summary>
        /// Available BTC on this exchange.
        /// </summary>
        public decimal BtcBalance { get; set; }
    }
}
