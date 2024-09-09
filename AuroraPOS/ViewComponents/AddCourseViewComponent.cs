using AuroraPOS.Data;
using AuroraPOS.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPOS.ViewComponents
{
	[ViewComponent(Name = "AddCourse")]
	public class AddCourseViewComponent : ViewComponent
	{
		private readonly AppDbContext _dbContext;
		public AddCourseViewComponent(AppDbContext dbContext)
		{ 
			_dbContext = dbContext;
		}
		public async Task<IViewComponentResult> InvokeAsync()
		{
		
			return View();
		}
	}
}
