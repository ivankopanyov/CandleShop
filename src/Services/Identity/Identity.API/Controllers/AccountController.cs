namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ITokenCreationService _jwtService;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<Role> roleManager, ITokenCreationService jwtService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
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

        string administrator = "Administrator";

        if (await _userManager.Users.CountAsync() == 1)
        {
            if (await _roleManager.FindByNameAsync(administrator) == null)
                await _roleManager.CreateAsync(new Role(administrator, 10));

            await _userManager.AddToRolesAsync(user, new string[] { administrator });
        }
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

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "Rank9")]
    [HttpGet]
    [Route("all")]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _userManager.Users.ToListAsync();
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