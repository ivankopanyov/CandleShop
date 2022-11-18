namespace CandleShop.Services.Identity.API.Model.Entities;

public class Role : IdentityRole
{
    public int AccessLevel { get; set; }

    public Role() : base() { }

    public Role(string roleName, int rank) : base(roleName) => AccessLevel = rank;
}
