using Microsoft.AspNetCore.Authorization;

namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/identity/roles")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly UserManager<User> _userManager;

    public RolesController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    [Authorize(AuthenticationSchemes = "Bearer", Roles = Constants.Roles.SUPERVISOR)]
    [HttpPost]
    [Route("add")]
    public async Task<IActionResult> CreateAsync([FromBody] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest();

        IdentityResult result = await _roleManager.CreateAsync(new IdentityRole(name.Trim()));

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

    [HttpGet]
    [Route("all")]
    public async Task<IEnumerable<IdentityRole>> GetAllAsync()
    {
        return await _roleManager.Roles.ToArrayAsync();
    }

    [HttpGet]
    [Route("get")]
    public async Task<ActionResult<IList<IdentityRole>>> GetAsync(string email)
    {
        var user = await _userManager.FindByNameAsync(email);

        if (user == null)
            return BadRequest();

        return Ok(await _userManager.GetRolesAsync(user));
    }

    [Authorize(AuthenticationSchemes = "Bearer", Roles = Constants.Roles.SUPERVISOR)]
    [HttpPut]
    [Route("addrole")]
    public async Task<IActionResult> AddRole(string userId, string roleId)
    {
        User user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound();

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            return NotFound();

        await _userManager.AddToRolesAsync(user, new string[] { role.Name });

        return Ok();
    }
}
