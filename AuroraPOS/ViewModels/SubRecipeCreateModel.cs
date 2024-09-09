using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
	public class SubRecipeCreateModel
	{
		public long ID { get; set; }
		public string Name { get; set; }	
		public long CategoryID { get; set; }
		public long SubCategoryID { get; set; }
		public bool IsActive { get; set; }
		public decimal Minimum { get; set; }
		public decimal Maximum { get; set; }
		public int MaxUnitID { get; set; }
		public int MinUnitID { get; set; }
		public int UnitNumber { get; set; }
		public int SaftyStock { get; set; }
		public string Barcode { get; set; }
		public List<ItemUnit> ItemUnits { get; set; }
		public List<SubRecipeItemModel> Items { get; set; }
		public decimal Total { get; set; }
	}

	public class SubRecipeItemModel
	{
		public long ItemID { get; set; }
		public long ArticleID { get; set; }
		public bool IsArticle { get; set; }
		public decimal FirstQty { get; set; }
		public decimal QTY { get; set; }
		public int UnitID { get; set; }
		public decimal UnitPrice { get; set; }
	}

}
