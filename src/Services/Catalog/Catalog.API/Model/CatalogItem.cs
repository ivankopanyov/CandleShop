namespace CandleShop.Services.Catalog.API.Model;

public class CatalogItem
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int CatalogCategoryItemsId { get; set; }

    public virtual CatalogCategoryItems CatalogCategoryItems { get; set; }
}
