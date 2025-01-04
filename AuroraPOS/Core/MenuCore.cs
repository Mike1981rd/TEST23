using AuroraPOS.Data;
using AuroraPOS.Services;
using AuroraPOS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraPOS.Core
{
    public class MenuCore
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        public MenuCore(IUserService userService, AppDbContext dbContext, IHttpContextAccessor context)
        {
            _userService = userService;
            _dbContext = dbContext;
            _context = context;
        }

        public List<CategoryViewModel>? GetAllActiveCategoryList()
        {
            var categoryList = (_dbContext.Categories.Include(s => s.Taxs).Include(s => s.Propinas).Include(s => s.PrinterChannels).Where(s => s.IsActive).Select(s => new CategoryViewModel()
            {
                ID = s.ID,
                Name = s.Name,
                Plato = s.Plato,
                IsActive = s.IsActive,
                GroupID = s.Group.ID,
                GroupName = s.Group.GroupName,
                CourseID = s.CourseID,
                Taxes = s.Taxs == null ? new List<long>() : s.Taxs.Select(x => x.ID).ToList(),
                Propinas = s.Propinas == null ? new List<long>() : s.Propinas.Select(x => x.ID).ToList(),
                PrinterChannels = s.PrinterChannels == null ? new List<long>() : s.PrinterChannels.Select(x => x.ID).ToList()

            }).ToList());

            if (categoryList != null)
            {
                return categoryList;
            }
            return null;
            
        }
    }
}
