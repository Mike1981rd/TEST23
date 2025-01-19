using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT;

public class GetUserWithoutCloseStationResponse
{
    public int status{ get; set; }
    public List<Order>? ordersOpen { get; set; }
    public bool? todos { get; set; }
    public string? usuarios {  get; set; }

}