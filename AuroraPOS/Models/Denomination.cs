namespace AuroraPOS.Models
{
	public class Denomination : BaseEntity
	{
		public string Name { get; set; }
		public decimal Amount { get; set; }
		public int DisplayOrder { get; set; }
	}
}
