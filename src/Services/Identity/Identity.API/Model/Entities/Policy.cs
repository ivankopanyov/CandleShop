namespace CandleShop.Services.Identity.API.Model.Entities;

public class Policy
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }

    public int MinimumAccessLevel { get; set; }
}
