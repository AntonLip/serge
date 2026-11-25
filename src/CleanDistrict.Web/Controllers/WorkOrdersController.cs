using CleanDistrict.Application;
using CleanDistrict.Domain;
using CleanDistrict.Infrastructure;
using CleanDistrict.Web.Helpers;
using CleanDistrict.Web.Hubs;
using CleanDistrict.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanDistrict.Web.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public class WorkOrdersController(CleanDistrictDbContext db, IFileStorage files, IHubContext<DispatchHub> hub) : Controller
{
    public async Task<IActionResult> Index(WorkOrderStatus? status)
    {
        var query = db.WorkOrders.AsNoTracking().Include(x => x.ServiceRequest).ThenInclude(x => x.Object).Include(x => x.Crew).AsQueryable();
        if (User.IsInRole(AppRoles.CrewLeader))
        {
            var crewId = await CurrentCrewId();
            query = query.Where(x => x.CrewId == crewId);
        }
        if (status.HasValue) query = query.Where(x => x.Status == status);
        ViewBag.Statuses = new SelectList(Enum.GetValues<WorkOrderStatus>().Select(x => new { Value = x, Text = Display.Text(x) }), "Value", "Text", status);
        return View(await query.OrderByDescending(x => x.PlannedStartUtc).ToListAsync());
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var order = await FullOrder().FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (!await CanAccess(order)) return Forbid();
        return View(order);
    }

    public async Task<IActionResult> Print(Guid id)
    {
        var order = await FullOrder().FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (!await CanAccess(order)) return Forbid();
        return View(order);
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Dispatcher)]
    public async Task<IActionResult> Create(Guid requestId)
    {
        var request = await db.ServiceRequests.Include(x => x.Object).FirstOrDefaultAsync(x => x.Id == requestId);
        if (request is null) return NotFound();
        if (request.Status != RequestStatus.Approved) { TempData["Error"] = "Наряд создаётся только для согласованной заявки."; return RedirectToAction("Details", "Requests", new { id = requestId }); }
        await SetLists();
        ViewBag.RequestNumber = request.Number;
        return View(new WorkOrderInputModel { ServiceRequestId = requestId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Dispatcher)]
    public async Task<IActionResult> Create(WorkOrderInputModel input)
    {
        var request = await db.ServiceRequests.FindAsync(input.ServiceRequestId);
        if (request is null) return NotFound();
        if (request.Status != RequestStatus.Approved) ModelState.AddModelError("", "Заявка уже обработана или не согласована.");
        var start = TimeConverter.ToUtc(input.PlannedStartLocal);
        var end = TimeConverter.ToUtc(input.PlannedEndLocal);
        if (end <= start) ModelState.AddModelError(nameof(input.PlannedEndLocal), "Окончание должно быть позже начала.");
        if (!await db.Crews.AnyAsync(x => x.Id == input.CrewId && x.IsActive)) ModelState.AddModelError(nameof(input.CrewId), "Выберите активную бригаду.");
        input.VehicleIds = input.VehicleIds.Distinct().ToList();
        var vehicles = await db.Vehicles.Include(x => x.Driver).Where(x => input.VehicleIds.Contains(x.Id)).ToListAsync();
        foreach (var vehicle in vehicles)
        {
            if (!vehicle.IsActive || vehicle.Status is VehicleStatus.Maintenance or VehicleStatus.Repair or VehicleStatus.Decommissioned)
                ModelState.AddModelError(nameof(input.VehicleIds), $"Техника {vehicle.Name} ({vehicle.RegistrationNumber}) недоступна.");
            if (vehicle.DriverId is null) ModelState.AddModelError(nameof(input.VehicleIds), $"За техникой {vehicle.RegistrationNumber} не закреплён водитель.");
        }
        if (vehicles.Count != input.VehicleIds.Count) ModelState.AddModelError(nameof(input.VehicleIds), "Выбранная техника не найдена.");

        if (await db.WorkOrders.AnyAsync(x => x.CrewId == input.CrewId && (x.Status == WorkOrderStatus.Planned || x.Status == WorkOrderStatus.InProgress) && x.PlannedStartUtc < end && x.PlannedEndUtc > start))
            ModelState.AddModelError(nameof(input.CrewId), "Бригада уже занята в выбранный период.");
        if (input.VehicleIds.Count > 0 && await db.WorkOrders.AnyAsync(x => (x.Status == WorkOrderStatus.Planned || x.Status == WorkOrderStatus.InProgress) && x.PlannedStartUtc < end && x.PlannedEndUtc > start && x.Vehicles.Any(v => input.VehicleIds.Contains(v.VehicleId))))
            ModelState.AddModelError(nameof(input.VehicleIds), "Выбранная техника уже назначена на это время.");
        var driverIds = vehicles.Where(x => x.DriverId.HasValue).Select(x => x.DriverId!.Value).ToList();
        if (driverIds.Count > 0 && await db.WorkOrders.AnyAsync(x => (x.Status == WorkOrderStatus.Planned || x.Status == WorkOrderStatus.InProgress) && x.PlannedStartUtc < end && x.PlannedEndUtc > start && x.Vehicles.Any(v => driverIds.Contains(v.DriverId))))
            ModelState.AddModelError(nameof(input.VehicleIds), "Закреплённый водитель уже занят в выбранный период.");

        if (!ModelState.IsValid) { await SetLists(); ViewBag.RequestNumber = request.Number; return View(input); }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = new WorkOrder
        {
            Number = $"Н-{DateTime.Now.Year}/{(await db.WorkOrders.CountAsync() + 1):D4}", ServiceRequestId = request.Id,
            CrewId = input.CrewId, PlannedStartUtc = start, PlannedEndUtc = end, Notes = input.Notes,
            Vehicles = vehicles.Select(x => new WorkOrderVehicle { VehicleId = x.Id, DriverId = x.DriverId!.Value }).ToList()
        };
        db.Add(order);
        request.Status = RequestStatus.Assigned;
        db.Add(new RequestStatusHistory { ServiceRequestId = request.Id, FromStatus = RequestStatus.Approved, ToStatus = RequestStatus.Assigned, ChangedByName = User.Identity?.Name ?? "", Comment = $"Создан наряд {order.Number}" });
        foreach (var vehicle in vehicles) vehicle.Status = VehicleStatus.Assigned;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await hub.Clients.All.SendAsync("RequestChanged", request.Id);
        TempData["Success"] = $"Наряд {order.Number} создан.";
        return RedirectToAction(nameof(Details), new { order.Id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Administrator + "," + AppRoles.CrewLeader)]
    public async Task<IActionResult> Start(Guid id)
    {
        var order = await db.WorkOrders.Include(x => x.ServiceRequest).FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (!await CanAccess(order)) return Forbid();
        if (order.Status != WorkOrderStatus.Planned || order.ServiceRequest.Status != RequestStatus.Assigned)
        {
            TempData["Error"] = "Наряд нельзя перевести в работу."; return RedirectToAction(nameof(Details), new { id });
        }
        order.Status = WorkOrderStatus.InProgress; order.ActualStartUtc = DateTime.UtcNow;
        order.ServiceRequest.Status = RequestStatus.InProgress;
        db.Add(new RequestStatusHistory { ServiceRequestId = order.ServiceRequestId, FromStatus = RequestStatus.Assigned, ToStatus = RequestStatus.InProgress, ChangedByName = User.Identity?.Name ?? "", Comment = "Бригада приступила к работам" });
        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("RequestChanged", order.ServiceRequestId);
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.CrewLeader)]
    public async Task<IActionResult> Complete(Guid id)
    {
        var order = await FullOrder().FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (!await CanAccess(order)) return Forbid();
        if (order.Status != WorkOrderStatus.InProgress) { TempData["Error"] = "Завершить можно только наряд в работе."; return RedirectToAction(nameof(Details), new { id }); }
        ViewBag.OrderNumber = order.Number;
        return View(new WorkReportInputModel { WorkOrderId = id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Administrator + "," + AppRoles.CrewLeader)]
    public async Task<IActionResult> Complete(WorkReportInputModel input, List<IFormFile> photos)
    {
        var order = await db.WorkOrders.Include(x => x.ServiceRequest).Include(x => x.Vehicles).FirstOrDefaultAsync(x => x.Id == input.WorkOrderId);
        if (order is null) return NotFound();
        if (!await CanAccess(order)) return Forbid();
        if (order.Status != WorkOrderStatus.InProgress) ModelState.AddModelError("", "Наряд уже завершён или отменён.");
        if (photos.Count > 5) ModelState.AddModelError("photos", "Можно прикрепить не более пяти фотографий.");
        if (photos.Any(x => x.Length > 5 * 1024 * 1024 || !new[] { ".jpg", ".jpeg", ".png" }.Contains(Path.GetExtension(x.FileName).ToLowerInvariant())))
            ModelState.AddModelError("photos", "Фотографии должны быть JPG/PNG размером до 5 МБ.");
        if (!ModelState.IsValid) { ViewBag.OrderNumber = order.Number; return View(input); }

        var report = new WorkReport { WorkOrderId = order.Id, ActualVolume = input.ActualVolume, LaborHours = input.LaborHours, MaterialsCost = input.MaterialsCost, Comment = input.Comment, CompletedAtUtc = DateTime.UtcNow };
        foreach (var photo in photos)
        {
            await using var stream = photo.OpenReadStream();
            var storedName = await files.SaveAsync(stream, Path.GetExtension(photo.FileName));
            report.Photos.Add(new WorkReportPhoto { OriginalFileName = Path.GetFileName(photo.FileName), StoredFileName = storedName });
        }
        db.Add(report);
        order.Status = WorkOrderStatus.Completed; order.ActualEndUtc = report.CompletedAtUtc;
        order.ServiceRequest.Status = RequestStatus.Completed;
        db.Add(new RequestStatusHistory { ServiceRequestId = order.ServiceRequestId, FromStatus = RequestStatus.InProgress, ToStatus = RequestStatus.Completed, ChangedByName = User.Identity?.Name ?? "", Comment = "Получен отчёт бригадира" });
        foreach (var assignment in order.Vehicles)
        {
            var vehicle = await db.Vehicles.FindAsync(assignment.VehicleId);
            var hasOtherAssignments = await db.WorkOrderVehicles.AnyAsync(x => x.VehicleId == assignment.VehicleId && x.WorkOrderId != order.Id && (x.WorkOrder.Status == WorkOrderStatus.Planned || x.WorkOrder.Status == WorkOrderStatus.InProgress));
            if (vehicle?.Status == VehicleStatus.Assigned && !hasOtherAssignments) vehicle.Status = VehicleStatus.Available;
        }
        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("RequestChanged", order.ServiceRequestId);
        TempData["Success"] = "Отчёт сохранён, заявка отмечена как выполненная.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    public async Task<IActionResult> Photo(Guid id)
    {
        var photo = await db.WorkReportPhotos.Include(x => x.WorkReport).ThenInclude(x => x.WorkOrder).FirstOrDefaultAsync(x => x.Id == id);
        if (photo is null) return NotFound();
        if (!await CanAccess(photo.WorkReport.WorkOrder)) return Forbid();
        var stream = await files.OpenReadAsync(photo.StoredFileName);
        return stream is null ? NotFound() : File(stream, "application/octet-stream", photo.OriginalFileName);
    }

    private IQueryable<WorkOrder> FullOrder() => db.WorkOrders.Include(x => x.ServiceRequest).ThenInclude(x => x.Object).ThenInclude(x => x.Client)
        .Include(x => x.ServiceRequest).ThenInclude(x => x.Tariff).Include(x => x.Crew).ThenInclude(x => x.Members)
        .Include(x => x.Vehicles).ThenInclude(x => x.Vehicle).Include(x => x.Vehicles).ThenInclude(x => x.Driver)
        .Include(x => x.Report!).ThenInclude(x => x.Photos);

    private async Task<Guid?> CurrentCrewId() => (await db.Employees.FirstOrDefaultAsync(x => x.UserId == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value))?.CrewId;

    private async Task<bool> CanAccess(WorkOrder order) => !User.IsInRole(AppRoles.CrewLeader) || await CurrentCrewId() == order.CrewId;

    private async Task SetLists()
    {
        ViewBag.Crews = new SelectList(await db.Crews.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Vehicles = new MultiSelectList(await db.Vehicles.Where(x => x.IsActive && x.Status != VehicleStatus.Decommissioned && x.Status != VehicleStatus.Repair && x.Status != VehicleStatus.Maintenance).OrderBy(x => x.Name).Select(x => new { x.Id, Name = x.Name + " · " + x.RegistrationNumber + " · " + x.Status }).ToListAsync(), "Id", "Name");
    }
}
