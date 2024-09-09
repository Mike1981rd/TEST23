using AuroraPOS.Models;
using System.Diagnostics.Eventing.Reader;

namespace AuroraPOS.ViewModels
{
    public class PhysicalCountViewModel
    {
        public long ItemID { get; set; }
        public string Name { get; set; }
        public ItemType ItemType { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public long CategoryID { get; set; }
        public long SubCategoryID { get; set; }
        public string SubCategory { get; set; }
        public string Brand { get; set; }
        public decimal StockQty { get; set; }
        public string Barcode { get; set; }
        public int ScannerUnit { get; set; }
        public List<ItemUnit> Units { get; set; }
        public List<Warehouse> Warehouses { get; set; }
    }

    public class PhysicalCountUpdateModel
    {
        public long WarehouseID { get; set; }
        public long ItemID { get; set; }
        public ItemType ItemType { get; set; }
        public decimal Difference { get; set; }
        public int? UnitID { get; set; }
        

    }
}
