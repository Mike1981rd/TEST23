using AuroraPOS.Data;
using AuroraPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace AuroraPOS.Services
{
	public interface IUserService
	{
        User GetAllowedUser(string username);
        bool ValidatePassword(User user, string password);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _dbContext;
        public UserService(AppDbContext context) 
        { 
            _dbContext = context;
        }

        public User GetAllowedUser(string username)
        {
            username = username.Trim().ToLower();
            var user = _dbContext.User.Include(s=>s.Roles).ThenInclude(s=>s.Permissions).FirstOrDefault(s=>s.Username.ToLower() == username.ToLower() && s.IsActive);
            return user;
        }

        public bool ValidatePassword(User user, string password)
        {
            var hash = password;
            return hash == user.Password;
        }

    }
}
