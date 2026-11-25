using CleanDistrict.Application;
using CleanDistrict.Domain;
using CleanDistrict.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CleanDistrict.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class AdminController(CleanDistrictDbContext db) : Controller
{
    public IActionResult Index() => View();

    public async Task<IActionResult> Tariffs() => View(await db.Tariffs.OrderBy(x => x.Name).ToListAsync());
    public IActionResult CreateTariff() => View(new Tariff());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTariff([Bind("Name,ServiceType,Unit,BasePrice,AreaRate,CrewHourlyRate,VehicleHourlyRate,MaterialCost,UrgencyCoefficient,VatRate")] Tariff tariff)
    {
        if (!ModelState.IsValid) return View(tariff);
        db.Add(tariff); await db.SaveChangesAsync(); return RedirectToAction(nameof(Tariffs));
    }
    public async Task<IActionResult> EditTariff(Guid id) => await db.Tariffs.FindAsync(id) is { } tariff ? View(tariff) : NotFound();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTariff(Guid id, [Bind("Name,ServiceType,Unit,BasePrice,AreaRate,CrewHourlyRate,VehicleHourlyRate,MaterialCost,UrgencyCoefficient,VatRate")] Tariff input)
    {
        var tariff = await db.Tariffs.FindAsync(id); if (tariff is null) return NotFound();
        if (!ModelState.IsValid) return View(input);
        tariff.Name = input.Name; tariff.ServiceType = input.ServiceType; tariff.Unit = input.Unit; tariff.BasePrice = input.BasePrice;
        tariff.AreaRate = input.AreaRate; tariff.CrewHourlyRate = input.CrewHourlyRate; tariff.VehicleHourlyRate = input.VehicleHourlyRate;
        tariff.MaterialCost = input.MaterialCost; tariff.UrgencyCoefficient = input.UrgencyCoefficient; tariff.VatRate = input.VatRate;
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Tariffs));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTariff(Guid id)
    {
        var tariff = await db.Tariffs.FindAsync(id); if (tariff is null) return NotFound();
        tariff.IsActive = !tariff.IsActive; await db.SaveChangesAsync(); return RedirectToAction(nameof(Tariffs));
    }

    public async Task<IActionResult> Employees() => View(await db.Employees.Include(x => x.Crew).OrderBy(x => x.FullName).ToListAsync());
    public async Task<IActionResult> CreateEmployee() { await SetCrews(); return View(new Employee()); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee([Bind("FullName,Position,Phone,CrewId")] Employee employee)
    {
        if (!ModelState.IsValid) { await SetCrews(); return View(employee); }
        db.Add(employee); await db.SaveChangesAsync(); return RedirectToAction(nameof(Employees));
    }
    public async Task<IActionResult> EditEmployee(Guid id) { var employee = await db.Employees.FindAsync(id); if (employee is null) return NotFound(); await SetCrews(); return View(employee); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEmployee(Guid id, [Bind("FullName,Position,Phone,CrewId")] Employee input)
    {
        var employee = await db.Employees.FindAsync(id); if (employee is null) return NotFound();
        if (!ModelState.IsValid) { await SetCrews(); return View(input); }
        employee.FullName = input.FullName; employee.Position = input.Position; employee.Phone = input.Phone; employee.CrewId = input.CrewId;
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Employees));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEmployee(Guid id)
    {
        var employee = await db.Employees.FindAsync(id); if (employee is null) return NotFound();
        employee.IsActive = !employee.IsActive; await db.SaveChangesAsync(); return RedirectToAction(nameof(Employees));
    }

    public async Task<IActionResult> Crews() => View(await db.Crews.Include(x => x.Members).OrderBy(x => x.Name).ToListAsync());
    public IActionResult CreateCrew() => View(new Crew());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCrew([Bind("Name,Specialization")] Crew crew)
    {
        if (!ModelState.IsValid) return View(crew);
        db.Add(crew); await db.SaveChangesAsync(); return RedirectToAction(nameof(Crews));
    }
    public async Task<IActionResult> EditCrew(Guid id) => await db.Crews.FindAsync(id) is { } crew ? View(crew) : NotFound();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCrew(Guid id, [Bind("Name,Specialization")] Crew input)
    {
        var crew = await db.Crews.FindAsync(id); if (crew is null) return NotFound();
        if (!ModelState.IsValid) return View(input);
        crew.Name = input.Name; crew.Specialization = input.Specialization; await db.SaveChangesAsync(); return RedirectToAction(nameof(Crews));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCrew(Guid id)
    {
        var crew = await db.Crews.FindAsync(id); if (crew is null) return NotFound();
        crew.IsActive = !crew.IsActive; await db.SaveChangesAsync(); return RedirectToAction(nameof(Crews));
    }

    private async Task SetCrews() => ViewBag.Crews = new SelectList(await db.Crews.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
}
