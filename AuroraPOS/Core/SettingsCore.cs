using AuroraPOS.Data;
using AuroraPOS.Services;
using Microsoft.EntityFrameworkCore;

namespace AuroraPOS.Core
{
    public class SettingsCore
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        public SettingsCore(IUserService userService, AppDbContext dbContext, IHttpContextAccessor context)
        {
            _userService = userService;
            _dbContext = dbContext;
            _context = context;
        }

        public DateTime? GetDia(int stationId)
        {
            var objStation = _dbContext.Stations.Where(d => d.ID == stationId).FirstOrDefault();

            var objDay = _dbContext.WorkDay.Where(d => d.IsActive == true && d.IDSucursal == objStation.IDSucursal).FirstOrDefault();

            if (objDay == null)
            {
                return null;
            }

            DateTime dtNow = new DateTime(objDay.Day.Year, objDay.Day.Month, objDay.Day.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

            return dtNow;
        }
    }
}
