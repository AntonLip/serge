using System.Text;
using CleanDistrict.Application;
using CleanDistrict.Domain;
using CleanDistrict.Infrastructure;
using CleanDistrict.Web.Helpers;
using CleanDistrict.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanDistrict.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Manager + "," + AppRoles.Director)]
public class ReportsController(CleanDistrictDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var requests = await db.ServiceRequests.Include(x => x.WorkOrder).ToListAsync();
        var completed = requests.Where(x => x.Status is RequestStatus.Completed or RequestStatus.Closed).ToList();
        var onTime = completed.Count(x => x.WorkOrder?.ActualEndUtc <= x.DesiredDateUtc);
        var model = new ReportsIndexViewModel
        {
            TotalRequests = requests.Count, CompletedRequests = completed.Count,
            OnTimePercent = completed.Count == 0 ? 0 : Math.Round(100d * onTime / completed.Count, 1),
            EstimatedRevenue = requests.Where(x => x.Status != RequestStatus.Cancelled).Sum(x => x.EstimatedCost ?? 0m),
            FleetCosts = await db.MaintenanceRecords.Where(x => x.Status == MaintenanceStatus.Completed).SumAsync(x => x.Cost)
                + await db.Repairs.Where(x => x.CompletedAtUtc != null).SumAsync(x => x.Cost)
        };
        return View(model);
    }

    public async Task<IActionResult> Requests(DateTime? from, DateTime? to)
    {
        var items = await RequestQuery(from, to).OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
        ViewBag.From = from?.ToString("yyyy-MM-dd"); ViewBag.To = to?.ToString("yyyy-MM-dd");
        return View(items);
    }

    public async Task<IActionResult> Objects()
    {
        var requests = await db.ServiceRequests.Include(x => x.Object).ThenInclude(x => x.Client).ToListAsync();
        return View(requests.GroupBy(x => x.Object).Select(x => new ObjectReportRow
        {
            ObjectName = x.Key.Name, ClientName = x.Key.Client.Name, RequestCount = x.Count(),
            CompletedCount = x.Count(r => r.Status == RequestStatus.Completed || r.Status == RequestStatus.Closed),
            TotalCost = x.Where(r => r.Status != RequestStatus.Cancelled).Sum(r => r.EstimatedCost ?? 0m)
        }).OrderBy(x => x.ObjectName).ToList());
    }

    public async Task<IActionResult> Resources()
    {
        var orders = await db.WorkOrders.Include(x => x.Crew).Include(x => x.Vehicles).ThenInclude(x => x.Vehicle).ToListAsync();
        var model = new ResourceReportViewModel
        {
            Crews = orders.GroupBy(x => x.Crew).Select(x => new CrewLoadRow
            {
                CrewName = x.Key.Name, OrderCount = x.Count(), CompletedCount = x.Count(o => o.Status == WorkOrderStatus.Completed),
                PlannedHours = Math.Round(x.Sum(o => (o.PlannedEndUtc - o.PlannedStartUtc).TotalHours), 1)
            }).OrderBy(x => x.CrewName).ToList(),
            Vehicles = orders.SelectMany(x => x.Vehicles.Select(v => new { Order = x, v.Vehicle })).GroupBy(x => x.Vehicle).Select(x => new VehicleLoadRow
            {
                VehicleName = x.Key.Name, RegistrationNumber = x.Key.RegistrationNumber, AssignmentCount = x.Count(),
                PlannedHours = Math.Round(x.Sum(v => (v.Order.PlannedEndUtc - v.Order.PlannedStartUtc).TotalHours), 1)
            }).OrderBy(x => x.VehicleName).ToList()
        };
        return View(model);
    }

    public async Task<IActionResult> FleetCosts()
    {
        var vehicles = await db.Vehicles.Include(x => x.MaintenanceRecords).Include(x => x.Repairs).ToListAsync();
        return View(vehicles.Select(x => new FleetCostRow
        {
            VehicleName = x.Name, RegistrationNumber = x.RegistrationNumber,
            MaintenanceCost = x.MaintenanceRecords.Where(m => m.Status == MaintenanceStatus.Completed).Sum(m => m.Cost),
            RepairCost = x.Repairs.Where(r => r.CompletedAtUtc != null).Sum(r => r.Cost)
        }).OrderByDescending(x => x.Total).ToList());
    }

    public async Task<IActionResult> ExportRequests(DateTime? from, DateTime? to)
    {
        var items = await RequestQuery(from, to).OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
        var csv = new StringBuilder("Номер;Дата;Клиент;Объект;Услуга;Статус;Стоимость (руб.)\n");
        foreach (var x in items) csv.AppendLine(string.Join(';', Csv(x.Number), Csv(Display.DateTime(x.CreatedAtUtc)), Csv(x.Object.Client.Name), Csv(x.Object.Name), Csv(x.Tariff?.Name ?? x.RequestedService), Csv(Display.Text(x.Status)), x.EstimatedCost?.ToString("0.00") ?? ""));
        return CsvFile(csv, "requests.csv");
    }

    public async Task<IActionResult> ExportObjects()
    {
        var requests = await db.ServiceRequests.Include(x => x.Object).ThenInclude(x => x.Client).ToListAsync();
        var csv = new StringBuilder("Объект;Клиент;Заявок;Выполнено;Стоимость (руб.)\n");
        foreach (var x in requests.GroupBy(x => x.Object).OrderBy(x => x.Key.Name)) csv.AppendLine(string.Join(';', Csv(x.Key.Name), Csv(x.Key.Client.Name), x.Count(), x.Count(r => r.Status == RequestStatus.Completed || r.Status == RequestStatus.Closed), x.Sum(r => r.EstimatedCost ?? 0m).ToString("0.00")));
        return CsvFile(csv, "objects.csv");
    }

    public async Task<IActionResult> ExportFleetCosts()
    {
        var vehicles = await db.Vehicles.Include(x => x.MaintenanceRecords).Include(x => x.Repairs).ToListAsync();
        var csv = new StringBuilder("Техника;Номер;ТО (руб.);Ремонты (руб.);Итого (руб.)\n");
        foreach (var x in vehicles) csv.AppendLine(string.Join(';', Csv(x.Name), Csv(x.RegistrationNumber), x.MaintenanceRecords.Where(m => m.Status == MaintenanceStatus.Completed).Sum(m => m.Cost).ToString("0.00"), x.Repairs.Where(r => r.CompletedAtUtc != null).Sum(r => r.Cost).ToString("0.00"), (x.MaintenanceRecords.Where(m => m.Status == MaintenanceStatus.Completed).Sum(m => m.Cost) + x.Repairs.Where(r => r.CompletedAtUtc != null).Sum(r => r.Cost)).ToString("0.00")));
        return CsvFile(csv, "fleet-costs.csv");
    }

    private IQueryable<ServiceRequest> RequestQuery(DateTime? from, DateTime? to)
    {
        var query = db.ServiceRequests.Include(x => x.Object).ThenInclude(x => x.Client).Include(x => x.Tariff).AsQueryable();
        if (from.HasValue) query = query.Where(x => x.CreatedAtUtc >= TimeConverter.ToUtc(from.Value));
        if (to.HasValue) query = query.Where(x => x.CreatedAtUtc < TimeConverter.ToUtc(to.Value.AddDays(1)));
        return query;
    }

    private static string Csv(object? value) => $"\"{value?.ToString()?.Replace("\"", "\"\"")}\"";
    private static FileContentResult CsvFile(StringBuilder csv, string name) => new(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv") { FileDownloadName = name };
}
