namespace AuroraPOS.Models
{
	public class Product : BaseEntity
	{
		public string Name { get; set; }
		public Category Category { get; set; }
		public SubCategory? SubCategory { get; set; }
		public string? Printer { get; set; }
		public ICollection<Tax>? Taxes { get; set; }
		public ICollection<Propina>? Propinas { get; set; }
		public ICollection<PrinterChannel>? PrinterChannels { get; set; }
		public string? Barcode { get; set; }
		public string? BackColor { get; set; }
		public string? TextColor { get; set; }
		public List<decimal> Price { get; set; }
		public decimal ProductCost { get; set; }
		public string? Photo { get; set; }
		public decimal InventoryCount { get; set; }
		public bool InventoryCountDownActive { get; set; }
		public long CourseID { get; set; }
		public bool HasServingSize { get; set; }
		
		public List<ProductRecipeItem>? RecipeItems { get; set; }
		public List<ProductServingSize>? ServingSizes { get; set; }
		public ICollection<SubCategory>?  SubCategories { get; set; }
		public virtual ICollection<Question>?  Questions { get; set; }
		

		public Product()
		{
			Price = new List<decimal>();
		}
	}

	public class ProductRecipeItem: BaseEntity
	{
		public long ItemID { get; set; }
		public ItemType Type { get; set; }
		public decimal Qty { get; set; }
		public int UnitNum { get; set; }
		public int ServingSizeID { get; set; }
	}

	public class ProductServingSize : BaseEntity
	{
		public long ServingSizeID { get; set; }
		public string ServingSizeName { get; set; }
		public decimal Cost { get; set; }
		public bool IsDefault {  get; set; }
		public int Order { get; set; }
		public List<decimal> Price { get; set; }
	}
}
