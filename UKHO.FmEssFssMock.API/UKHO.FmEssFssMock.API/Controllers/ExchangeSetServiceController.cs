using Microsoft.AspNetCore.Mvc;
using UKHO.FmEssFssMock.API.Models.Response;
using UKHO.FmEssFssMock.API.Services;

namespace UKHO.FmEssFssMock.API.Controllers
{
    [ApiController]
    public class ExchangeSetServiceController : ControllerBase
    {
        private readonly ExchangeSetService _exchangeSetService;
        readonly FleetManagerController _fleetManagerController;

        public ExchangeSetServiceController(ExchangeSetService exchangeSetService, FleetManagerController fleetManagerController)
        {
            _exchangeSetService = exchangeSetService;
            _fleetManagerController = fleetManagerController;
        }

        [HttpPost]
        [Route("/ess/productData/productIdentifiers")]
        public IActionResult PostProductIdentifiers([FromBody] string[] productIdentifiers, [FromQuery] string? callbackUri)
        {
            if (productIdentifiers != null && productIdentifiers.Any())
            {
                ExchangeSetServiceResponse? response = _exchangeSetService.GetProductIdentifier("productIdentifier-" + string.Join("-", productIdentifiers));
                if (response != null)
                {
                    return Ok(response.ResponseBody);
                }
            }
            
            return BadRequest();
        }
    }
}
