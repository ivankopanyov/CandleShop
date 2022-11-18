namespace CandleShop.Services.Identity.API.Model.Entities;

public class Policy
{
    [Key]
    public string Name { get; set; }

    public int MinimumAccessLevel { get; set; }
}
