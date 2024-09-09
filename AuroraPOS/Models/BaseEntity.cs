using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AuroraPOS.Models
{
	public class BaseEntity
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long ID { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime UpdatedDate { get; set; }
		public bool IsActive { get; set; } = true;
		public bool IsDeleted { get; set; } = false;
		public string? CreatedBy { get; set; }
		public string? UpdatedBy { get; set; }

		[NotMapped]
        public DateTime? ForceDate { get; set; }

        [NotMapped]
        public string? ForceUpdatedBy { get; set; }
    }
}
