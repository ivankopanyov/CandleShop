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

    private int? Rank
    {
        get
        {
            var roleClaim = User.FindFirst(c => c.Type == ClaimTypes.Role && c.Issuer == _configuration["Jwt:Issuer"]);

            if (roleClaim == null || !int.TryParse(roleClaim.Value, out int rank))
                return null;

            return rank;
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

        var role = await _roleManager.Roles.FirstAsync(role => role.Rank == RoleSettings.MIN_RANK);
        if (role == null)
        {
            role = new Role($"{RoleSettings.DEFAULT_ROLE_NAME} with rank {RoleSettings.MIN_RANK}", RoleSettings.MIN_RANK);
            await _roleManager.CreateAsync(role);
        }

        await _userManager.AddToRolesAsync(user, new string[] { role.Name });
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

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "Rank2")]
    [HttpGet]
    [Route("all")]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _userManager.Users.ToArrayAsync();
    }

    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpGet]
    [Route("rank")]
    public int? GetRank()
    {
        var roleClaim = User.FindFirst(c => c.Type == ClaimTypes.Role && c.Issuer == _configuration["Jwt:Issuer"]);
        return roleClaim == null || !int.TryParse(roleClaim.Value, out int rank) ? null : rank;
    }

    #endregion

    private async Task<AuthenticateResponse> GetTokenAsync(User user)
    {
        var rolesNamesList = await _userManager.GetRolesAsync(user);
        var rolesNames = rolesNamesList.ToHashSet();
        var rank = _roleManager.Roles
            .Where(role => rolesNames.Contains(role.Name))
            .ToArray()
            .Select(role => role.Rank)
            .Max();

        return _jwtService.CreateToken(user, rank);
    }
}