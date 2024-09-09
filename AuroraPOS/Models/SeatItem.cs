namespace AuroraPOS.Models
{
    public class SeatItem : BaseEntity
    {
        public int SeatNum { get; set; }
        public string? ClientName { get; set; }
        public long ClientId { get; set; } = 0;
        public long ComprebanteId { get; set;} = 0;
        public long OrderId { get; set; }
        public bool IsPaid { get; set; }
        public decimal PaidAmount { get; set; }
        public List<OrderItem>? Items { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class DividerItem : BaseEntity
    {
        public int DividerNum { get; set; }
        public string? ClientName { get; set; }
        public long ClientId { get; set; } = 0;
        public long ComprebanteId { get; set; } = 0;
        public decimal PaidAmount { get; set; }
    }
    
}
