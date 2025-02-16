namespace AuroraPOS.ModelsJWT;

public class VoidOrderItemRequest
{
    public long ItemId { get; set; }
    public long ReasonId { get; set; }
    public string Pin { get; set; }
    public bool Consolidate { get; set; }
    public int stationId { get; set; }
}