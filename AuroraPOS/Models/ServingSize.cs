namespace AuroraPOS.Models
{
	public class ServingSize : BaseEntity
	{
		public string Name { get; set; }
		public string? Description { get; set; }
		public int Order { get; set; }
	}
}
