using CleanDistrict.Domain;

namespace CleanDistrict.Web.Models;

public class ReportsIndexViewModel
{
    public int TotalRequests { get; set; }
    public int CompletedRequests { get; set; }
    public double OnTimePercent { get; set; }
    public decimal EstimatedRevenue { get; set; }
    public decimal FleetCosts { get; set; }
}

public class ObjectReportRow
{
    public string ObjectName { get; set; } = "";
    public string ClientName { get; set; } = "";
    public int RequestCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal TotalCost { get; set; }
}

public class CrewLoadRow
{
    public string CrewName { get; set; } = "";
    public int OrderCount { get; set; }
    public int CompletedCount { get; set; }
    public double PlannedHours { get; set; }
}

public class VehicleLoadRow
{
    public string VehicleName { get; set; } = "";
    public string RegistrationNumber { get; set; } = "";
    public int AssignmentCount { get; set; }
    public double PlannedHours { get; set; }
}

public class ResourceReportViewModel
{
    public List<CrewLoadRow> Crews { get; set; } = [];
    public List<VehicleLoadRow> Vehicles { get; set; } = [];
}

public class FleetCostRow
{
    public string VehicleName { get; set; } = "";
    public string RegistrationNumber { get; set; } = "";
    public decimal MaintenanceCost { get; set; }
    public decimal RepairCost { get; set; }
    public decimal Total => MaintenanceCost + RepairCost;
}
