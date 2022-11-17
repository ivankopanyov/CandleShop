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
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest();

        if (accessLevel <= AccessLevels.Min())
            return BadRequest();

        if (accessLevel >= AccessLevel)
            return Forbid();

        IdentityResult result = await _roleManager.CreateAsync(new Role(name.Trim(), accessLevel));

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/getAll")]
    [HttpGet]
    [Route("getAll")]
    public ActionResult<IEnumerable<Role>> GetAll()
    {
        int? access = AccessLevel;
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        var roles = accessLevel == AccessLevels.Max() ?
            _roleManager.Roles.Where(role => role.AccessLevel <= accessLevel) :
            _roleManager.Roles.Where(role => role.AccessLevel < accessLevel);

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

        var role = accessLevel == AccessLevels.Max() ?
            await _roleManager.Roles.FirstAsync(role => role.Id == id && role.AccessLevel <= accessLevel) :
            await _roleManager.Roles.FirstAsync(role => role.Id == id && role.AccessLevel < accessLevel);

        if (role == null)
            return NotFound();

        return Ok(role);
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/findByName")]
    [HttpGet]
    [Route("findByName")]
    public ActionResult<IEnumerable<Role>> FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest();

        int? access = AccessLevel;
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        var roles = accessLevel == AccessLevels.Max() ?
            _roleManager.Roles.Where(role => role.Name.Contains(name) && role.AccessLevel <= accessLevel) :
            _roleManager.Roles.Where(role => role.Name.Contains(name) && role.AccessLevel < accessLevel);

        return Ok(roles);
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/getInRange")]
    [HttpGet]
    [Route("getInRange")]
    public ActionResult<IEnumerable<Role>> GetInRange(int min, int max)
    {
        int? access = AccessLevel;
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        var roles = accessLevel == AccessLevels.Max() ?
            _roleManager.Roles.Where(role => role.AccessLevel >= min && role.AccessLevel <= accessLevel && role.AccessLevel <= max) :
            _roleManager.Roles.Where(role => role.AccessLevel >= min && role.AccessLevel < accessLevel && role.AccessLevel <= max);

        return Ok(roles);
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/change")]
    [HttpPut]
    [Route("change")]
    public async Task<ActionResult> ChangeAsync(string id, string newName, int newAccessLevel)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(newName))
            return BadRequest();

        int? access = AccessLevel;
        if (access == null)
            return BadRequest();

        int accessLevel = (int)access;

        if (newAccessLevel <= AccessLevels.Min())
            return BadRequest();

        if (newAccessLevel >= AccessLevel)
            return Forbid();

        var role = accessLevel == AccessLevels.Max() ?
            await _roleManager.Roles.FirstAsync(role => role.Id == id && role.AccessLevel <= accessLevel) :
            await _roleManager.Roles.FirstAsync(role => role.Id == id && role.AccessLevel < accessLevel);

        if (role == null)
            return NotFound();

        int minAccessLevel = AccessLevels.Min();

        if (role.AccessLevel == minAccessLevel && newAccessLevel != minAccessLevel)
            return BadRequest();

        if (role.Name != newName)
            await _roleManager.SetRoleNameAsync(role, newName);

        role.AccessLevel = newAccessLevel;

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
