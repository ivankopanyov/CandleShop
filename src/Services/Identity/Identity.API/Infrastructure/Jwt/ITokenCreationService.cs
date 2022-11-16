namespace CandleShop.Services.Identity.API.Infrastructure.Jwt;

public interface ITokenCreationService
{
    AuthenticateResponse CreateToken(IdentityUser user, int rank);
}
