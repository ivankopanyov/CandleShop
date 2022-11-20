using CandleShop.Services.Identity.API.Model.Entities;

namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class PoliciesController : ControllerBase
{
    private readonly RoleManager<Role> _roleManager;

    private readonly UserManager<User> _userManager;

    private readonly IdentityContext _identityContext;

    private HashSet<int> AccessLevels => _roleManager.Roles.Select(role => role.AccessLevel).ToHashSet();

    public PoliciesController(RoleManager<Role> roleManager, UserManager<User> userManager, IdentityContext identityContext)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _identityContext = identityContext;
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "policies/getAll")]
    [HttpGet]
    [Route("getAll")]
    public async Task<IEnumerable<Policy>> GetAll()
    {
        int accessLevel = await GetAccessLevelAsync();

        return accessLevel == AccessLevels.Max() ? 
            _identityContext.Policies : 
            _identityContext.Policies.Where(policy => policy.MinimumAccessLevel <= accessLevel);
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "policies/get")]
    [HttpGet]
    [Route("get")]
    public async Task<ActionResult<Policy>> GetAsync(int id)
    {
        if (id <= 0)
            return NotFound();

        var policy = await _identityContext.Policies.FirstOrDefaultAsync(policy => policy.Id == id);
        if (policy == null)
            return NotFound();

        int accessLevel = await GetAccessLevelAsync();

        return (accessLevel != AccessLevels.Max() && accessLevel < policy.MinimumAccessLevel) ?
            NotFound() : Ok(policy);
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "policies/findByName")]
    [HttpGet]
    [Route("findByName")]
    public async Task<IEnumerable<Policy>> FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Array.Empty<Policy>();

        int accessLevel = await GetAccessLevelAsync();
        return accessLevel == AccessLevels.Max() ?
            _identityContext.Policies.Where(policy => policy.Name.ToUpper().Contains(name.Trim().ToUpper())) :
            _identityContext.Policies.Where(policy => policy.Name.ToUpper().Contains(name.Trim().ToUpper()) &&
               policy.MinimumAccessLevel <= accessLevel);
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "policies/getInRange")]
    [HttpGet]
    [Route("getInRange")]
    public async Task<IEnumerable<Policy>> GetInRange(int minAccessLevel, int maxAccessLevel)
    {
        int accessLevel = await GetAccessLevelAsync();

        if (minAccessLevel < 0)
            minAccessLevel = 0;

        int max = 0;
        await _identityContext.Policies.ForEachAsync(policy => {
            if (policy.MinimumAccessLevel > max)
                max = policy.MinimumAccessLevel;
        });

        maxAccessLevel = Math.Min(max, maxAccessLevel);
        if (accessLevel != AccessLevels.Max())
            maxAccessLevel = Math.Min(maxAccessLevel, accessLevel);

        return _identityContext.Policies.Where(policy => policy.MinimumAccessLevel >= minAccessLevel && policy.MinimumAccessLevel <= maxAccessLevel);
    }

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

        int max = AccessLevels.Max();
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

    private async Task<User?> GetCurrentUserAsync()
    {
        var idClaim = User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier);

        if (idClaim == null)
            return null;

        return await _userManager.Users.FirstOrDefaultAsync(user => user.Id == idClaim.Value);
    }

    private async Task<IList<string>> GetRolesAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Array.Empty<string>();

        return await _userManager.GetRolesAsync(user);
    }

    private async Task<int> GetAccessLevelAsync()
    {
        HashSet<string> rolesNames = (await GetRolesAsync()).ToHashSet();
        HashSet<int> accessLevels = new HashSet<int>() { 0 };
        await _roleManager.Roles.ForEachAsync(role => {
            if (rolesNames.Contains(role.Name))
                accessLevels.Add(role.AccessLevel);
        });
        return accessLevels.Max();
    }
}
