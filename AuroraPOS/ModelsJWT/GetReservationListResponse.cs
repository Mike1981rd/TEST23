namespace AuroraPOS.ModelsJWT;

public class GetReservationListResponse
{
    public string? Error { get; set; }
    public bool Success { get; set; }
    public string? draw { get; set; }
    public int? recordsFiltered { get; set; }
    public int? recordsTotal { get; set; } 
    public List<CustomerData>? data { get; set; }
}