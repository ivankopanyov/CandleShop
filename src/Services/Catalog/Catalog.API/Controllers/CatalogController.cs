namespace CandleShop.Services.Catalog.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class CatalogController : ControllerBase
{
    private readonly CatalogContext _catalogContext;

    public CatalogController(CatalogContext context) 
        => _catalogContext = context ?? throw new ArgumentNullException(nameof(context));

    [HttpGet]
    [Route("categories/{id:int}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(CatalogCategory), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<CatalogCategory>> CategoryByIdAsync(int id)
    {
        if (id <= 0)
            return BadRequest();

        var category = await _catalogContext.CatalogCategories.SingleOrDefaultAsync(ci => ci.Id == id);

        if (category != null)
            return category;

        return NotFound();
    }

    [Route("categories")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> CreateCategoryAsync([FromBody] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { Message = "Название категории не должно быть пустым." });

        var catalogCategory = new CatalogCategory
        {
            Name = name
        };

        _catalogContext.CatalogCategories.Add(catalogCategory);

        await _catalogContext.SaveChangesAsync();

        return CreatedAtAction(nameof(CategoryByIdAsync), new { id = catalogCategory.Id }, null);
    }
}
