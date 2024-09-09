using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
	public class RoleViewModel 
	{
		public Role Role { get; set; }
		public List<PermissionGroupModel> PermissionGroups { get; set; }
	}
}
