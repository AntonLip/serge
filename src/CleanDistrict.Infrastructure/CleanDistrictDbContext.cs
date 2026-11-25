using CleanDistrict.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CleanDistrict.Infrastructure;

public class CleanDistrictDbContext(DbContextOptions<CleanDistrictDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ServiceObject> ServiceObjects => Set<ServiceObject>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<CostEstimate> CostEstimates => Set<CostEstimate>();
    public DbSet<CostEstimateItem> CostEstimateItems => Set<CostEstimateItem>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Crew> Crews => Set<Crew>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<RequestStatusHistory> RequestStatusHistories => Set<RequestStatusHistory>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderVehicle> WorkOrderVehicles => Set<WorkOrderVehicle>();
    public DbSet<WorkReport> WorkReports => Set<WorkReport>();
    public DbSet<WorkReportPhoto> WorkReportPhotos => Set<WorkReportPhoto>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<Repair> Repairs => Set<Repair>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Client>().HasIndex(x => x.Name);
        modelBuilder.Entity<ServiceObject>().HasIndex(x => x.Name);
        modelBuilder.Entity<ServiceObject>()
            .HasOne(x => x.Client).WithMany(x => x.Objects).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ServiceObject>()
            .HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Contract>().HasIndex(x => x.Number).IsUnique();
        modelBuilder.Entity<Contract>()
            .HasOne(x => x.Client).WithMany(x => x.Contracts).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CostEstimate>()
            .HasOne(x => x.Object).WithMany(x => x.Estimates).HasForeignKey(x => x.ObjectId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CostEstimate>()
            .HasOne(x => x.Tariff).WithMany().HasForeignKey(x => x.TariffId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CostEstimateItem>()
            .HasOne(x => x.CostEstimate).WithMany(x => x.Items).HasForeignKey(x => x.CostEstimateId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Employee>()
            .HasOne(x => x.Crew).WithMany(x => x.Members).HasForeignKey(x => x.CrewId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ServiceRequest>().HasIndex(x => x.Number).IsUnique();
        modelBuilder.Entity<ServiceRequest>().HasIndex(x => new { x.Status, x.DesiredDateUtc });
        modelBuilder.Entity<ServiceRequest>()
            .HasOne(x => x.Object).WithMany(x => x.Requests).HasForeignKey(x => x.ObjectId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ServiceRequest>()
            .HasOne(x => x.Tariff).WithMany().HasForeignKey(x => x.TariffId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ServiceRequest>()
            .HasOne(x => x.CostEstimate).WithMany().HasForeignKey(x => x.CostEstimateId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RequestStatusHistory>()
            .HasOne(x => x.ServiceRequest).WithMany(x => x.History).HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkOrder>().HasIndex(x => x.Number).IsUnique();
        modelBuilder.Entity<WorkOrder>().HasIndex(x => x.ServiceRequestId).IsUnique();
        modelBuilder.Entity<WorkOrder>()
            .HasOne(x => x.ServiceRequest).WithOne(x => x.WorkOrder)
            .HasForeignKey<WorkOrder>(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WorkOrder>()
            .HasOne(x => x.Crew).WithMany().HasForeignKey(x => x.CrewId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkOrderVehicle>().HasKey(x => new { x.WorkOrderId, x.VehicleId });
        modelBuilder.Entity<WorkOrderVehicle>()
            .HasOne(x => x.WorkOrder).WithMany(x => x.Vehicles).HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WorkOrderVehicle>()
            .HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkOrderVehicle>()
            .HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkReport>().HasIndex(x => x.WorkOrderId).IsUnique();
        modelBuilder.Entity<WorkReport>()
            .HasOne(x => x.WorkOrder).WithOne(x => x.Report)
            .HasForeignKey<WorkReport>(x => x.WorkOrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WorkReportPhoto>()
            .HasOne(x => x.WorkReport).WithMany(x => x.Photos).HasForeignKey(x => x.WorkReportId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vehicle>().HasIndex(x => x.RegistrationNumber).IsUnique();
        modelBuilder.Entity<Vehicle>()
            .HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<MaintenanceRecord>()
            .HasOne(x => x.Vehicle).WithMany(x => x.MaintenanceRecords).HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Repair>()
            .HasOne(x => x.Vehicle).WithMany(x => x.Repairs).HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Cascade);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(x => x.GetProperties())
                     .Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }
    }
}
