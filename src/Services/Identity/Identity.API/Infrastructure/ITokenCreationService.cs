namespace CandleShop.Services.Identity.API.Infrastructure;

public interface ITokenCreationService
{
    AuthenticateResponse CreateToken(IdentityUser user);
}
