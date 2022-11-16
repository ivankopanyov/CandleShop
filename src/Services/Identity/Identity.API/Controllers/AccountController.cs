using Microsoft.AspNetCore.Authorization;

namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/identity/account")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenCreationService _jwtService;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager, ITokenCreationService jwtService)
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

        if (await _userManager.Users.CountAsync() == 1)
        {
            var admin = Constants.Roles.SUPERVISOR;

            if (await _roleManager.FindByNameAsync(admin) == null)
                await _roleManager.CreateAsync(new IdentityRole(admin));

            await _userManager.AddToRolesAsync(user, new string[] { admin });
        }

        var roles = _userManager.GetRolesAsync(user).Result;
        var token = _jwtService.CreateToken(user, roles);

        return Ok(token);
    }

    [HttpPost]
    [Route("signin")]
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

        var roles = _userManager.GetRolesAsync(user).Result;
        var token = _jwtService.CreateToken(user, roles);

        return Ok(token);
    }

    #endregion

    #region GET

    [Authorize(AuthenticationSchemes = "Bearer", Roles = Constants.Roles.SUPERVISOR)]
    [HttpGet]
    [Route("all")]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _userManager.Users.ToListAsync();
    }

    #endregion
}