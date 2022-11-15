namespace CandleShop.Services.Identity.API.Controllers;

public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly UserManager<User> _userManager;

    public RolesController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest();

        if (await _roleManager.FindByNameAsync(name) != null)
            return BadRequest();

        IdentityResult result = await _roleManager.CreateAsync(new IdentityRole(name.Trim()));

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(string id)
    {
        IdentityRole role = await _roleManager.FindByIdAsync(id);

        if (role != null)
            return NotFound();

        IdentityResult result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

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
