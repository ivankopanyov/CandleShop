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
    [Route("all")]
    [ProducesResponseType(typeof(CatalogCategory), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<CatalogCategory>> AllCategoriesAsync()
    {
        var categories = await _catalogContext.CatalogCategories.ToDictionaryAsync(cc => cc.Id, cc => cc);
        var categoriesItems = await _catalogContext.CatalogCategoriesItems.ToArrayAsync();

        foreach (var categoryItems in categoriesItems)
            categories[categoryItems.ParentCategoryId].SubcategoriesItems.Add(categoryItems);

        CatalogCategory baseCategory = null!;

        foreach(var category in categories)
            if (category.Value.ParentCategoryId == null)
                baseCategory = category.Value;
            else
                categories[(int)category.Value.ParentCategoryId].Subcategories.Add(category.Value);

        return baseCategory!;
    }

    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(CatalogCategory), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<CatalogCategory>> CategoryByIdAsync(int id)
    {
        if (id <= 0)
            return BadRequest();

        var category = await _catalogContext.CatalogCategories
            .SingleOrDefaultAsync(cc => cc.Id == id);

        return category != null ? category : NotFound();
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

        var categoryItems = await _catalogContext.CatalogCategoriesItems
            .SingleOrDefaultAsync(cci => cci.Id == id);

        return categoryItems != null ? categoryItems : NotFound();
    }

    #endregion

    #region POST

    [Route("add")]
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

        await _catalogContext.CatalogCategories.AddAsync(catalogCategory);
        await _catalogContext.SaveChangesAsync();

        return CreatedAtAction(nameof(CategoryByIdAsync), new { id = catalogCategory.Id }, null);
    }

    [Route("add/categoryItems")]
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

        await _catalogContext.CatalogCategoriesItems.AddAsync(catalogCategoryItems);
        await _catalogContext.SaveChangesAsync();

        return CreatedAtAction(nameof(CategoryItemsByIdAsync), new { id = catalogCategoryItems.Id }, null);
    }

    #endregion
}