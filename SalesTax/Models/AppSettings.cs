namespace SalesTax.Models
{
    // Do to the way configuration binding works we can't make these classes readonly or into records.
    public class AppSettings
    {
        public Connectionstrings ConnectionStrings { get; set; } = new();
        public bool InMemory { get; set; } = false;
    }

    public class Connectionstrings
    {
        public string WashingtonStateContext { get; set; } = string.Empty;
        public string BaseDataUrl { get; set; } = string.Empty;
        public string GeocodingServiceBaseURL { get; set; } = string.Empty;
        public string LOCODELookupBaseURL { get; set; } = string.Empty;
    }
}
