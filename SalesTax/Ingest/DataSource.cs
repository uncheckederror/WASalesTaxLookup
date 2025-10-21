using Flurl.Http;

using nietras.SeparatedValues;

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
                catch (FlurlHttpException)
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
                catch
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
                catch
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

                using var reader = Sep.New(',').Reader(o => o with { CreateToString = SepToString.PoolPerCol() }).FromFile(pathToCSV);

                addresses = [.. reader.ParallelEnumerate((SepReader.Row row, out AddressRange range) =>
                {
                    range = new AddressRange(row["ADDR_LOW"].Parse<int>(), row["ADDR_HIGH"].Parse<int>(), row["ODD_EVEN"].Parse<char>(),
                        row["STREET"].ToString(), row["ZIP"].ToString(), row["PLUS4"].ToString(), row["PERIOD"].ToString(),
                        row["CODE"].Parse<int>(), row["RTA"].Parse<char>(), row["PTBA_NAME"].ToString(), row["CEZ_NAME"].ToString());
                    return true;
                })];

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

                using var reader = Sep.New(',').Reader(o => o with { CreateToString = SepToString.PoolPerCol() }).FromFile(pathToCSV);

                rates = reader.ParallelEnumerate((SepReader.Row row, out (int key, TaxRate rate) kv) =>
                {
                    var effDate = DateTime.ParseExact(row["Effective Date"].Span, "yyyyMMdd", CultureInfo.InvariantCulture);
                    var expDate = DateTime.ParseExact(row["Expiration Date"].Span, "yyyyMMdd", CultureInfo.InvariantCulture);
                    var rate = new TaxRate(row["Name"].ToString(), row["Code"].Parse<int>(), row["State"].Parse<double>(),
                        row["Local"].Parse<double>(), row["RTA"].Parse<double>(), row["Rate"].Parse<double>(), effDate, expDate);
                    kv = (rate.LocationCode, rate);
                    return true;
                }).ToDictionary();
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

                using var reader = Sep.New(',').Reader(o => o with { HasHeader = false, CreateToString = SepToString.PoolPerCol() }).FromFile(pathToCSV);

                zips = [.. reader.ParallelEnumerate((SepReader.Row row, out ShortZip local) =>
                {
                    local = new ShortZip(row[0].ToString(), row[3].Parse<int>());
                    return true;
                })];
            }
            catch (Exception ex)
            {
                Log.Fatal("[Ingest] Failed to load Short Zip Codes into memory.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex.StackTrace ?? "No stacktrace found.");
            }

            return zips.ToFrozenSet();
        }
    }
}