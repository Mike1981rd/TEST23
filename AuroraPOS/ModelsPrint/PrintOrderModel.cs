namespace AuroraPOS;

public class PrintOrderModel
{
    public string Titulo { get; set; }   
    public string Empresa { get; set; }
    public string Empresa1 { get; set; }
    public string Order { get; set; }
    public string Table { get; set; }
    public string Person { get; set; }
    public string Camarero { get; set; }
    public string Rnc { get; set; }
    public string Devuelto { get; set; }
    public string Propina { get; set; }
    public string Delivery { get; set; }
    public string PromesaPagoMonto { get; set; }
    public string PromesaPagoDevuelto { get; set; }
    public string Subtotal { get; set; }
    public string Descuento { get; set; }
    public string Descuentos { get; set; }
    public string Tax { get; set; }
    public string Taxes { get; set; }
    public string Taxes1 { get; set; }
    public string Total { get; set; }
    public string Type { get; set; }
    public string CustomerRNC { get; set; }
    public string CustomerName { get; set; }
    public string CustomerAddress { get; set; }
    public string CustomerPhone { get; set; }
    public List<PrintOrderItemModel>? Items { get; set; }
    
}