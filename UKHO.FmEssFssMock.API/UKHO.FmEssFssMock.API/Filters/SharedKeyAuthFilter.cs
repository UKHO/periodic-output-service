using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using UKHO.FmEssFssMock.API.Common;

namespace UKHO.FmEssFssMock.API.Filters
{
    public class SharedKeyAuthFilter : IAuthorizationFilter
    {
        private readonly IOptions<SharedKeyConfiguration> _sharedKeyConfig;

        public const string SharedKeyHeaderName = "key";

        public SharedKeyAuthFilter(IOptions<SharedKeyConfiguration> sharedKeyConfig)
        {
            _sharedKeyConfig = sharedKeyConfig;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Query.TryGetValue(SharedKeyHeaderName, out var extractedSharedKey))
            {
                context.Result = new UnauthorizedObjectResult("Shared key is missing in request.");
                return;
            }

            var configSharedKey = _sharedKeyConfig.Value.Key;

            if (!configSharedKey.Equals(extractedSharedKey))
            {
                context.Result = new UnauthorizedObjectResult("Invalid Shared Key.");
            }
        }
    }
}
