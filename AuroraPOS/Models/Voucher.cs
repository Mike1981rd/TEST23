namespace AuroraPOS.Models
{
	public class Voucher : BaseEntity
	{
		public string Name { get; set; }
		public string Class { get; set; }
		public decimal Reorder { get; set; }
		public int Secuencia { get; set; }
		public bool IsPrimary { get; set; }
		public bool IsRequireRNC { get; set; }

		public List<Tax>? Taxes { get; set; }
	}
}
