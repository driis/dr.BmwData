using System.Text.Json.Serialization;

namespace dr.BmwData.Models;

/// <summary>
/// Response containing the list of mapped vehicles.
/// </summary>
public record VehicleMappingResponse(VehicleMapping[] Mappings);

/// <summary>
/// Represents a vehicle mapping to the customer account.
/// </summary>
public record VehicleMapping(
    string Vin,
    DateTime MappedSince,
    MappingType MappingType);

/// <summary>
/// Type of vehicle mapping.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MappingType
{
    PRIMARY,
    SECONDARY
}
