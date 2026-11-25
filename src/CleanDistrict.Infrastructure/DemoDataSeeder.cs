using CleanDistrict.Application;
using CleanDistrict.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanDistrict.Infrastructure;

public static class DemoDataSeeder
{
    public const string DemoPassword = "Demo2026!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<CleanDistrictDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var accounts = new[]
        {
            (AppRoles.Administrator, "admin@clean-district.demo", "Администратор системы"),
            (AppRoles.Manager, "manager@clean-district.demo", "Ирина Соколова"),
            (AppRoles.Dispatcher, "dispatcher@clean-district.demo", "Олег Смирнов"),
            (AppRoles.CrewLeader, "crew@clean-district.demo", "Андрей Козлов"),
            (AppRoles.Mechanic, "mechanic@clean-district.demo", "Павел Орлов"),
            (AppRoles.Director, "director@clean-district.demo", "Марина Волкова")
        };

        foreach (var (role, email, fullName) in accounts)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
                await userManager.CreateAsync(user, DemoPassword);
            }
            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
        }

        if (await db.Clients.AnyAsync())
        {
            await EnsureClientAccount(Id(101));
            return;
        }

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var manager = new Employee { Id = Id(1), FullName = "Ирина Соколова", Position = "Персональный менеджер", Phone = "+375 29 000-01-01", UserId = await UserId("manager@clean-district.demo") };
        var dispatcher = new Employee { Id = Id(2), FullName = "Олег Смирнов", Position = "Диспетчер", Phone = "+375 29 000-01-02", UserId = await UserId("dispatcher@clean-district.demo") };
        var mechanic = new Employee { Id = Id(3), FullName = "Павел Орлов", Position = "Механик", Phone = "+375 29 000-01-03", UserId = await UserId("mechanic@clean-district.demo") };
        var director = new Employee { Id = Id(4), FullName = "Марина Волкова", Position = "Руководитель", Phone = "+375 29 000-01-04", UserId = await UserId("director@clean-district.demo") };
        var crew1 = new Crew { Id = Id(11), Name = "Бригада №1", Specialization = "Механизированная уборка" };
        var crew2 = new Crew { Id = Id(12), Name = "Бригада №2", Specialization = "Ручная уборка территорий" };
        var crew3 = new Crew { Id = Id(13), Name = "Ремонтная бригада", Specialization = "Ямочный и дорожный ремонт" };
        var leader1 = new Employee { Id = Id(5), FullName = "Андрей Козлов", Position = "Бригадир, водитель КДМ", Phone = "+375 29 000-01-05", UserId = await UserId("crew@clean-district.demo"), Crew = crew1 };
        var leader2 = new Employee { Id = Id(6), FullName = "Сергей Лебедев", Position = "Бригадир", Phone = "+375 29 000-01-06", Crew = crew2 };
        var leader3 = new Employee { Id = Id(7), FullName = "Дмитрий Морозов", Position = "Бригадир, водитель самосвала", Phone = "+375 29 000-01-07", Crew = crew3 };
        var worker1 = new Employee { Id = Id(8), FullName = "Иван Петров", Position = "Дорожный рабочий, тракторист", Phone = "+375 29 000-01-08", Crew = crew1 };
        var worker2 = new Employee { Id = Id(9), FullName = "Алексей Сидоров", Position = "Дорожный рабочий", Phone = "+375 29 000-01-09", Crew = crew2 };
        db.AddRange(crew1, crew2, crew3, manager, dispatcher, mechanic, director, leader1, leader2, leader3, worker1, worker2);
        await db.SaveChangesAsync();

        var clients = new[]
        {
            new Client { Id = Id(101), Name = "ООО «Городской квартал»", ContactName = "Елена Новикова", Phone = "+375 17 100-10-01", Email = "client1@example.demo", Address = "г. Минск, ул. Центральная, 10" },
            new Client { Id = Id(102), Name = "УП «Зелёный двор»", ContactName = "Николай Романов", Phone = "+375 17 100-10-02", Email = "client2@example.demo", Address = "г. Минск, пр. Независимости, 25" },
            new Client { Id = Id(103), Name = "ООО «Логистик Парк»", ContactName = "Ольга Климович", Phone = "+375 17 100-10-03", Email = "client3@example.demo", Address = "Минский р-н, промышленная зона" },
            new Client { Id = Id(104), Name = "ЖСК «Солнечный»", ContactName = "Виктор Громов", Phone = "+375 17 100-10-04", Email = "client4@example.demo", Address = "г. Минск, ул. Садовая, 4" }
        };
        db.AddRange(clients);
        var objects = new[]
        {
            new ServiceObject { Id = Id(201), Name = "Жилой комплекс «Квартал»", Address = "ул. Центральная, 10–14", AreaSquareMeters = 12500, Client = clients[0], Manager = manager, Notes = "Вывоз контейнеров по вторникам" },
            new ServiceObject { Id = Id(202), Name = "Офисный центр «Меридиан»", Address = "пр. Независимости, 25", AreaSquareMeters = 4600, Client = clients[0], Manager = manager },
            new ServiceObject { Id = Id(203), Name = "Территория «Зелёный двор»", Address = "ул. Парковая, 7", AreaSquareMeters = 18700, Client = clients[1], Manager = manager },
            new ServiceObject { Id = Id(204), Name = "Складской комплекс «Северный»", Address = "Промышленная ул., 3", AreaSquareMeters = 32000, Client = clients[2], Manager = manager },
            new ServiceObject { Id = Id(205), Name = "Внутридворовые проезды ЖСК", Address = "ул. Садовая, 4–8", AreaSquareMeters = 7800, Client = clients[3], Manager = manager },
            new ServiceObject { Id = Id(206), Name = "Парковка «Логистик Парк»", Address = "Логистическая ул., 1", AreaSquareMeters = 15300, Client = clients[2], Manager = manager }
        };
        db.AddRange(objects);
        db.AddRange(
            new Contract { Id = Id(301), Number = "Д-2026/001", Client = clients[0], StartDate = today.AddMonths(-3), EndDate = today.AddMonths(9), Status = ContractStatus.Active, TotalAmount = 180000, Services = "Уборка территории, вывоз снега, обработка проездов" },
            new Contract { Id = Id(302), Number = "Д-2026/002", Client = clients[1], StartDate = today.AddMonths(-2), EndDate = today.AddDays(20), Status = ContractStatus.Active, TotalAmount = 96000, Services = "Ручная и механизированная уборка" },
            new Contract { Id = Id(303), Number = "Д-2026/003", Client = clients[2], StartDate = today.AddMonths(-1), EndDate = today.AddMonths(11), Status = ContractStatus.Active, TotalAmount = 250000, Services = "Комплексное содержание складской территории" },
            new Contract { Id = Id(304), Number = "Д-2025/014", Client = clients[3], StartDate = today.AddYears(-1), EndDate = today.AddDays(-15), Status = ContractStatus.Expired, TotalAmount = 72000, Services = "Сезонное обслуживание внутридворовых проездов" });

        var tariffs = new[]
        {
            new Tariff { Id = Id(401), Name = "Механизированная уборка", ServiceType = "Уборка", Unit = "м²", BasePrice = 180, AreaRate = 0.035m, CrewHourlyRate = 55, VehicleHourlyRate = 95, MaterialCost = 20, UrgencyCoefficient = 1.4m },
            new Tariff { Id = Id(402), Name = "Ручная уборка территории", ServiceType = "Уборка", Unit = "м²", BasePrice = 120, AreaRate = 0.055m, CrewHourlyRate = 45, VehicleHourlyRate = 0, MaterialCost = 15, UrgencyCoefficient = 1.35m },
            new Tariff { Id = Id(403), Name = "Уборка и вывоз снега", ServiceType = "Зимнее содержание", Unit = "м²", BasePrice = 250, AreaRate = 0.06m, CrewHourlyRate = 60, VehicleHourlyRate = 110, MaterialCost = 35, UrgencyCoefficient = 1.6m },
            new Tariff { Id = Id(404), Name = "Противогололёдная обработка", ServiceType = "Зимнее содержание", Unit = "м²", BasePrice = 150, AreaRate = 0.04m, CrewHourlyRate = 45, VehicleHourlyRate = 90, MaterialCost = 60, UrgencyCoefficient = 1.5m },
            new Tariff { Id = Id(405), Name = "Ямочный ремонт", ServiceType = "Дорожный ремонт", Unit = "м²", BasePrice = 300, AreaRate = 8.5m, CrewHourlyRate = 70, VehicleHourlyRate = 105, MaterialCost = 120, UrgencyCoefficient = 1.5m },
            new Tariff { Id = Id(406), Name = "Погрузка и вывоз мусора", ServiceType = "Вывоз", Unit = "рейс", BasePrice = 220, AreaRate = 0, CrewHourlyRate = 50, VehicleHourlyRate = 100, MaterialCost = 25, UrgencyCoefficient = 1.4m },
            new Tariff { Id = Id(407), Name = "Полив и мойка дорожных покрытий", ServiceType = "Летнее содержание", Unit = "м²", BasePrice = 160, AreaRate = 0.025m, CrewHourlyRate = 45, VehicleHourlyRate = 90, MaterialCost = 10, UrgencyCoefficient = 1.3m },
            new Tariff { Id = Id(408), Name = "Аварийный выезд бригады", ServiceType = "Аварийные работы", Unit = "выезд", BasePrice = 350, AreaRate = 0, CrewHourlyRate = 75, VehicleHourlyRate = 120, MaterialCost = 50, UrgencyCoefficient = 1.8m }
        };
        db.AddRange(tariffs);
        await db.SaveChangesAsync();

        var estimateInput = new PricingInput(1200, 8, 4, 80, false);
        var estimateResult = PricingCalculator.Calculate(tariffs[0], estimateInput);
        var estimate = new CostEstimate
        {
            Id = Id(501), Object = objects[0], Tariff = tariffs[0], Area = estimateInput.Area,
            CrewHours = estimateInput.CrewHours, VehicleHours = estimateInput.VehicleHours,
            MaterialsCost = estimateInput.MaterialsCost, Subtotal = estimateResult.Subtotal,
            VatRate = tariffs[0].VatRate, VatAmount = estimateResult.VatAmount, Total = estimateResult.Total,
            Items = estimateResult.Lines.Select(x => new CostEstimateItem { Description = x.Description, Quantity = x.Quantity, Unit = x.Unit, UnitPrice = x.UnitPrice, Amount = x.Amount }).ToList()
        };
        db.Add(estimate);

        var vehicles = new[]
        {
            new Vehicle { Id = Id(601), Name = "КДМ МАЗ-5903", RegistrationNumber = "1234 АК-7", Type = VehicleType.CombinedRoadMachine, Status = VehicleStatus.Assigned, Mileage = 84500, EngineHours = 6200, Driver = leader1, NextMaintenanceDate = today.AddDays(12), NextMaintenanceMileage = 90000, NextMaintenanceEngineHours = 6500 },
            new Vehicle { Id = Id(602), Name = "Самосвал МАЗ-6501", RegistrationNumber = "2345 ВК-7", Type = VehicleType.DumpTruck, Status = VehicleStatus.Available, Mileage = 112000, EngineHours = 7400, Driver = leader3, NextMaintenanceDate = today.AddDays(-3), NextMaintenanceMileage = 115000 },
            new Vehicle { Id = Id(603), Name = "Трактор МТЗ-82", RegistrationNumber = "3456 СМ-7", Type = VehicleType.Tractor, Status = VehicleStatus.Assigned, Mileage = 58000, EngineHours = 9100, Driver = worker1, NextMaintenanceDate = today.AddDays(35), NextMaintenanceEngineHours = 9500 },
            new Vehicle { Id = Id(604), Name = "КДМ МАЗ-5903", RegistrationNumber = "4567 ЕО-7", Type = VehicleType.CombinedRoadMachine, Status = VehicleStatus.Maintenance, Mileage = 96200, EngineHours = 7100, Driver = leader1, NextMaintenanceDate = today, NextMaintenanceEngineHours = 7100 },
            new Vehicle { Id = Id(605), Name = "Самосвал КамАЗ-65115", RegistrationNumber = "5678 КР-7", Type = VehicleType.DumpTruck, Status = VehicleStatus.Repair, Mileage = 143000, EngineHours = 8600, Driver = leader3, NextMaintenanceDate = today.AddDays(50) },
            new Vehicle { Id = Id(606), Name = "Трактор МТЗ-320", RegistrationNumber = "6789 МТ-7", Type = VehicleType.Tractor, Status = VehicleStatus.Available, Mileage = 23000, EngineHours = 3200, NextMaintenanceDate = today.AddDays(60), NextMaintenanceEngineHours = 3500 }
        };
        db.AddRange(vehicles);
        db.AddRange(
            new MaintenanceRecord { Id = Id(701), Vehicle = vehicles[0], Description = "Замена масла и фильтров", PlannedDate = today.AddDays(12), Cost = 600 },
            new MaintenanceRecord { Id = Id(702), Vehicle = vehicles[1], Description = "Проверка тормозной системы", PlannedDate = today.AddDays(-3), Cost = 450 },
            new MaintenanceRecord { Id = Id(703), Vehicle = vehicles[3], Description = "Плановое ТО-2", PlannedDate = today, StartedAtUtc = now.AddHours(-2), Status = MaintenanceStatus.InProgress, Cost = 1200 },
            new Repair { Id = Id(751), Vehicle = vehicles[4], Description = "Замена гидравлического шланга подъёма кузова", StartedAtUtc = now.AddDays(-1), Parts = "Шланг РВД, масло", Cost = 480 });
        await db.SaveChangesAsync();

        var requests = new[]
        {
            NewRequest(801, "З-2026/001", objects[0], tariffs[0], "Плановая механизированная уборка придомовой территории", RequestStatus.New, RequestPriority.Normal, now.AddDays(2), 820),
            NewRequest(802, "З-2026/002", objects[1], tariffs[1], "Согласована ручная уборка после ремонтных работ", RequestStatus.Approved, RequestPriority.Normal, now.AddDays(1), 560),
            NewRequest(803, "З-2026/003", objects[2], tariffs[2], "Уборка снега на основных проездах", RequestStatus.Assigned, RequestPriority.High, now.AddHours(8), 1450),
            NewRequest(804, "З-2026/004", objects[3], tariffs[7], "Аварийная очистка проезда после разлива мусора", RequestStatus.InProgress, RequestPriority.Emergency, now.AddHours(-1), 1100),
            NewRequest(805, "З-2026/005", objects[4], tariffs[4], "Ямочный ремонт внутридворового проезда", RequestStatus.Completed, RequestPriority.Normal, now.AddHours(-4), 3200),
            NewRequest(806, "З-2026/006", objects[5], tariffs[5], "Вывоз крупногабаритного мусора", RequestStatus.Closed, RequestPriority.Normal, now.AddDays(-2), 780),
            NewRequest(807, "З-2026/007", objects[3], tariffs[6], "Просроченная мойка складской площадки", RequestStatus.Approved, RequestPriority.High, now.AddDays(-1), 900),
            NewRequest(808, "З-2026/008", objects[1], tariffs[3], "Отменённая обработка пешеходных дорожек", RequestStatus.Cancelled, RequestPriority.Normal, now.AddDays(3), 410)
        };
        requests[0].CostEstimate = estimate;
        requests[0].EstimatedCost = estimate.Total;
        db.AddRange(requests);
        await db.SaveChangesAsync();

        var orders = new[]
        {
            NewOrder(901, "Н-2026/001", requests[2], crew1, now.AddHours(4), now.AddHours(8), WorkOrderStatus.Planned),
            NewOrder(902, "Н-2026/002", requests[3], crew3, now.AddHours(-2), now.AddHours(2), WorkOrderStatus.InProgress),
            NewOrder(903, "Н-2026/003", requests[4], crew3, now.AddDays(-1).AddHours(-4), now.AddDays(-1), WorkOrderStatus.Completed),
            NewOrder(904, "Н-2026/004", requests[5], crew2, now.AddDays(-3), now.AddDays(-3).AddHours(4), WorkOrderStatus.Completed)
        };
        orders[1].ActualStartUtc = now.AddHours(-2);
        orders[2].ActualStartUtc = orders[2].PlannedStartUtc;
        orders[2].ActualEndUtc = orders[2].PlannedEndUtc;
        orders[3].ActualStartUtc = orders[3].PlannedStartUtc;
        orders[3].ActualEndUtc = orders[3].PlannedEndUtc;
        db.AddRange(orders);
        await db.SaveChangesAsync();
        db.AddRange(
            new WorkOrderVehicle { WorkOrder = orders[0], Vehicle = vehicles[2], Driver = worker1 },
            new WorkOrderVehicle { WorkOrder = orders[1], Vehicle = vehicles[0], Driver = leader1 },
            new WorkOrderVehicle { WorkOrder = orders[2], Vehicle = vehicles[1], Driver = leader3 },
            new WorkOrderVehicle { WorkOrder = orders[3], Vehicle = vehicles[5], Driver = worker1 });
        db.AddRange(
            new WorkReport { Id = Id(1001), WorkOrder = orders[2], ActualVolume = 18, LaborHours = 24, MaterialsCost = 940, Comment = "Дефектные участки обработаны, покрытие восстановлено", CompletedAtUtc = now.AddDays(-1) },
            new WorkReport { Id = Id(1002), WorkOrder = orders[3], ActualVolume = 2, LaborHours = 12, MaterialsCost = 60, Comment = "Мусор погружен и вывезен, площадка очищена", CompletedAtUtc = now.AddDays(-3) });

        foreach (var request in requests)
        {
            db.Add(new RequestStatusHistory { ServiceRequest = request, FromStatus = RequestStatus.New, ToStatus = request.Status, ChangedByName = "Демонстрационный seed", ChangedAtUtc = request.CreatedAtUtc, Comment = "Начальные демонстрационные данные" });
        }
        await db.SaveChangesAsync();
        await EnsureClientAccount(Id(101));

        async Task EnsureClientAccount(Guid clientId)
        {
            if (!await db.Clients.AnyAsync(x => x.Id == clientId)) return;
            const string email = "client@clean-district.demo";
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser { UserName = email, Email = email, FullName = "Елена Новикова", EmailConfirmed = true, ClientId = clientId };
                var result = await userManager.CreateAsync(user, DemoPassword);
                if (!result.Succeeded) throw new InvalidOperationException("Не удалось создать демонстрационный доступ клиента: " + string.Join("; ", result.Errors.Select(x => x.Description)));
            }
            if (user.ClientId != clientId)
            {
                user.ClientId = clientId;
                await userManager.UpdateAsync(user);
            }
            if (!await userManager.IsInRoleAsync(user, AppRoles.Client))
                await userManager.AddToRoleAsync(user, AppRoles.Client);
        }

        async Task<string?> UserId(string email) => (await userManager.FindByEmailAsync(email))?.Id;
    }

    private static Guid Id(int value) => new($"00000000-0000-0000-0000-{value:D12}");

    private static ServiceRequest NewRequest(int id, string number, ServiceObject serviceObject, Tariff tariff, string description, RequestStatus status, RequestPriority priority, DateTime desiredDate, decimal cost) =>
        new() { Id = Id(id), Number = number, Object = serviceObject, Tariff = tariff, Description = description, Status = status, Priority = priority, DesiredDateUtc = desiredDate, EstimatedCost = cost, CreatedByName = "Олег Смирнов" };

    private static WorkOrder NewOrder(int id, string number, ServiceRequest request, Crew crew, DateTime start, DateTime end, WorkOrderStatus status) =>
        new() { Id = Id(id), Number = number, ServiceRequest = request, Crew = crew, PlannedStartUtc = start, PlannedEndUtc = end, Status = status };
}
