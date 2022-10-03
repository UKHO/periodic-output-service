namespace AvcsXmlCatalogueBuilder;

public class RulesBasedUnitsAndFoliosProvider : IUnitsAndFoliosProvider
{
    private const string PAYS = "PAYSF";
    private readonly IEnumerable<string> listOfAgenciesExcludedFromPays;

    private readonly ISet<string> paysFolioContents = new HashSet<string>();
    private readonly ISet<string> units = new HashSet<string>();

    public RulesBasedUnitsAndFoliosProvider(string listOfAgenciesExcludedFromPays)
    {
        this.listOfAgenciesExcludedFromPays = listOfAgenciesExcludedFromPays.Split(",").Select(a => a.Trim()).ToList();
    }

    public (IEnumerable<string> units, IEnumerable<string> folios) GetUnitsAndFolios(string productName)
    {
        units.Add(productName);
        if (!listOfAgenciesExcludedFromPays.Contains(productName.Substring(0, 2)))
        {
            paysFolioContents.Add(productName);
            return (new[] { productName }, new[] { PAYS });
        }

        return (new[] { productName }, new string[] { });
    }

    public IDictionary<string, IEnumerable<string>> Units =>
        units.ToDictionary(u => u, u => (IEnumerable<string>)new[] { u });

    public IDictionary<string, IEnumerable<string>> Folios => new Dictionary<string, IEnumerable<string>>
        { { PAYS, paysFolioContents.OrderBy(e => e).ToArray() } };
}