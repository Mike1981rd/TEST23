namespace AuroraPOS.Models
{
    public class WarehouseStockChangeHistory : BaseEntity
    {
        public Warehouse Warehouse { get; set; }
        public long ItemId { get; set; }
        public long ReasonId { get; set; }
        public int UnitNum { get; set; }
        public decimal BeforeBalance { get; set; }
        public decimal AfterBalance { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public ItemType ItemType { get; set; }   // 1 article , 2 subrecipe
        public string? Description { get; set; }
        public string? Reason { get; set; }
        public StockChangeReason ReasonType { get; set; }
    }

    public enum StockChangeReason
    {
        None,
        PurchaseOrder,
        Production,
        Move,
        Damage,
        PhysicalCheck,
        Kitchen,
        Void
    }
}
