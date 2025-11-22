namespace dr.BmwData;

public class BmwOptions
{
    public const string SectionName = "BmwData";

    public string BaseUrl { get; set; } = "https://bmw-cardata.bmwgroup.com/thirdparty/public/home";
    public string ApiKey { get; set; } = string.Empty;
    public string DeviceFlowBaseUrl { get; set; } = "https://customer.bmwgroup.com";
    public string ClientId { get; set; } = string.Empty;
}
