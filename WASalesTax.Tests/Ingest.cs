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
        private WashingtonStateContext db;

        public Ingest(ITestOutputHelper output)
        {
            this.output = output;
            db = new WashingtonStateContext();
        }

        [Fact]
        public async Task IngestDataAsync()
        {
            // Figure out the current period and the filename for the source data.
            var period = new Period(DateTime.Now);
            var stateFile = $"State_{period.Year.ToString().Substring(2)}Q{period.PeriodNumber}";
            var zipBaseFile = $"ZIP4Q{period.PeriodNumber}{period.Year.ToString().Substring(2)}C";
            var rateBaseFile = $"Rates_{period.Year.ToString().Substring(2)}Q{period.PeriodNumber}";

            // Delete the existing database if it exists and then recreate it.
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // Ingest the data into the SQLite database.
            var baseUrl = config.GetConnectionString("BaseDataUrl");

            var checkRates = await DataSource.TryIngestTaxRatesAsync($"{baseUrl}{rateBaseFile}.zip").ConfigureAwait(false);
            var checkZip = await DataSource.TryIngestShortZipCodesAsync($"{baseUrl}{zipBaseFile}.zip").ConfigureAwait(false);
            var checkAddresses = await DataSource.TryIngestAddressesAsync($"{baseUrl}{stateFile}.zip").ConfigureAwait(false);

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
