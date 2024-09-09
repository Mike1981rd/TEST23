using System;
namespace AuroraPOS.ViewModels
{
	public class OrderDeliveryModel
	{
        public long OrderId { get; set; }
        public string? Address1 { get; set; } = string.Empty;
        public string? Adress2 { get; set; } = string.Empty;
        public string? Comments { get; set; } = string.Empty;
        public long ZoneId { get; set; }
    }
}

