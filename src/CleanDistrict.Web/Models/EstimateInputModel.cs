using System.ComponentModel.DataAnnotations;

namespace CleanDistrict.Web.Models;

public class EstimateInputModel
{
    [Required, Display(Name = "Объект")] public Guid ObjectId { get; set; }
    [Required, Display(Name = "Тариф")] public Guid TariffId { get; set; }
    [Range(0, 10000000), Display(Name = "Площадь, м²")] public decimal Area { get; set; }
    [Range(0, 100000), Display(Name = "Часы бригады")] public decimal CrewHours { get; set; }
    [Range(0, 100000), Display(Name = "Машино-часы")] public decimal VehicleHours { get; set; }
    [Range(0, 100000000), Display(Name = "Дополнительные материалы, ₽")] public decimal MaterialsCost { get; set; }
    [Display(Name = "Срочный заказ")] public bool IsUrgent { get; set; }
}
