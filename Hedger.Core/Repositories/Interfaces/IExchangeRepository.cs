using Hedger.Core.Domain;
using Hedger.Core.Models;

namespace Hedger.Core.Repositories.Interfaces
{
    public interface IExchangeRepository
    {
        /// <summary>
        /// Returns a random selection of exchanges built from the file.
        /// </summary>
        /// <param name="exchangeCount">
        /// How many exchanges to return. If &lt;= 0 or greater than available,
        /// all available exchanges will be returned.
        /// </param>
        List<ExchangeState> GetExchanges(ExchangeConfigModel exchangeConfig);
    }
}
