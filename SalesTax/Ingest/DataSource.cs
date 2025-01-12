using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

using Flurl.Http;

using SalesTax.Models;
using SalesTax.Parsing;

using Serilog;

using System.Collections.Frozen;
using System.Globalization;
using System.IO.Compression;

namespace SalesTax.Ingest
{
    public class DataSource
    {
        public static async Task<string> GetTaxRatesURLAsync(string baseUrl)
        {
            // Figure out the current period and the filename for the source data from the State.
            var period = new Period(DateTime.Now);

            var rateBaseFile = $"Rates_{period.Year.ToString()[2..]}Q{period.PeriodNumber}";
            var dateSegment = $"/{period.Year}-{period.Month:00}/";

            string ratesUrl = $"{baseUrl}{dateSegment}{rateBaseFile}.zip";
            bool ratesUrlInvalid = true;
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

            return ratesUrl;
        }

        public static async Task<string> GetZipsURLAsync(string baseUrl)
        {
            // Figure out the current period and the filename for the source data from the State.
            var period = new Period(DateTime.Now);
            var zipBaseFile = $"Zip4Q{period.PeriodNumber}{period.Year.ToString()[2..]}C";
            var dateSegment = $"/{period.Year}-{period.Month:00}/";
            string zipUrl = $"{baseUrl}{dateSegment}{zipBaseFile}.zip";
            bool zipUrlInvalid = true;
            int month = period.Month;
            int year = period.Year;

            // Sometimes the time period portion of th Url won't match with the year and quarter of the file. Like when the Q1 is published the prior november.
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

            return zipUrl;
        }

        public static async Task<string> GetAddressesURLAsync(string baseUrl)
        {
            // Figure out the current period and the filename for the source data from the State.
            var period = new Period(DateTime.Now);
            var stateFile = $"State_{period.Year.ToString()[2..]}Q{period.PeriodNumber}";
            var dateSegment = $"/{period.Year}-{period.Month:00}/";
            string addressUrl = $"{baseUrl}{dateSegment}{stateFile}.zip";
            bool addressUrlInvalid = true;
            int month = period.Month;
            int year = period.Year;

            // Sometimes the time period portion of th Url won't match with the year and quarter of the file. Like when the Q1 is published the prior november.
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

            return addressUrl;
        }

        public static async Task<FrozenSet<AddressRange>> TryIngestAddressesAsync(string url)
        {
            // Were using a set here because the lookups occur on multiple keys, not just a single property that we can use to build a dictionary.
            List<AddressRange> addresses = [];

            try
            {
                var pathtoFile = await url.DownloadFileAsync(AppContext.BaseDirectory);
                var pathToCSV = Path.Combine(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(pathtoFile).TrimEnd(['_', '0']) + ".txt");

                var fileTypes = new string[] { ".txt", ".csv" };
                // If a file with the same name already exists it will break the downloading process, so we need to make sure they are deleted.
                foreach (var type in fileTypes)
                {
                    var filePath = Path.Combine(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(pathtoFile) + type);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                if (!File.Exists(pathToCSV))
                {
                    ZipFile.ExtractToDirectory(pathtoFile, AppContext.BaseDirectory);
                }

                //db.ChangeTracker.AutoDetectChangesEnabled = false;
                using var reader = new StreamReader(pathToCSV);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<AddressRangeMap>();

                await foreach (var row in csv.GetRecordsAsync<AddressRange>())
                {
                    addresses.Add(row);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("[Ingest] Failed to load Addresses into the database.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex.StackTrace ?? "No stacktrace found.");
            }

            return addresses.ToFrozenSet();
        }

        public static async Task<FrozenDictionary<int, TaxRate>> TryIngestTaxRatesAsync(string url)
        {
            Dictionary<int, TaxRate> rates = [];

            try
            {
                var pathtoFile = await url.DownloadFileAsync(AppContext.BaseDirectory);
                var pathToCSV = Path.Combine(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(pathtoFile) + ".csv");

                var fileTypes = new string[] { ".txt", ".csv" };
                // If a file with the same name already exists it will break the downloading process, so we need to make sure they are deleted.
                foreach (var type in fileTypes)
                {
                    var filePath = Path.Combine(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(pathtoFile) + type);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                if (!File.Exists(pathToCSV))
                {
                    ZipFile.ExtractToDirectory(pathtoFile, AppContext.BaseDirectory);
                }

                using var reader = new StreamReader(pathToCSV);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<TaxRateMap>();

                await foreach (var row in csv.GetRecordsAsync<TaxRate>())
                {
                    rates.Add(row.LocationCode, row);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("[Ingest] Failed to load Tax Rates into memory.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex.StackTrace ?? "No stacktrace found.");
            }

            return rates.ToFrozenDictionary();
        }

        public static async Task<FrozenSet<ShortZip>> TryIngestShortZipCodesAsync(string url)
        {
            // Were using a set here because the lookups occur on multiple keys, not just a single property that we can use to build a dictionary.
            List<ShortZip> zips = [];

            try
            {
                var pathtoFile = await url.DownloadFileAsync(AppContext.BaseDirectory);

                var fileName = string.Empty;

                // https://stackoverflow.com/questions/47973286/get-the-filename-of-a-file-that-was-created-through-zipfile-extracttodirectory
                //open archive
                using (var archive = ZipFile.OpenRead(pathtoFile))
                {
                    //since there is only one entry grab the first
                    var entry = archive.Entries.FirstOrDefault();
                    //the relative path of the entry in the zip archive
                    fileName = entry?.FullName ?? string.Empty;
                }
                var pathToCSV = Path.Combine(AppContext.BaseDirectory, fileName);

                if (!File.Exists(pathToCSV))
                {
                    ZipFile.ExtractToDirectory(pathtoFile, AppContext.BaseDirectory);
                }

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false,
                    Delimiter = ","
                };
                using var reader = new StreamReader(pathToCSV);
                using var csv = new CsvReader(reader, config);
                csv.Context.RegisterClassMap<ShortZipMap>();

                await foreach (var row in csv.GetRecordsAsync<ShortZip>())
                {
                    zips.Add(row);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("[Ingest] Failed to load Short Zip Codes into memory.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex.StackTrace ?? "No stacktrace found.");
            }

            return zips.ToFrozenSet();
        }

        public class AddressRangeMap : ClassMap<AddressRange>
        {
            public AddressRangeMap()
            {
                Map(m => m.AddressRangeLowerBound).Name("ADDR_LOW");
                Map(m => m.AddressRangeUpperBound).Name("ADDR_HIGH");
                Map(m => m.OddOrEven).Name("ODD_EVEN");
                Map(m => m.Street).Name("STREET");
                Map(m => m.State).Name("STATE");
                Map(m => m.ZipCode).Name("ZIP");
                Map(m => m.ZipCodePlus4).Name("PLUS4");
                Map(m => m.Period).Name("PERIOD");
                Map(m => m.LocationCode).Name("CODE");
                Map(m => m.RTA).Name("RTA");
                Map(m => m.PTBAName).Name("PTBA_NAME");
                Map(m => m.CEZName).Name("CEZ_NAME");
            }
        }

        public class TaxRateMap : ClassMap<TaxRate>
        {
            public TaxRateMap()
            {
                Map(m => m.Name).Name("Name");
                Map(m => m.LocationCode).Name("Code");
                Map(m => m.State).Name("State");
                Map(m => m.Local).Name("Local");
                Map(m => m.RTA).Name("RTA");
                Map(m => m.Rate).Name("Rate");
                Map(m => m.EffectiveDate).Name("Effective Date").TypeConverter<DateTimeConverter<string>>();
                Map(m => m.ExpirationDate).Name("Expiration Date").TypeConverter<DateTimeConverter<string>>();
            }
        }

        public class ShortZipMap : ClassMap<ShortZip>
        {
            public ShortZipMap()
            {
                Map(m => m.Zip).Index(0);
                Map(m => m.Plus4LowerBound).Index(1);
                Map(m => m.Plus4UpperBound).Index(2);
                Map(m => m.LocationCode).Index(3);
                Map(m => m.State).Index(4);
                Map(m => m.Local).Index(5);
                Map(m => m.TotalRate).Index(6);
                Map(m => m.EffectiveStartDate).Index(7).TypeConverter<DateTimeConverter<string>>();
                Map(m => m.EffectiveEndDate).Index(8).TypeConverter<DateTimeConverter<string>>();
            }
        }

        public class DateTimeConverter<T> : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                return DateTime.ParseExact(text,
                                  "yyyyMMdd",
                                   CultureInfo.InvariantCulture);
            }

            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                return value.ToString();
            }
        }
    }
}