using Flurl.Http;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using SalesTax.Ingest;
using SalesTax.Models;
using SalesTax.Parsing;

using Scalar.AspNetCore;

using Serilog;
using Serilog.Events;

using System.Collections.Frozen;
using System.ComponentModel;
var builder = WebApplication.CreateBuilder(args);

// Setup logging.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    // Enabled only for running locally or debugging.
    //.WriteTo.Console()
    .WriteTo.File("log.txt",
    rollingInterval: RollingInterval.Day,
    rollOnFileSizeLimit: true,
    retainedFileCountLimit: 2,
    retainedFileTimeLimit: TimeSpan.FromDays(3))
    .CreateLogger();

Log.Information("The WA Sales Tax Rate Lookup API is starting up.");

// Get the configuration keys
var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
var typedConfig = new AppSettings();
config.Bind(typedConfig);
builder.Services.AddSingleton(typedConfig);

builder.Services.AddProblemDetails();

List<string> urls = [];

var ingestRates = Task.Run(async () =>
{
    string ratesUrl = await DataSource.GetTaxRatesURLAsync(typedConfig.ConnectionStrings.BaseDataUrl);
    var rates = await DataSource.TryIngestTaxRatesAsync(ratesUrl);
    builder.Services.AddSingleton(rates);
    urls.Add(ratesUrl);
});

if (typedConfig.EnableLegacy)
{
    var ingestZips = Task.Run(async () =>
    {
        string zipsUrl = await DataSource.GetZipsURLAsync(typedConfig.ConnectionStrings.BaseDataUrl);
        var zips = await DataSource.TryIngestShortZipCodesAsync(zipsUrl);
        builder.Services.AddSingleton(zips);
    });

    var ingestAddresses = Task.Run(async () =>
    {
        string addressUrl = await DataSource.GetAddressesURLAsync(typedConfig.ConnectionStrings.BaseDataUrl);
        var addresses = await DataSource.TryIngestAddressesAsync(addressUrl);
        builder.Services.AddSingleton(addresses);
    });

    // Broken into separte tasks and run in parallel to reduce the startup time of the app.
    await Task.WhenAll(ingestRates, ingestZips, ingestAddresses);
}
else
{
    await ingestRates;
}

Log.Information("Ingest(s) completed.");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "WA Sales Tax Lookup API",
            Version = "v1",
            Description = $"Find the correct sales tax rate to apply to transactions in Washington State based on an address. If the transaction is happening in a physical location query for the tax rate using the street address of that location. If the transaction is happening online, query for the tax rate using the billing address provided by the customer in their order. \nLearn more about WA Sales Tax Rate Lookup URL Interface provided by the Washington State Department of Revenue here: https://dor.wa.gov/wa-sales-tax-rate-lookup-url-interface Review the source code for this project on Github: https://github.com/uncheckederror/WASalesTaxLookup The API responds with the Tax Rates found at {urls.FirstOrDefault()}",
            License = new()
            {
                Url = new("https://github.com/uncheckederror/WASalesTaxLookup/blob/master/LICENSE"),
                Name = "GNU Affero General Public License v3.0"
            },
            Contact = new() { Name = "Thomas Ryan", Url = new("https://thomasryan.dev/") },
            TermsOfService = new("https://github.com/uncheckederror/WASalesTaxLookup"),
        };
        return Task.CompletedTask;
    });
});

builder.Services.AddHttpContextAccessor();

// Cache the OpenAPI document per https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-9.0&tabs=visual-studio
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.Cache().Expire(TimeSpan.FromMinutes(10)));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// https://github.com/andrewlock/NetEscapades.AspNetCore.SecurityHeaders
app.UseSecurityHeaders();

app.UseOutputCache();

app.MapOpenApi().CacheOutput();
app.UseHttpsRedirection();
app.MapScalarApiReference(options =>
{
    options.WithTitle("WA Sales Tax Rate Lookup API");
    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapGet("/GetTaxRate", Endpoints.GISLookupAsync)
    .CacheOutput()
    .WithSummary("Find a sales tax rate")
    .WithDescription("Get a tax rate for a specific address in Washington State. Supply a valid street address (no apt or unit numbers), city, and zip code and recieve a tax rate.");

if (typedConfig.EnableLegacy)
{
    // Must keep for legacy compatiblity with the State DOR's API.
    app.MapGet("/AddressRates.aspx", Endpoints.LegacyLookupAsync)
    .CacheOutput()
    .WithSummary("Find a sales tax rate using the same API provided by the State of Washington's Department of Revenue")
    .WithDescription("An exact match to the existing API endpoint offered by the State of Washington's Department of Revenue.");
}

// Exists only to preserve compatibility, not intened for new clients.
app.MapGet("/AddressRates", Endpoints.GISLookupAsync)
    .CacheOutput()
    .ExcludeFromDescription();

// Exists only to preserve compatibility, not intened for new clients.
app.MapGet("/PreciseRate",
    async ([FromServices] FrozenDictionary<int,
    TaxRate> TaxRates,
    [FromServices] AppSettings AppSettings,
    int houseNumber, string streetName, string shortZipCode, string zipPlus4
    ) =>
    {
        return await Endpoints.GISLookupAsync(TaxRates, AppSettings, $"{houseNumber} {streetName}", string.Empty, shortZipCode);
    })
    .CacheOutput()
    .ExcludeFromDescription();

// This sets the root of the app to the interactive docs, suppressed from the docs as it's purely for convenience.
app.MapGet("/", () => TypedResults.LocalRedirect("/scalar/v1"))
    .CacheOutput()
    .ExcludeFromDescription()
    .WithSummary("API Documentation")
    .WithDescription("This endpoint shows the Scalar API docs at the root of the app rather than just as /scaler/v1 as a convience for the users of this API.");

app.Run();

// Required to support the integration tests in the test project.
public partial class Program { }

public static class Endpoints
{
    public static async Task LegacyLookupAsync
    (
        HttpContext context,
        [FromServices] FrozenDictionary<int, TaxRate> TaxRates,
        [FromServices] FrozenSet<ShortZip> ZipCodes,
        [FromServices] FrozenSet<AddressRange> AddressRanges,
        [Description("The format of the response, either \"xml\" or \"text\".")] string output = "",
        [Description("The street address of the customer/point of sale. (ex. \"6500 Linderson way\") Please do not include unit, office, or apt numbers. Just the simple physical address.")] string addr = "",
        [Description("The city of that the customer/point of resides in. (ex. \"Olympia\")")] string city = "",
        [Description("The 5 digit Zip Code. (ex. \"98501\") Plus4 Zip Codes are optional. (ex. \"98501-6561\" or \"985016561\" )")] string zip = ""
    )
    {
        bool useXml = false;
        useXml = output is "xml";
        string xmlStart = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
        if (useXml)
        {
            context.Response.ContentType = "text/xml; charset=utf-8";
        }
        else
        {
            context.Response.ContentType = "text/html; charset=utf-8";
        }

        zip = zip.Trim();

        // Fail fast on invalid zip codes.
        if (!string.IsNullOrWhiteSpace(zip) && (zip.Length == 5 || zip.Length == 9 || zip.Length == 10))
        {
            ShortZip matchingZip;

            // Find a representitive zip code entry as a starting place.
            switch (zip.Length)
            {
                case 5:
                    matchingZip = ZipCodes.FirstOrDefault(x => x.Zip == zip);
                    break;
                case 9:
                    matchingZip = ZipCodes.FirstOrDefault(x => x.Zip == zip[..5]);
                    break;
                case 10:
                    zip = zip.Replace("-", string.Empty);
                    matchingZip = ZipCodes.FirstOrDefault(x => x.Zip == zip[..5]);
                    break;
                default:
                    matchingZip = new ShortZip("",0);
                    break;
            }

            // If no zip code if found return an invalid response.
            if (string.IsNullOrWhiteSpace(matchingZip.Zip))
            {
                context.Response.StatusCode = 400;

                if (useXml)
                {
                    await context.Response.WriteAsync(xmlStart + "<response loccode=\"\" localrate=\"\" rate=\"\" code=\"4\" debughint=\"Invalid ZIP\"><addressline/><rate/></response>");
                }
                else
                {
                    await context.Response.WriteAsync("LocationCode=-1 Rate=-1 ResultCode=4 debughint=Invalid ZIP");
                }
            }
            else if (string.IsNullOrWhiteSpace(addr) && zip.Length == 5)
            {
                // 5 digit ZIP only, no address provided.
                var rate = TaxRates[matchingZip.LocationCode];

                context.Response.StatusCode = 200;

                if (useXml)
                {
                    var relatedAddressRange = AddressRanges.Where(x => x.ZipCode == matchingZip.Zip).FirstOrDefault();
                    await context.Response.WriteAsync(xmlStart + $"<response loccode=\"{rate.LocationCode}\" localrate=\"{rate.Local.ToString().TrimStart('0')}\" rate=\"{rate.Rate.ToString().TrimStart('0')}\" code=\"5\" xmlns=\"\"><addressline code=\"{rate.LocationCode}\" state=\"WA\" zip=\"{matchingZip.Zip}\" period=\"{Period.CurrentPeriod().PeriodLit}\" rta=\"{relatedAddressRange.RTA}\" ptba=\"{relatedAddressRange.PTBAName}\" cez=\"{relatedAddressRange.CEZName}\" />{rate.ToXML()}</response>");
                }
                else
                {
                    await context.Response.WriteAsync($"LocationCode={rate.LocationCode} Rate={rate.Rate:.0000} ResultCode=3");
                }
            }
            else
            {
                List<AddressRange> relatedAddressRanges;

                if (zip.Length == 9)
                {
                    var plus4 = zip[5..];
                    relatedAddressRanges = AddressRanges.Where(x => x.ZipCode == matchingZip.Zip && x.ZipCodePlus4 == plus4).ToList();

                    // Skip address parsing if there's only one matching address range for the 9 digit ZIP.
                    if (relatedAddressRanges.Count == 1)
                    {
                        var match = relatedAddressRanges.FirstOrDefault();

                        var rate = TaxRates[match.LocationCode];
                        context.Response.StatusCode = 200;

                        if (useXml)
                        {
                            await context.Response.WriteAsync(xmlStart + $"<response loccode=\"{rate.LocationCode}\" localrate=\"{rate.Local.ToString().TrimStart('0')}\" rate=\"{rate.Rate.ToString().TrimStart('0')}\" code=\"1\" xmlns=\"\">{match.ToXML()}{rate.ToXML()}</response>");
                        }
                        else
                        {
                            await context.Response.WriteAsync($"LocationCode={rate.LocationCode} Rate={rate.Rate.ToString().TrimStart('0')} ResultCode=3");
                        }
                    }
                }
                else
                {
                    relatedAddressRanges = AddressRanges.Where(x => x.ZipCode == matchingZip.Zip).ToList();
                }

                // Fail fast if no address ranges for this zip can be found.
                if (relatedAddressRanges is null || relatedAddressRanges.Count == 0)
                {
                    context.Response.StatusCode = 400;

                    if (useXml)
                    {
                        await context.Response.WriteAsync(xmlStart + "<response loccode=\"\" localrate=\"\" rate=\"\" code=\"4\" debughint=\"Invalid ZIP\"><addressline/><rate/></response>");
                    }
                    else
                    {
                        await context.Response.WriteAsync("LocationCode=-1 Rate=-1 ResultCode=4 debughint=Invalid ZIP");
                    }
                }
                else
                {
                    // Parse the street address and find a similar address range.
                    var parsedStreetAddress = new AddressLineTokenizer(addr);

                    if (!string.IsNullOrWhiteSpace(parsedStreetAddress.Street.Lexum))
                    {
                        AddressRange match = default;
                        double score = -3;
                        double mscore;

                        // Score the potential matches and select the highest rated.
                        foreach (var canidate in relatedAddressRanges)
                        {
                            if ((mscore = parsedStreetAddress.Match(canidate)) > score)
                            {
                                match = canidate;
                                score = mscore;
                            }
                        }

                        // If the score is to low or no match is found fail out.
                        if (match.LocationCode is 0 || score < -0.1)
                        {
                            context.Response.StatusCode = 404;

                            if (useXml)
                            {
                                await context.Response.WriteAsync(xmlStart + "<response loccode=\"\" localrate=\"\" rate=\"\" code=\"3\" debughint=\"Address not found\"><addressline/><rate/></response>");
                            }
                            else
                            {
                                await context.Response.WriteAsync("LocationCode=-1 Rate=-1 ResultCode=3 debughint=Invalid ZIP");
                            }
                        }
                        else
                        {
                            // Return the tax rate for the matching address range.
                            var rate = TaxRates[match.LocationCode];
                            context.Response.StatusCode = 200;

                            var check = int.TryParse(parsedStreetAddress.House.Lexum, out int houseNumber);

                            if (useXml)
                            {
                                string addressRangeXML = check ? match.ToXML(houseNumber) : match.ToXML();
                                await context.Response.WriteAsync(xmlStart + $"<response loccode=\"{rate.LocationCode}\" localrate=\"{rate.Local.ToString().TrimStart('0')}\" rate=\"{rate.Rate.ToString().TrimStart('0')}\" code=\"2\" xmlns=\"\">{addressRangeXML}{rate.ToXML()}</response>");
                            }
                            else
                            {
                                await context.Response.WriteAsync($"LocationCode={rate.LocationCode} Rate={rate.Rate.ToString().TrimStart('0')} ResultCode=2");
                            }
                        }
                    }
                }
            }
        }
        else
        {
            context.Response.StatusCode = 400;
            if (useXml)
            {
                await context.Response.WriteAsync(xmlStart + "<response loccode=\"\" localrate=\"\" rate=\"\" code=\"4\" debughint=\"Invalid ZIP\"><addressline/><rate/></response>");
            }
            else
            {
                await context.Response.WriteAsync("LocationCode=-1 Rate=-1 ResultCode=4 debughint=Invalid ZIP");
            }
        }
    }
    public static async Task<Results<Ok<TaxRate>, BadRequest<ProblemDetails>>> GISLookupAsync
    (
        [FromServices] FrozenDictionary<int, TaxRate> TaxRates,
        [FromServices] AppSettings AppSettings,
        [Description("The house number and street name. For example '6500 Linderson Way' or '201 S Jackson St'. Please do not include unit, office, or apt numbers. Just the simple physical address.")] string addr = "",
        [Description("The city that the address exists within. For example 'Seattle' or 'Tumwater'. Optional only if a zip code has been provided.")] string city = "",
        [Description("The 5 digit zip code that the street address exists within. For example '98104' or '98501'. Optional only if a city has been provided.")] string zip = ""
    )
    {
        if (string.IsNullOrWhiteSpace(addr))
        {
            return TypedResults.BadRequest(new ProblemDetails() { Status = 400, Title = "Invalid streetAddress", Type = "Validation failure", Detail = "The streetAddress parameter is required and may not be blank, null, or whitespace." });
        }

        // The ZIP code and city are optional in this geocoder, but you have to supply one or the other. So we're not going to validate either.

        var geocodeResponse = await AppSettings.ConnectionStrings.GeocodingServiceBaseURL
           .PostUrlEncodedAsync(new
           {
               Address = addr,
               City = city,
               Postal = zip,
               matchOutOfRange = true,
               f = "json"
           });

        var validAddresses = await geocodeResponse.GetJsonAsync<AddressGeocodeResponse>();

        // If we get back an empty array of features from the geocoding API.
        if (validAddresses.candidates.Length is 0)
        {
            return TypedResults.BadRequest(new ProblemDetails() { Status = 400, Title = "Failed to find address", Type = "Lookup failure", Detail = $"Failed to geocoded the address provided. Please try another address, or reformat the current address." });
        }

        var topAddress = validAddresses.candidates.FirstOrDefault().location;

        var locodeResponse = await AppSettings.ConnectionStrings.LOCODELookupBaseURL
                   .PostUrlEncodedAsync(new
                   {
                       esriTimeRelationOverlay = "esriTimeRelationOverlaps",
                       geometry = $"{topAddress.x}, {topAddress.y}",
                       geometryType = "esriGeometryPoint",
                       inSR = validAddresses.spatialReference.wkid,
                       spatialRel = "esriSpatialRelIntersects",
                       outFields = "LOCCODE",
                       f = "json"
                   });

        var locodes = await locodeResponse.GetJsonAsync<LOCODEQueryResponse>();
        string locode = locodes.features.FirstOrDefault().attributes.LOCCODE;

        if (string.IsNullOrWhiteSpace(locode))
        {
            return TypedResults.BadRequest(new ProblemDetails() { Status = 400, Title = "Failed to find address", Type = "Lookup failure", Detail = $"Failed to geocoded the address provided. Please try another address, or reformat the current address." });
        }

        bool validInt = int.TryParse(locode, out int LOCODE);

        if (!validInt)
        {
            return TypedResults.BadRequest(new ProblemDetails() { Status = 500, Title = "Failed match address", Type = "Lookup failure", Detail = "Failed to find a valid LOCODE for the coordinates geocoded for the address provided. Please try another address, or reformat the current address." });
        }

        bool matchRate = TaxRates.TryGetValue(LOCODE, out var taxRate);

        if (!matchRate)
        {
            return TypedResults.BadRequest(new ProblemDetails() { Status = 500, Title = "Failed to match tax rate", Type = "Lookup failure", Detail = "Failed found a valid LOCODE for the provided address, but no matching Tax Rate was found for this LOCODE. Did the Tax Rates ingest correctly?" });
        }

        return TypedResults.Ok(taxRate);
    }
}
