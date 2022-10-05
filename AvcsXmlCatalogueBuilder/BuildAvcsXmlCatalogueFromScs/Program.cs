// See https://aka.ms/new-console-template for more information

using System.Xml;
using AvcsXmlCatalogueBuilder;
using BuildAvcsXmlCatalogueFromScs;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Hello, World!");


var environmentName = "qa";
// environmentName = "prod";

var builder = new ConfigurationBuilder()
    .SetBasePath(Environment.CurrentDirectory)
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{environmentName}.json", true, true)
    .AddEnvironmentVariables();

using var config = builder.Build() as ConfigurationRoot;

var outputFilePathTemplate = config["outputFilePathTemplate"];

var scsTenantId = config["scs:tennantId"];
var scsClientId = config["scs:clientId"];
var scsBaseUrl = config["scs:baseUrl"];
var scsAuthProvider = new InteractiveAuthProvider(scsClientId,
    scsTenantId,
    $"https://login.microsoftonline.com/{scsTenantId}/oauth2/v2.0/authorize");

var catalogueBuilder = new CatalogueBuilder(
    new ScsCatalogProvider(scsAuthProvider, scsBaseUrl),
    new RulesBasedUnitsAndFoliosProvider(config["agenciesExcludedFromPays"]));

await File.WriteAllTextAsync( string.Format(outputFilePathTemplate, environmentName),
    await catalogueBuilder.BuildAsync());
