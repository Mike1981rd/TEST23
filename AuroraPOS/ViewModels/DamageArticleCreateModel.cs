namespace AuroraPOS.ViewModels
{
    public class DamageArticleCreateModel
    {
        public long ID { get; set; }
        public long WarehouseID { get; set; }
        public long ItemID { get; set; }
        public bool IsArticle { get; set; }
        public int UnitNum { get; set; }
        public decimal Qty { get; set; }
        public string Description { get; set; }
        public string DamageDate { get; set; }
        public List<string> Images { get; set; }
    }
}
