using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
    public class UserCreateModel : User
    {
        public List<long> RoleIds { get; set; }
        public List<long> CompanyIds { get; set; }
    }
}
