namespace CandleShop.Services.Identity.API.Infrastructure;

public interface ITokenCreationService
{
    AuthenticationResponse CreateToken(IdentityUser user);
}
