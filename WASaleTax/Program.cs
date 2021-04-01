
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;

using System;
using System.IO;
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
                var stateFile = $"State_{period.Year.ToString().Substring(2)}Q{period.PeriodNumber}";
                var zipBaseFile = $"ZIP4Q{period.PeriodNumber}{period.Year.ToString().Substring(2)}C";
                var rateBaseFile = $"Rates_{period.Year.ToString().Substring(2)}Q{period.PeriodNumber}";

                var fileTypes = new string[] { ".txt", ".csv", ".zip" };
                var fileNames = new string[] { stateFile, zipBaseFile, rateBaseFile };

                // If a file with the same name already exists it will break the downloading process, so we need to make sure they are deleted.
                foreach (var name in fileNames)
                {
                    foreach (var type in fileTypes)
                    {
                        var filePath = Path.Combine(AppContext.BaseDirectory, name, type);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                }

                // Put the ORM to work and make sure we have a database
                using var db = new WashingtonStateContext(contextOptions);
                await db.Database.EnsureCreatedAsync();

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
                    // Handles senarios where data was only partially ingests or has expired.
                    await db.Database.EnsureDeletedAsync();
                    await db.Database.EnsureCreatedAsync();

                    // Ingest the data into the SQLite database.
                    var baseUrl = config.GetConnectionString("BaseDataUrl");
                    var checkRates = await DataSource
                        .TryIngestTaxRatesAsync($"{baseUrl}{rateBaseFile}.zip", db)
                        .ConfigureAwait(false);
                    var checkZip = await DataSource
                        .TryIngestShortZipCodesAsync($"{baseUrl}{zipBaseFile}.zip", db)
                        .ConfigureAwait(false);
                    var checkAddresses = await DataSource
                        .TryIngestAddressesAsync($"{baseUrl}{stateFile}.zip", db)
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
