using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
    public class WarehouseStockArticleViewModel
    {
        public long ID { get; set; }
        public long ItemId { get; set; }
        public ItemType ItemType { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Qty { get; set; }
        public string UnitName { get; set; } 
    }

   
}
