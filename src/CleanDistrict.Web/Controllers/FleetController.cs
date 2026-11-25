using CleanDistrict.Application;
using CleanDistrict.Domain;
using CleanDistrict.Infrastructure;
using CleanDistrict.Web.Helpers;
using CleanDistrict.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CleanDistrict.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Mechanic + "," + AppRoles.Dispatcher + "," + AppRoles.Director)]
public class FleetController(CleanDistrictDbContext db) : Controller
{
    private const string WriteRoles = AppRoles.Administrator + "," + AppRoles.Mechanic;

    public async Task<IActionResult> Index(VehicleStatus? status)
    {
        var query = db.Vehicles.AsNoTracking().Include(x => x.Driver).AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status);
        ViewBag.Statuses = new SelectList(Enum.GetValues<VehicleStatus>().Select(x => new { Value = x, Text = Display.Text(x) }), "Value", "Text", status);
        return View(await query.OrderBy(x => x.Name).ThenBy(x => x.RegistrationNumber).ToListAsync());
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vehicle = await db.Vehicles.Include(x => x.Driver).Include(x => x.MaintenanceRecords.OrderByDescending(x => x.PlannedDate))
            .Include(x => x.Repairs.OrderByDescending(x => x.StartedAtUtc)).FirstOrDefaultAsync(x => x.Id == id);
        return vehicle is null ? NotFound() : View(vehicle);
    }

    [Authorize(Roles = WriteRoles)] public async Task<IActionResult> Create() { await SetDrivers(); return View(new Vehicle()); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Create([Bind("Name,RegistrationNumber,Type,Mileage,EngineHours,DriverId,NextMaintenanceDate,NextMaintenanceMileage,NextMaintenanceEngineHours")] Vehicle vehicle)
    {
        await ValidateVehicle(vehicle);
        if (!ModelState.IsValid) { await SetDrivers(); return View(vehicle); }
        db.Add(vehicle); await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { vehicle.Id });
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Edit(Guid id) { var vehicle = await db.Vehicles.FindAsync(id); if (vehicle is null) return NotFound(); await SetDrivers(); return View(vehicle); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Edit(Guid id, [Bind("Name,RegistrationNumber,Type,Mileage,EngineHours,DriverId,NextMaintenanceDate,NextMaintenanceMileage,NextMaintenanceEngineHours")] Vehicle input)
    {
        var vehicle = await db.Vehicles.FindAsync(id);
        if (vehicle is null) return NotFound();
        await ValidateVehicle(input, id);
        if (!ModelState.IsValid) { await SetDrivers(); return View(input); }
        vehicle.Name = input.Name; vehicle.RegistrationNumber = input.RegistrationNumber; vehicle.Type = input.Type;
        vehicle.Mileage = input.Mileage; vehicle.EngineHours = input.EngineHours; vehicle.DriverId = input.DriverId;
        vehicle.NextMaintenanceDate = input.NextMaintenanceDate; vehicle.NextMaintenanceMileage = input.NextMaintenanceMileage; vehicle.NextMaintenanceEngineHours = input.NextMaintenanceEngineHours;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = WriteRoles)]
    public IActionResult CreateMaintenance(Guid vehicleId) => View(new MaintenanceInputModel { VehicleId = vehicleId });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CreateMaintenance(MaintenanceInputModel input)
    {
        var vehicle = await db.Vehicles.FindAsync(input.VehicleId);
        if (vehicle is null) return NotFound();
        if (vehicle.Status == VehicleStatus.Decommissioned) ModelState.AddModelError("", "Нельзя планировать ТО для списанной техники.");
        if (!ModelState.IsValid) return View(input);
        db.Add(new MaintenanceRecord { VehicleId = input.VehicleId, Description = input.Description, PlannedDate = input.PlannedDate, Cost = input.Cost });
        vehicle.NextMaintenanceDate = input.PlannedDate;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = input.VehicleId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> StartMaintenance(Guid id)
    {
        var record = await db.MaintenanceRecords.Include(x => x.Vehicle).FirstOrDefaultAsync(x => x.Id == id);
        if (record is null) return NotFound();
        if (record.Status != MaintenanceStatus.Planned) { TempData["Error"] = "ТО уже начато или завершено."; return RedirectToAction(nameof(Details), new { id = record.VehicleId }); }
        if (await HasActiveAssignments(record.VehicleId)) { TempData["Error"] = "Сначала завершите или отмените активные назначения техники."; return RedirectToAction(nameof(Details), new { id = record.VehicleId }); }
        record.Status = MaintenanceStatus.InProgress; record.StartedAtUtc = DateTime.UtcNow; record.Vehicle.Status = VehicleStatus.Maintenance;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = record.VehicleId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CompleteMaintenance(Guid id, decimal cost, string? notes, DateOnly? nextDate)
    {
        var record = await db.MaintenanceRecords.Include(x => x.Vehicle).FirstOrDefaultAsync(x => x.Id == id);
        if (record is null) return NotFound();
        if (record.Status != MaintenanceStatus.InProgress) { TempData["Error"] = "Завершить можно только начатое ТО."; return RedirectToAction(nameof(Details), new { id = record.VehicleId }); }
        record.Status = MaintenanceStatus.Completed; record.CompletedAtUtc = DateTime.UtcNow; record.Cost = cost; record.Notes = notes;
        record.Vehicle.Status = await HasActiveAssignments(record.VehicleId) ? VehicleStatus.Assigned : VehicleStatus.Available;
        record.Vehicle.NextMaintenanceDate = nextDate ?? record.PlannedDate.AddDays(90);
        record.Vehicle.NextMaintenanceMileage = record.Vehicle.Mileage + 10000;
        record.Vehicle.NextMaintenanceEngineHours = record.Vehicle.EngineHours + 500;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = record.VehicleId });
    }

    [Authorize(Roles = WriteRoles)] public IActionResult CreateRepair(Guid vehicleId) => View(new RepairInputModel { VehicleId = vehicleId });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CreateRepair(RepairInputModel input)
    {
        var vehicle = await db.Vehicles.FindAsync(input.VehicleId);
        if (vehicle is null) return NotFound();
        if (vehicle.Status == VehicleStatus.Decommissioned) ModelState.AddModelError("", "Техника списана.");
        if (await HasActiveAssignments(input.VehicleId)) ModelState.AddModelError("", "У техники есть активные назначения. Перевод в ремонт пока недоступен.");
        if (!ModelState.IsValid) return View(input);
        db.Add(new Repair { VehicleId = input.VehicleId, Description = input.Description, Cost = input.Cost, Parts = input.Parts, StartedAtUtc = DateTime.UtcNow });
        vehicle.Status = VehicleStatus.Repair;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = input.VehicleId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CompleteRepair(Guid id, decimal cost, string? parts)
    {
        var repair = await db.Repairs.Include(x => x.Vehicle).FirstOrDefaultAsync(x => x.Id == id);
        if (repair is null) return NotFound();
        if (repair.CompletedAtUtc.HasValue) return RedirectToAction(nameof(Details), new { id = repair.VehicleId });
        repair.CompletedAtUtc = DateTime.UtcNow; repair.Cost = cost; repair.Parts = parts;
        var hasMaintenance = await db.MaintenanceRecords.AnyAsync(x => x.VehicleId == repair.VehicleId && x.Status == MaintenanceStatus.InProgress);
        var hasAssignments = await HasActiveAssignments(repair.VehicleId);
        repair.Vehicle.Status = hasMaintenance ? VehicleStatus.Maintenance : hasAssignments ? VehicleStatus.Assigned : VehicleStatus.Available;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = repair.VehicleId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Decommission(Guid id)
    {
        var vehicle = await db.Vehicles.FindAsync(id);
        if (vehicle is null) return NotFound();
        if (await HasActiveAssignments(id)) { TempData["Error"] = "Нельзя списать технику с активными назначениями."; return RedirectToAction(nameof(Details), new { id }); }
        vehicle.Status = VehicleStatus.Decommissioned; vehicle.IsActive = false;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateVehicle(Vehicle vehicle, Guid? id = null)
    {
        if (await db.Vehicles.AnyAsync(x => x.RegistrationNumber == vehicle.RegistrationNumber && x.Id != id)) ModelState.AddModelError(nameof(vehicle.RegistrationNumber), "Регистрационный номер уже используется.");
        if (vehicle.DriverId.HasValue && !await db.Employees.AnyAsync(x => x.Id == vehicle.DriverId && x.IsActive)) ModelState.AddModelError(nameof(vehicle.DriverId), "Водитель не найден.");
    }

    private async Task SetDrivers() => ViewBag.Drivers = new SelectList(await db.Employees.Where(x => x.IsActive && (x.Position.Contains("водитель") || x.Position.Contains("тракторист"))).OrderBy(x => x.FullName).ToListAsync(), "Id", "FullName");
    private async Task<bool> HasActiveAssignments(Guid vehicleId) => await db.WorkOrderVehicles.AnyAsync(x => x.VehicleId == vehicleId && (x.WorkOrder.Status == WorkOrderStatus.Planned || x.WorkOrder.Status == WorkOrderStatus.InProgress));
}
