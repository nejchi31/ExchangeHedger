using Hedger.Core.Enum;

namespace Hedger.Core
{
    public class ProcessedOrder
    {
        public string Exchange { get; set; } = string.Empty;

        public OrderTypeEnum OrderType { get; set; }

        /// <summary>
        /// Amount of BTC to buy/sell.
        /// </summary>
        public decimal AmountBtc { get; set; }

        /// <summary>
        /// Price per BTC in EUR.
        /// </summary>
        public decimal Price { get; set; }
    }
}
