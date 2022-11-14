namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/identity/account")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITokenCreationService _jwtService;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, ITokenCreationService jwtService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
    }

    #region POST

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register(RegisterViewModel user)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var result = await _userManager.CreateAsync(
            new User()
            {
                UserName = user.Email, 
                Email = user.Email 
            },
            user.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        user.Password = null;
        return Created("", user);
    }

    [HttpPost]
    [Route("signin")]
    public async Task<ActionResult<AuthenticationResponse>> SignIn(AuthenticationViewModel request)
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

    [HttpGet]
    [Route("all")]
    public async Task<ActionResult<List<User>>> GetUser()
    {
        var users = await _userManager.Users.ToListAsync();

        return users;
    }

    #endregion
}