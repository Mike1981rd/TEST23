namespace AuroraPOS.Models
{
    public class Kitchen :BaseEntity
    {
        public string Name {  get; set; }
        public long PrinterID { get; set; }
        public List<long> Stations { get; set; }
    }

    public class KitchenOrder : BaseEntity
    {
        public long KitchenID { get; set; }
        public long OrderID {  get; set; }
        public bool IsCompleted { get; set; } = false;
        public DateTime StartTime { get; set; }
        public DateTime CompleteTime { get; set; }
        public KitchenOrderStatus Status { get; set; }
    }

    public class KitchenOrderItem : BaseEntity
    {
        public long KitchenOrderID {  get; set; }
        public long OrderItemID { get; set; }
        public DateTime CompleteTime { get; set; } = DateTime.Now;
        public KitchOrderItemStatus Status { get; set; }
    }

    public enum KitchenOrderStatus
    {
        Open,
        Started,
        Done,
        Void
    }

    public enum KitchOrderItemStatus
    {
        Open,
        Started,
        Done,
        Rush,
        Void
    }
}
