using AuroraPOS.Data;
using AuroraPOS.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPOS.ViewComponents
{
	[ViewComponent(Name = "ArticleList")]
	public class ArticleListViewComponent : ViewComponent
	{
		private readonly AppDbContext _dbContext;
		public ArticleListViewComponent(AppDbContext dbContext)
		{ 
			_dbContext = dbContext;
		}
		public async Task<IViewComponentResult> InvokeAsync()
		{
			return View();
		}
	}

}
