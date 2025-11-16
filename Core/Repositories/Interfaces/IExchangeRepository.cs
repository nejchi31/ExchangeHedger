using Hedger.Core.Models;

namespace Hedger.Core.Repositories.Interfaces
{
    public interface IExchangeRepository
    {        
        /// <summary>
        /// Returns current state of all exchanges: order books + balances.
        /// </summary>
        List<ExchangeState> GetAllExchanges();
    }
}
