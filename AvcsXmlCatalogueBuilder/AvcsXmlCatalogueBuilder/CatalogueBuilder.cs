using System.Text;
using System.Xml;
using System.Xml.Serialization;
using AvcsXmlCatalogueBuilder.Models.SalesCatalogue;
using UKHO.WeekNumberUtils;

namespace AvcsXmlCatalogueBuilder
{
    public class CatalogueBuilder
    {
        private const string PublicationElementName = "UKHOCatalogueFile";

        private readonly ICatalogProvider catalogueProvider;
        private readonly IUnitsAndFoliosProvider unitsAndFoliosProvider;

        public CatalogueBuilder(ICatalogProvider catalogueProvider, IUnitsAndFoliosProvider unitsAndFoliosProvider)
        {
            this.catalogueProvider = catalogueProvider;
            this.unitsAndFoliosProvider = unitsAndFoliosProvider;
        }

        public async Task<string> BuildAsync()
        {
            var encs = await catalogueProvider.GetEncsAsync();
            var encsArray = encs.Select(e =>
            {
                var cd = e.BaseCellLocation.Substring(4, 1);
                var (units, folios) = unitsAndFoliosProvider.GetUnitsAndFolios(e.ProductName);
                var ukhoCatalogueFileProductsDigitalEnc = new UKHOCatalogueFileProductsDigitalENC
                {
                    ShortName = e.ProductName,
                    Metadata = new S57Metadata
                    {
                        CatalogueNumber = e.ProductName,
                        Base_isdt = e.BaseCellIssueDate,
                        Edtn = e.BaseCellEditionNumber,
                        UPDN = e.LatestUpdateNumber?.ToString() ?? "0",
                        Last_update_isdt = e.IssueDateLatestUpdate ?? DateTime.MinValue,
                        Last_update_isdtSpecified = e.IssueDateLatestUpdate.HasValue,
                        DatasetTitle =
                            e.ProductName, //This is incorrect, but we don't have the title in the SCS catalogue endpoints atm
                        GeographicLimit = new GeographicLimit
                        {
                            BoundingBox = new BoundingBox
                            {
                                NorthLimit = Convert.ToDouble(e.CellLimitNorthernmostLatitude),
                                EastLimit = Convert.ToDouble(e.CellLimitEasternmostLatitude),
                                SouthLimit = Convert.ToDouble(e.CellLimitSouthernmostLatitude),
                                WestLimit = Convert.ToDouble(e.CellLimitWesternmostLatitude)
                            }
                        },
                        CD = new S57MetadataCD
                        {
                            Base = cd
                        },
                        Unit = units.Select(u => new MetadataTypeUnit { ID = u }).ToArray(),
                        Folio = folios.Select(f => new Folio { ID = f }).ToArray(),
                        Status = new CTypeChartStatus()
                        {
                            ChartStatus = new CTypeChartStatusChartStatus()
                            {
                                Value = GetChartStatus(e),
                                date = GetChartStatusDate(e)
                            }
                        }
                    }
                };
                return ukhoCatalogueFileProductsDigitalEnc;
            }).OrderBy(e => e.ShortName).ToArray();


            var chartStatuses = encsArray.ToDictionary(e => e.ShortName, e => e.Metadata.Status.ChartStatus.Value);

            var ukhoCatalogueFileProductsUnitOfSales = unitsAndFoliosProvider.Units.Select(kv =>
                    new UKHOCatalogueFileProductsUnitOfSale
                    {
                        ID = kv.Key,
                        CatalogueNumber = kv.Key,
                        SubUnit = kv.Value.ToArray(),
                        Status = chartStatuses.ContainsKey(kv.Key) && chartStatuses[kv.Key] == TypeChartStatus.Cancelled
                            ? TypeUnitStatus.NotForSale
                            : TypeUnitStatus.ForSale,
                        StatusSpecified = true,
                        Usage = MapUnitUsage(kv.Key),
                        UnitType = MapUnitType(kv.Key)
                    }).Concat(unitsAndFoliosProvider.Folios.Select(kv =>
                    new UKHOCatalogueFileProductsUnitOfSale
                    {
                        ID = kv.Key,
                        CatalogueNumber = kv.Key,
                        Title = "World Folio",
                        SubUnit = kv.Value.ToArray(),
                        Status = TypeUnitStatus.ForSale,
                        Usage = productTypeCode.C03,
                        UnitType = UnitType.AVCSFolioTransit
                    }))
                .ToArray();

            var catalogueFile = new UKHOCatalogueFile
            {
                BaseFileMetadata = GetStandardBaseFileMetadata(),
                Products = new UKHOCatalogueFileProducts
                {
                    Digital = new UKHOCatalogueFileProductsDigital[] { new() { ENC = encsArray } },
                    UnitOfSale = ukhoCatalogueFileProductsUnitOfSales
                }
            };

            var result = new MemoryStream();
            var serializer = MakeUkhoCatalogueFileProductsXmlSerializer();

            using (var sw = new XmlTextWriter(result, Encoding.UTF8) { Formatting = Formatting.Indented })
            {
                serializer.Serialize(sw, catalogueFile);
                result.Position = 0;
                return new StreamReader(result).ReadToEnd();
            }
        }

        private DateTime GetChartStatusDate(SalesCatalogueDataProductResponse salesCatalogueDataProductResponse)
        {
            switch (GetChartStatus(salesCatalogueDataProductResponse))
            {
                case TypeChartStatus.Base:
                    return salesCatalogueDataProductResponse.BaseCellIssueDate;
                case TypeChartStatus.Updated:
                    return salesCatalogueDataProductResponse.IssueDateLatestUpdate.GetValueOrDefault();
                case TypeChartStatus.Reissued:
                    return salesCatalogueDataProductResponse.BaseCellIssueDate;
                case TypeChartStatus.New:
                    break;
                case TypeChartStatus.Edition:
                    break;
                case TypeChartStatus.Published:
                    break;
                case TypeChartStatus.Cancelled:
                    return salesCatalogueDataProductResponse
                        .BaseCellIssueDate; // this isn't correct, but I don't have any better information atm..
            }

            throw new NotImplementedException("GetChartStatusDate not implemented for " +
                                              salesCatalogueDataProductResponse.ProductName);
        }

        private TypeChartStatus GetChartStatus(SalesCatalogueDataProductResponse salesCatalogueDataProductResponse)
        {
            if (salesCatalogueDataProductResponse.CancelledEditionNumber is > 0)
                return TypeChartStatus.Cancelled;
            if (salesCatalogueDataProductResponse.BaseCellUpdateNumber is > 0 &&
                salesCatalogueDataProductResponse.BaseCellUpdateNumber ==
                salesCatalogueDataProductResponse.LatestUpdateNumber)
                return TypeChartStatus.Reissued;
            if (salesCatalogueDataProductResponse.LatestUpdateNumber > 0)
                return TypeChartStatus.Updated;
            return TypeChartStatus.Base;
        }

        private UnitType MapUnitType(string productName)
        {
            switch (int.Parse(productName.Substring(2, 1)))
            {
                case 1: return UnitType.AVCSUnitsOverview;
                case 2: return UnitType.AVCSUnitsGeneral;
                case 3: return UnitType.AVCSUnitsCoastal;
                case 4: return UnitType.AVCSUnitsApproach;
                case 5: return UnitType.AVCSUnitsHarbour;
                case 6: return UnitType.AVCSUnitsBerthing;
            }

            return UnitType.AVCSUnitsHarbour;
        }

        private productTypeCode MapUnitUsage(string productName)
        {
            switch (int.Parse(productName.Substring(2, 1)))
            {
                case 1: return productTypeCode.C12;
                case 2: return productTypeCode.C15;
                case 3: return productTypeCode.C18;
                case 4: return productTypeCode.C21;
                case 5: return productTypeCode.C24;
                case 6: return productTypeCode.C25;
            }

            return productTypeCode.C24;
        }

        private XmlSerializer MakeUkhoCatalogueFileProductsXmlSerializer()
        {
            var xmlOverrides = new XmlAttributeOverrides();
            var ignoreAttribute = new XmlAttributes { XmlIgnore = true };

            // Apply an override to ignore the Polygon property which otherwise breaks serialization
            xmlOverrides.Add(typeof(ARCSMetadataPanel), "Polygon", ignoreAttribute);

            AddXmlSerializerXmlOverrides(xmlOverrides, ignoreAttribute);

            return new XmlSerializer(typeof(UKHOCatalogueFile),
                    xmlOverrides,
                    new Type[0],
                    new XmlRootAttribute { ElementName = PublicationElementName },
                    null)
                { };
        }

        private void AddXmlSerializerXmlOverrides(XmlAttributeOverrides xmlOverrides, XmlAttributes ignoreAttribute)
        {
            xmlOverrides.Add(typeof(UKHOCatalogueFileProducts), "Paper", ignoreAttribute);
            xmlOverrides.Add(typeof(UKHOCatalogueFileProducts), "Software", ignoreAttribute);
            xmlOverrides.Add(typeof(UKHOCatalogueFileProducts), "ThirdPartyProduct", ignoreAttribute);
            xmlOverrides.Add(typeof(UKHOCatalogueFileProducts), "Agents", ignoreAttribute);
        }

        protected static UKHOCatalogueFileBaseFileMetadata GetStandardBaseFileMetadata()
        {
            var currentDate = DateTime.UtcNow.Date;

            var weekNumber = WeekNumber.GetUKHOWeekFromDateTime(currentDate);


            return new UKHOCatalogueFileBaseFileMetadata
            {
                MD_FileIdentifier = $"{weekNumber.Year}{weekNumber.Week:D2}1",
                MD_CharacterSet = "",
                MD_PointOfContact =
                    new UKHOCatalogueFileBaseFileMetadataMD_PointOfContact
                    {
                        ResponsibleParty = new ResponsibleParty
                        {
                            organisationName = "The United Kingdom Hydrographic Office",
                            contactInfo = new ResponsiblePartyContactInfo
                            {
                                fax = "+44 (0)1823 284077",
                                phone = "+44 (0)1823 337900",
                                address = new ResponsiblePartyContactInfoAddress
                                {
                                    deliveryPoint = "Admiralty Way",
                                    city = "Taunton",
                                    administrativeArea = "IMT",
                                    postalCode = "TA1 2DN",
                                    country = "United Kingdom",
                                    electronicMailAddress = "helpdesk@ukho.gov.uk"
                                }
                            }
                        }
                    },
                MD_DateStamp = currentDate,
                MD_StandardName = string.Empty,
                MD_StandardVersion = string.Empty
            };
        }
    }
}