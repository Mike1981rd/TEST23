namespace AuroraPOS.Models
{
    public class InventoryItem : BaseEntity
    {
        public string Name { get; set; }
        public Category? Category { get; set; }
        public SubCategory? SubCategory { get; set; }
        public Warehouse? Warehouse { get; set; }
        public Tax? Tax { get; set; }
        public double Performance { get; set; }
        public Brand? Brand { get; set; }
        public double MinimumQuantity { get; set; }
        public int MinimumUnit { get; set; }
        public double MaximumQuantity { get; set; }
        public int MaximumUnit { get; set; }
        public int DefaultUnitNum { get; set; }
        public int ScannerUnit { get; set; }
        public string? Photo { get; set; }
        public string? Barcode { get; set; }
        public long PrimarySupplier { get; set; }
        public int SaftyStock { get; set; }
        public DepleteCondition DepleteCondition { get; set; }
        public virtual ICollection<ItemUnit>? Items { get; set; }
        public virtual ICollection<Supplier>? Suppliers { get; set; }
    }

    public enum DepleteCondition
    {
        None = 0,
        Delivery = 1,
    }
}
