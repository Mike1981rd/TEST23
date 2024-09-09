namespace AuroraPOS.Models
{
    public class PrinterChannel : BaseEntity
    {
        public string Name { get; set; }
		public bool IsDefault { get; set; }
		public virtual ICollection<Category> Categories { get; set; }
		public virtual ICollection<Product> Products { get; set; }
	}
}
