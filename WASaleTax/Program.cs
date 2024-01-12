
using Flurl.Http;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;

using System;
using System.Linq;
using System.Threading.Tasks;

using WASalesTax.Ingest;
using WASalesTax.Parsing;

namespace WASalesTax
{
    public class Program
    {
        // https://github.com/serilog/serilog-aspnetcore
        public static async Task<int> Main(string[] args)
        {
            // Setup logging.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logWAStateSalesTax.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Get the configuration keys
            var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

            // Ready the ORM configuration
            var contextOptions = new DbContextOptionsBuilder<WashingtonStateContext>()
                    .UseSqlite(config.GetConnectionString("WashingtonStateContext"))
                    .Options;

            try
            {
                // Figure out the current period and the filename for the source data from the State.
                var period = new Period(DateTime.Now);
                var stateFile = $"State_{period.Year.ToString()[2..]}Q{period.PeriodNumber}";
                var zipBaseFile = $"Zip4Q{period.PeriodNumber}{period.Year.ToString()[2..]}C";
                var rateBaseFile = $"Rates_{period.Year.ToString()[2..]}Q{period.PeriodNumber}";
                var dateSegment = $"/{period.Year}-{period.Month:00}/";

                var baseUrl = config.GetConnectionString("BaseDataUrl");

                string ratesUrl = $"{baseUrl}{dateSegment}{rateBaseFile}.zip";
                string zipUrl = $"{baseUrl}{dateSegment}{zipBaseFile}.zip";
                string addressUrl = $"{baseUrl}{dateSegment}{stateFile}.zip";
                bool ratesUrlInvalid = true;
                bool zipUrlInvalid = true;
                bool addressUrlInvalid = true;

                int month = period.Month;
                int year = period.Year;

                // Sometimes the time period portion of th Url won't match with the year and quarter of the file. Like when the Q1 is published the prior november.
                while (ratesUrlInvalid)
                {
                    try
                    {
                        var pathtoFile = await ratesUrl.DownloadFileAsync(AppContext.BaseDirectory);
                        ratesUrlInvalid = false;
                    }
                    catch (FlurlHttpException ex)
                    {
                        if (month > 0 && year >= period.Year - 2)
                        {
                            month--;
                            ratesUrl = $"{baseUrl}{year}-{month:00}/{rateBaseFile}.zip";
                        }
                        else
                        {
                            month = 12;
                            year--;
                            ratesUrl = $"{baseUrl}{year}-{month:00}/{rateBaseFile}.zip";
                        }
                    }
                }

                month = period.Month;
                year = period.Year;

                while (zipUrlInvalid)
                {
                    try
                    {
                        var pathtoFile = await zipUrl.DownloadFileAsync(AppContext.BaseDirectory);
                        zipUrlInvalid = false;
                    }
                    catch (FlurlHttpException ex)
                    {
                        if (month > 0 && year >= period.Year - 2)
                        {
                            month--;
                            zipUrl = $"{baseUrl}{year}-{month:00}/{zipBaseFile}.zip";
                        }
                        else
                        {
                            month = 12;
                            year--;
                            zipUrl = $"{baseUrl}{year}-{month:00}/{zipBaseFile}.zip";
                        }
                    }
                }

                month = period.Month;
                year = period.Year;

                while (addressUrlInvalid)
                {
                    try
                    {
                        var pathtoFile = await addressUrl.DownloadFileAsync(AppContext.BaseDirectory);
                        addressUrlInvalid = false;
                    }
                    catch (FlurlHttpException ex)
                    {
                        if (month > 0 && year >= period.Year - 2)
                        {
                            month--;
                            addressUrl = $"{baseUrl}{year}-{month:00}/{stateFile}.zip";
                        }
                        else
                        {
                            month = 12;
                            year--;
                            addressUrl = $"{baseUrl}{year}-{month:00}/{stateFile}.zip";
                        }
                    }
                }


                // Put the ORM to work and make sure we have a database
                using var db = new WashingtonStateContext(contextOptions);
                await db.Database.MigrateAsync();

                var checkForTaxRates = db.TaxRates.FirstOrDefault();
                var checkForAddresses = db.AddressRanges.FirstOrDefault();
                var checkForZips = db.ZipCodes.FirstOrDefault();

                // If there is no data in the database or the data is expired, reingest everything.
                if (checkForTaxRates is null
                    || checkForAddresses is null
                    || checkForZips is null
                    || checkForTaxRates.ExpirationDate < DateTime.Now)
                {
                    Log.Information("Ingesting data from the State of Washington.");
                    Log.Information("This may take some time. (ex. 10 minutes)");

                    // Delete the existing database if it exists and then recreate it.
                    // Handles scenarios where data was only partially ingests or has expired.
                    await db.Database.EnsureDeletedAsync();
                    await db.Database.MigrateAsync();

                    // Ingest the data into the SQLite database.
                    var checkRates = await DataSource
                        .TryIngestTaxRatesAsync(ratesUrl, db)
                        .ConfigureAwait(false);
                    var checkZip = await DataSource
                        .TryIngestShortZipCodesAsync(zipUrl, db)
                        .ConfigureAwait(false);
                    var checkAddresses = await DataSource
                        .TryIngestAddressesAsync(addressUrl, db)
                        .ConfigureAwait(false);
                    Log.Information("Data ingest complete.");
                }

                // Start up the REST API.
                Log.Information("Starting web host");
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args)
                    .UseSerilog()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    });
    }
}
