namespace AuroraPOS.ModelsJWT;

public class ChangeQtyItemRequest
{
    public long ItemId { get; set; }
    public int Qty { get; set; }
    public int stationId { get; set; }
}