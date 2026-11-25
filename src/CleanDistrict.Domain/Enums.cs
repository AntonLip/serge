namespace CleanDistrict.Domain;

public enum ContractStatus { Draft, Active, Expired, Closed }
public enum RequestPriority { Normal, High, Emergency }
public enum RequestSource { Staff, Client }
public enum RequestStatus { New, Approved, Assigned, InProgress, Completed, Closed, Cancelled }
public enum WorkOrderStatus { Planned, InProgress, Completed, Cancelled }
public enum VehicleType { CombinedRoadMachine, DumpTruck, Tractor, Other }
public enum VehicleStatus { Available, Assigned, Maintenance, Repair, Decommissioned }
public enum MaintenanceStatus { Planned, InProgress, Completed }
