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
public class EstimatesController(CleanDistrictDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.CostEstimates.AsNoTracking().Include(x => x.Object).ThenInclude(x => x.Client).Include(x => x.Tariff).OrderByDescending(x => x.CreatedAtUtc).ToListAsync());

    public async Task<IActionResult> Details(Guid id)
    {
        var estimate = await Query().FirstOrDefaultAsync(x => x.Id == id);
        return estimate is null ? NotFound() : View(estimate);
    }

    public async Task<IActionResult> Print(Guid id)
    {
        var estimate = await Query().FirstOrDefaultAsync(x => x.Id == id);
        return estimate is null ? NotFound() : View(estimate);
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Manager)]
    public async Task<IActionResult> Create(Guid? objectId)
    {
        await SetLists();
        return View(new EstimateInputModel { ObjectId = objectId ?? Guid.Empty });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Manager)]
    public async Task<IActionResult> Create(EstimateInputModel input)
    {
        if (!await db.ServiceObjects.AnyAsync(x => x.Id == input.ObjectId && x.IsActive)) ModelState.AddModelError(nameof(input.ObjectId), "Выберите объект.");
        var tariff = await db.Tariffs.FirstOrDefaultAsync(x => x.Id == input.TariffId && x.IsActive);
        if (tariff is null) ModelState.AddModelError(nameof(input.TariffId), "Выберите активный тариф.");
        if (!ModelState.IsValid) { await SetLists(); return View(input); }

        var result = PricingCalculator.Calculate(tariff!, new PricingInput(input.Area, input.CrewHours, input.VehicleHours, input.MaterialsCost, input.IsUrgent));
        var estimate = new CostEstimate
        {
            ObjectId = input.ObjectId, TariffId = input.TariffId, Area = input.Area, CrewHours = input.CrewHours,
            VehicleHours = input.VehicleHours, MaterialsCost = input.MaterialsCost, IsUrgent = input.IsUrgent,
            Subtotal = result.Subtotal, VatRate = tariff!.VatRate, VatAmount = result.VatAmount, Total = result.Total,
            Items = result.Lines.Select(x => new CostEstimateItem { Description = x.Description, Quantity = x.Quantity, Unit = x.Unit, UnitPrice = x.UnitPrice, Amount = x.Amount }).ToList()
        };
        db.Add(estimate);
        await db.SaveChangesAsync();
        TempData["Success"] = "Расчёт сохранён. Значения тарифа зафиксированы в позициях расчёта.";
        return RedirectToAction(nameof(Details), new { estimate.Id });
    }

    private IQueryable<CostEstimate> Query() => db.CostEstimates.Include(x => x.Object).ThenInclude(x => x.Client).Include(x => x.Object).ThenInclude(x => x.Manager).Include(x => x.Tariff).Include(x => x.Items);

    private async Task SetLists()
    {
        ViewBag.Objects = new SelectList(await db.ServiceObjects.Where(x => x.IsActive).Include(x => x.Client).OrderBy(x => x.Name).Select(x => new { x.Id, Name = x.Name + " — " + x.Client.Name }).ToListAsync(), "Id", "Name");
        ViewBag.Tariffs = new SelectList(await db.Tariffs.Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, Name = x.Name + " (НДС " + x.VatRate + "%)" }).ToListAsync(), "Id", "Name");
    }
}
