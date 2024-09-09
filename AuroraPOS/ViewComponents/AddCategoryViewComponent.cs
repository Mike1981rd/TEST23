using AuroraPOS.Data;
using AuroraPOS.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPOS.ViewComponents
{
	[ViewComponent(Name = "AddCategory")]
	public class AddCategoryViewComponent : ViewComponent
	{
		private readonly AppDbContext _dbContext;
		public AddCategoryViewComponent(AppDbContext dbContext)
		{ 
			_dbContext = dbContext;
		}
		public async Task<IViewComponentResult> InvokeAsync()
		{
			var model = new AddCategoryViewModel();
			model.groups = _dbContext.Groups.ToList();
			model.printerChannels = _dbContext.PrinterChannels.Where(s => s.IsActive).ToList();
			model.taxes = _dbContext.Taxs.Where(c => c.IsActive && c.IsInArticle).ToList();
			model.propinas = _dbContext.Propinas.Where(s => s.IsActive).ToList();
			model.courses = _dbContext.Courses.Where(s => s.IsActive).ToList();
			return View(model);
		}
	}

	public class AddCategoryViewModel
	{
		public List<Group> groups { get; set; }
		public List<PrinterChannel> printerChannels { get; set; }
		public List<Tax> taxes { get; set; }
		public List<Propina> propinas { get; set; }
		public List<Course> courses { get; set; }
	}
}
