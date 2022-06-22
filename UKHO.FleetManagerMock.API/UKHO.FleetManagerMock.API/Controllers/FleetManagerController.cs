using Microsoft.AspNetCore.Mvc;
using UKHO.FleetManagerMock.API.Filters;

namespace UKHO.FleetManagerMock.API.Controllers
{
    [ApiController]
    public class FleetManagerController : BaseController
    {
    
        protected IConfiguration configuration;

        public FleetManagerController(IHttpContextAccessor httpContextAccessor, IConfiguration configuration):base(httpContextAccessor)
        {
            this.configuration = configuration;
        }

    }
}

