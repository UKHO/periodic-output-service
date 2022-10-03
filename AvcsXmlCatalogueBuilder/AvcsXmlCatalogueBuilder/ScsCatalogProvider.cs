using System.Net.Http.Headers;
using AvcsXmlCatalogueBuilder.Models.SalesCatalogue;
using Newtonsoft.Json;

namespace AvcsXmlCatalogueBuilder;

public interface ICatalogProvider
{
    Task<IEnumerable<SalesCatalogueDataProductResponse>> GetEncsAsync();
}

public class ScsCatalogProvider : ICatalogProvider
{
    private readonly IAuthTokenProvider authTokenProvider;
    private readonly string scsBaseUrl;

    public ScsCatalogProvider(IAuthTokenProvider authTokenProvider, string scsBaseUrl)
    {
        this.authTokenProvider = authTokenProvider;
        this.scsBaseUrl = scsBaseUrl;
    }

    public async Task<IEnumerable<SalesCatalogueDataProductResponse>> GetEncsAsync()
    {
        var uri = new Uri("v1/productData/encs57/catalogue/essData", UriKind.Relative);

        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(scsBaseUrl);
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AvcsXmlCatalogueBuilder",
            "1.0.0.0"));
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await authTokenProvider.GetAuthToken());

        var response = await httpClient.GetAsync(uri);

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var saleCatDataResponse = JsonConvert.DeserializeObject<List<SalesCatalogueDataProductResponse>>(responseBody);
        return saleCatDataResponse;
    }
}