using System;

namespace WASalesTax.Models
{
    public class ShortZip
    {
        // https://dor.wa.gov/taxes-rates/zip-plus-4-short-data-field-descriptions
        // Using integer row Ids for this table because the CSV we're ingesting the data from only has 170 thousand rows; well below the 2.1 millon upper limit of an Int. 
        public int ShortZipId { get; set; }
        public string Zip { get; set; }
        public string Plus4LowerBound { get; set; }
        public string Plus4UpperBound { get; set; }
        public int LocationCode { get; set; }
        public string State { get; set; }
        public string Local { get; set; }
        public string TotalRate { get; set; }
        public DateTime EffectiveStartDate { get; set; }
        public DateTime EffectiveEndDate { get; set; }
    }
}
