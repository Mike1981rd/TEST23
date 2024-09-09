namespace AuroraPOS.Models
{
	public class WarehouseStock : BaseEntity
	{
		public virtual Warehouse Warehouse { get; set; }
		public long ItemId { get; set; }
		public ItemType ItemType { get; set; }
		public decimal Qty { get; set; }
	}

	public enum ItemType
	{
		Article,
		SubRecipe,
		Product
	}
}
