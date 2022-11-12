using CandleShop.Services.Identity.API.Model;

namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/identity/account")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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