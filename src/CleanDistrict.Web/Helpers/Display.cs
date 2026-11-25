using CleanDistrict.Domain;

namespace CleanDistrict.Web.Helpers;

public static class Display
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Minsk");

    public static string DateTime(DateTime value) => TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.SpecifyKind(value, DateTimeKind.Utc), Zone).ToString("dd.MM.yyyy HH:mm");
    public static string Date(DateOnly value) => value.ToString("dd.MM.yyyy");
    public static string Money(decimal value) => value.ToString("N2", new System.Globalization.CultureInfo("ru-RU")) + " ₽";
    public static string Money(decimal? value) => value.HasValue ? Money(value.Value) : "Не назначена";

    public static string Text(ContractStatus value) => value switch { ContractStatus.Draft => "Черновик", ContractStatus.Active => "Действует", ContractStatus.Expired => "Истёк", _ => "Закрыт" };
    public static string Text(RequestSource value) => value == RequestSource.Client ? "От клиента" : "От сотрудника";
    public static string Text(RequestPriority value) => value switch { RequestPriority.Normal => "Обычная", RequestPriority.High => "Высокий", _ => "Аварийная" };
    public static string Text(RequestStatus value) => value switch { RequestStatus.New => "Новая", RequestStatus.Approved => "Согласована", RequestStatus.Assigned => "Назначена", RequestStatus.InProgress => "В работе", RequestStatus.Completed => "Выполнена", RequestStatus.Closed => "Закрыта", _ => "Отменена" };
    public static string Text(WorkOrderStatus value) => value switch { WorkOrderStatus.Planned => "Запланирован", WorkOrderStatus.InProgress => "В работе", WorkOrderStatus.Completed => "Выполнен", _ => "Отменён" };
    public static string Text(VehicleType value) => value switch { VehicleType.CombinedRoadMachine => "КДМ", VehicleType.DumpTruck => "Самосвал", VehicleType.Tractor => "Трактор", _ => "Другая техника" };
    public static string Text(VehicleStatus value) => value switch { VehicleStatus.Available => "Доступна", VehicleStatus.Assigned => "Назначена", VehicleStatus.Maintenance => "На ТО", VehicleStatus.Repair => "В ремонте", _ => "Списана" };
    public static string Text(MaintenanceStatus value) => value switch { MaintenanceStatus.Planned => "Запланировано", MaintenanceStatus.InProgress => "Выполняется", _ => "Завершено" };

    public static string Badge(RequestStatus value) => value switch { RequestStatus.New => "text-bg-primary", RequestStatus.Approved => "text-bg-info", RequestStatus.Assigned => "text-bg-warning", RequestStatus.InProgress => "text-bg-warning", RequestStatus.Completed => "text-bg-success", RequestStatus.Closed => "text-bg-secondary", _ => "text-bg-dark" };
    public static string Badge(VehicleStatus value) => value switch { VehicleStatus.Available => "text-bg-success", VehicleStatus.Assigned => "text-bg-info", VehicleStatus.Maintenance => "text-bg-warning", VehicleStatus.Repair => "text-bg-danger", _ => "text-bg-secondary" };
}
