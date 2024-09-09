using Microsoft.Win32;

namespace AuroraPOS.Models
{
	public class Permission : BaseEntity
	{
		public string Value { get; set; }
		public string DisplayValue { get; set; }
		public int Level { get; set; }
		public string Group { get; set; }
		public int GroupOrder { get; set; }
		public ICollection<Role> Roles { get; set; }
	}
}
