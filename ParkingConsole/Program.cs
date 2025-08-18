using System.Text;
using System.Text.Json;
using Microsoft.Azure.Devices.Client;
using ParkingSystem;
using DotNetEnv;

Env.Load();

var deviceConn = Environment.GetEnvironmentVariable("DEVICE_CONN");

// var deviceConn = "HostName=parkingiothub.azure-devices.net;DeviceId=parking-console-01;SharedAccessKey=gKASyXoyfOBMfvgq7wDwXI1Urs4pX7QHC2qMLWsuhd0=";

if (string.IsNullOrWhiteSpace(deviceConn))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: DEVICE_CONN environment variable not set.");
    Console.ResetColor();
    Console.WriteLine("Set it to your device connection string, then re-run.");
    return;
}

// ===== JSON options =====
var jsonOpts = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};

// ===== Device client =====
using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConn, TransportType.Mqtt);
await deviceClient.OpenAsync();
Console.WriteLine("Connected to IoT Hub.\n");

// ===== Simple types =====
string ReadNonEmpty(string prompt, string? defaultValue = null)
{
    while (true)
    {
        Console.Write(defaultValue is null ? $"{prompt}: " : $"{prompt} [{defaultValue}]: ");
        var s = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(s))
        {
            if (defaultValue is not null) return defaultValue;
            Console.WriteLine("  Please enter a value.");
            continue;
        }
        return s.Trim();
    }
}

string ReadChoice(string prompt, params (string Key, string Label)[] choices)
{
    Console.WriteLine(prompt);
    foreach (var (key, label) in choices)
        Console.WriteLine($"  {key}) {label}");
    while (true)
    {
        Console.Write("Choose: ");
        var pick = Console.ReadLine()?.Trim();
        if (pick is null) continue;
        foreach (var (key, label) in choices)
            if (string.Equals(pick, key, StringComparison.OrdinalIgnoreCase))
                return key;
        Console.WriteLine("  Invalid choice. Try again.");
    }
}

bool TryReadMenuPick(out int pick)
{
    Console.WriteLine();
    Console.WriteLine("===== Smart Parking Simulator =====");
    Console.WriteLine("1. Send ENTRY message");
    Console.WriteLine("2. Send EXIT message");
    Console.WriteLine("3. Send MAINTENANCE message");
    Console.WriteLine("4. Exit");
    Console.Write("Pick: ");
    var s = Console.ReadLine();
    return int.TryParse(s, out pick);
}

string ReadVehicleType()
{
    var choice = ReadChoice("Vehicle type?",
        ("1", "car"),
        ("2", "van"),
        ("3", "truck")
    );
    return choice switch
    {
        "1" => "car",
        "2" => "van",
        "3" => "truck",
        _ => "car"
    };
}

string ReadMaintenanceDescription()
{
    var choice = ReadChoice("Maintenance description?",
        ("1", "gate malfunction"),
        ("2", "payment machine broken"),
        ("3", "cleaning required"),
        ("4", "lighting broken")
    );
    return choice switch
    {
        "1" => "gate malfunction",
        "2" => "payment machine broken",
        "3" => "cleaning required",
        "4" => "lighting broken",
        _ => "gate malfunction"
    };
}

string DefaultPlateIfBlank(string plate)
{
    if (!string.IsNullOrWhiteSpace(plate)) return plate.Trim().ToUpperInvariant();
    // Generate a simple fake plate if user leaves it blank, e.g. "1-ABC-234"
    var rnd = new Random();
    string Letters(int n) => new string(Enumerable.Range(0, n).Select(_ => (char)('A' + rnd.Next(0, 26))).ToArray());
    return $"{rnd.Next(1,10)}-{Letters(3)}-{rnd.Next(100, 999)}";
}


async Task SendMessageAsync(ParkingEvent evt)
{
    var payload = JsonSerializer.Serialize(evt, jsonOpts);
    using var msg = new Message(Encoding.UTF8.GetBytes(payload))
    {
        ContentType = "application/json",
        ContentEncoding = "utf-8",
        MessageId = Guid.NewGuid().ToString()
    };

    // Application properties (great for routing)
    var messageType = evt.IsMaintenance ? "maintenance" : evt.EventType; // entry | exit | maintenance
    msg.Properties["messageType"] = messageType;
    msg.Properties["isMaintenance"] = evt.IsMaintenance ? "true" : "false";
    msg.Properties["parkingName"] = evt.ParkingName;

    await deviceClient.SendEventAsync(msg);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"> Sent {messageType.ToUpper()} message:");
    Console.ResetColor();
    Console.WriteLine(payload);
    Console.WriteLine();
}

while (true)
{
    if (!TryReadMenuPick(out var pick)) continue;

    if (pick == 4)
    {
        Console.WriteLine("Exiting…");
        break;
    }

    var parkingName = ReadNonEmpty("Parking name", "P1");
    var timestamp = DateTimeOffset.UtcNow;

    if (pick == 1) // ENTRY
    {
        var plate = DefaultPlateIfBlank(ReadNonEmpty("Car license plate (leave blank to auto-generate)", ""));
        var vehicleType = ReadVehicleType();

        var evt = new ParkingEvent(
            ParkingName: parkingName,
            Timestamp: timestamp,
            LicensePlate: plate,
            VehicleType: vehicleType,
            EventType: "entry",
            IsMaintenance: false,
            MaintenanceDescription: null
        );

        await SendMessageAsync(evt);
    }
    else if (pick == 2) // EXIT
    {
        var plate = DefaultPlateIfBlank(ReadNonEmpty("Car license plate (leave blank to auto-generate)", ""));
        var vehicleType = ReadVehicleType();

        var evt = new ParkingEvent(
            ParkingName: parkingName,
            Timestamp: timestamp,
            LicensePlate: plate,
            VehicleType: vehicleType,
            EventType: "exit",
            IsMaintenance: false,
            MaintenanceDescription: null
        );

        await SendMessageAsync(evt);
    }
    else if (pick == 3) // MAINTENANCE
    {
        var desc = ReadMaintenanceDescription();

        // For maintenance, we still include a plate/vehicleType fields to match your schema;
        // if you prefer, you can use placeholders.
        var plate = DefaultPlateIfBlank(ReadNonEmpty("Car license plate (optional, blank allowed)", ""));
        var vehicleType = ReadVehicleType();

        var evt = new ParkingEvent(
            ParkingName: parkingName,
            Timestamp: timestamp,
            LicensePlate: plate,
            VehicleType: vehicleType,
            EventType: "maintenance", // note: EventType is "maintenance" here for completeness
            IsMaintenance: true,
            MaintenanceDescription: desc
        );

        await SendMessageAsync(evt);
    }
}
