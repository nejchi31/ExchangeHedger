namespace Hedger.Core.Models
{
    public class OrderBook
    {
        /// <summary>
        /// Bids = people willing to BUY BTC (you use them when the user is SELLING).
        /// </summary>
        public List<OrderBookEntry> Bids { get; set; } = new List<OrderBookEntry>();

        /// <summary>
        /// Asks = people willing to SELL BTC (you use them when the user is BUYING).
        /// </summary>
        public List<OrderBookEntry> Asks { get; set; } = new List<OrderBookEntry>();
    }
}
