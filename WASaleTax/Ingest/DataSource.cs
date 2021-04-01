using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

using Flurl.Http;

using Serilog;

using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using WASalesTax.Models;

namespace WASalesTax.Ingest
{
    public class DataSource
    {
        private const int batchSize = 100;
        public static async Task<bool> TryIngestAddressesAsync(string url, WashingtonStateContext db)
        {
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

                db.ChangeTracker.AutoDetectChangesEnabled = false;

                using (var reader = new StreamReader(pathToCSV))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var count = 0;
                    csv.Context.RegisterClassMap<AddressRangeMap>();
                    await foreach (var row in csv.GetRecordsAsync<AddressRange>())
                    {
                        count++;
                        await db.AddAsync(row);

                        if (count != 0 && count % batchSize == 0)
                        {
                            await db.SaveChangesAsync();
                        }
                    }

                    await db.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal("[Ingest] Failed to load Addresses into the database.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex.StackTrace);
                return false;
            }
        }

        public static async Task<bool> TryIngestTaxRatesAsync(string url, WashingtonStateContext db)
        {
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

                db.ChangeTracker.AutoDetectChangesEnabled = false;

                using (var reader = new StreamReader(pathToCSV))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var count = 0;
                    csv.Context.RegisterClassMap<TaxRateMap>();
                    await foreach (var row in csv.GetRecordsAsync<TaxRate>())
                    {
                        count++;
                        await db.AddAsync(row);

                        if (count != 0 && count % batchSize == 0)
                        {
                            await db.SaveChangesAsync();
                        }
                    }

                    await db.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal("[Ingest] Failed to load Tax Rates into the database.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex.StackTrace);
                return false;
            }
        }

        public static async Task<bool> TryIngestShortZipCodesAsync(string url, WashingtonStateContext db)
        {
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
                    fileName = entry.FullName;
                }
                var pathToCSV = Path.Combine(AppContext.BaseDirectory, fileName);

                if (!File.Exists(pathToCSV))
                {
                    ZipFile.ExtractToDirectory(pathtoFile, AppContext.BaseDirectory);
                }

                db.ChangeTracker.AutoDetectChangesEnabled = false;

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false,
                    Delimiter = ","
                };
                using (var reader = new StreamReader(pathToCSV))
                using (var csv = new CsvReader(reader, config))
                {
                    var count = 0;
                    csv.Context.RegisterClassMap<ShortZipMap>();
                    await foreach (var row in csv.GetRecordsAsync<ShortZip>())
                    {
                        count++;
                        await db.AddAsync(row);

                        if (count != 0 && count % batchSize == 0)
                        {
                            await db.SaveChangesAsync();
                        }
                    }

                    await db.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal("[Ingest] Failed to load Short Zip Codes into the database.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex.StackTrace);
                return false;
            }
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
