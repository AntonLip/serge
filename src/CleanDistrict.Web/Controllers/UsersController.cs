using CleanDistrict.Application;
using CleanDistrict.Infrastructure;
using CleanDistrict.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CleanDistrict.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class UsersController(UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var users = await userManager.Users.Where(x => x.ClientId == null).OrderBy(x => x.FullName).ToListAsync();
        var model = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            model.Add(new UserListItemViewModel { Id = user.Id, FullName = user.FullName, Email = user.Email ?? "", Roles = string.Join(", ", roles.Select(AppRoles.DisplayName)), IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow });
        }
        return View(model);
    }

    public IActionResult Create()
    {
        SetRoles();
        return View(new CreateUserViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!AppRoles.All.Where(x => x != AppRoles.Client).Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Выберите допустимую роль.");
        if (!ModelState.IsValid) { SetRoles(); return View(model); }
        var user = new ApplicationUser { FullName = model.FullName, Email = model.Email, UserName = model.Email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            SetRoles();
            return View(model);
        }
        await userManager.AddToRoleAsync(user, model.Role);
        TempData["Success"] = "Пользователь создан.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockout(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (user.Id == userManager.GetUserId(User))
        {
            TempData["Error"] = "Нельзя заблокировать текущую учётную запись.";
            return RedirectToAction(nameof(Index));
        }
        var locked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        await userManager.SetLockoutEndDateAsync(user, locked ? null : DateTimeOffset.UtcNow.AddYears(100));
        return RedirectToAction(nameof(Index));
    }

    private void SetRoles() => ViewBag.Roles = new SelectList(AppRoles.All.Where(x => x != AppRoles.Client).Select(x => new { Value = x, Text = AppRoles.DisplayName(x) }), "Value", "Text");
}
