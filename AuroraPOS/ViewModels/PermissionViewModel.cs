namespace AuroraPOS.ViewModels
{
	public class PermissionGroupModel
	{
		public string Name { get; set; }
		public int Order { get; set; }
		public bool IsSelected { get; set; } = false;
		public List<PermissionViewModel> Permissions { get; set; }
		public PermissionGroupModel()
		{ 
			Permissions = new List<PermissionViewModel>();
		}
	}
	public class PermissionViewModel
	{
		public long ID { get; set; }
		public string Name { get; set; }
		public int Order { get; set; }
		public bool IsSelected { get; set; } = false;
	}
}
