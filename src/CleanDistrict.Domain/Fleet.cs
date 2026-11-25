using System.ComponentModel.DataAnnotations;

namespace CleanDistrict.Domain;

public class Vehicle : BaseEntity
{
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(30)] public string RegistrationNumber { get; set; } = "";
    public VehicleType Type { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Available;
    [Range(0, 100000000)] public decimal Mileage { get; set; }
    [Range(0, 10000000)] public decimal EngineHours { get; set; }
    public Guid? DriverId { get; set; }
    public Employee? Driver { get; set; }
    public DateOnly? NextMaintenanceDate { get; set; }
    [Range(0, 100000000)] public decimal? NextMaintenanceMileage { get; set; }
    [Range(0, 10000000)] public decimal? NextMaintenanceEngineHours { get; set; }
    public bool IsActive { get; set; } = true;
    public List<MaintenanceRecord> MaintenanceRecords { get; set; } = [];
    public List<Repair> Repairs { get; set; } = [];
}

public class MaintenanceRecord : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    [Required, MaxLength(300)] public string Description { get; set; } = "";
    public DateOnly PlannedDate { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Planned;
    [Range(0, 100000000)] public decimal Cost { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
}

public class Repair : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    [Required, MaxLength(2000)] public string Description { get; set; } = "";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    [MaxLength(2000)] public string? Parts { get; set; }
    [Range(0, 100000000)] public decimal Cost { get; set; }
}
