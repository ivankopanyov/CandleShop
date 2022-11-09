namespace CandleShop.Services.Catalog.API.Model;

public class CatalogItem
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int CatalogCategoryId { get; set; }

    public CatalogCategory CatalogCategory { get; set; }
}
