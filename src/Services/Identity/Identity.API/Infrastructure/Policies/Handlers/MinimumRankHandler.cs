namespace CandleShop.Services.Identity.API.Infrastructure.Policies.Handlers;

public class MinimumRankHandler : AuthorizationHandler<MinimumRankRequirement>
{
    private readonly IConfiguration _configuration;

    public MinimumRankHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumRankRequirement requirement)
    {
        var roleClaim = context.User.FindFirst(c => c.Type == ClaimTypes.Role && c.Issuer == _configuration["Jwt:Issuer"]);

        if (roleClaim == null || !int.TryParse(roleClaim.Value, out int rank))
            return Task.CompletedTask;

        if (rank >= requirement.MinimumRank)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }

    public static int? GetRank(Claim? roleClaim)
    {
        if (roleClaim == null || !int.TryParse(roleClaim.Value, out int rank))
            return null;

        return rank;
    }
}