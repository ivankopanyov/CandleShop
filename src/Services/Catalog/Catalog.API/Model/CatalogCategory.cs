namespace CandleShop.Services.Catalog.API.Model;

public class CatalogCategory
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int? ParentCategoryId { get; set; }

    public virtual ICollection<CatalogCategory> Subcategories { get; set; }

    public virtual ICollection<CatalogCategoryItems> SubcategoriesItems { get; set; }

    public CatalogCategory()
    {
        Subcategories = new HashSet<CatalogCategory>();
        SubcategoriesItems = new HashSet<CatalogCategoryItems>();
    }
}
