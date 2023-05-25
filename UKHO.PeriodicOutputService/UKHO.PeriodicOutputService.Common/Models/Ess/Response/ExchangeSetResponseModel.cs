using Newtonsoft.Json;

namespace UKHO.PeriodicOutputService.Common.Models.Ess.Response
{
    public class ExchangeSetResponseModel
    {
        [JsonProperty("_links")]
        public Links? Links { get; set; }

        public DateTime? ExchangeSetUrlExpiryDateTime { get; set; }

        [JsonProperty("requestedProductCount")]
        public int RequestedProductCount { get; set; }

        [JsonProperty("exchangeSetCellCount")]
        public int ExchangeSetCellCount { get; set; }

        [JsonProperty("requestedProductsAlreadyUpToDateCount")]
        public int RequestedProductsAlreadyUpToDateCount { get; set; }

        [JsonProperty("requestedAioProductCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? RequestedAioProductCount { get; set; } = null;

        [JsonProperty("aioExchangeSetCellCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? AioExchangeSetCellCount { get; set; } = null;

        [JsonProperty("RequestedAioProductsAlreadyUpToDateCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? RequestedAioProductsAlreadyUpToDateCount { get; set; } = null;

        public IEnumerable<RequestedProductsNotInExchangeSet>? RequestedProductsNotInExchangeSet { get; set; }

        public DateTime ResponseDateTime { get; set; }

        [JsonProperty("fssBatchId", NullValueHandling = NullValueHandling.Ignore)]
        public string BatchId { get; set; }
    }

    public class Links
    {
        public LinkSetBatchStatusUri? ExchangeSetBatchStatusUri { get; set; }
        public LinkSetBatchDetailsUri? ExchangeSetBatchDetailsUri { get; set; }
        public LinkSetFileUri? ExchangeSetFileUri { get; set; }

        [JsonProperty("aioExchangeSetFileUri", NullValueHandling = NullValueHandling.Ignore)]
        public LinkSetFileUri? AioExchangeSetFileUri { get; set; } = null;
    }

    public class LinkSetBatchStatusUri
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
    }

    public class LinkSetBatchDetailsUri
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
    }
    public class LinkSetFileUri
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
    }

    public class RequestedProductsNotInExchangeSet
    {
        [JsonProperty("productName")]
        public string? ProductName { get; set; }

        [JsonProperty("reason")]
        public string? Reason { get; set; }

    }

}
