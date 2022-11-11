namespace CandleShop.Services.Catalog.API.Model;

public class CatalogCategoryItems
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int ParentCategoryId { get; set; }

    public CatalogCategory ParentCategory { get; set; }

    public ICollection<CatalogItem> CatalogItems { get; set; }
}
