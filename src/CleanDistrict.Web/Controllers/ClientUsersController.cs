using CleanDistrict.Application;
using CleanDistrict.Infrastructure;
using CleanDistrict.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CleanDistrict.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Manager)]
public class ClientUsersController(CleanDistrictDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var users = await userManager.Users.AsNoTracking().Include(x => x.Client)
            .Where(x => x.ClientId != null).OrderBy(x => x.Client!.Name).ThenBy(x => x.FullName)
            .Select(x => new ClientUserListItemViewModel
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email ?? "",
                ClientName = x.Client!.Name,
                IsLockedOut = x.LockoutEnd.HasValue && x.LockoutEnd > DateTimeOffset.UtcNow
            }).ToListAsync();
        return View(users);
    }

    public async Task<IActionResult> Create()
    {
        await SetClients();
        return View(new CreateClientUserViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateClientUserViewModel model)
    {
        if (!await db.Clients.AnyAsync(x => x.Id == model.ClientId && x.IsActive))
            ModelState.AddModelError(nameof(model.ClientId), "Выберите активную организацию.");
        if (!ModelState.IsValid)
        {
            await SetClients();
            return View(model);
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new ApplicationUser
        {
            FullName = model.FullName,
            Email = model.Email,
            UserName = model.Email,
            EmailConfirmed = true,
            ClientId = model.ClientId
        };
        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded) result = await userManager.AddToRoleAsync(user, AppRoles.Client);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            await SetClients();
            return View(model);
        }

        await transaction.CommitAsync();
        TempData["Success"] = "Доступ клиента создан.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockout(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (user.ClientId is null || !await userManager.IsInRoleAsync(user, AppRoles.Client)) return BadRequest();
        var locked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        await userManager.SetLockoutEndDateAsync(user, locked ? null : DateTimeOffset.UtcNow.AddYears(100));
        TempData["Success"] = locked ? "Доступ клиента разблокирован." : "Доступ клиента заблокирован.";
        return RedirectToAction(nameof(Index));
    }

    private async Task SetClients() => ViewBag.Clients = new SelectList(
        await db.Clients.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
}
