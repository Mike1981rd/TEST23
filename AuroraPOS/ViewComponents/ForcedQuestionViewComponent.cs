using AuroraPOS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraPOS.ViewComponents
{
    [ViewComponent(Name = "ForcedQuestion")]
    public class ForcedQuestionViewComponent : ViewComponent
    {
        private readonly AppDbContext _dbContext;
        public ForcedQuestionViewComponent(AppDbContext dbContext) 
        {
            _dbContext = dbContext;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            
            return View();
        }
    }
}
