using AuroraPOS.Models;

namespace AuroraPOS.Data
{
    public class ExtendedAppDbContext 
    {
        public AppDbContext _context;
        public UserResolverService _userService;

        public ExtendedAppDbContext(AppDbContext context, UserResolverService userService)
        {
            _context = context;
            _userService = userService;
            _context.CurrentUser = _userService.GetGivenName();
        }
    }
}
