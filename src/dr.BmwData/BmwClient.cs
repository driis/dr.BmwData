using dr.BmwData.Models;

namespace dr.BmwData;

public class BmwClient
{
    public async Task<TelemetryData> GetTelemetryAsync(string vin)
    {
        // TODO: Implement actual API call
        // For now, return mock data
        await Task.Delay(100); // Simulate network delay

        return new TelemetryData
        {
            Vin = vin,
            Timestamp = DateTime.UtcNow,
            Mileage = 12345.6,
            FuelLevel = 75.0
        };
    }
}
