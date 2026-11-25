using System.ComponentModel.DataAnnotations;

namespace CleanDistrict.Web.Models;

public class WorkOrderInputModel
{
    [Required] public Guid ServiceRequestId { get; set; }
    [Required, Display(Name = "Бригада")] public Guid CrewId { get; set; }
    [Required, Display(Name = "Начало")] public DateTime PlannedStartLocal { get; set; } = DateTime.Now.AddHours(2);
    [Required, Display(Name = "Окончание")] public DateTime PlannedEndLocal { get; set; } = DateTime.Now.AddHours(6);
    [Display(Name = "Техника")] public List<Guid> VehicleIds { get; set; } = [];
    [MaxLength(1000), Display(Name = "Примечания")] public string? Notes { get; set; }
}

public class WorkReportInputModel
{
    [Required] public Guid WorkOrderId { get; set; }
    [Range(0.01, 100000000), Display(Name = "Фактический объём")] public decimal ActualVolume { get; set; }
    [Range(0, 100000), Display(Name = "Трудозатраты, чел.-ч")] public decimal LaborHours { get; set; }
    [Range(0, 100000000), Display(Name = "Материалы, ₽")] public decimal MaterialsCost { get; set; }
    [Required, MaxLength(3000), Display(Name = "Отчёт о выполнении")] public string Comment { get; set; } = "";
}
