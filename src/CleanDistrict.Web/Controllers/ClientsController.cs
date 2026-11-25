using CleanDistrict.Application;
using CleanDistrict.Domain;
using CleanDistrict.Infrastructure;
using CleanDistrict.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CleanDistrict.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Manager + "," + AppRoles.Director)]
public class ClientsController(CleanDistrictDbContext db) : Controller
{
    private const string WriteRoles = AppRoles.Administrator + "," + AppRoles.Manager;

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var query = db.Clients.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search) || x.ContactName.Contains(search));
        page = Math.Max(1, page);
        var count = await query.CountAsync();
        var items = await query.OrderBy(x => x.Name).Skip((page - 1) * 10).Take(10).ToListAsync();
        ViewBag.Search = search;
        return View(new PagedList<Client> { Page = page, TotalPages = Math.Max(1, (int)Math.Ceiling(count / 10d)), TotalCount = count }.Also(x => x.AddRange(items)));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var client = await db.Clients.Include(x => x.Objects).ThenInclude(x => x.Manager)
            .Include(x => x.Contracts).FirstOrDefaultAsync(x => x.Id == id);
        if (client is null) return NotFound();
        client.Objects = client.Objects.Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
        client.Contracts = client.Contracts.OrderByDescending(x => x.StartDate).ToList();
        return View(client);
    }

    [Authorize(Roles = WriteRoles)] public IActionResult Create() => View(new Client());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Create([Bind("Name,ContactName,Phone,Email,Address")] Client client)
    {
        if (!ModelState.IsValid) return View(client);
        db.Add(client);
        await db.SaveChangesAsync();
        TempData["Success"] = "Клиент добавлен.";
        return RedirectToAction(nameof(Details), new { client.Id });
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Edit(Guid id) => await db.Clients.FindAsync(id) is { } client ? View(client) : NotFound();

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Edit(Guid id, [Bind("Name,ContactName,Phone,Email,Address")] Client input)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return NotFound();
        if (!ModelState.IsValid) return View(input);
        client.Name = input.Name; client.ContactName = input.ContactName; client.Phone = input.Phone; client.Email = input.Email; client.Address = input.Address;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return NotFound();
        client.IsActive = false;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CreateObject(Guid clientId)
    {
        if (await db.Clients.FindAsync(clientId) is null) return NotFound();
        await SetManagers();
        return View(new ServiceObject { ClientId = clientId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CreateObject([Bind("Name,Address,AreaSquareMeters,ClientId,ManagerId,Notes")] ServiceObject serviceObject)
    {
        ValidateObject(serviceObject);
        if (!ModelState.IsValid) { await SetManagers(); return View(serviceObject); }
        db.Add(serviceObject);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = serviceObject.ClientId });
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> EditObject(Guid id)
    {
        var serviceObject = await db.ServiceObjects.FindAsync(id);
        if (serviceObject is null) return NotFound();
        await SetManagers();
        return View(serviceObject);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> EditObject(Guid id, [Bind("Name,Address,AreaSquareMeters,ClientId,ManagerId,Notes")] ServiceObject input)
    {
        var serviceObject = await db.ServiceObjects.FindAsync(id);
        if (serviceObject is null) return NotFound();
        ValidateObject(input);
        if (!ModelState.IsValid) { await SetManagers(); return View(input); }
        serviceObject.Name = input.Name; serviceObject.Address = input.Address; serviceObject.AreaSquareMeters = input.AreaSquareMeters;
        serviceObject.ManagerId = input.ManagerId; serviceObject.Notes = input.Notes;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = serviceObject.ClientId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> DeactivateObject(Guid id)
    {
        var serviceObject = await db.ServiceObjects.FindAsync(id);
        if (serviceObject is null) return NotFound();
        serviceObject.IsActive = false;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = serviceObject.ClientId });
    }

    private void ValidateObject(ServiceObject serviceObject)
    {
        if (!db.Clients.Any(x => x.Id == serviceObject.ClientId && x.IsActive)) ModelState.AddModelError(nameof(serviceObject.ClientId), "Клиент не найден.");
        if (!db.Employees.Any(x => x.Id == serviceObject.ManagerId && x.IsActive)) ModelState.AddModelError(nameof(serviceObject.ManagerId), "Менеджер не найден.");
    }

    private async Task SetManagers() => ViewBag.Managers = new SelectList(await db.Employees.Where(x => x.IsActive && x.Position.Contains("менеджер")).OrderBy(x => x.FullName).ToListAsync(), "Id", "FullName");
}

internal static class CollectionExtensions
{
    public static T Also<T>(this T value, Action<T> action) { action(value); return value; }
}
