using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
	public class ArticleCreateViewModel
	{
		public long ID { get; set; }
		public string Name { get; set; }
		public long CategoryID { get; set; }
		public long SubCategoryID { get; set; }
		public long BrandID { get; set; }
		public long TaxID { get; set; }
		public double Performance { get; set; }
		public double MinQty { get; set; }
		public double MaxQty { get; set; }
		public string Barcode { get; set; }
		public int MaxUnitID { get; set; }
		public int MinUnitID { get; set; }
		public int DefaultUnitID { get; set; }
		public int ScannerUnit { get; set; }
		public bool IsActive { get; set; }
		public string Photo { get; set; }
		public int DepleteCondition { get; set; }
		public int SaftyStock { get; set; }
		public long PrimarySupplier { get; set; }
		public List<long> Suppliers { get; set; }
		public List<ItemUnit> ItemUnits { get; set; }

		public string ImageUpload { get; set; }

    }


}
