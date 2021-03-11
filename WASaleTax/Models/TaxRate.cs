using System;
using System.ComponentModel.DataAnnotations;

namespace WASalesTax.Models
{
    public class TaxRate
    {
        // https://dor.wa.gov/taxes-rates/location-codes-and-rate-tables-field-descriptions
        // Using integer row Ids for this table because the CSV we're ingesting the data from only has 364 rows.
        public string Name { get; set; }
        [Key]
        public int LocationCode { get; set; }
        public double State { get; set; }
        public double Local { get; set; }
        public double RTA { get; set; }
        public double Rate { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpirationDate { get; set; }

        public string ToXML()
        {
            return $"<rate name=\"{Name}\" code=\"{LocationCode}\" staterate=\"{State:.000}\" localrate=\"{Local:.000}\" />";
        }
    }
}
