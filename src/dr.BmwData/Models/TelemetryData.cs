namespace dr.BmwData.Models;

public class TelemetryData
{
    public string Vin { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double Mileage { get; set; }
    public double FuelLevel { get; set; }
    // Add more properties as we discover them
}
