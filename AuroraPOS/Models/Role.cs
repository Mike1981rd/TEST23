namespace AuroraPOS.Models
{
	public class Role : BaseEntity
	{
		public string RoleName { get; set; }
		public int Priority { get; set; }
		public virtual ICollection<User> Users { get; set; }
		public virtual ICollection<Permission>? Permissions { get; set; }
	}
}
