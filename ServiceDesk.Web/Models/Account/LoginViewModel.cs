using System.ComponentModel.DataAnnotations;

namespace ServiceDesk.Web.Models.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
