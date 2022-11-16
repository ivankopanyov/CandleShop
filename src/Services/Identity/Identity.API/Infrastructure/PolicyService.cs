using Microsoft.AspNetCore.Authorization;

namespace Identity.API.Infrastructure;

public class PolicyService
{
    private readonly AuthorizationOptions _options;

    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly string[] _globalRoles;

    private readonly string[] _roles;

    private readonly string[] _departments;

    public PolicyService(RoleManager<IdentityRole> roleManager, AuthorizationOptions options)
    {
        _options = options;
        _roleManager = roleManager;

        _globalRoles = new[]
        {
            Constants.Roles.SUPERVISOR,
            Constants.Roles.ADMINISTRATOR
        };

        _roles = new[]
        {
            Constants.Roles.SENIOR_ADMINISTRATOR,
            Constants.Roles.ADMINISTRATOR
        };

        _departments = new[]
        {
            Constants.Departments.CATALOG
        };
    }

    public PolicyService CreateRoles()
    {
        foreach (var role in _globalRoles)
            _roleManager.CreateAsync(new IdentityRole(role));

        foreach (var role in _roles)
            foreach (var department in _departments)
                _roleManager.CreateAsync(new IdentityRole($"{department} {role}"));

        return this;
    }

    public PolicyService ApplyPolicy()
    {
        var globalRolesList = "";

        for (int i = 0; i < _globalRoles.Length; i++)
        {
            var separator = i > 0 ? ", " : "";
            globalRolesList += $"{separator}{_globalRoles[i]}";
            _options.AddPolicy(_globalRoles[i], policy => policy.RequireRole(globalRolesList));
        }

        foreach (var role in _roles)
        {
            var rolesList = globalRolesList;
            foreach (var department in _departments)
            {
                var roleName = $"{department} {role}";
                rolesList += $", {roleName}";
                _options.AddPolicy(roleName, policy => policy.RequireRole(rolesList));
            }
        }

        return this;
    }
}
