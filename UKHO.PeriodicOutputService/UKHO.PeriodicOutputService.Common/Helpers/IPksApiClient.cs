using Azure.Core;
using System;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IPksApiClient
    {
        Task<HttpResponseMessage> PostPksDataAsync(HttpMethod httpMethod, string payloadJson, string accessToken, string uri, string correlationId = "");
    }
}
