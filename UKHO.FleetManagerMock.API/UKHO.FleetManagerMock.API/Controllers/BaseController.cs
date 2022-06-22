using Microsoft.AspNetCore.Mvc;
using UKHO.FleetManagerMock.API.Filters;

namespace UKHO.FleetManagerMock.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BaseController : ControllerBase
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        protected new HttpContext HttpContext => httpContextAccessor.HttpContext;

        public BaseController(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        protected string GetCurrentCorrelationId()
        {
            return httpContextAccessor.HttpContext.Request.Headers[CorrelationIdMiddleware.XCorrelationIdHeaderKey].FirstOrDefault();
        }

    }
}

