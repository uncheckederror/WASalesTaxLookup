using System.ComponentModel;

namespace SalesTax.Models
{
    // https://dor.wa.gov/taxes-rates/location-codes-and-rate-tables-field-descriptions
    public readonly record struct TaxRate
    (
        [property: Description("Location code name")]
        string Name,
        [property: Description("Location code number, unique Id")]
        int LocationCode,
        [property: Description("State rate")]
        double State,
        [property: Description("Local rate (For location codes in RTA areas, includes RTA rate)")]
        double Local,
        [property: Description("RTA rate (currently defaulted to zero)")]
        double RTA,
        [property: Description("Combined state and local rates")]
        double Rate,
        [property: Description("Rate effective start date")]
        DateTime EffectiveDate,
        [property: Description("Rate expiration date")]
        DateTime ExpirationDate
    )
    {
        public string ToXML()
        {
            return $"<rate name=\"{Name}\" code=\"{LocationCode}\" staterate=\"{State.ToString().TrimStart('0')}\" localrate=\"{Local.ToString().TrimStart('0')}\" />";
        }
    }
}
