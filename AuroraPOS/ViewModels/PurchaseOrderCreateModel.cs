namespace AuroraPOS.ViewModels
{
	public class PurchaseOrderCreateModel
	{
		public long PurchaseOrderId { get; set; }
		public long SupplierID { get; set; }
		public long WarehouseID { get; set; }
		public string NCF { get; set; }
		public string Term { get; set; }		
		public decimal Shipping { get; set; }
		public int Status { get; set; }
		public string Description { get; set; }
		public string DiscountType { get; set; }
		public string DiscountTypeTotal { get; set; }
		public decimal DiscountPercent { get; set; }
		public decimal DiscountAmount { get; set; }
		public decimal DiscountTotal { get; set; }
		public long TaxId { get; set; }
		public decimal Discount { get; set; }
		public decimal SubTotal { get; set; }
		public decimal Total { get; set; }
		public decimal TaxTotal { get; set; }
		public List<POrderItemCreateModel> Items { get; set; }
        public int OrderStatus { get; set; }

    }

	public class POrderItemCreateModel
	{
		public long ItemID { get; set; }
		public long ArticleID { get; set; }
		public decimal QTY { get; set; }
		public int UnitNum { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal DiscountAmount { get; set;}
		public long TaxID { get; set; }
	}
}
