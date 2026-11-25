using System.ComponentModel.DataAnnotations;
using CleanDistrict.Domain;

namespace CleanDistrict.Web.Models;

public class RequestInputModel
{
    [Required, Display(Name = "Объект")] public Guid ObjectId { get; set; }
    [Required, Display(Name = "Услуга / тариф")] public Guid TariffId { get; set; }
    public Guid? CostEstimateId { get; set; }
    [Required, MaxLength(3000), Display(Name = "Описание работ")] public string Description { get; set; } = "";
    [Display(Name = "Приоритет")] public RequestPriority Priority { get; set; }
    [Required, Display(Name = "Желаемый срок")] public DateTime DesiredDateLocal { get; set; } = DateTime.Now.AddDays(1);
    [Range(0, 1000000000), Display(Name = "Расчётная стоимость, ₽")] public decimal EstimatedCost { get; set; }
}
