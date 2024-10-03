using Microsoft.AspNetCore.Mvc;
using UKHO.FmEssFssMock.API.Enums;
using UKHO.FmEssFssMock.API.Models.Request;
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
        public IActionResult GetProductDataSinceDateTime([FromQuery] string sinceDateTime, [FromQuery] string? exchangeSetStandard)
        {
            if (!string.IsNullOrEmpty(sinceDateTime))
            {
                if (!string.IsNullOrEmpty(exchangeSetStandard))
                {
                    exchangeSetStandard = exchangeSetStandard.ToLower();
                }

                if (string.IsNullOrEmpty(exchangeSetStandard) || exchangeSetStandard.Equals(ExchangeSetStandard.s63.ToString()) || exchangeSetStandard.Equals(ExchangeSetStandard.s57.ToString()))
                {
                    ExchangeSetServiceResponse response = _exchangeSetService.CreateExchangeSetForGetProductDataSinceDateTime(sinceDateTime, exchangeSetStandard);
                    return Ok(response.ResponseBody);
                }
            }
            return BadRequest();
        }

        [HttpPost]
        [Route("/ess/productData/productIdentifiers")]
        public IActionResult PostProductIdentifiers([FromBody] string[] productIdentifiers, [FromQuery] string? exchangeSetStandard)
        {
            if (productIdentifiers.Any())
            {
                if (!string.IsNullOrEmpty(exchangeSetStandard))
                {
                    exchangeSetStandard = exchangeSetStandard.ToLower();
                }

                if (string.IsNullOrEmpty(exchangeSetStandard) || exchangeSetStandard.Equals(ExchangeSetStandard.s63.ToString()) || exchangeSetStandard.Equals(ExchangeSetStandard.s57.ToString()))
                {
                    ExchangeSetServiceResponse? response = _exchangeSetService.CreateExchangeSetForPostProductIdentifier(productIdentifiers, exchangeSetStandard);
                    return Ok(response?.ResponseBody);
                }
            }
            return BadRequest();
        }

        [HttpPost]
        [Route("/ess/productData/productVersions")]
        public IActionResult PostProductVersions([FromBody] List<ProductVersionRequest> productVersionRequest, [FromQuery] string? exchangeSetStandard)
        {
            if (productVersionRequest.Any())
            {
                if (!string.IsNullOrEmpty(exchangeSetStandard))
                {
                    exchangeSetStandard = exchangeSetStandard.ToLower();
                }

                if (string.IsNullOrEmpty(exchangeSetStandard) || exchangeSetStandard.Equals(ExchangeSetStandard.s63.ToString()) || exchangeSetStandard.Equals(ExchangeSetStandard.s57.ToString()) && !productVersionRequest.Any(i => i.EditionNumber == null && i.UpdateNumber == null))
                {
                    ExchangeSetServiceResponse response = _exchangeSetService.CreateExchangeSetForPostProductVersion(productVersionRequest, exchangeSetStandard);
                    return Ok(response.ResponseBody);
                }
            }
            return BadRequest();
        }
    }
}
