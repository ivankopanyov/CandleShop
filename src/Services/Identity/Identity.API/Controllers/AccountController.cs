using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Net;

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

        var token = _jwtService.CreateToken(user);

        return Ok(token);
    }

    #endregion

    #region GET


    [Authorize(AuthenticationSchemes = "Bearer", Policy = "RequireAdministratorRole")]
    [HttpGet]
    [Route("all")]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _userManager.Users.ToListAsync();
    }

    #endregion
}