using System.ComponentModel.DataAnnotations;

namespace CleanDistrict.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите email"), EmailAddress] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Введите пароль"), DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class CreateUserViewModel
{
    [Required, Display(Name = "ФИО")] public string FullName { get; set; } = "";
    [Required, EmailAddress, Display(Name = "Email")] public string Email { get; set; } = "";
    [Required, MinLength(8), DataType(DataType.Password), Display(Name = "Пароль")] public string Password { get; set; } = "";
    [Required, Display(Name = "Роль")] public string Role { get; set; } = "";
}

public class UserListItemViewModel
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Roles { get; set; } = "";
    public bool IsLockedOut { get; set; }
}
