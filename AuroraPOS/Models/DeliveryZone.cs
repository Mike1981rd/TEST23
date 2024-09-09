using System;
namespace AuroraPOS.Models
{
	public class DeliveryZone : BaseEntity
    {
        public string Name { get; set; }
        public bool IsPrimary { get; set; }
        public decimal Time { get; set; }
        public decimal Cost { get; set; }
    }
}

