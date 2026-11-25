namespace CleanDistrict.Application;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Dispatcher = "Dispatcher";
    public const string CrewLeader = "CrewLeader";
    public const string Mechanic = "Mechanic";
    public const string Director = "Director";
    public const string Client = "Client";
    public const string Staff = Administrator + "," + Manager + "," + Dispatcher + "," + CrewLeader + "," + Mechanic + "," + Director;

    public static readonly string[] All = [Administrator, Manager, Dispatcher, CrewLeader, Mechanic, Director, Client];

    public static string DisplayName(string role) => role switch
    {
        Administrator => "Администратор",
        Manager => "Менеджер",
        Dispatcher => "Диспетчер",
        CrewLeader => "Бригадир",
        Mechanic => "Механик",
        Director => "Руководитель",
        Client => "Клиент",
        _ => role
    };
}
