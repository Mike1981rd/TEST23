using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
	public class RoleViewModel 
	{
		public Role Role { get; set; }
		public List<PermissionGroupModel> PermissionGroups { get; set; }
	}

    public class RoleCardViewModel
    {
        public long ID { get; set; }
        public string RoleName { get; set; }
        public int Priority { get; set; }
        public int UserCount { get; set; }
        public List<string> UserImages { get; set; }
    }
}
