namespace CandleShop.Services.Identity.API.Controllers;

public class IdentityController : ControllerBase
{
    protected readonly UserManager<User> _userManager;
    protected readonly RoleManager<Role> _roleManager;
    protected readonly IConfiguration _configuration;

    protected HashSet<int> AccessLevels => _roleManager.Roles.Select(role => role.AccessLevel).ToHashSet();

    protected int AccessLevelMin => AccessLevels.Min();

    protected int AccessLevelMax => AccessLevels.Max();

    public IdentityController(UserManager<User> userManager, RoleManager<Role> roleManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    protected async Task<User?> GetCurrentUserAsync()
    {
        var idClaim = User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier);

        if (idClaim == null)
            return null;

        return await _userManager.Users.FirstOrDefaultAsync(user => user.Id == idClaim.Value);
    }

    protected async Task<IList<string>> GetRolesAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Array.Empty<string>();

        return await _userManager.GetRolesAsync(user);
    }

    protected async Task<int> GetAccessLevelAsync()
    {
        HashSet<string> rolesNames = (await GetRolesAsync()).ToHashSet();
        HashSet<int> accessLevels = new HashSet<int>() { 0 };
        await _roleManager.Roles.ForEachAsync(role => {
            if (rolesNames.Contains(role.Name))
                accessLevels.Add(role.AccessLevel);
        });
        return accessLevels.Max();
    }
}
