using Identity.API.Model.Entities;

namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class AccountController : ControllerBase
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

    public AccountController(
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

    #region POST

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register(RegisterViewModel registerModel)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new User()
        {
            UserName = registerModel.Email,
            Email = registerModel.Email
        };

        var result = await _userManager.CreateAsync(user, registerModel.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var accessLevel = 0;
        string roleName = null!;
        foreach (var role in _roleManager.Roles)
            if (roleName == null || role.AccessLevel < accessLevel)
            {
                accessLevel = role.AccessLevel;
                roleName = role.Name;
            }

        await _userManager.AddToRolesAsync(user, new string[] { roleName });
        var token = await GetTokenAsync(user);

        return Ok(token);
    }

    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<AuthenticateResponse>> SignIn(AuthenticateRequest request)
    {

        if (!ModelState.IsValid)
            return BadRequest();

        var user = await _userManager.FindByNameAsync(request.Email);

        if (user == null)
            return BadRequest();

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
            return BadRequest();

        var token = await GetTokenAsync(user);

        return Ok(token);
    }

    #endregion

    #region GET

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "account/all")]
    [HttpGet]
    [Route("all")]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _userManager.Users.ToArrayAsync();
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "account/accessLevel")]
    [HttpGet]
    [Route("accessLevel")]
    public int? GetAccessLevel() => AccessLevel;

    #endregion

    private async Task<AuthenticateResponse> GetTokenAsync(User user)
    {
        var rolesNamesList = await _userManager.GetRolesAsync(user);
        var rolesNames = rolesNamesList.ToHashSet();
        var rank = _roleManager.Roles
            .Where(role => rolesNames.Contains(role.Name))
            .ToArray()
            .Select(role => role.AccessLevel)
            .Max();

        return _jwtService.CreateToken(user, rank);
    }
}