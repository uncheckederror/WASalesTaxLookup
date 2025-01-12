namespace SalesTax.Models
{
    public class AddressRange
    {
        // https://dor.wa.gov/taxes-rates/datafield-descriptions
        // Using integer row Ids for this table because the CSV we're ingesting the data from only has 1.2 million rows; well below the 2.1 millon upper limit of an Int. 
        //public int AddressRangeId { get; set; }
        public int? AddressRangeLowerBound { get; set; }
        public int? AddressRangeUpperBound { get; set; }
        public char OddOrEven { get; set; }
        public string Street { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string ZipCodePlus4 { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public int LocationCode { get; set; }
        public char RTA { get; set; }
        public string PTBAName { get; set; } = string.Empty;
        public string CEZName { get; set; } = string.Empty;
        public string ToXML()
        {
            return $"<addressline code=\"{LocationCode}\" street=\"{Street}\" househigh=\"{AddressRangeUpperBound}\" houselow=\"{AddressRangeLowerBound}\" evenodd=\"{OddOrEven}\" state=\"{State}\" zip=\"{ZipCode}\" plus4=\"{ZipCodePlus4}\" period=\"{Period}\" rta=\"{RTA}\" ptba=\"{PTBAName}\" cez=\"{CEZName}\" />";
        }
    }
}
