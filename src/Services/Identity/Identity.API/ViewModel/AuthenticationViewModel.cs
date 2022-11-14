namespace CandleShop.Services.Identity.API.ViewModel;

public class AuthenticationViewModel
{
    [Required]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }
}
