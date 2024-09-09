using AuroraPOS.Data;
using AuroraPOS.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPOS.ViewComponents
{
	[ViewComponent(Name = "AddSupplier")]
	public class AddSupplierViewComponent : ViewComponent
	{
		private readonly AppDbContext _dbContext;
		public AddSupplierViewComponent(AppDbContext dbContext)
		{ 
			_dbContext = dbContext;
		}
		public async Task<IViewComponentResult> InvokeAsync()
		{
		
			return View();
		}
	}
}
