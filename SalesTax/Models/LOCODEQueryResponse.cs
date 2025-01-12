namespace SalesTax.Models
{
    // Removed content from the response
    // public readonly record struct LOCODEQueryResponse(string displayFieldName, Fieldaliases fieldAliases, Field[] fields, Feature[] features);
    public readonly record struct LOCODEQueryResponse(Feature[] features);
    public readonly record struct Fieldaliases(string LOCCODE);
    public readonly record struct Field(string name, string type, string alias, int length);
    public readonly record struct Feature(Attributes attributes);
    public readonly record struct Attributes(string LOCCODE);
}
