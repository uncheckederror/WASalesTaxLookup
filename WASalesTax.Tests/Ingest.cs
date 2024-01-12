using Flurl.Http;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using System;
using System.Threading.Tasks;

using WASalesTax;
using WASalesTax.Ingest;
using WASalesTax.Parsing;

using Xunit;
using Xunit.Abstractions;

namespace WASaleTax.Tests
{
    public class Ingest
    {
        private readonly ITestOutputHelper output;
        private readonly WashingtonStateContext db;
        private readonly IConfiguration configuration;

        public Ingest(ITestOutputHelper output)
        {
            this.output = output;

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var contextOptions = new DbContextOptionsBuilder<WashingtonStateContext>()
                    .UseSqlite(config.GetConnectionString("WashingtonStateContext"))
                    .Options;
            db = new WashingtonStateContext(contextOptions);
            configuration = config;
        }

        [Fact]
        public async Task IngestDataAsync()
        {
            // Figure out the current period and the filename for the source data from the State.
            var period = new Period(DateTime.Now);
            var stateFile = $"State_{period.Year.ToString()[2..]}Q{period.PeriodNumber}";
            var zipBaseFile = $"Zip4Q{period.PeriodNumber}{period.Year.ToString()[2..]}C";
            var rateBaseFile = $"Rates_{period.Year.ToString()[2..]}Q{period.PeriodNumber}";
            var dateSegment = $"{period.Year}-{period.Month:00}/";

            // Delete the existing database if it exists and then recreate it.
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            // Ingest the data into the SQLite database.
            var baseUrl = configuration.GetConnectionString("BaseDataUrl");

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

            var checkRates = await DataSource.TryIngestTaxRatesAsync(ratesUrl, db).ConfigureAwait(false);
            var checkZip = await DataSource.TryIngestShortZipCodesAsync(zipUrl, db).ConfigureAwait(false);
            var checkAddresses = await DataSource.TryIngestAddressesAsync(addressUrl, db).ConfigureAwait(false);

            Assert.True(checkRates);
            Assert.True(checkZip);
            Assert.True(checkAddresses);

            var taxRateCount = await db.TaxRates.CountAsync();
            var zipCount = await db.ZipCodes.CountAsync();
            var addressCount = await db.AddressRanges.CountAsync();

            Assert.True(taxRateCount > 0);
            Assert.True(zipCount > 0);
            Assert.True(addressCount > 0);

            output.WriteLine($"TaxRates: {taxRateCount}");
            output.WriteLine($"ZipCodes: {zipCount}");
            output.WriteLine($"AddressRanges: {addressCount}");
        }
    }
}
