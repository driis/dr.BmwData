using dr.BmwData.Models;

namespace dr.BmwData;

public interface ITelemetryService
{
    /// <summary>
    /// Gets the list of vehicles mapped to the customer account.
    /// </summary>
    Task<VehicleMappingResponse> GetVehicleMappingsAsync();

    /// <summary>
    /// Gets telematic data for a specific vehicle and container.
    /// </summary>
    /// <param name="vin">Vehicle identification number.</param>
    /// <param name="containerId">The container ID to retrieve data for.</param>
    Task<TelematicDataResponse> GetTelematicDataAsync(string vin, string containerId);
}
