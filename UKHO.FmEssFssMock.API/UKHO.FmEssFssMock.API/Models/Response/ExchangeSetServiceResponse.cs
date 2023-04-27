using Newtonsoft.Json;

namespace UKHO.FmEssFssMock.API.Models.Response
{
    public class ExchangeSetServiceResponse
    {
        public string? Id { get; set; }
        public ExchangeSetResponse? ResponseBody { get; set; }
    }

    public class ExchangeSetResponse
    {
        [JsonProperty("_links")]
        public BatchLinks? _links { get; set; }

        [JsonProperty("exchangeSetUrlExpiryDateTime")]
        public string? ExchangeSetUrlExpiryDateTime { get; set; }

        [JsonProperty("requestedProductCount")]
        public int? RequestedProductCount { get; set; }

        [JsonProperty("exchangeSetCellCount")]
        public int? ExchangeSetCellCount { get; set; }

        [JsonProperty("requestedProductsAlreadyUpToDateCount")]
        public int? RequestedProductsAlreadyUpToDateCount { get; set; }

        [JsonProperty("requestedAioProductCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? RequestedAioProductCount { get; set; } = null;

        [JsonProperty("aioExchangeSetCellCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? AioExchangeSetCellCount { get; set; } = null;

        [JsonProperty("RequestedAioProductsAlreadyUpToDateCount", NullValueHandling = NullValueHandling.Ignore)]
        public int? RequestedAioProductsAlreadyUpToDateCount { get; set; } = null;

        [JsonProperty("requestedProductsNotInExchangeSet")]
        public IEnumerable<RequestedProductsNotInExchangeSet>? RequestedProductsNotInExchangeSet { get; set; }
    }

    public class RequestedProductsNotInExchangeSet
    {
        [JsonProperty("productName")]
        public string? ProductName { get; set; }

        [JsonProperty("reason")]
        public string? Reason { get; set; }
    }

    public class BatchLinks
    {
        [JsonProperty("exchangeSetBatchStatusUri")]
        public LinkSetBatchStatusUri? ExchangeSetBatchStatusUri { get; set; }

        [JsonProperty("exchangeSetBatchDetailsUri")]
        public LinkSetBatchDetailsUri? ExchangeSetBatchDetailsUri { get; set; }

        [JsonProperty("exchangeSetFileUri")]
        public LinkSetFileUri? ExchangeSetFileUri { get; set; }

        [JsonProperty("aioExchangeSetFileUri", NullValueHandling = NullValueHandling.Ignore)]
        public LinkSetFileUri? AioExchangeSetFileUri { get; set; } = null;

        [JsonProperty("errorFileUri", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore]
        public LinkSetErrorFileUri? ExchangeSetErrorFileUri { get; set; }
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

    public class LinkSetErrorFileUri
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
    }
}
