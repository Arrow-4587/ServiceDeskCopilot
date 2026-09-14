using System.ComponentModel.DataAnnotations;

namespace ServiceDesk.Web.Models.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
