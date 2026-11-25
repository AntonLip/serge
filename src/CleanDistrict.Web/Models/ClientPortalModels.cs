using System.ComponentModel.DataAnnotations;

namespace CleanDistrict.Web.Models;

public class CreateClientUserViewModel
{
    [Required(ErrorMessage = "Выберите клиента"), Display(Name = "Организация")]
    public Guid ClientId { get; set; }

    [Required(ErrorMessage = "Введите ФИО"), MaxLength(150), Display(Name = "ФИО")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Введите email"), EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Введите пароль"), MinLength(8), DataType(DataType.Password), Display(Name = "Пароль")]
    public string Password { get; set; } = "";
}

public class ClientUserListItemViewModel
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string ClientName { get; set; } = "";
    public bool IsLockedOut { get; set; }
}

public class ClientRequestInputModel
{
    [Required(ErrorMessage = "Выберите объект"), Display(Name = "Объект")]
    public Guid ObjectId { get; set; }

    [Required(ErrorMessage = "Укажите нужную услугу"), MaxLength(200), Display(Name = "Нужная услуга")]
    public string RequestedService { get; set; } = "";

    [Required(ErrorMessage = "Опишите работы"), MaxLength(3000), Display(Name = "Описание работ")]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Укажите желаемый срок"), Display(Name = "Желаемый срок")]
    public DateTime DesiredDateLocal { get; set; } = DateTime.Now.AddDays(1);

    [Required(ErrorMessage = "Укажите контактное лицо"), MaxLength(150), Display(Name = "Контактное лицо")]
    public string ContactName { get; set; } = "";

    [Required(ErrorMessage = "Укажите телефон"), MaxLength(40), Phone, Display(Name = "Контактный телефон")]
    public string ContactPhone { get; set; } = "";
}

public class ProcessClientRequestInputModel
{
    public Guid RequestId { get; set; }

    [Required(ErrorMessage = "Выберите тариф"), Display(Name = "Услуга / тариф")]
    public Guid TariffId { get; set; }

    [Range(0, 1000000000, ErrorMessage = "Стоимость должна быть от 0 до 1 000 000 000 ₽"), Display(Name = "Согласованная стоимость, ₽")]
    public decimal EstimatedCost { get; set; }
}
