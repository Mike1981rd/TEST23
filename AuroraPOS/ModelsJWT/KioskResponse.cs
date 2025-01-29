using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT;

public class KioskResponse
{
    public Order? order { get; set; }
    public int currentOrderID { get; set; }
    public int sucursalId { get; set; }
    public List<Product>? products { get; set; }
    public List<Denomination>? denominations { get; set; }
    public List<PaymentMethod>? paymentMethods { get; set; }
    public List<t_sucursal>? branchs { get; set; }
    public List<User>? otherUsers { get; set; }
    public List<CancelReason>? cancelReasons { get; set; }
    
    public List<Discount>? discounts { get; set; }
    
    public bool showExpectedPayment { get; set; }
    public string? Error { get; set; }
    public bool Success { get; set; }
}