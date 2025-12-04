namespace dr.BmwData.Models;

/// <summary>
/// Response containing telematic data for a vehicle.
/// </summary>
public record TelematicDataResponse(Dictionary<string, TelematicDataEntry> TelematicData);

/// <summary>
/// A single telematic data entry with value, unit, and timestamp.
/// </summary>
public record TelematicDataEntry(
    string Value,
    string Unit,
    DateTime? Timestamp);
