using System.ComponentModel;

namespace SalesTax.Models
{
    // A short zip code in the format described here: https://dor.wa.gov/taxes-rates/zip-plus-4-short-data-field-descriptions
    public class ShortZip
    {
        // https://dor.wa.gov/taxes-rates/zip-plus-4-short-data-field-descriptions
        // Using integer row Ids for this table because the CSV we're ingesting the data from only has 170 thousand rows; well below the 2.1 millon upper limit of an Int. 
        [Description("Internal Id unique to this API.")]
        public int ShortZipId { get; set; }
        [Description("Five-digit zip code")]
        public string Zip { get; set; }
        [Description("Beginning four-digit zip code extension")]
        public string Plus4LowerBound { get; set; }
        [Description("Ending four-digit zip code extension")]
        public string Plus4UpperBound { get; set; }
        [Description("Location code number")]
        public int LocationCode { get; set; }
        [Description("State rate")]
        public string State { get; set; }
        [Description("Local rate (For location codes in RTA areas, includes RTA rate)")]
        public string Local { get; set; }
        [Description("Combined state and local rates")]
        public string TotalRate { get; set; }
        [Description("Rate effective start date")]
        public DateTime EffectiveStartDate { get; set; }
        [Description("Rate expiration date")]
        public DateTime EffectiveEndDate { get; set; }
    }
}
