using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
    public class RoleCreateModel : Role
    {
        public List<long> PermissionIds { get; set; } 
    }
}
