namespace Hedger.Core.Domain
{
    public class ExchangeConfigModel
    {
        public int EurBalanceMin { get; set; }
        public int EurBalanceMax { get; set; }
        public int BtcBalanceMin { get; set; }
        public int BtcBalanceMax { get; set; }

        public int ExchangeGenerateCount { get; set; }
    }
}
