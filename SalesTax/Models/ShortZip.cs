namespace SalesTax.Models
{
    // A short zip code in the format described here: https://dor.wa.gov/taxes-rates/zip-plus-4-short-data-field-descriptions
    // 170 thousand rows; well below the 2.1 millon upper limit of an Int. 
    public record ShortZip(string Zip, int LocationCode);
}
