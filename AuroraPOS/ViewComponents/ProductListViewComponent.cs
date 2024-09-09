using AuroraPOS.Data;
using AuroraPOS.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPOS.ViewComponents
{
	[ViewComponent(Name = "ProductList")]
	public class ProductListViewComponent : ViewComponent
	{
		private readonly AppDbContext _dbContext;
		public ProductListViewComponent(AppDbContext dbContext)
		{ 
			_dbContext = dbContext;
		}
		public async Task<IViewComponentResult> InvokeAsync()
		{
			return View();
		}
	}

}
