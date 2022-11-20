namespace CandleShop.Services.Identity.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class UsersController : IdentityController
{
    public UsersController(UserManager<User> userManager, RoleManager<Role> roleManager, IConfiguration configuration) : 
        base(userManager, roleManager, configuration) { }
}
