namespace CandleShop.Services.Identity.API.Model;

public class Role : IdentityRole
{
    public int Rank { get; set; }

    public Role() : base() { }

    public Role(string roleName, int rank) : base(roleName) => Rank = rank;
}
