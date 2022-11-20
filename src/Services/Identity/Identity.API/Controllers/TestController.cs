namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class TestController : IdentityController
{
    private readonly IdentityContext _identityContext;

    public TestController(UserManager<User> userManager, RoleManager<Role> roleManager, IConfiguration configuration, IdentityContext identityContext) : 
        base(userManager, roleManager, configuration)
    {
        _identityContext = identityContext;
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "test/allUsers")]
    [HttpGet]
    [Route("allUsers")]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {
        return await _userManager.Users.ToArrayAsync();
    }

    [HttpGet]
    [Route("users")]
    public async Task<ActionResult<List<User>>> GetUsers(int pageSize, int pageIndex)
    {
        if (pageSize <= 0 || pageIndex < 0)
            return new List<User>();

        return await _userManager.Users
            .OrderBy(x => x.Id)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();
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
