using Hedger.Api.Mapping;
using Hedger.Api.Model;
using Hedger.Core.Domain;
using Hedger.Core.Enum;
using Hedger.Core.Interfaces;
using Hedger.Core.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Hedger.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangeController : ControllerBase
    {
        private readonly IExchangeService _metaExchangeService;
        private readonly IExchangeRepository _exchangeRepository;

        public ExchangeController(
            IExchangeService metaExchangeService,
            IExchangeRepository exchangeRepository)
        {
            _metaExchangeService = metaExchangeService;
            _exchangeRepository = exchangeRepository;
        }

        /// <summary>
        /// Returns the best execution plan for a buy/sell BTC order.
        /// </summary>
        [HttpPost("execute-order-plan")]
        public async Task<ActionResult<ExecutionResponseDTO>> Post([FromBody] ExecutionRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!ModelState.IsValid)
            {
                throw new ArgumentException("Request model is invalid.");
            }

            var orderType = request.OrderType.MapToDomainOrderType();

            if (request.AmountBtc <= 0)
            {
                throw new ArgumentException("AmountBtc must be positive.", nameof(request.AmountBtc));
            }

            // 2) Build exchange config (for now hardcoded; could be moved to config later)
            var config = new ExchangeConfigModel
            {
                ExchangeGenerateCount = 3, // e.g. simulate 3 exchanges
                EurBalanceMin = 0,
                EurBalanceMax = 40_000,
                BtcBalanceMin = 0,
                BtcBalanceMax = 5
            };

            // 3) Load exchanges based on config
            var exchanges = _exchangeRepository.GetExchanges(config);

            // 4) Execute order
            var result = await _metaExchangeService.ExecuteOrder(exchanges, orderType, request.AmountBtc);

            // 5) Map to DTO and return
            var response = result.MapToDTO();
            return Ok(response);
        }

        [HttpPost("test-exception")]
        public IActionResult TestException()
        {
            throw new InvalidOperationException("This is a test error - testing exception middleware");
        }
    }
}

