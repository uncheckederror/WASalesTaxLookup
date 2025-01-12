namespace SalesTax.Models
{
    public readonly record struct AddressGeocodeResponse(Spatialreference spatialReference, Candidate[] candidates);
    public readonly record struct Spatialreference(int wkid, int latestWkid);
    public readonly record struct Candidate(string address, Location location, float score, Extent extent);
    public readonly record struct Location(float x, float y);
    public readonly record struct Extent(float xmin, float ymin, float xmax, float ymax);
}
