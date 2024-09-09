using System.ComponentModel.DataAnnotations.Schema;

namespace AuroraPOS.Models
{
    public class PaymentMethod : BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Tasa { get; set; }
        public decimal Tip { get; set; }
        public string PaymentType { get; set; }
        public string Image { get; set; } = string.Empty;
		public int DisplayOrder { get; set; }

        [NotMapped]
        public string ImageUpload { get; set; } = string.Empty;

    }
}
