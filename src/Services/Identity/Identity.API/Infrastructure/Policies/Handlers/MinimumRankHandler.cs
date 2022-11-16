namespace CandleShop.Services.Identity.API.Infrastructure.Policies.Handlers;

public class MinimumRankHandler : AuthorizationHandler<MinimumRankRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumRankRequirement requirement)
    {
        var roleClaim = context.User.FindFirst(c => c.Type == ClaimTypes.Role && c.Issuer == "vehiclequotes.endpointdev.com");

        if (roleClaim == null || !int.TryParse(roleClaim.Value, out int rank))
            return Task.CompletedTask;

        if (rank >= requirement.MinimumRank)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}