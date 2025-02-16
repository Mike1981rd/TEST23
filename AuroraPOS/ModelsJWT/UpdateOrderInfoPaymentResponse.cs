namespace AuroraPOS.ModelsJWT;

public class UpdateOrderInfoPaymentResponse
{
    public string? Message { get; set; }
    public bool Success { get; set; }
    public int status {  get; set; }
    public string? ComprobanteName { get; set; }
}