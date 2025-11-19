using Hedger.Core.Enum;
using Hedger.Core.Models;

namespace Hedger.Core.Interfaces
{
    public interface IExchangeService
    {
        Task<ExchangeResult> ExecuteMetaOrder(List<ExchangeState> exchanges, OrderTypeEnum orderType,decimal targetBtc);
    }
}
