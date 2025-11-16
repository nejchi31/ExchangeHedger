using Hedger.Core.Domain;
using Hedger.Core.Enum;
using Hedger.Core.Interfaces;
using Hedger.Core.Repositories;
using Hedger.Core.Repositories.RepositoryImpl;
using Hedger.Core.Services;

class Program
{
    static void Main(string[] args)
    {
        var repo = new ExchangeRepository(@"C:\Users\nejc.necemer\OneDrive - L-TEK d.o.o\Dokumenti\StuttgartBoerse\order_books_data");
        IExchangeService service = new ExchangeService();

        var exchanges = repo.GetAllExchanges();

        var result = service.ExecuteMetaOrder(exchanges, OrderTypeEnum.Buy, 1.0m);

        // print result...
    }
}