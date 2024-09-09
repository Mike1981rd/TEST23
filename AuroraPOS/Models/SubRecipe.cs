namespace AuroraPOS.Models
{
	public class SubRecipe : BaseEntity
	{
		public string Name { get; set; }
		public Category? Category { get; set; }
		public SubCategory? SubCategory { get; set; }
		public decimal MinimumQuantity { get; set; }
		public int MinimumUnit { get; set; }
		public decimal MaximumQuantity { get; set; }
		public int MaximumUnit { get; set; }
		public string? Barcode { get; set; }
        public int SaftyStock { get; set; }
        public virtual ICollection<SubRecipeItem> Items  { get; set; }
		public virtual ICollection<ItemUnit>? ItemUnits { get; set; }
		public int UnitNumber { get; set; }
		public decimal Total { get; set; }
	}

	public class SubRecipeItem : BaseEntity
	{
		public long ItemID { get; set; }
		public bool IsArticle { get; set; }
		public decimal FirstQty { get; set; }
		public decimal Qty { get; set; }
		public int UnitNum { get; set; }
		public decimal UnitCost { get; set; }
		public decimal ItemCost { get; set; }

	}
}
