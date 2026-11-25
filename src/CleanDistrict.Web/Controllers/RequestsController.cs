using System.Security.Claims;
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
public class RequestsController(CleanDistrictDbContext db, IHubContext<DispatchHub> hub) : Controller
{
    private const string WriteRoles = AppRoles.Administrator + "," + AppRoles.Manager + "," + AppRoles.Dispatcher;

    public async Task<IActionResult> Index(RequestStatus? status, RequestPriority? priority, RequestSource? source, Guid? objectId, bool overdue = false, int page = 1)
    {
        var query = db.ServiceRequests.AsNoTracking().Include(x => x.Object).ThenInclude(x => x.Client).Include(x => x.Tariff).AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (priority.HasValue) query = query.Where(x => x.Priority == priority);
        if (source.HasValue) query = query.Where(x => x.Source == source);
        if (objectId.HasValue) query = query.Where(x => x.ObjectId == objectId);
        if (overdue) query = query.Where(x => x.DesiredDateUtc < DateTime.UtcNow && x.Status != RequestStatus.Completed && x.Status != RequestStatus.Closed && x.Status != RequestStatus.Cancelled);
        page = Math.Max(1, page);
        var count = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Priority).ThenBy(x => x.DesiredDateUtc).Skip((page - 1) * 12).Take(12).ToListAsync();
        ViewBag.Status = status; ViewBag.Priority = priority; ViewBag.Source = source; ViewBag.ObjectId = objectId; ViewBag.Overdue = overdue;
        ViewBag.Statuses = new SelectList(Enum.GetValues<RequestStatus>().Select(x => new { Value = x, Text = Display.Text(x) }), "Value", "Text", status);
        ViewBag.Priorities = new SelectList(Enum.GetValues<RequestPriority>().Select(x => new { Value = x, Text = Display.Text(x) }), "Value", "Text", priority);
        ViewBag.Sources = new SelectList(Enum.GetValues<RequestSource>().Select(x => new { Value = x, Text = Display.Text(x) }), "Value", "Text", source);
        ViewBag.Objects = new SelectList(await db.ServiceObjects.OrderBy(x => x.Name).ToListAsync(), "Id", "Name", objectId);
        var model = new PagedList<ServiceRequest> { Page = page, TotalPages = Math.Max(1, (int)Math.Ceiling(count / 12d)), TotalCount = count };
        model.AddRange(items);
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var request = await db.ServiceRequests.Include(x => x.Object).ThenInclude(x => x.Client).Include(x => x.Object).ThenInclude(x => x.Manager)
            .Include(x => x.Tariff).Include(x => x.CostEstimate).Include(x => x.History.OrderByDescending(x => x.ChangedAtUtc))
            .Include(x => x.WorkOrder!).ThenInclude(x => x.Crew).FirstOrDefaultAsync(x => x.Id == id);
        if (request is null) return NotFound();
        if (request.Source == RequestSource.Client && request.Status == RequestStatus.New)
            ViewBag.Tariffs = new SelectList(await db.Tariffs.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        return View(request);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Create(Guid? objectId, Guid? estimateId)
    {
        await SetLists();
        var model = new RequestInputModel { ObjectId = objectId ?? Guid.Empty };
        if (estimateId.HasValue)
        {
            var estimate = await db.CostEstimates.FindAsync(estimateId);
            if (estimate is not null)
            {
                model.CostEstimateId = estimate.Id; model.ObjectId = estimate.ObjectId; model.TariffId = estimate.TariffId; model.EstimatedCost = estimate.Total;
            }
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Create(RequestInputModel input)
    {
        if (!await db.ServiceObjects.AnyAsync(x => x.Id == input.ObjectId && x.IsActive)) ModelState.AddModelError(nameof(input.ObjectId), "Выберите объект.");
        if (!await db.Tariffs.AnyAsync(x => x.Id == input.TariffId && x.IsActive)) ModelState.AddModelError(nameof(input.TariffId), "Выберите тариф.");
        if (input.CostEstimateId.HasValue && !await db.CostEstimates.AnyAsync(x => x.Id == input.CostEstimateId && x.ObjectId == input.ObjectId)) ModelState.AddModelError(nameof(input.CostEstimateId), "Расчёт не относится к выбранному объекту.");
        if (!ModelState.IsValid) { await SetLists(); return View(input); }
        var number = $"З-{DateTime.Now.Year}/{(await db.ServiceRequests.CountAsync() + 1):D4}";
        var request = new ServiceRequest
        {
            Number = number, ObjectId = input.ObjectId, TariffId = input.TariffId, CostEstimateId = input.CostEstimateId,
            Source = RequestSource.Staff, Description = input.Description, Priority = input.Priority, DesiredDateUtc = TimeConverter.ToUtc(input.DesiredDateLocal),
            EstimatedCost = input.EstimatedCost, CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier), CreatedByName = User.Identity?.Name ?? "Сотрудник"
        };
        db.Add(request);
        db.Add(new RequestStatusHistory { ServiceRequest = request, FromStatus = RequestStatus.New, ToStatus = RequestStatus.New, ChangedByName = request.CreatedByName, Comment = "Заявка зарегистрирована" });
        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("RequestChanged", request.Id);
        TempData["Success"] = $"Заявка {request.Number} зарегистрирована.";
        return RedirectToAction(nameof(Details), new { request.Id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ProcessClient(ProcessClientRequestInputModel input)
    {
        var request = await db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == input.RequestId);
        if (request is null) return NotFound();
        if (request.Source != RequestSource.Client || request.Status != RequestStatus.New)
        {
            TempData["Error"] = "Заявка уже обработана или не является клиентской.";
            return RedirectToAction(nameof(Details), new { id = input.RequestId });
        }
        if (!await db.Tariffs.AnyAsync(x => x.Id == input.TariffId && x.IsActive))
            ModelState.AddModelError(nameof(input.TariffId), "Выберите активный тариф.");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Выберите активный тариф и укажите стоимость от 0 до 1 000 000 000 ₽.";
            return RedirectToAction(nameof(Details), new { id = input.RequestId });
        }

        request.TariffId = input.TariffId;
        request.EstimatedCost = input.EstimatedCost;
        request.Status = RequestStatus.Approved;
        db.Add(new RequestStatusHistory
        {
            ServiceRequestId = request.Id,
            FromStatus = RequestStatus.New,
            ToStatus = RequestStatus.Approved,
            ChangedByName = User.Identity?.Name ?? "Сотрудник",
            Comment = "Назначены тариф и стоимость, заявка согласована"
        });
        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("RequestChanged", request.Id);
        TempData["Success"] = "Тариф и стоимость назначены, заявка согласована.";
        return RedirectToAction(nameof(Details), new { id = request.Id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ChangeStatus(Guid id, RequestStatus to, string? comment)
    {
        var request = await db.ServiceRequests.Include(x => x.WorkOrder!).ThenInclude(x => x.Vehicles).FirstOrDefaultAsync(x => x.Id == id);
        if (request is null) return NotFound();
        if (request.Source == RequestSource.Client && request.Status == RequestStatus.New && to == RequestStatus.Approved
            && (request.TariffId is null || request.EstimatedCost is null))
        {
            TempData["Error"] = "Сначала назначьте тариф и стоимость через обработку клиентской заявки.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var allowed = (request.Status == RequestStatus.New && to == RequestStatus.Approved)
            || (request.Status == RequestStatus.Completed && to == RequestStatus.Closed)
            || (to == RequestStatus.Cancelled && request.Status is RequestStatus.New or RequestStatus.Approved or RequestStatus.Assigned);
        if (!allowed || !RequestWorkflow.CanTransition(request.Status, to))
        {
            TempData["Error"] = "Такой переход статуса недопустим.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var from = request.Status;
        request.Status = to;
        if (to == RequestStatus.Cancelled && request.WorkOrder is not null)
        {
            request.WorkOrder.Status = WorkOrderStatus.Cancelled;
            foreach (var assignment in request.WorkOrder.Vehicles)
            {
                var vehicle = await db.Vehicles.FindAsync(assignment.VehicleId);
                var hasOtherAssignments = await db.WorkOrderVehicles.AnyAsync(x => x.VehicleId == assignment.VehicleId && x.WorkOrderId != request.WorkOrder.Id && (x.WorkOrder.Status == WorkOrderStatus.Planned || x.WorkOrder.Status == WorkOrderStatus.InProgress));
                if (vehicle?.Status == VehicleStatus.Assigned && !hasOtherAssignments) vehicle.Status = VehicleStatus.Available;
            }
        }
        db.Add(new RequestStatusHistory { ServiceRequestId = id, FromStatus = from, ToStatus = to, ChangedByName = User.Identity?.Name ?? "", Comment = comment });
        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("RequestChanged", id);
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task SetLists()
    {
        ViewBag.Objects = new SelectList(await db.ServiceObjects.Where(x => x.IsActive).Include(x => x.Client).OrderBy(x => x.Name).Select(x => new { x.Id, Name = x.Name + " — " + x.Client.Name }).ToListAsync(), "Id", "Name");
        ViewBag.Tariffs = new SelectList(await db.Tariffs.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Estimates = new SelectList(await db.CostEstimates.OrderByDescending(x => x.CreatedAtUtc).Select(x => new { x.Id, Name = x.Object.Name + " — " + x.Total + " ₽" }).ToListAsync(), "Id", "Name");
    }
}
