namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class RolesController : IdentityController
{
    public RolesController(UserManager<User> userManager, RoleManager<Role> roleManager, IConfiguration configuration) : 
        base(userManager, roleManager, configuration) { }

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
    public async Task<List<Role>> GetAll() =>  await _roleManager.Roles.ToListAsync();

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/get")]
    [HttpGet]
    [Route("get")]
    public async Task<ActionResult<Role>> GetAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var role = await _roleManager.Roles.FirstOrDefaultAsync(role => role.Id == id);
        return role == null ? NotFound() : Ok(role);
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/findByName")]
    [HttpGet]
    [Route("findByName")]
    public async Task<List<Role>> FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new List<Role>();

        return await _roleManager.Roles.Where(role => role.NormalizedName.Contains(name.Trim().ToUpper())).ToListAsync();
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "roles/getInRange")]
    [HttpGet]
    [Route("getInRange")]
    public async Task<List<Role>> GetInRange(int minAccessLevel, int maxAccessLevel) =>
        await _roleManager.Roles.Where(role => role.AccessLevel >= minAccessLevel && role.AccessLevel <= maxAccessLevel).ToListAsync();

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

        int max = AccessLevelMax;
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

        var max = AccessLevelMax;

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
}
