using Microsoft.EntityFrameworkCore;

using WASalesTax.Models;

namespace WASalesTax
{
    public class WashingtonStateContext : DbContext
    {
        public DbSet<AddressRange> AddressRanges { get; set; }
        public DbSet<TaxRate> TaxRates { get; set; }
        public DbSet<ShortZip> ZipCodes { get; set; }

        public WashingtonStateContext(DbContextOptions<WashingtonStateContext> options)
            : base(options)
        {
        }
    }
}
