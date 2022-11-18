using System.Collections.Immutable;

namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly RoleManager<Role> _roleManager;

    private readonly UserManager<User> _userManager;

    private readonly IConfiguration _configuration;

    private HashSet<int> AccessLevels => _roleManager.Roles.Select(role => role.AccessLevel).ToHashSet();

    public RolesController(RoleManager<Role> roleManager, UserManager<User> userManager, IConfiguration configuration)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/create")]
    [HttpPost]
    [Route("add")]
    public async Task<IActionResult> CreateAsync(string name, int accessLevel)
    {
        int? access = await GetAccessLevelAsync();
        if (access == null)
            return BadRequest();

        int userAccessLevel = (int)access;

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest();

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
    public async Task<ActionResult<IEnumerable<Role>>> GetAll()
    {
        int? access = await GetAccessLevelAsync();
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        IEnumerable<Role> roles = Array.Empty<Role>();

        if (accessLevel == AccessLevels.Max())
            roles = _roleManager.Roles;
        else
        {
            var userRoles = await GetRolesAsync();
            roles = _roleManager.Roles.Where(role => role.AccessLevel < accessLevel || userRoles.Contains(role.Name));
        }

        return Ok(roles);
    }



    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpGet]
    [Route("getClaim")]
    public string GetClaim()
    {
        var idClaim = User.FindFirst(c => c.Type == ClaimTypes.Sid && c.Issuer == _configuration["Jwt:Issuer"]);
        return idClaim.Value;
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/get")]
    [HttpGet]
    [Route("get")]
    public async Task<ActionResult<IEnumerable<Role>>> GetAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        int? access = await GetAccessLevelAsync();
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

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
    public async Task<ActionResult<IEnumerable<Role>>> FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest();

        int? access = await GetAccessLevelAsync();
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        if (accessLevel == AccessLevels.Max())
            return Ok(await _roleManager.Roles.Where(role => role.NormalizedName.Contains(name.Trim().ToUpper())).ToArrayAsync());

        var userRoles = await GetRolesAsync();

        return Ok(_roleManager.Roles.Where(role => role.Name.Contains(name) && (role.AccessLevel < accessLevel || userRoles.Contains(role.Name))));
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/getInRange")]
    [HttpGet]
    [Route("getInRange")]
    public async Task<ActionResult<IEnumerable<Role>>> GetInRange(int minAccessLevel, int maxAccessLevel)
    {
        int? access = await GetAccessLevelAsync();
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        int min = AccessLevels.Min();
        if (minAccessLevel < 1)
            minAccessLevel = 1;

        int max = AccessLevels.Max();
        if (maxAccessLevel >= accessLevel)
            maxAccessLevel = accessLevel == max ? accessLevel : accessLevel - 1;

        if (accessLevel == max)
            return Ok(_roleManager.Roles.Where(role => role.AccessLevel >= minAccessLevel && role.AccessLevel <= maxAccessLevel));

        var userRoles = await GetRolesAsync();

        return Ok(_roleManager.Roles.Where(role => (role.AccessLevel >= minAccessLevel && role.AccessLevel <= maxAccessLevel) || userRoles.Contains(role.Name)));
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/change")]
    [HttpPut]
    [Route("change")]
    public async Task<ActionResult> ChangeAsync(RoleDto role)
    {
        if (string.IsNullOrWhiteSpace(role.Id) || string.IsNullOrWhiteSpace(role.Name) || role.AccessLevel < 1)
            return BadRequest();

        int? access = await GetAccessLevelAsync();
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        Role roleEntity = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == role.Id);

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

        int? access = await GetAccessLevelAsync();
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

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
        var idClaim = User.FindFirst(c => c.Type == ClaimTypes.Sid && c.Issuer == _configuration["Jwt:Issuer"]);

        if (idClaim == null)
            return null;

        return await _userManager.Users.FirstOrDefaultAsync(user => user.Id == idClaim.Value);
    }

    private async Task<string[]> GetRolesAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Array.Empty<string>();

        return (await _userManager.GetRolesAsync(user)).ToArray();
    }

    private async Task<int?> GetAccessLevelAsync()
    {
        int? max = null;
        var rolesNames = await GetRolesAsync();

        foreach (var roleName in rolesNames)
        {
            var role = await _roleManager.Roles.FirstOrDefaultAsync(role => role.Name == roleName);
            if (role != null && (max == null || role.AccessLevel > max))
                max = role.AccessLevel;
        }

        return max;
    }
}
