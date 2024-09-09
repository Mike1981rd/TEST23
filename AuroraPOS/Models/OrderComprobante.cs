namespace AuroraPOS.Models
{
    public class OrderComprobante : BaseEntity
    {
        public long OrderId { get; set; }
        public long VoucherId { get; set; }
        public string ComprobanteNumber { get; set; }
        public string ComprobanteName { get; set; }
    }

}
