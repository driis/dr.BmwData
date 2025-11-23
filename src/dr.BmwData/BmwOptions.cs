namespace dr.BmwData;

public class BmwOptions
{
    public const string SectionName = "BmwData";

    public string BaseUrl { get; set; } = "https://bmw-cardata.bmwgroup.com/thirdparty/public/home";
    public string DeviceFlowBaseUrl { get; set; } = "https://customer.bmwgroup.com";
    public string ClientId { get; set; } = string.Empty;
    public string RefreshToken { get; set; }  = string.Empty;
    
    /// <summary>
    /// Initial polling interval in milliseconds. Default is 1000ms (1 second).
    /// </summary>
    public int InitialPollIntervalMs { get; set; } = 1000;
    
    /// <summary>
    /// Interval increment in milliseconds when slow_down is received. Default is 5000ms (5 seconds).
    /// </summary>
    public int SlowDownIncrementMs { get; set; } = 5000;
}
