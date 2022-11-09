namespace CandleShop.Services.Catalog.API.Infrastructure.EntityConfigurations;

class CatalogItemEntityTypeConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("Catalog");

        builder.Property(ci => ci.Id)
            .IsRequired();

        builder.Property(ci => ci.Name)
            .IsRequired(true)
            .HasMaxLength(50);

        builder.HasOne(ci => ci.CatalogCategory)
            .WithMany()
            .HasForeignKey(ci => ci.CatalogCategoryId);
    }
}
