namespace AuroraPOS.Models
{
	public class Category : BaseEntity
	{
		public string? Name { get; set; }		
		public int Plato { get; set; }
		public long CourseID { get; set; }
		public Group Group { get; set; }
		public virtual ICollection<Tax>? Taxs { get; set; }
		public virtual ICollection<Propina>? Propinas { get; set; }
		public virtual ICollection<PrinterChannel>? PrinterChannels { get; set; }
		public virtual ICollection<SubCategory> SubCategories { get; set; }
		public virtual ICollection<Question>? Questions { get; set; }
	}
}
