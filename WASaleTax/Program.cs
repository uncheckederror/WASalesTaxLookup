
using Microsoft.AspNetCore.Hosting;
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
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

            try
            {
                // Figure out the current period and the filename for the source data.
                var period = new Period(DateTime.Now);
                var stateFile = $"State_{period.Year.ToString().Substring(2)}Q{period.PeriodNumber}";
                var zipBaseFile = $"ZIP4Q{period.PeriodNumber}{period.Year.ToString().Substring(2)}C";
                var rateBaseFile = $"Rates_{period.Year.ToString().Substring(2)}Q{period.PeriodNumber}";

                using var db = new WashingtonStateContext();
                await db.Database.EnsureCreatedAsync();

                var checkForData = db.TaxRates.FirstOrDefault();

                // If there is no data in the database or the data is expired.
                if (checkForData is null || checkForData.ExpirationDate < DateTime.Now)
                {
                    Log.Information("Ingesting data from the State of Washington.");
                    Log.Information("This may take some time. (ex. 10 minutes)");

                    // Delete the existing database if it exists and then recreate it.
                    await db.Database.EnsureDeletedAsync();
                    await db.Database.EnsureCreatedAsync();

                    // Ingest the data into the SQLite database.
                    var baseUrl = config.GetConnectionString("BaseDataUrl");

                    var checkRates = await DataSource.TryIngestTaxRatesAsync($"{baseUrl}{rateBaseFile}.zip").ConfigureAwait(false);
                    var checkZip = await DataSource.TryIngestShortZipCodesAsync($"{baseUrl}{zipBaseFile}.zip").ConfigureAwait(false);
                    var checkAddresses = await DataSource.TryIngestAddressesAsync($"{baseUrl}{stateFile}.zip").ConfigureAwait(false);
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
