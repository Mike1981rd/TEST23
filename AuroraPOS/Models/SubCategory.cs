namespace AuroraPOS.Models
{
	public class SubCategory : BaseEntity
	{
		public string Name { get; set; }
		public string? Description { get; set; }
		public virtual Category Category { get; set; }
	}
}
