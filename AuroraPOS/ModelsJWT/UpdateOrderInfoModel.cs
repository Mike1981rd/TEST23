using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT;

public class UpdateOrderInfoModel
{
    public int status { get; set; }
    public string ComprobanteName { get; set; }
    public decimal deliverycost { get; set; }
    public decimal deliverytime { get; set; }
    public string customerName { get; set; }
}