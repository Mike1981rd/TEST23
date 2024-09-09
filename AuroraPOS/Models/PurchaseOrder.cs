using AuroraPOS.ViewModels;

namespace AuroraPOS.Models
{
	public class PurchaseOrder : BaseEntity
	{
		public virtual Supplier Supplier { get; set; }
		public virtual Warehouse Warehouse { get; set; }
		public string NCF { get; set; }
		public string Term { get; set; }
		public PurchaseOrderStatus Status { get; set; }
		public PaymentStatus PaymentStatus { get; set; }
		public DateTime OrderTime { get;  set; }
		public virtual ICollection<PurchaseOrderItem> Items { get; set; }

		public string? Description { get; set; }
		public bool IsDiscountPercent { get; set; }
		public bool IsDiscountPercentItems { get; set; }
		public decimal Discount { get; set; }
		public decimal DiscountPercent { get; set; }
		public decimal DiscountAmount { get; set; }
		public decimal DiscountTotal { get; set; }
		public decimal Shipping { get; set; }
		public decimal TaxTotal { get; set; }
		public decimal SubTotal { get; set; }
		public decimal Total { get; set; }
		public decimal PaidAmount { get; set; }
	}

	public class PurchaseOrderItem : BaseEntity
	{
		public virtual PurchaseOrder PurchaseOrder { get; set; }
		public InventoryItem Item { get;set;}
		public int UnitNum { get; set; }	
		public decimal Qty { get; set; }
		public decimal Discount { get; set; }
		public decimal UnitCost { get; set; }
		public virtual Tax? Tax { get; set; }
		public decimal TaxRate { get; set; }
	}

	public enum PurchaseOrderStatus
	{
		None = 0,
		Pending = 1,
		Cancelled = 3,
		Ordered = 2,
		Received = 4
	}

	public enum PaymentStatus
	{
		NotPaid,
		Partly,
		Paid,
		SeatPaid,
		DividerPaid
	}
}
