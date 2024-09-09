using System.Net;
using AuroraPOS.Data;
using AuroraPOS.ModelsCentral;
using AuroraPOS.Security;
using Microsoft.EntityFrameworkCore;

namespace AuroraPOS.Services
{
    public interface ICentralService
    {
        User GetAllowedUser(string username);
        User GetAllowedUserByPin(string pin);
        bool ValidatePassword(User user, string password);
    }

    public class CentralService : ICentralService
    {
        private readonly DbAlfaCentralContext _dbContext;
        public CentralService()
        {
            _dbContext = new DbAlfaCentralContext();
        }

        public User GetAllowedUser(string username)
        {
            username = username.Trim().ToLower();
            var user = _dbContext.Users.FirstOrDefault(s => s.Username.Trim().ToLower() == username && s.IsActive);
            return user;
        }

        public User GetAllowedUserByPin(string pin)
        {
            pin = pin.Trim().ToLower();
            var user = _dbContext.Users.FirstOrDefault(s => s.Pin.ToLower() == pin && s.IsActive);
            return user;
        }

        public List<Company> GetAllowedCompanies(string username)
        {
            username = username.Trim().ToLower();
            var user = _dbContext.Users.FirstOrDefault(s=>s.Username.Trim().ToLower() == username && s.IsActive);
            var allcompanies = GetAllCompanies();
            var companies = new List<Company>();
            if (user != null)
            {
                var userComapanies = _dbContext.UserCompanies.Where(s => s.UserId == user.Id);
                foreach (var c in userComapanies)
                {
                    var company = allcompanies.FirstOrDefault(s => s.Id == c.CompanyId);
                    if (company != null)
                        companies.Add(company);
                }    
            }
            
            return companies;
        }

        public List<Company> GetAllCompanies()
        {            
            var lst = (from c in _dbContext.Companies
                       select c).ToList();
            return lst;
        }

        public bool ValidatePassword(User user, string password)
        {
            var hash = Encriptacion.CreateHash(password);
            return hash == user.Password;
        }

    }
}
