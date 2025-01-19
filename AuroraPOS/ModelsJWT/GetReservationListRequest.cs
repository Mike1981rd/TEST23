namespace AuroraPOS.ModelsJWT;

public class GetReservationListRequest
{
    public int stationId { get; set; }
    public int areaId { get; set; }
    public string draw { get; set; } 
    public int start { get; set; }
    public int length { get; set; } 
    public string sortColumn { get; set; } 
    public string sortColumnDirection { get; set; } 
    public string searchValue { get; set; } 
}