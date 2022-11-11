namespace CandleShop.Services.Catalog.API.Infrastructure;

public class CatalogContext : DbContext
{
    private readonly IConfiguration _configuration;

    public DbSet<CatalogItem> CatalogItems { get; set; }

    public DbSet<CatalogCategory> CatalogCategories { get; set; }

    public DbSet<CatalogCategoryItems> CatalogCategoriesItems { get; set; }

    public CatalogContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new CatalogCategoryEntityTypeConfiguration());
        builder.ApplyConfiguration(new CatalogCategoryItemsEntityTypeConfiguration());
        builder.ApplyConfiguration(new CatalogItemEntityTypeConfiguration());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite(_configuration.GetConnectionString("WebApiDatabase"));
    }
}
