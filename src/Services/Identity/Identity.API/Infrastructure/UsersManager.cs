using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CandleShop.Services.Identity.API.Infrastructure;

public class UsersManager : UserManager<User>
{
    private RoleManager<Role> _roleManager;

    private AuthorizationHandlerContext _context;

    public UsersManager(IUserStore<User> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<User> passwordHasher, IEnumerable<IUserValidator<User>> userValidators, IEnumerable<IPasswordValidator<User>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<User>> logger, RoleManager<Role> roleManager, AuthorizationHandlerContext context) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
        _roleManager = roleManager;
        _context = context;
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var idClaim = _context.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier);

        if (idClaim == null)
            return null;

        return await Users.FirstOrDefaultAsync(user => user.Id == idClaim.Value);
    }

    private async Task<IList<string>> GetRolesAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Array.Empty<string>();

        return await GetRolesAsync(user);
    }

    private async Task<int> GetAccessLevelAsync()
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
