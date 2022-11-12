using Microsoft.Extensions.Configuration;

namespace CandleShop.Services.Identity.API.Infrastructure;

public class IdentityContext : IdentityDbContext<User>
{
    private readonly IConfiguration _configuration;

    public IdentityContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite(_configuration.GetConnectionString("WebApiDatabase"));
    }
}