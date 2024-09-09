namespace AuroraPOS.ViewModels
{
    public class ArticleWarehouseStockViewModel
    {
        public string WarehouseName { get; set; }
        public string UnitName { get; set; }
        public long UnitID { get; set; }
        public string UnitRate { get; set; }
        public decimal Qty { get; set; }
    }

    public class ArticleQtyConvertionModel
    {
        public long ArticleID { get; set; }
        public decimal Qty { get; set; }
        public decimal Qty1 { get; set; }
        public int OriginalNumber { get; set; }
        public int CurrentNumber { get; set; }
    }
}
