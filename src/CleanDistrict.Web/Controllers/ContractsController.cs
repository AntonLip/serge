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
public class ContractsController(CleanDistrictDbContext db, IFileStorage files) : Controller
{
    private const string WriteRoles = AppRoles.Administrator + "," + AppRoles.Manager;
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];

    public async Task<IActionResult> Index(string? search, ContractStatus? status, int page = 1)
    {
        var query = db.Contracts.AsNoTracking().Include(x => x.Client).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Number.Contains(search) || x.Client.Name.Contains(search));
        if (status.HasValue) query = query.Where(x => x.Status == status);
        page = Math.Max(1, page);
        var count = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.StartDate).Skip((page - 1) * 10).Take(10).ToListAsync();
        ViewBag.Search = search;
        ViewBag.Statuses = new SelectList(Enum.GetValues<ContractStatus>().Select(x => new { Value = x, Text = DisplayName(x) }), "Value", "Text", status);
        var model = new PagedList<Contract> { Page = page, TotalPages = Math.Max(1, (int)Math.Ceiling(count / 10d)), TotalCount = count };
        model.AddRange(items);
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var contract = await db.Contracts.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id);
        return contract is null ? NotFound() : View(contract);
    }

    [Authorize(Roles = WriteRoles)] public async Task<IActionResult> Create() { await SetClients(); return View(new Contract { StartDate = DateOnly.FromDateTime(DateTime.Now), EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(1)) }); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Create([Bind("Number,ClientId,StartDate,EndDate,Status,TotalAmount,Services")] Contract contract, IFormFile? file)
    {
        await Validate(contract, file);
        if (!ModelState.IsValid) { await SetClients(); return View(contract); }
        if (file is not null) await AttachFile(contract, file);
        db.Add(contract);
        await db.SaveChangesAsync();
        TempData["Success"] = "Договор зарегистрирован.";
        return RedirectToAction(nameof(Details), new { contract.Id });
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Edit(Guid id) { var contract = await db.Contracts.FindAsync(id); if (contract is null) return NotFound(); await SetClients(); return View(contract); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> Edit(Guid id, [Bind("Number,ClientId,StartDate,EndDate,Status,TotalAmount,Services")] Contract input, IFormFile? file)
    {
        var contract = await db.Contracts.FindAsync(id);
        if (contract is null) return NotFound();
        await Validate(input, file, id);
        if (!ModelState.IsValid) { await SetClients(); return View(input); }
        contract.Number = input.Number; contract.ClientId = input.ClientId; contract.StartDate = input.StartDate; contract.EndDate = input.EndDate;
        contract.Status = input.Status; contract.TotalAmount = input.TotalAmount; contract.Services = input.Services;
        if (file is not null) await AttachFile(contract, file);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Print(Guid id)
    {
        var contract = await db.Contracts.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id);
        return contract is null ? NotFound() : View(contract);
    }

    public async Task<IActionResult> Download(Guid id)
    {
        var contract = await db.Contracts.FindAsync(id);
        if (contract?.StoredFileName is null) return NotFound();
        var stream = await files.OpenReadAsync(contract.StoredFileName, HttpContext.RequestAborted);
        return stream is null ? NotFound() : File(stream, "application/octet-stream", contract.OriginalFileName ?? "contract");
    }

    private async Task Validate(Contract contract, IFormFile? file, Guid? id = null)
    {
        if (!await db.Clients.AnyAsync(x => x.Id == contract.ClientId && x.IsActive)) ModelState.AddModelError(nameof(contract.ClientId), "Выберите клиента.");
        if (contract.EndDate < contract.StartDate) ModelState.AddModelError(nameof(contract.EndDate), "Дата окончания не может быть раньше даты начала.");
        if (await db.Contracts.AnyAsync(x => x.Number == contract.Number && x.Id != id)) ModelState.AddModelError(nameof(contract.Number), "Договор с таким номером уже существует.");
        if (file is { Length: > 10 * 1024 * 1024 }) ModelState.AddModelError("file", "Размер файла не должен превышать 10 МБ.");
        if (file is not null && !AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant())) ModelState.AddModelError("file", "Допустимы PDF, JPG и PNG.");
    }

    private async Task AttachFile(Contract contract, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        contract.StoredFileName = await files.SaveAsync(stream, Path.GetExtension(file.FileName));
        contract.OriginalFileName = Path.GetFileName(file.FileName);
    }

    private async Task SetClients() => ViewBag.Clients = new SelectList(await db.Clients.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
    private static string DisplayName(ContractStatus value) => value switch { ContractStatus.Draft => "Черновик", ContractStatus.Active => "Действует", ContractStatus.Expired => "Истёк", _ => "Закрыт" };
}
