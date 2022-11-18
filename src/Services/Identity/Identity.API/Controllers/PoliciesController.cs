using Identity.API.Model.Entities;
using Microsoft.AspNetCore.Identity;

namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class PoliciesController : ControllerBase
{
    private readonly IdentityContext _identityContext;

    public PoliciesController(IdentityContext identityContext)
    {
        _identityContext = identityContext;
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "policies/all")]
    [HttpGet]
    [Route("all")]
    public async Task<ActionResult<IEnumerable<Policy>>> GetAll()
    {
        return await _identityContext.Policies.ToArrayAsync();
    }
}
