
using Hedger.Core.Enum;
using Hedger.Core.Models;

namespace Hedger.Core.Domain
{
    public class BookEntry
    {
        public ExchangeState Exchange { get; set; } = null!;
        public BookSideEnum Side { get; set; }
        public decimal Price { get; set; }
        public decimal AvailableBtc { get; set; }
    }
}
