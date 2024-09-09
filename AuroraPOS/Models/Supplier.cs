using System.ComponentModel.DataAnnotations;

namespace AuroraPOS.Models
{
	public class Supplier : BaseEntity
	{
		[Required]
		public string Name { get; set; }
		[Required]
		public string RNC { get; set; }
		public string? PhoneNumber { get; set; } = "";
		public string? Email { get; set; }
		public string? Country { get; set; } = "";
		public string? City { get; set; } 
		public string? Direction { get; set; } 
		public bool IsTaxIncluded { get; set; } = false;
		public bool IsFormalSupplier { get; set; } = false;
		public string? Seller { get; set; } = "";
		public string? CellPhone { get; set; } = "";
		public string? Avatar { get; set; }
		public int DeliveryTime { get; set; }
		public virtual ICollection<InventoryItem>? Articles { get; set; }
	}
}
