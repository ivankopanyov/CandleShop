namespace CandleShop.Services.Identity.API.Model;

public class Policy
{
    [Key]
    public string Name { get; set; }

    public int MinimumAccessLevel { get; set; }
}
