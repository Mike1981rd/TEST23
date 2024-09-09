namespace AuroraPOS.Models
{
	public class Group : BaseEntity
	{
		public string GroupName { get; set; }
		public string? Note { get; set; } 
		public virtual ICollection<Category> Categories { get; set; }
	}
}
