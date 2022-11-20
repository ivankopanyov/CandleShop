using Microsoft.AspNetCore.Identity;

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

        var token = _jwtService.CreateToken(user);

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

        var token = _jwtService.CreateToken(user);

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

    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpGet]
    [Route("claims")]
    public List<string> GetClaims () 
    {
        List<string> result = new List<string>();
        foreach (var c in User.Claims)
            result.Add($"{c.Type} - {c.Value}");

        return result;
    }

    #endregion
}