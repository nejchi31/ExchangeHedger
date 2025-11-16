using Hedger.Api.Model;
using Hedger.Core.Enum;
using Hedger.Core.Models;

namespace Hedger.Api.Mapping
{
    public static class ExchangeMappings
    {
        public static OrderTypeEnum MapToDomainOrderType(this string value)
        {
            return value?.ToLower() switch
            {
                "buy" => OrderTypeEnum.Buy,
                "sell" => OrderTypeEnum.Sell,
                _ => throw new ArgumentException($"Unknown order type: {value}")
            };
        }

        public static ExecutionResponseDTO MapToDTO(this ExchangeResult result)
        {
            return new ExecutionResponseDTO
            {
                FullyFilled = result.FullyFilled,
                TotalBtc = result.TotalBtc,
                TotalEur = result.TotalEur,
                Orders = result.Orders
                    .Select(o => new ExecutionOrderDTO
                    {
                        Exchange = o.Exchange,
                        OrderType = o.OrderType.ToString(),
                        AmountBtc = o.AmountBtc,
                        Price = o.Price
                    })
                    .ToList()
            };
        }
    }
}
