namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly RoleManager<Role> _roleManager;

    private readonly UserManager<User> _userManager;

    private HashSet<int> AccessLevels => _roleManager.Roles.Select(role => role.AccessLevel).ToHashSet();

    public RolesController(RoleManager<Role> roleManager, UserManager<User> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/create")]
    [HttpPost]
    [Route("add")]
    public async Task<IActionResult> CreateAsync(string name, int accessLevel)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest();

        int userAccessLevel = await GetAccessLevelAsync();

        if (accessLevel <= 0)
            return BadRequest();

        if (accessLevel >= userAccessLevel)
            return Forbid();

        IdentityResult result = await _roleManager.CreateAsync(new Role(name.Trim(), accessLevel));

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/getAll")]
    [HttpGet]
    [Route("getAll")]
    public async Task<IEnumerable<Role>> GetAll()
    {
        int accessLevel = await GetAccessLevelAsync();

        if (accessLevel == AccessLevels.Max())
            return _roleManager.Roles;
        
        var userRoles = await GetRolesAsync();
        return _roleManager.Roles.Where(role => role.AccessLevel < accessLevel || userRoles.Contains(role.Name));
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/get")]
    [HttpGet]
    [Route("get")]
    public async Task<ActionResult<Role>> GetAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        int accessLevel = await GetAccessLevelAsync();

        var role = await _roleManager.Roles.FirstOrDefaultAsync(role => role.Id == id);

        if (role == null || accessLevel < role.AccessLevel)
            return NotFound();

        if (accessLevel == AccessLevels.Max() || accessLevel > role.AccessLevel)
            return Ok(role);

        var userRoles = await GetRolesAsync();
        return userRoles.Contains(role.Name) ? Ok(role) : NotFound();
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/findByName")]
    [HttpGet]
    [Route("findByName")]
    public async Task<IEnumerable<Role>> FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Array.Empty<Role>();

        int accessLevel = await GetAccessLevelAsync();

        if (accessLevel == AccessLevels.Max())
            return _roleManager.Roles.Where(role => role.NormalizedName.Contains(name.Trim().ToUpper()));

        var userRoles = await GetRolesAsync();
        return _roleManager.Roles.Where(role => role.Name.Contains(name) && (role.AccessLevel < accessLevel || userRoles.Contains(role.Name)));
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/getInRange")]
    [HttpGet]
    [Route("getInRange")]
    public async Task<IEnumerable<Role>> GetInRange(int minAccessLevel, int maxAccessLevel)
    {
        int accessLevel = await GetAccessLevelAsync();

        int min = AccessLevels.Min();
        if (minAccessLevel < 1)
            minAccessLevel = 1;

        int max = AccessLevels.Max();
        if (maxAccessLevel >= accessLevel)
            maxAccessLevel = accessLevel == max ? accessLevel : accessLevel - 1;

        if (accessLevel == max)
            return _roleManager.Roles.Where(role => role.AccessLevel >= minAccessLevel && role.AccessLevel <= maxAccessLevel);

        var userRoles = await GetRolesAsync();
        return _roleManager.Roles.Where(role => (role.AccessLevel >= minAccessLevel && role.AccessLevel <= maxAccessLevel) || userRoles.Contains(role.Name));
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/change")]
    [HttpPut]
    [Route("change")]
    public async Task<ActionResult> ChangeAsync(RoleDto role)
    {
        if (string.IsNullOrWhiteSpace(role.Id) || string.IsNullOrWhiteSpace(role.Name) || role.AccessLevel < 1)
            return BadRequest();

        int accessLevel = await GetAccessLevelAsync();

        Role roleEntity = (await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == role.Id))!;

        if (roleEntity == null)
            return NotFound();

        int max = AccessLevels.Max();
        IdentityResult result;
        
        if (roleEntity.AccessLevel >= accessLevel)
        {
            if (accessLevel == max)
            {
                if (roleEntity.AccessLevel == max)
                {
                    int secondMax = 0;
                    foreach (var level in AccessLevels)
                        if (level > secondMax && level < max)
                            secondMax = level;

                    if (role.AccessLevel <= secondMax)
                        return BadRequest();
                }
                else if (role.AccessLevel >= max || role.AccessLevel < 1)
                    return BadRequest();

                roleEntity.Name = role.Name;
                roleEntity.AccessLevel = role.AccessLevel;

                result = await _roleManager.UpdateAsync(roleEntity);

                return result.Succeeded ? Ok() : BadRequest(result.Errors);
            }
            else if (roleEntity.AccessLevel > accessLevel)
                return NotFound();

            var userRoles = await GetRolesAsync();

            return userRoles.Contains(role.Name) ? Forbid() : NotFound();

        } else if (role.AccessLevel >= accessLevel || role.AccessLevel < 1)
            return BadRequest();

        roleEntity.Name = role.Name;
        roleEntity.AccessLevel = role.AccessLevel;

        result = await _roleManager.UpdateAsync(roleEntity);

        return result.Succeeded ? Ok() : BadRequest(result.Errors);
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/remove")]
    [HttpDelete]
    [Route("remove")]
    public async Task<ActionResult> RemoveAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        int accessLevel = await GetAccessLevelAsync();

        var role = await _roleManager.Roles.FirstOrDefaultAsync(role => role.Id == id);

        if (role == null)
            return NotFound();

        var max = AccessLevels.Max();

        if (role.AccessLevel == max)
            return accessLevel == max ? BadRequest() : NotFound();

        if (role.AccessLevel > accessLevel)
            return NotFound();

        if (role.AccessLevel == accessLevel)
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = (await _userManager.GetRolesAsync(user)).ToHashSet();

            return userRoles.Contains(role.Name) ? Forbid() : NotFound();
        }

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

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
