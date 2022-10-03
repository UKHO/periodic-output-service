namespace AvcsXmlCatalogueBuilder;

public interface IUnitsAndFoliosProvider
{
    (IEnumerable<string> units, IEnumerable<string> folios) GetUnitsAndFolios(string productName);
    IDictionary<string, IEnumerable<string>> Units { get; }
    IDictionary<string, IEnumerable<string>> Folios { get; }
}