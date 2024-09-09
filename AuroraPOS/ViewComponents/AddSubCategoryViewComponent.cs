using AuroraPOS.Data;
using AuroraPOS.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPOS.ViewComponents
{
	[ViewComponent(Name = "AddSubCategory")]
	public class AddSubCategoryViewComponent : ViewComponent
	{
		private readonly AppDbContext _dbContext;
		public AddSubCategoryViewComponent(AppDbContext dbContext)
		{ 
			_dbContext = dbContext;
		}
		public async Task<IViewComponentResult> InvokeAsync()
		{
		
			return View();
		}
	}
}
