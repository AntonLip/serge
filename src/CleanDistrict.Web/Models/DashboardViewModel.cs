using CleanDistrict.Domain;

namespace CleanDistrict.Web.Models;

public class DashboardViewModel
{
    public int NewRequests { get; set; }
    public int OverdueRequests { get; set; }
    public int ActiveWorkOrders { get; set; }
    public int AvailableVehicles { get; set; }
    public int DueMaintenance { get; set; }
    public int ExpiringContracts { get; set; }
    public List<ServiceRequest> RecentRequests { get; set; } = [];
}
