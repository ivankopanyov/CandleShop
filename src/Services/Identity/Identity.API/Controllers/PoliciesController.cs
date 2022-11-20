namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class PoliciesController : IdentityController
{
    private readonly IdentityContext _identityContext;

    public PoliciesController(UserManager<User> userManager, RoleManager<Role> roleManager, IConfiguration configuration, IdentityContext identityContext) : base(userManager, roleManager, configuration)
    {
        _identityContext = identityContext;
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "policies/getAll")]
    [HttpGet]
    [Route("getAll")]
    public async Task<List<Policy>> GetAll() => await _identityContext.Policies.ToListAsync();

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "policies/get")]
    [HttpGet]
    [Route("get")]
    public async Task<ActionResult<Policy>> GetAsync(int id)
    {
        if (id <= 0)
            return NotFound();

        var policy = await _identityContext.Policies.FirstOrDefaultAsync(policy => policy.Id == id);
        return policy == null ? NotFound() : Ok(policy);
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "policies/findByName")]
    [HttpGet]
    [Route("findByName")]
    public async Task<List<Policy>> FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new List<Policy>();

        return await _identityContext.Policies.Where(policy => policy.Name.ToUpper().Contains(name.Trim().ToUpper())).ToListAsync();
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "policies/getInRange")]
    [HttpGet]
    [Route("getInRange")]
    public async Task<IEnumerable<Policy>> GetInRange(int minAccessLevel, int maxAccessLevel) =>
        await _identityContext.Policies.Where(policy => policy.MinimumAccessLevel >= minAccessLevel && policy.MinimumAccessLevel <= maxAccessLevel).ToListAsync();

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "policies/change")]
    [HttpPut]
    [Route("change")]
    public async Task<ActionResult> ChangeAsync(PolicyDto policy)
    {
        if (policy.Id <= 0)
            return NotFound();

        if (policy.MinimumAccessLevel < 0)
            return BadRequest();

        int accessLevel = await GetAccessLevelAsync();

        Policy policyEntity = (await _identityContext.Policies.FirstOrDefaultAsync(p => p.Id == policy.Id))!;
        if (policyEntity == null)
            return NotFound();

        int max = AccessLevelMax;
        if (accessLevel == max)
        {
            if (policy.MinimumAccessLevel > accessLevel)
                return BadRequest();
        }
        else
        {
            if (policyEntity.MinimumAccessLevel > accessLevel)
                return NotFound();

            if (policy.MinimumAccessLevel > accessLevel)
                return BadRequest();
        }

        policyEntity.MinimumAccessLevel = policy.MinimumAccessLevel;
        _identityContext.Policies.Update(policyEntity);
        await _identityContext.SaveChangesAsync();
        return Ok();
    }
}
