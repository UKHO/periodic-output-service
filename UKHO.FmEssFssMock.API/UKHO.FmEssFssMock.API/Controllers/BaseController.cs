﻿using Microsoft.AspNetCore.Mvc;
using UKHO.FmEssFssMock.API.Filters;

namespace UKHO.FmEssFssMock.API.Controllers
{
    [ApiController]
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
