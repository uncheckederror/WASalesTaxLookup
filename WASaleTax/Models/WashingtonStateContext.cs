using Microsoft.EntityFrameworkCore;

namespace WASalesTax.Models
{
    public class WashingtonStateContext(DbContextOptions<WashingtonStateContext> options) : DbContext(options)
    {
        public DbSet<AddressRange> AddressRanges { get; set; }
        public DbSet<TaxRate> TaxRates { get; set; }
        public DbSet<ShortZip> ZipCodes { get; set; }
    }
}
