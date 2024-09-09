using System.ComponentModel.DataAnnotations.Schema;

namespace AuroraPOS.Models
{
    public class OrderTransaction : BaseEntity
    {
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public Order? Order { get; set; }
        public string? Method { get; set; }
        public string? Note { get; set; }
        public decimal BeforeBalance { get; set; }
        public decimal AfterBalance { get; set; }
        public decimal Difference { get; set; }
        public long ReferenceId { get; set; } = 0;
        public string? Memo { get; set; }
        public int SeatNum { get; set; } = 0;
        public int DividerNum { get; set; } = 0;
        public string PaymentType { get; set; }
        public TransactionStatus Status { get; set; }
        public TransactionType Type { get; set; }

		[NotMapped] // Esto indica a Entity Framework que ignore esta propiedad al mapearla a la base de datos
		public decimal TemporaryDifference { get; set; }

	}

	public enum TransactionStatus
    {
        Open,
        Closed
    }

    public enum TransactionType
    {
        Payment,
        CloseDrawer,
        Tip,
        Refund
    }
}
