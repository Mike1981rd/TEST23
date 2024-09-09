using AuroraPOS.Data;
using AuroraPOS.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPOS.ViewComponents
{
	[ViewComponent(Name = "AddWarehouse")]
	public class AddWarehouseViewComponent : ViewComponent
	{
		private readonly AppDbContext _dbContext;
		public AddWarehouseViewComponent(AppDbContext dbContext)
		{ 
			_dbContext = dbContext;
		}
		public async Task<IViewComponentResult> InvokeAsync()
		{
			
			return View();
		}
	}

}
