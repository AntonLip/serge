using System.ComponentModel.DataAnnotations;

namespace CleanDistrict.Web.Models;

public class MaintenanceInputModel
{
    [Required] public Guid VehicleId { get; set; }
    [Required, MaxLength(300), Display(Name = "Вид работ")] public string Description { get; set; } = "";
    [Display(Name = "Плановая дата")] public DateOnly PlannedDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(7));
    [Range(0, 100000000), Display(Name = "Плановая стоимость, ₽")] public decimal Cost { get; set; }
}

public class RepairInputModel
{
    [Required] public Guid VehicleId { get; set; }
    [Required, MaxLength(2000), Display(Name = "Неисправность и работы")] public string Description { get; set; } = "";
    [Range(0, 100000000), Display(Name = "Предварительная стоимость, ₽")] public decimal Cost { get; set; }
    [MaxLength(2000), Display(Name = "Запчасти")] public string? Parts { get; set; }
}
