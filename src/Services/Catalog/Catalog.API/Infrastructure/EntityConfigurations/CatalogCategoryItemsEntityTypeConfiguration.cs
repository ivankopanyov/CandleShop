namespace CandleShop.Services.Catalog.API.Infrastructure.EntityConfigurations;

public class CatalogCategoryItemsEntityTypeConfiguration : IEntityTypeConfiguration<CatalogCategoryItems>
{
    public void Configure(EntityTypeBuilder<CatalogCategoryItems> builder)
    {
        builder.ToTable("CatalogCategoriesItems");

        builder.HasKey(cci => cci.Id);

        builder.Property(cci => cci.Id)
            .IsRequired();

        builder.Property(cci => cci.Name)
            .IsRequired(true)
            .HasMaxLength(50);

        builder.HasOne(cci => cci.ParentCategory)
            .WithMany(cc => cc.SubcategoriesItems)
            .HasForeignKey(cci => cci.ParentCategoryId);

        builder.HasMany(cci => cci.CatalogItems)
            .WithOne(ci => ci.CatalogCategoryItems)
            .HasForeignKey(ci => ci.CatalogCategoryItemsId);
    }
}
