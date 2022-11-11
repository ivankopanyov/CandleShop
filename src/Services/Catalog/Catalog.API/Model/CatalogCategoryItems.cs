namespace CandleShop.Services.Catalog.API.Model;

public class CatalogCategoryItems
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int ParentCategoryId { get; set; }

    public virtual ICollection<CatalogItem> CatalogItems { get; set; }

    public CatalogCategoryItems()
    {
        CatalogItems = new HashSet<CatalogItem>();
    }
}
