namespace AuroraPOS.ViewModels
{
    public class MoveArticleCreateModel
    {
        public long ID { get; set; }
        public long FromWarehouseID { get; set; }   
        public long ToWarehouseID { get; set; }
        public string MoveDate { get; set; }
        public string? Description { get; set; }
        public List<MoveArticleCreateModelItem> MoveItems { get; set; }
    }

    public class MoveArticleCreateModelItem
    {
        public long ItemID { get; set; }
        public bool IsArticle { get; set; }
        public decimal Qty { get; set; }
        public int UnitNum { get; set; }
    }
}
