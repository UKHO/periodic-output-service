using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface ISalesCatalogueClient
    {
        public Task<HttpResponseMessage> CallSalesCatalogueServiceApi(HttpMethod method, string? requestBody, string authToken, string uri);
    }
}
