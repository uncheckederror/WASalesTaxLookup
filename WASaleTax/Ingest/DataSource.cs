using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

using Flurl.Http;

using Microsoft.EntityFrameworkCore;

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
        public static async Task<bool> TryIngestAddressesAsync(string url, WashingtonStateContext db)
        {
            try
            {
                if (await db.AddressRanges.AnyAsync())
                {
                    return true;
                }

                var pathtoFile = await url.DownloadFileAsync(AppContext.BaseDirectory);
                var pathToCSV = Path.Combine(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(pathtoFile) + ".txt");

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
                var rowId = 0;
                csv.Context.RegisterClassMap<AddressRangeMap>();
                using var transaction = await db.Database.BeginTransactionAsync();

                await foreach (var row in csv.GetRecordsAsync<AddressRange>())
                {
                    var command = db.Database.GetDbConnection().CreateCommand();
                    command.CommandText =
                        $"insert into AddressRanges (AddressRangeId, AddressRangeLowerBound, AddressRangeUpperBound, OddOrEven, Street, State, ZipCode, ZipCodePlus4, Period, LocationCode, RTA, PTBAName, CEZName) values ($AddressRangeId, $AddressRangeLowerBound, $AddressRangeUpperBound, $OddOrEven, $Street, $State, $ZipCode, $ZipCodePlus4, $Period, $LocationCode, $RTA, $PTBAName, $CEZName);";

                    var parameterId = command.CreateParameter();
                    parameterId.ParameterName = "$AddressRangeId";
                    command.Parameters.Add(parameterId);
                    parameterId.Value = rowId++;

                    var parameterLowerBound = command.CreateParameter();
                    parameterLowerBound.ParameterName = "$AddressRangeLowerBound";
                    command.Parameters.Add(parameterLowerBound);
                    parameterLowerBound.Value = row?.AddressRangeLowerBound is null ? DBNull.Value : row.AddressRangeLowerBound;

                    var parameterUpperBound = command.CreateParameter();
                    parameterUpperBound.ParameterName = "$AddressRangeUpperBound";
                    command.Parameters.Add(parameterUpperBound);
                    parameterUpperBound.Value = row?.AddressRangeUpperBound is null ? DBNull.Value : row.AddressRangeUpperBound;

                    var parameterOddOrEven = command.CreateParameter();
                    parameterOddOrEven.ParameterName = "$OddOrEven";
                    command.Parameters.Add(parameterOddOrEven);
                    parameterOddOrEven.Value = row.OddOrEven;

                    var parameterStreet = command.CreateParameter();
                    parameterStreet.ParameterName = "$Street";
                    command.Parameters.Add(parameterStreet);
                    parameterStreet.Value = string.IsNullOrWhiteSpace(row?.Street) ? DBNull.Value : row.Street;

                    var parameterState = command.CreateParameter();
                    parameterState.ParameterName = "$State";
                    command.Parameters.Add(parameterState);
                    parameterState.Value = string.IsNullOrWhiteSpace(row?.State) ? DBNull.Value : row.State;

                    var parameterZipCode = command.CreateParameter();
                    parameterZipCode.ParameterName = "$ZipCode";
                    command.Parameters.Add(parameterZipCode);
                    parameterZipCode.Value = string.IsNullOrWhiteSpace(row?.ZipCode) ? DBNull.Value : row.ZipCode;

                    var parameterZipCodePlus4 = command.CreateParameter();
                    parameterZipCodePlus4.ParameterName = "$ZipCodePlus4";
                    command.Parameters.Add(parameterZipCodePlus4);
                    parameterZipCodePlus4.Value = string.IsNullOrWhiteSpace(row?.ZipCodePlus4) ? DBNull.Value : row.ZipCodePlus4;

                    var parameterPeriod = command.CreateParameter();
                    parameterPeriod.ParameterName = "$Period";
                    command.Parameters.Add(parameterPeriod);
                    parameterPeriod.Value = string.IsNullOrWhiteSpace(row?.Period) ? DBNull.Value : row.Period;

                    var parameterLocationCode = command.CreateParameter();
                    parameterLocationCode.ParameterName = "$LocationCode";
                    command.Parameters.Add(parameterLocationCode);
                    parameterLocationCode.Value = row.LocationCode;

                    var parameterRTA = command.CreateParameter();
                    parameterRTA.ParameterName = "$RTA";
                    command.Parameters.Add(parameterRTA);
                    parameterRTA.Value = row.RTA;

                    var parameterPTBAName = command.CreateParameter();
                    parameterPTBAName.ParameterName = "$PTBAName";
                    command.Parameters.Add(parameterPTBAName);
                    parameterPTBAName.Value = string.IsNullOrWhiteSpace(row?.PTBAName) ? DBNull.Value : row.PTBAName;

                    var parameterCEZName = command.CreateParameter();
                    parameterCEZName.ParameterName = "$CEZName";
                    command.Parameters.Add(parameterCEZName);
                    parameterCEZName.Value = string.IsNullOrWhiteSpace(row?.CEZName) ? DBNull.Value : row.CEZName;

                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

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

                using var reader = new StreamReader(pathToCSV);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<TaxRateMap>();
                using var transaction = await db.Database.BeginTransactionAsync();

                await foreach (var row in csv.GetRecordsAsync<TaxRate>())
                {
                    var command = db.Database.GetDbConnection().CreateCommand();
                    command.CommandText =
                        $"insert into TaxRates (Name, LocationCode, State, Local, RTA, Rate, EffectiveDate, ExpirationDate) values ($Name, $LocationCode, $State, $Local, $RTA, $Rate, $EffectiveDate, $ExpirationDate);";

                    var parameterName = command.CreateParameter();
                    parameterName.ParameterName = "$Name";
                    command.Parameters.Add(parameterName);
                    parameterName.Value = string.IsNullOrWhiteSpace(row?.Name) ? DBNull.Value : row.Name;

                    var parameterLocationCode = command.CreateParameter();
                    parameterLocationCode.ParameterName = "$LocationCode";
                    command.Parameters.Add(parameterLocationCode);
                    parameterLocationCode.Value = row.LocationCode;

                    var parameterState = command.CreateParameter();
                    parameterState.ParameterName = "$State";
                    command.Parameters.Add(parameterState);
                    parameterState.Value = row.State;

                    var parameterLocal = command.CreateParameter();
                    parameterLocal.ParameterName = "$Local";
                    command.Parameters.Add(parameterLocal);
                    parameterLocal.Value = row.Local;

                    var parameterRTA = command.CreateParameter();
                    parameterRTA.ParameterName = "$RTA";
                    command.Parameters.Add(parameterRTA);
                    parameterRTA.Value = row.RTA;

                    var parameterRate = command.CreateParameter();
                    parameterRate.ParameterName = "$Rate";
                    command.Parameters.Add(parameterRate);
                    parameterRate.Value = row.Rate;

                    var parameterEffectiveDate = command.CreateParameter();
                    parameterEffectiveDate.ParameterName = "$EffectiveDate";
                    command.Parameters.Add(parameterEffectiveDate);
                    parameterEffectiveDate.Value = row.EffectiveDate;

                    var parameterExpirationDate = command.CreateParameter();
                    parameterExpirationDate.ParameterName = "$ExpirationDate";
                    command.Parameters.Add(parameterExpirationDate);
                    parameterExpirationDate.Value = row.ExpirationDate;

                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
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
                using var reader = new StreamReader(pathToCSV);
                using var csv = new CsvReader(reader, config);
                var rowId = 0;
                csv.Context.RegisterClassMap<ShortZipMap>();
                using var transaction = await db.Database.BeginTransactionAsync();

                await foreach (var row in csv.GetRecordsAsync<ShortZip>())
                {
                    var command = db.Database.GetDbConnection().CreateCommand();
                    command.CommandText =
                        $"insert into ZipCodes (ShortZipId, Zip, Plus4LowerBound, Plus4UpperBound, LocationCode, State, Local, TotalRate, EffectiveStartDate, EffectiveEndDate) values ($ShortZipId, $Zip, $Plus4LowerBound, $Plus4UpperBound, $LocationCode, $State, $Local, $TotalRate, $EffectiveStartDate, $EffectiveEndDate);";

                    var parameterShortZipId = command.CreateParameter();
                    parameterShortZipId.ParameterName = "$ShortZipId";
                    command.Parameters.Add(parameterShortZipId);
                    parameterShortZipId.Value = rowId++;

                    var parameterZip = command.CreateParameter();
                    parameterZip.ParameterName = "$Zip";
                    command.Parameters.Add(parameterZip);
                    parameterZip.Value = string.IsNullOrWhiteSpace(row?.Zip) ? DBNull.Value : row.Zip;

                    var parameterPlus4LowerBound = command.CreateParameter();
                    parameterPlus4LowerBound.ParameterName = "$Plus4LowerBound";
                    command.Parameters.Add(parameterPlus4LowerBound);
                    parameterPlus4LowerBound.Value = string.IsNullOrWhiteSpace(row?.Plus4LowerBound) ? DBNull.Value : row.Plus4LowerBound;

                    var parameterPlus4UpperBound = command.CreateParameter();
                    parameterPlus4UpperBound.ParameterName = "$Plus4UpperBound";
                    command.Parameters.Add(parameterPlus4UpperBound);
                    parameterPlus4UpperBound.Value = string.IsNullOrWhiteSpace(row?.Plus4UpperBound) ? DBNull.Value : row.Plus4UpperBound;

                    var parameterLocationCode = command.CreateParameter();
                    parameterLocationCode.ParameterName = "$LocationCode";
                    command.Parameters.Add(parameterLocationCode);
                    parameterLocationCode.Value = row.LocationCode;

                    var parameterState = command.CreateParameter();
                    parameterState.ParameterName = "$State";
                    command.Parameters.Add(parameterState);
                    parameterState.Value = string.IsNullOrWhiteSpace(row?.State) ? DBNull.Value : row.State;

                    var parameterLocal = command.CreateParameter();
                    parameterLocal.ParameterName = "$Local";
                    command.Parameters.Add(parameterLocal);
                    parameterLocal.Value = string.IsNullOrWhiteSpace(row?.Local) ? DBNull.Value : row.Local;

                    var parameterTotalRate = command.CreateParameter();
                    parameterTotalRate.ParameterName = "$TotalRate";
                    command.Parameters.Add(parameterTotalRate);
                    parameterTotalRate.Value = string.IsNullOrWhiteSpace(row?.TotalRate) ? DBNull.Value : row.TotalRate;

                    var parameterEffectiveStartDate = command.CreateParameter();
                    parameterEffectiveStartDate.ParameterName = "$EffectiveStartDate";
                    command.Parameters.Add(parameterEffectiveStartDate);
                    parameterEffectiveStartDate.Value = row.EffectiveStartDate;

                    var parameterEffectiveEndDate = command.CreateParameter();
                    parameterEffectiveEndDate.ParameterName = "$EffectiveEndDate";
                    command.Parameters.Add(parameterEffectiveEndDate);
                    parameterEffectiveEndDate.Value = row.EffectiveEndDate;

                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

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
