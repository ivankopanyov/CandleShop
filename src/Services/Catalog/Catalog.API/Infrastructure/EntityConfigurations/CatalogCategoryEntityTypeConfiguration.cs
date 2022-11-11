namespace CandleShop.Services.Catalog.API.Infrastructure.EntityConfigurations;

class CatalogCategoryEntityTypeConfiguration : IEntityTypeConfiguration<CatalogCategory>
{
    public void Configure(EntityTypeBuilder<CatalogCategory> builder)
    {
        builder.ToTable("CatalogCategories");

        builder.HasKey(cc => cc.Id);

        builder.Property(cc => cc.Id)
            .IsRequired();

        builder.Property(cc => cc.Name)
            .IsRequired(true)
            .HasMaxLength(50);

        builder.HasMany(cc => cc.Subcategories)
            .WithOne()
            .HasForeignKey(cc => cc.ParentCategoryId);

        builder.HasMany(cc => cc.SubcategoriesItems)
            .WithOne()
            .HasForeignKey(cci => cci.ParentCategoryId);
    }
}
