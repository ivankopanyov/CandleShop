namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ITokenCreationService _jwtService;
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

    public UsersController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<Role> roleManager,
        ITokenCreationService jwtService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _configuration = configuration;
    }
}
