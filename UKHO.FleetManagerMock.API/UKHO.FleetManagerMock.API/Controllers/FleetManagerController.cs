using Microsoft.AspNetCore.Mvc;
using UKHO.FleetManagerMock.API.Filters;
using UKHO.FleetManagerMock.API.Services;

namespace UKHO.FleetManagerMock.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FleetManagerController : ControllerBase
    {
        protected IConfiguration configuration;
        private readonly FleetManagerService _fleetManagerService;

        public FleetManagerController(IConfiguration configuration, FleetManagerService fleetManager)
        {
            this.configuration = configuration;
            this._fleetManagerService = fleetManager;
           
        }




        [HttpGet("GetJwtAuthUnpToken")]
        public IActionResult GetJwtAuthUnpToken()
        {
            string appName = this.configuration.GetSection("FleetManagerB2BApiConfiguration")["userName"];



            string site = this.configuration.GetSection("FleetManagerB2BApiConfiguration")["password"];
            return Ok(appName);
        }



    }
}

