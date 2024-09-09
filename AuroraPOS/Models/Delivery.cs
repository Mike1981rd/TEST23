using System;
namespace AuroraPOS.Models
{
	public class Delivery : BaseEntity
    {
        public StatusEnum Status { get; set; }
        public DateTime StatusUpdated { get; set; }
        public string? Address1 { get; set; } = string.Empty;
        public string? Adress2 { get; set; } = string.Empty;
        public string? Comments { get; set; } = string.Empty;
        public DateTime DeliveryTime { get; set; }
        public long? OrderID { get; set; }
        public Order Order { get; set; }
        public long? DeliveryCarrierID { get; set; }
        public DeliveryCarrier? Carrier { get; set; }
        public long? DeliveryZoneID { get; set; }
        public DeliveryZone? Zone { get; set; }
        public long? CustomerID { get; set; }
        public Customer? Customer { get; set; }
    }

    public enum StatusEnum
    {
        Nuevo,
        EnRuta,
        Entregado,
        Cerrado,
        Cancelado        
    }
}

