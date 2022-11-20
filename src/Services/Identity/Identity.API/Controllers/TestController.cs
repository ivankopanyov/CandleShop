namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class TestController : IdentityController
{
    public TestController(UserManager<User> userManager, RoleManager<Role> roleManager, IConfiguration configuration) : 
        base(userManager, roleManager, configuration) { }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "test/allUsers")]
    [HttpGet]
    [Route("allUsers")]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {
        return await _userManager.Users.ToArrayAsync();
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "test/allClaims")]
    [HttpGet]
    [Route("allClaims")]
    public List<string> GetAllClaims()
    {
        List<string> result = new List<string>();
        foreach (var c in User.Claims)
            result.Add($"{c.Type} - {c.Value}");

        return result;
    }
}
