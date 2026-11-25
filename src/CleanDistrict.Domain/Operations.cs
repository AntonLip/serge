using System.ComponentModel.DataAnnotations;

namespace CleanDistrict.Domain;

public class Employee : BaseEntity
{
    [Required, MaxLength(150)] public string FullName { get; set; } = "";
    [Required, MaxLength(100)] public string Position { get; set; } = "";
    [MaxLength(40)] public string? Phone { get; set; }
    [MaxLength(450)] public string? UserId { get; set; }
    public Guid? CrewId { get; set; }
    public Crew? Crew { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Crew : BaseEntity
{
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [Required, MaxLength(200)] public string Specialization { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<Employee> Members { get; set; } = [];
}

public class ServiceRequest : BaseEntity
{
    [Required, MaxLength(40)] public string Number { get; set; } = "";
    public Guid ObjectId { get; set; }
    public ServiceObject Object { get; set; } = null!;
    public Guid? TariffId { get; set; }
    public Tariff? Tariff { get; set; }
    public RequestSource Source { get; set; }
    [MaxLength(200)] public string? RequestedService { get; set; }
    [MaxLength(150)] public string? ContactName { get; set; }
    [MaxLength(40)] public string? ContactPhone { get; set; }
    public Guid? CostEstimateId { get; set; }
    public CostEstimate? CostEstimate { get; set; }
    [Required, MaxLength(3000)] public string Description { get; set; } = "";
    public RequestPriority Priority { get; set; } = RequestPriority.Normal;
    public RequestStatus Status { get; set; } = RequestStatus.New;
    public DateTime DesiredDateUtc { get; set; }
    [Range(0, 1000000000)] public decimal? EstimatedCost { get; set; }
    [MaxLength(450)] public string? CreatedByUserId { get; set; }
    [Required, MaxLength(150)] public string CreatedByName { get; set; } = "";
    public WorkOrder? WorkOrder { get; set; }
    public List<RequestStatusHistory> History { get; set; } = [];
}

public class RequestStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;
    public RequestStatus FromStatus { get; set; }
    public RequestStatus ToStatus { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(150)] public string ChangedByName { get; set; } = "";
    [MaxLength(1000)] public string? Comment { get; set; }
}

public class WorkOrder : BaseEntity
{
    [Required, MaxLength(40)] public string Number { get; set; } = "";
    public Guid ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;
    public Guid CrewId { get; set; }
    public Crew Crew { get; set; } = null!;
    public DateTime PlannedStartUtc { get; set; }
    public DateTime PlannedEndUtc { get; set; }
    public DateTime? ActualStartUtc { get; set; }
    public DateTime? ActualEndUtc { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Planned;
    [MaxLength(1000)] public string? Notes { get; set; }
    public List<WorkOrderVehicle> Vehicles { get; set; } = [];
    public WorkReport? Report { get; set; }
}

public class WorkOrderVehicle
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = null!;
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public Guid DriverId { get; set; }
    public Employee Driver { get; set; } = null!;
}

public class WorkReport : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = null!;
    [Range(0.01, 100000000)] public decimal ActualVolume { get; set; }
    [Range(0, 100000)] public decimal LaborHours { get; set; }
    [Range(0, 100000000)] public decimal MaterialsCost { get; set; }
    [Required, MaxLength(3000)] public string Comment { get; set; } = "";
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
    public List<WorkReportPhoto> Photos { get; set; } = [];
}

public class WorkReportPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkReportId { get; set; }
    public WorkReport WorkReport { get; set; } = null!;
    [MaxLength(255)] public string OriginalFileName { get; set; } = "";
    [MaxLength(255)] public string StoredFileName { get; set; } = "";
}
