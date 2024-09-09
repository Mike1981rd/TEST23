using AuroraPOS.Data;
using System.Security.Claims;

namespace AuroraPOS.Models
{
    public class UserResolverService
    {
        public readonly IHttpContextAccessor _context;
        private readonly AppDbContext _dbContext;
        public UserResolverService(AppDbContext dbContext, IHttpContextAccessor context)
        {
            _context = context;
            _dbContext = dbContext;
        }
     
        public string GetGivenName()
        {
            return _context.HttpContext.User.FindFirst(ClaimTypes.GivenName)?.Value;
        }

        public string GetSurname()
        {
            return _context.HttpContext.User.FindFirst(ClaimTypes.Surname).Value;
        }

        public string GetNameIdentifier()
        {
            return _context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
        }

        public string GetEmails()
        {
            return _context.HttpContext.User.FindFirst("emails").Value;
        }
    }
}
