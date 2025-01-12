using Flurl.Http;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using SalesTax.Ingest;
using SalesTax.Models;

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


using Xunit;
using Xunit.Abstractions;

namespace WASalesTax.Tests;
public class RemoteDependencies : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITestOutputHelper output;
    private readonly AppSettings configuration;
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public RemoteDependencies(ITestOutputHelper output, WebApplicationFactory<Program> factory)
    {
        this.output = output;
        _factory = factory;
        _client = factory.CreateClient();
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var AppSettings = new AppSettings();
        config.Bind(AppSettings);
        configuration = AppSettings;
    }

    [Fact]
    public async Task GeocodeAddress()
    {
        // Get the layer definitions for the Districts Report map service.
        // https://webgis.dor.wa.gov/arcgis/rest/services/Locators/WA_Address/GeocodeServer/findAddressCandidates?Address=1622+S+Graham+St&Address2=&Address3=&Neighborhood=&City=Seattle&Subregion=WA&Region=&Postal=&PostalExt=&CountryCode=&SingleLine=&outFields=&maxLocations=&matchOutOfRange=true&langCode=&locationType=&sourceCountry=&category=&location=&searchExtent=&outSR=&magicKey=&preferredLabelValues=&f=pjson
        var geocodeResponse = await configuration.ConnectionStrings.GeocodingServiceBaseURL
                               .PostUrlEncodedAsync(new
                               {
                                   Address = $"1622 S Graham St",
                                   City = "Seattle",
                                   Postal = "98108",
                                   matchOutOfRange = true,
                                   f = "json"
                               });

        var validAddresses = await geocodeResponse.GetJsonAsync<AddressGeocodeResponse>();

        Assert.NotEmpty(validAddresses.candidates);
        foreach (var result in validAddresses.candidates)
        {
            Assert.True(result.location.x < 0);
            Assert.True(result.location.y > 0);
        }
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(validAddresses));
    }

    [Fact]
    public async Task QueryForLOCODE()
    {
        // Get the layer definitions for the Districts Report map service.
        // https://webgis.dor.wa.gov/arcgis/rest/services/Programs/WADOR_SalesTax/MapServer/4/query?where=&text=&objectIds=&time=&timeRelation=esriTimeRelationOverlaps&geometry=-122.311388543841%2C+47.547320805635&geometryType=esriGeometryPoint&inSR=4326&spatialRel=esriSpatialRelIntersects&distance=&units=esriSRUnit_Foot&relationParam=&outFields=LOCCODE&returnGeometry=false&returnTrueCurves=false&maxAllowableOffset=&geometryPrecision=&outSR=&havingClause=&returnIdsOnly=false&returnCountOnly=false&orderByFields=&groupByFieldsForStatistics=&outStatistics=&returnZ=false&returnM=false&gdbVersion=&historicMoment=&returnDistinctValues=false&resultOffset=&resultRecordCount=&returnExtentOnly=false&sqlFormat=none&datumTransformation=&parameterValues=&rangeValues=&quantizationParameters=&featureEncoding=esriDefault&f=pjson
        var point = new Location(-122.311388543841f, 47.547320805635f);
        var locodeResponse = await configuration.ConnectionStrings.LOCODELookupBaseURL
                               .PostUrlEncodedAsync(new
                               {
                                   esriTimeRelationOverlay = "esriTimeRelationOverlaps",
                                   geometry = $"{point.x}, {point.y}",
                                   geometryType = "esriGeometryPoint",
                                   inSR = 4326,
                                   spatialRel = "esriSpatialRelIntersects",
                                   outFields = "LOCCODE",
                                   f = "json"
                               });

        var locodes = await locodeResponse.GetJsonAsync<LOCODEQueryResponse>();

        Assert.NotEmpty(locodes.features);
        foreach (var result in locodes.features)
        {
            Assert.False(string.IsNullOrWhiteSpace(result.attributes.LOCCODE));
        }
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(locodes));
    }

    [Fact]
    public async Task GeocodeToLOCODE()
    {
        var geocodeResponse = await configuration.ConnectionStrings.GeocodingServiceBaseURL
                   .PostUrlEncodedAsync(new
                   {
                       Address = $"1622 S Graham St",
                       City = "Seattle",
                       Postal = "98108",
                       matchOutOfRange = true,
                       f = "json"
                   });

        var validAddresses = await geocodeResponse.GetJsonAsync<AddressGeocodeResponse>();

        var topAddress = validAddresses.candidates.FirstOrDefault().location;

        var locodeResponse = await configuration.ConnectionStrings.LOCODELookupBaseURL
                   .PostUrlEncodedAsync(new
                   {
                       esriTimeRelationOverlay = "esriTimeRelationOverlaps",
                       geometry = $"{topAddress.x}, {topAddress.y}",
                       geometryType = "esriGeometryPoint",
                       inSR = 4326,
                       spatialRel = "esriSpatialRelIntersects",
                       outFields = "LOCCODE",
                       f = "json"
                   });

        var locodes = await locodeResponse.GetJsonAsync<LOCODEQueryResponse>();

        Assert.NotEmpty(locodes.features);
        foreach (var result in locodes.features)
        {
            Assert.False(string.IsNullOrWhiteSpace(result.attributes.LOCCODE));
        }
        output.WriteLine(validAddresses.candidates.FirstOrDefault().address);
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(locodes));
    }

    [Fact]
    public async Task GetTaxRates()
    {
        var url = await DataSource.GetTaxRatesURLAsync(configuration.ConnectionStrings.BaseDataUrl);

        Assert.False(string.IsNullOrWhiteSpace(url));

        var rates = await DataSource.TryIngestTaxRatesAsync(url);

        Assert.NotNull(rates);
        Assert.True(rates.Count > 0);
        output.WriteLine($"{url}");
        output.WriteLine($"{rates.Count} Rates in Dict");
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(rates.FirstOrDefault()));

        var lookupValue = 1726;
        _ = rates.TryGetValue(lookupValue, out var taxRate);
        Assert.Equal(1726, taxRate.LocationCode);
    }

    [Fact]
    public async Task GetURLs()
    {
        var rate = await DataSource.GetTaxRatesURLAsync(configuration.ConnectionStrings.BaseDataUrl);
        var zip = await DataSource.GetZipsURLAsync(configuration.ConnectionStrings.BaseDataUrl);
        var address = await DataSource.GetAddressesURLAsync(configuration.ConnectionStrings.BaseDataUrl);

        Assert.False(string.IsNullOrWhiteSpace(rate));
        Assert.False(string.IsNullOrWhiteSpace(zip));
        Assert.False(string.IsNullOrWhiteSpace(address));
    }

    [Fact]
    public async Task TestAddressesGISLookup()
    {
        var url = await DataSource.GetTaxRatesURLAsync(configuration.ConnectionStrings.BaseDataUrl);
        Assert.False(string.IsNullOrWhiteSpace(url));
        var rates = await DataSource.TryIngestTaxRatesAsync(url);

        var response = await Endpoints.GISLookupAsync(rates, configuration, "4415 31st Ave W", "Seattle", "98199");

        var typedResponse = (Ok<TaxRate>)response.Result;
        var rate = typedResponse.Value;
        Assert.True(rate.LocationCode > 0);
        Assert.True(rate.Rate > 0);
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(rate));
    }

    [Fact]
    public async Task ResultCode1()
    {
        string testParameters = "/AddressRates.aspx?output=xml&addr=6500%20Linderson%20way&city=&zip=98501";
        var res = await _client.GetStringAsync(testParameters);
        output.WriteLine(res);

        var stateResponse = await $"https://webgis.dor.wa.gov/webapi{testParameters}".GetStringAsync();
        output.WriteLine(stateResponse);
        Assert.Equal(res, stateResponse);
    }
}
