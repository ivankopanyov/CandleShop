namespace CandleShop.Services.Identity.API.Infrastructure.Policies.Requirements;

public class MinimumRankRequirement : IAuthorizationRequirement
{
    public string PolicyName { get; init; }

    public MinimumRankRequirement(string policyName) => PolicyName = policyName;
}
