namespace dr.BmwData.Console;

public class BmwOptions
{
    public const string SectionName = "BmwData";

    public string BaseUrl { get; set; } = "https://bmw-cardata.bmwgroup.com/thirdparty/public/home";
    public string ApiKey { get; set; } = string.Empty;
}
