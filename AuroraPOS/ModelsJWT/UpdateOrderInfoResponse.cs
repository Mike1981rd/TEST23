namespace AuroraPOS.ModelsJWT;

public class UpdateOrderInfoResponse
{
    public int status { get; set; }
    public string? Message { get; set; }
    public bool? Success { get; set; }
    public UpdateOrderInfoModel? Data { get; set; }
}