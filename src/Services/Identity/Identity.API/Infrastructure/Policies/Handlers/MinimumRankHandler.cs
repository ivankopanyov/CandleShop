using SQLitePCL;

namespace CandleShop.Services.Identity.API.Infrastructure.Policies.Handlers;

public class MinimumRankHandler : AuthorizationHandler<MinimumRankRequirement>
{
    private readonly IConfiguration _configuration;

    private readonly IdentityContext _context;

    public MinimumRankHandler(IConfiguration configuration, IdentityContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumRankRequirement requirement)
    {
        var roleClaim = context.User.FindFirst(c => c.Type == ClaimTypes.Role && c.Issuer == _configuration["Jwt:Issuer"]);

        if (roleClaim == null || !int.TryParse(roleClaim.Value, out int rank))
            return Task.CompletedTask;

        var policy = _context.Policies.FirstOrDefault(p => p.Name == requirement.PolicyName);
        if (policy == null)
            return Task.CompletedTask;

        if (rank >= policy.MinimumRank)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}