namespace SalesTax.Models
{

    // https://dor.wa.gov/taxes-rates/datafield-descriptions
    // Using integer row Ids for this table because the CSV we're ingesting the data from only has 1.2 million rows; well below the 2.1 millon upper limit of an Int. 
    public record AddressRange(int? AddressRangeLowerBound, int? AddressRangeUpperBound, char OddOrEven, string Street, string ZipCode, string ZipCodePlus4, string Period, int LocationCode, char RTA, string PTBAName, string CEZName)
    {
        public string ToXML(int upperandlowerboundoverride = 0)
        {
            if(upperandlowerboundoverride is not 0)
            {
                return $"<addressline code=\"{LocationCode}\" street=\"{Street.AsSpan()}\" househigh=\"{upperandlowerboundoverride}\" houselow=\"{upperandlowerboundoverride}\" evenodd=\"{OddOrEven}\" state=\"WA\" zip=\"{ZipCode.AsSpan()}\" plus4=\"{ZipCodePlus4.AsSpan()}\" period=\"{Period.AsSpan()}\" rta=\"{RTA}\" ptba=\"{PTBAName.AsSpan()}\" cez=\"{CEZName.AsSpan()}\" />";
            }
            else
            {
                return $"<addressline code=\"{LocationCode}\" street=\"{Street.AsSpan()}\" househigh=\"{AddressRangeUpperBound}\" houselow=\"{AddressRangeLowerBound}\" evenodd=\"{OddOrEven}\" state=\"WA\" zip=\"{ZipCode.AsSpan()}\" plus4=\"{ZipCodePlus4.AsSpan()}\" period=\"{Period.AsSpan()}\" rta=\"{RTA}\" ptba=\"{PTBAName.AsSpan()}\" cez=\"{CEZName.AsSpan()}\" />";
            }
        } 
   };
}
