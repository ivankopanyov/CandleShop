namespace CandleShop.Services.Catalog.API.Model;

public class CatalogCategory
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int? ParentCategoryId { get; set; }

    public ICollection<CatalogCategory> Subcategories { get; set; }

    public ICollection<CatalogCategoryItems> SubcategoriesItems { get; set; }
}
