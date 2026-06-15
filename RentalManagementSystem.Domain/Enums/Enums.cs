namespace RentalManagementSystem.Domain.Enums;

public enum RoomStatus
{
    Available = 0,
    Occupied = 1,
    Maintenance = 2,
    Reserved = 3
}

public enum ContractStatus
{
    Active = 0,
    Expired = 1,
    Terminated = 2,
    Pending = 3
}

public enum InvoiceStatus
{
    Unpaid = 0,
    Paid = 1,
    Overdue = 2,
    Cancelled = 3,
    PartiallyPaid = 4
}

public enum UserRole
{
    Admin = 0,
    Manager = 1,
    Staff = 2
}

public enum TransactionType
{
    Income = 0,
    Expense = 1
}

public enum VehicleType
{
    Motorbike = 0,   // Xe máy
    Car = 1,         // Ô tô
    ElectricBike = 2,// Xe máy điện
    Bicycle = 3,     // Xe đạp
    Other = 4
}

public enum ServiceType
{
    Electric = 0,
    Water = 1,
    Internet = 2,
    Parking = 3,
    Cleaning = 4,
    Security = 5,
    Other = 6
}
