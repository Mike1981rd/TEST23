namespace AuroraPOS.ModelsJWT;

public class POSLoginResponse
{
    public int status { get; set; }
    public string db { get; set; }
    public string stationId { get; set; }
    public string stationName { get; set; }
    public string token { get; set; }
}