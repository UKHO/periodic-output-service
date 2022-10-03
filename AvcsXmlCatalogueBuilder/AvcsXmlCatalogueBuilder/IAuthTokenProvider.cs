namespace AvcsXmlCatalogueBuilder;

public interface IAuthTokenProvider
{
    Task<string> GetAuthToken();
}