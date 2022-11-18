using Identity.API.Model.Entities;

namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly RoleManager<Role> _roleManager;

    private readonly UserManager<User> _userManager;

    private readonly IConfiguration _configuration;

    private int? AccessLevel
    {
        get
        {
            var roleClaim = User.FindFirst(c => c.Type == ClaimTypes.Role && c.Issuer == _configuration["Jwt:Issuer"]);

            if (roleClaim == null || !int.TryParse(roleClaim.Value, out int accessLevel))
                return null;

            return accessLevel;
        }
    }

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
        int? access = AccessLevel;
        if (access == null)
            return BadRequest();

        int userAccessLevel = (int)access;

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest();

        if (accessLevel <= AccessLevels.Min())
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
        int? access = AccessLevel;
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        IQueryable<Role> roles;

        if (accessLevel == AccessLevels.Max())
            roles = _roleManager.Roles;
        else
        {
            var user = await _userManager.GetUserAsync(User);
            var userRoles = (await _userManager.GetRolesAsync(user)).ToHashSet();
            roles = _roleManager.Roles.Where(role => role.AccessLevel < accessLevel || userRoles.Contains(role.Name));
        }

        return Ok(roles);
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/get")]
    [HttpGet]
    [Route("get")]
    public async Task<ActionResult<IEnumerable<Role>>> GetAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        int? access = AccessLevel;
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        var role = await _roleManager.Roles.FirstAsync(role => role.Id == id);

        if (role == null || accessLevel < role.AccessLevel)
            return NotFound();

        if (accessLevel == AccessLevels.Max() || accessLevel > role.AccessLevel)
            return Ok(role);
        
        var user = await _userManager.GetUserAsync(User);
        var userRoles = (await _userManager.GetRolesAsync(user)).ToHashSet();

        return userRoles.Contains(role.Name) ? Ok(role) : NotFound();
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/findByName")]
    [HttpGet]
    [Route("findByName")]
    public async Task<ActionResult<IEnumerable<Role>>> FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest();

        int? access = AccessLevel;
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        if (accessLevel == AccessLevels.Max())
            return Ok(await _roleManager.Roles.Where(role => role.Name.Contains(name)).ToArrayAsync());

        var user = await _userManager.GetUserAsync(User);
        var userRoles = (await _userManager.GetRolesAsync(user)).ToHashSet();

        return Ok(_roleManager.Roles.Where(role => role.Name.Contains(name) && (role.AccessLevel < accessLevel || userRoles.Contains(role.Name))));
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/getInRange")]
    [HttpGet]
    [Route("getInRange")]
    public async Task<ActionResult<IEnumerable<Role>>> GetInRange(int minAccessLevel, int maxAccessLevel)
    {
        int? access = AccessLevel;
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        int min = AccessLevels.Min();
        if (minAccessLevel < min)
            minAccessLevel = min;

        int max = AccessLevels.Max();
        if (maxAccessLevel >= accessLevel)
            maxAccessLevel = accessLevel == max ? accessLevel : accessLevel - 1;

        if (accessLevel == max)
            return Ok(_roleManager.Roles.Where(role => role.AccessLevel >= minAccessLevel && role.AccessLevel <= maxAccessLevel));

        var user = await _userManager.GetUserAsync(User);
        var userRoles = (await _userManager.GetRolesAsync(user)).ToHashSet();

        return Ok(_roleManager.Roles.Where(role => (role.AccessLevel >= minAccessLevel && role.AccessLevel <= maxAccessLevel) || userRoles.Contains(role.Name)));
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/change")]
    [HttpPut]
    [Route("change")]
    public async Task<ActionResult> ChangeAsync(RoleDto role)
    {
        if (string.IsNullOrWhiteSpace(role.Id) || string.IsNullOrWhiteSpace(role.Name))
            return BadRequest();

        int? access = AccessLevel;
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        Role roleEntity = await _roleManager.Roles.FirstAsync(r => r.Id == role.Id);

        if (roleEntity == null)
            return NotFound();

        int min = AccessLevels.Min();
        int max = AccessLevels.Max();
        IdentityResult renameResult;

        if (roleEntity.AccessLevel == min)
        {
            if (roleEntity.AccessLevel != role.AccessLevel)
                return BadRequest();

            if (roleEntity.Name != role.Name && accessLevel != max)
                return Forbid();

            renameResult = await _roleManager.SetRoleNameAsync(roleEntity, role.Name);

            return renameResult.Succeeded ? Ok() : BadRequest(renameResult.Errors);
        }
        else if (roleEntity.AccessLevel >= accessLevel)
        {
            if (accessLevel == max)
            {
                if (roleEntity.AccessLevel == max)
                {
                    int secondMax = min;
                    foreach (var level in AccessLevels)
                        if ((secondMax == min || level > secondMax) && level < max)
                            secondMax = level;

                    if (role.AccessLevel <= secondMax)
                        return BadRequest();

                    renameResult = await _roleManager.SetRoleNameAsync(roleEntity, role.Name);

                    if (!renameResult.Succeeded)
                        return BadRequest(renameResult.Errors);

                    roleEntity.AccessLevel = role.AccessLevel;
                    return Ok();
                }

                if (role.AccessLevel >= max || role.AccessLevel <= min)
                    return BadRequest();

                renameResult = await _roleManager.SetRoleNameAsync(roleEntity, role.Name);

                if (!renameResult.Succeeded)
                    return BadRequest(renameResult.Errors);

                roleEntity.AccessLevel = role.AccessLevel;
                return Ok();
            }
            else if (roleEntity.AccessLevel > accessLevel)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var userRoles = (await _userManager.GetRolesAsync(user)).ToHashSet();

            return userRoles.Contains(role.Name) ? Forbid() : NotFound();
        }

        if (role.AccessLevel >= accessLevel || role.AccessLevel <= min)
            return BadRequest();

        renameResult = await _roleManager.SetRoleNameAsync(roleEntity, role.Name);

        if (!renameResult.Succeeded)
            return BadRequest(renameResult.Errors);

        roleEntity.AccessLevel = role.AccessLevel;
        return Ok();
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/remove")]
    [HttpDelete]
    [Route("remove")]
    public async Task<ActionResult> RemoveAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        int? access = AccessLevel;
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        var role = await _roleManager.Roles.FirstAsync(role => role.Id == id && role.AccessLevel < accessLevel);

        if (role == null)
            return NotFound();

        if (role.AccessLevel == AccessLevels.Min())
            return BadRequest();

        await _roleManager.DeleteAsync(role);

        return Ok();
    }
}
