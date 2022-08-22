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
        [Route("/ess/productData")]
        public IActionResult GetProductDataSinceDateTime([FromQuery] string sinceDateTime)
        {
            if (!string.IsNullOrEmpty(sinceDateTime))
            {
                ExchangeSetServiceResponse? response = _exchangeSetService.CreateExchangeSetForGetProductDataSinceDateTime(sinceDateTime);
                if (response == null)
                {
                    return BadRequest();
                }
                return Ok(response.ResponseBody);
            }
            return BadRequest();
        }

        [HttpPost]
        [Route("/ess/productData/productIdentifiers")]
        public IActionResult PostProductIdentifiers([FromBody] string[] productIdentifiers, [FromQuery] string? callbackUri)
        {
            if (productIdentifiers != null && productIdentifiers.Any())
            {
                ExchangeSetServiceResponse? response = _exchangeSetService.CreateExchangeSetForPostProductIdentifier(productIdentifiers);
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
