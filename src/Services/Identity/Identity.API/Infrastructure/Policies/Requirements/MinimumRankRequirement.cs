namespace CandleShop.Services.Identity.API.Infrastructure.Policies.Requirements;

public class MinimumRankRequirement : IAuthorizationRequirement
{
    public int MinimumRank { get; init; }

    public MinimumRankRequirement(int minimumRank) => MinimumRank = minimumRank;
}
