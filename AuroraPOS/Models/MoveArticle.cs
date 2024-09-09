namespace AuroraPOS.Models
{
    public class MoveArticle : BaseEntity
    {
        public Warehouse FromWarehouse { get; set; }
        public Warehouse ToWarehouse { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime MoveDate { get; set; }
        public MoveArticleStatus Status { get; set; }
        public string Description { get; set; }
        public ICollection<MoveItem>? Items { get; set; }

    }

    public class MoveItem : BaseEntity
    {
        public long ItemID { get; set; }
        public ItemType ItemType { get; set; }
        public decimal Qty { get; set; }
        public int UnitNum { get; set; }
    }

    public enum MoveArticleStatus
    {
        None = 0,
        Pending = 1,
        Moved = 2
    }
}
