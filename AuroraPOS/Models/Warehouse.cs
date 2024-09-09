namespace AuroraPOS.Models
{
	public class Warehouse : BaseEntity
	{
		public string WarehouseName { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public string WarehouseNum { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
	}
}
