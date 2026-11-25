using CleanDistrict.Application;
using CleanDistrict.Domain;
using CleanDistrict.Infrastructure;
using CleanDistrict.Web.Helpers;
using CleanDistrict.Web.Hubs;
using CleanDistrict.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanDistrict.Web.Controllers;

[Authorize(Roles = AppRoles.Client)]
public class PortalController(CleanDistrictDbContext db, UserManager<ApplicationUser> userManager, IHubContext<DispatchHub> hub) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await CurrentUser();
        if (user?.ClientId is not Guid clientId) return Forbid();
        var requests = await db.ServiceRequests.AsNoTracking().Include(x => x.Object)
            .Where(x => x.Object.ClientId == clientId)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
        ViewBag.ClientName = (await db.Clients.FindAsync(clientId))?.Name ?? "Моя организация";
        return View(requests);
    }

    public async Task<IActionResult> Create()
    {
        var user = await CurrentUser();
        if (user?.ClientId is not Guid clientId) return Forbid();
        await SetObjects(clientId);
        return View(new ClientRequestInputModel { ContactName = user.FullName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientRequestInputModel input)
    {
        var user = await CurrentUser();
        if (user?.ClientId is not Guid clientId) return Forbid();
        if (!await db.ServiceObjects.AnyAsync(x => x.Id == input.ObjectId && x.ClientId == clientId && x.IsActive && x.Client.IsActive))
            ModelState.AddModelError(nameof(input.ObjectId), "Выберите активный объект своей организации.");
        if (!ModelState.IsValid)
        {
            await SetObjects(clientId);
            return View(input);
        }

        var request = new ServiceRequest
        {
            Number = $"З-{DateTime.Now.Year}/{(await db.ServiceRequests.CountAsync() + 1):D4}",
            ObjectId = input.ObjectId,
            Source = RequestSource.Client,
            RequestedService = input.RequestedService,
            Description = input.Description,
            DesiredDateUtc = TimeConverter.ToUtc(input.DesiredDateLocal),
            ContactName = input.ContactName,
            ContactPhone = input.ContactPhone,
            Priority = RequestPriority.Normal,
            CreatedByUserId = user.Id,
            CreatedByName = user.FullName
        };
        db.Add(request);
        db.Add(new RequestStatusHistory
        {
            ServiceRequest = request,
            FromStatus = RequestStatus.New,
            ToStatus = RequestStatus.New,
            ChangedByName = user.FullName,
            Comment = "Заявка отправлена клиентом"
        });
        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("RequestChanged", request.Id);
        TempData["Success"] = $"Заявка {request.Number} отправлена. Сотрудник назначит тариф и согласует стоимость.";
        return RedirectToAction(nameof(Details), new { request.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var user = await CurrentUser();
        if (user?.ClientId is not Guid clientId) return Forbid();
        var request = await db.ServiceRequests.AsNoTracking().Include(x => x.Object).Include(x => x.Tariff)
            .Include(x => x.History.OrderByDescending(x => x.ChangedAtUtc))
            .FirstOrDefaultAsync(x => x.Id == id && x.Object.ClientId == clientId);
        return request is null ? NotFound() : View(request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string? comment)
    {
        var user = await CurrentUser();
        if (user?.ClientId is not Guid clientId) return Forbid();
        var request = await db.ServiceRequests.Include(x => x.Object)
            .FirstOrDefaultAsync(x => x.Id == id && x.Object.ClientId == clientId);
        if (request is null) return NotFound();
        if (request.Source != RequestSource.Client || request.Status != RequestStatus.New)
        {
            TempData["Error"] = "Отменить можно только свою новую заявку.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (string.IsNullOrWhiteSpace(comment))
        {
            TempData["Error"] = "Укажите причину отмены.";
            return RedirectToAction(nameof(Details), new { id });
        }

        request.Status = RequestStatus.Cancelled;
        db.Add(new RequestStatusHistory
        {
            ServiceRequestId = request.Id,
            FromStatus = RequestStatus.New,
            ToStatus = RequestStatus.Cancelled,
            ChangedByName = user.FullName,
            Comment = comment.Trim()
        });
        await db.SaveChangesAsync();
        await hub.Clients.All.SendAsync("RequestChanged", request.Id);
        TempData["Success"] = "Заявка отменена.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<ApplicationUser?> CurrentUser() => await userManager.GetUserAsync(User);

    private async Task SetObjects(Guid clientId) => ViewBag.Objects = new SelectList(
        await db.ServiceObjects.Where(x => x.ClientId == clientId && x.IsActive && x.Client.IsActive)
            .OrderBy(x => x.Name).Select(x => new { x.Id, Name = x.Name + " — " + x.Address }).ToListAsync(),
        "Id", "Name");
}
