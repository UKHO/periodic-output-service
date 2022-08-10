using Microsoft.AspNetCore.Mvc;
using UKHO.FmEssFssMock.API.Models.Response;
using UKHO.FmEssFssMock.API.Services;

namespace UKHO.FmEssFssMock.API.Controllers
{
    [ApiController]
    public class ExchangeSetServiceController : ControllerBase
    {
        private readonly ExchangeSetService _exchangeSetService;

        public ExchangeSetServiceController(ExchangeSetService exchangeSetService)
        {
            _exchangeSetService = exchangeSetService;
        }

        [HttpPost]
        [Route("/ess/productData/productIdentifiers")]
        public IActionResult PostProductIdentifiers([FromBody] string[] productIdentifiers, [FromQuery] string? callbackUri)
        {
            if (productIdentifiers != null && productIdentifiers.Any())
            {
                ExchangeSetServiceResponse? response = _exchangeSetService.CreateExchangeSet(productIdentifiers);
                if (response == null)
                {
                    return BadRequest();
                }
                return Ok(response.ResponseBody);
            }
            return BadRequest();
        }
    }
}
