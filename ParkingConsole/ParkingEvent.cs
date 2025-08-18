namespace ParkingSystem;

// ===== Message models =====
record ParkingEvent(
    string ParkingName,
    DateTimeOffset Timestamp,
    string LicensePlate,
    string VehicleType,
    string EventType,        // "entry" | "exit"
    bool IsMaintenance,      // false for entry/exit
    string? MaintenanceDescription // null for entry/exit
);