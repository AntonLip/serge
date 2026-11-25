using System.ComponentModel.DataAnnotations;

namespace CleanDistrict.Domain;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Client : BaseEntity
{
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [Required, MaxLength(150)] public string ContactName { get; set; } = "";
    [Required, MaxLength(40)] public string Phone { get; set; } = "";
    [EmailAddress, MaxLength(150)] public string? Email { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ServiceObject> Objects { get; set; } = [];
    public List<Contract> Contracts { get; set; } = [];
}

public class ServiceObject : BaseEntity
{
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [Required, MaxLength(300)] public string Address { get; set; } = "";
    [Range(0.01, 10000000)] public decimal AreaSquareMeters { get; set; }
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid ManagerId { get; set; }
    public Employee Manager { get; set; } = null!;
    [MaxLength(1000)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ServiceRequest> Requests { get; set; } = [];
    public List<CostEstimate> Estimates { get; set; } = [];
}

public class Contract : BaseEntity
{
    [Required, MaxLength(60)] public string Number { get; set; } = "";
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    [Range(0, 1000000000)] public decimal TotalAmount { get; set; }
    [Required, MaxLength(2000)] public string Services { get; set; } = "";
    [MaxLength(255)] public string? OriginalFileName { get; set; }
    [MaxLength(255)] public string? StoredFileName { get; set; }
}

public class Tariff : BaseEntity
{
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [Required, MaxLength(100)] public string ServiceType { get; set; } = "";
    [Required, MaxLength(40)] public string Unit { get; set; } = "";
    [Range(0, 1000000)] public decimal BasePrice { get; set; }
    [Range(0, 1000000)] public decimal AreaRate { get; set; }
    [Range(0, 1000000)] public decimal CrewHourlyRate { get; set; }
    [Range(0, 1000000)] public decimal VehicleHourlyRate { get; set; }
    [Range(0, 1000000)] public decimal MaterialCost { get; set; }
    [Range(1, 10)] public decimal UrgencyCoefficient { get; set; } = 1.5m;
    [Range(0, 100)] public decimal VatRate { get; set; } = 20m;
    public bool IsActive { get; set; } = true;
}

public class CostEstimate : BaseEntity
{
    public Guid ObjectId { get; set; }
    public ServiceObject Object { get; set; } = null!;
    public Guid TariffId { get; set; }
    public Tariff Tariff { get; set; } = null!;
    [Range(0, 10000000)] public decimal Area { get; set; }
    [Range(0, 100000)] public decimal CrewHours { get; set; }
    [Range(0, 100000)] public decimal VehicleHours { get; set; }
    [Range(0, 100000000)] public decimal MaterialsCost { get; set; }
    public bool IsUrgent { get; set; }
    public decimal Subtotal { get; set; }
    [Range(0, 100)] public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public List<CostEstimateItem> Items { get; set; } = [];
}

public class CostEstimateItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CostEstimateId { get; set; }
    public CostEstimate CostEstimate { get; set; } = null!;
    [MaxLength(300)] public string Description { get; set; } = "";
    public decimal Quantity { get; set; }
    [MaxLength(40)] public string Unit { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
