namespace CandleShop.Services.Catalog.API.Controllers;

[Route("api/v1/catalog/categories")]
[ApiController]
public class CategoryController : ControllerBase
{
    private readonly CatalogContext _catalogContext;

    public CategoryController(CatalogContext context)
        => _catalogContext = context ?? throw new ArgumentNullException(nameof(context));

    #region GET

    [HttpGet]
    [Route("category/{id:int}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(CatalogCategory), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<CatalogCategory>> CategoryByIdAsync(int id)
    {
        if (id <= 0)
            return BadRequest();

        var category = await _catalogContext.CatalogCategories.SingleOrDefaultAsync(cc => cc.Id == id);

        if (category != null)
            return category;

        return NotFound();
    }

    [HttpGet]
    [Route("categoryItems/{id:int}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(CatalogCategoryItems), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<CatalogCategoryItems>> CategoryItemsByIdAsync(int id)
    {
        if (id <= 0)
            return BadRequest();

        var catalogCategoryItems = await _catalogContext.CatalogCategoriesItems.SingleOrDefaultAsync(cci => cci.Id == id);

        if (catalogCategoryItems != null)
            return catalogCategoryItems;

        return NotFound();
    }

    #endregion

    #region POST

    [Route("category")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> CreateCategoryAsync([FromBody] CatalogCategoryDto category)
    {
        if (category.ParentId <= 0 || await _catalogContext.CatalogCategories.SingleOrDefaultAsync(cc => cc.Id == category.ParentId) == null)
            return BadRequest(new { Message = "Родительская категория указана некорректно." });

        if (string.IsNullOrWhiteSpace(category.Name))
            return BadRequest(new { Message = "Название категории не должно быть пустым." });

        var catalogCategory = new CatalogCategory
        {
            Name = category.Name.Trim(),
            ParentCategoryId = category.ParentId
        };

        _catalogContext.CatalogCategories.Add(catalogCategory);

        await _catalogContext.SaveChangesAsync();

        return CreatedAtAction(nameof(CategoryByIdAsync), new { id = catalogCategory.Id }, null);
    }

    [Route("categoryItems")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> CreateCategoryItemsAsync([FromBody] CatalogCategoryDto category)
    {
        if (category.ParentId <= 0 || await _catalogContext.CatalogCategories.SingleOrDefaultAsync(cc => cc.Id == category.ParentId) == null)
            return BadRequest(new { Message = "Родительская категория указана некорректно." });

        if (string.IsNullOrWhiteSpace(category.Name))
            return BadRequest(new { Message = "Название категории не должно быть пустым." });

        var catalogCategoryItems = new CatalogCategoryItems
        {
            Name = category.Name.Trim(),
            ParentCategoryId = category.ParentId
        };

        _catalogContext.CatalogCategoriesItems.Add(catalogCategoryItems);

        await _catalogContext.SaveChangesAsync();

        return CreatedAtAction(nameof(CategoryItemsByIdAsync), new { id = catalogCategoryItems.Id }, null);
    }

    #endregion
}