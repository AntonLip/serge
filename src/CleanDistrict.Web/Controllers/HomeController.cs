using System.Diagnostics;
using CleanDistrict.Application;
using CleanDistrict.Domain;
using CleanDistrict.Infrastructure;
using CleanDistrict.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanDistrict.Web.Controllers;

[Authorize]
public class HomeController(CleanDistrictDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole(AppRoles.Client)) return RedirectToAction("Index", "Portal");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var model = new DashboardViewModel
        {
            NewRequests = await db.ServiceRequests.CountAsync(x => x.Status == RequestStatus.New),
            OverdueRequests = await db.ServiceRequests.CountAsync(x => x.DesiredDateUtc < now && x.Status != RequestStatus.Completed && x.Status != RequestStatus.Closed && x.Status != RequestStatus.Cancelled),
            ActiveWorkOrders = await db.WorkOrders.CountAsync(x => x.Status == WorkOrderStatus.Planned || x.Status == WorkOrderStatus.InProgress),
            AvailableVehicles = await db.Vehicles.CountAsync(x => x.Status == VehicleStatus.Available && x.IsActive),
            DueMaintenance = await db.MaintenanceRecords.CountAsync(x => x.Status != MaintenanceStatus.Completed && x.PlannedDate <= today.AddDays(14)),
            ExpiringContracts = await db.Contracts.CountAsync(x => x.Status == ContractStatus.Active && x.EndDate <= today.AddDays(30)),
            RecentRequests = await db.ServiceRequests.Include(x => x.Object).ThenInclude(x => x.Client).OrderByDescending(x => x.CreatedAtUtc).Take(6).ToListAsync()
        };
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
