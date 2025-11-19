namespace Hedger.Core.Models
{
    public class OrderBookEntry
    {
        /// <summary>
        /// Price in EUR per 1 BTC.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Amount of BTC available at this price.
        /// </summary>
        public decimal Amount { get; set; }
    }
}
