
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;

using System;
using System.Linq;
using System.Threading.Tasks;

using WASalesTax.Ingest;

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
                using var db = new WashingtonStateContext();
                await db.Database.EnsureCreatedAsync();

                var checkForData = db.TaxRates.ToList();

                if (checkForData is null || !checkForData.Any())
                {
                    // Ingest the data into the SQLite database.
                    var checkRates = await DataSource.TryIngestTaxRatesAsync(config.GetConnectionString("TaxRateDataUrl")).ConfigureAwait(false);
                    var checkZip = await DataSource.TryIngestShortZipCodesAsync(config.GetConnectionString("ShortZipDataUrl")).ConfigureAwait(false);
                    var checkAddresses = await DataSource.TryIngestAddressesAsync(config.GetConnectionString("AddressDataUrl")).ConfigureAwait(false);
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
