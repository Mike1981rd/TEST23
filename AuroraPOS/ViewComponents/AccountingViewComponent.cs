using AuroraPOS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraPOS.ViewComponents
{
    [ViewComponent(Name = "Accounting")]
    public class AccountingViewComponent : ViewComponent
    {
        private readonly AppDbContext _dbContext;
        public AccountingViewComponent(AppDbContext dbContext) 
        {
            _dbContext = dbContext;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            
            return View();
        }
    }
}
