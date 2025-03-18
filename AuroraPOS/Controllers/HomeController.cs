using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AuroraPOS.Controllers
{
    [Authorize]
    public class HomeController : BaseController
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IWebHostEnvironment _hostingEnvironment;
		private readonly AppDbContext _dbContext;

		public HomeController(ExtendedAppDbContext dbContext, IWebHostEnvironment hostingEnvironment, ILogger<HomeController> logger)
		{
			_dbContext = dbContext._context;
			_hostingEnvironment = hostingEnvironment;
			_logger = logger;
		}
		
		public async Task<IActionResult> Index()
		{

            try
			{
                var user = _dbContext.User.ToList();
            }
            catch {
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            ViewBag.TopProducts = GetFirstTopSaleProducts();
            ViewBag.Branchs = _dbContext.t_sucursal.ToList();

            return View();
		}

		private List<TopSaleProductModel> GetFirstTopSaleProducts()
		{
            var items = _dbContext.OrderItems.Include(s => s.Order).ThenInclude(s => s.Station).Include(s => s.Product).ThenInclude(s => s.Category).Where(s => s.Status == OrderItemStatus.Paid).ToList();
            int category = 0;
            int branch = 0;

            var products = new List<TopSaleProductModel>();

            foreach (var item in items)
            {
                if (category > 0)
                {
                    if (item.Product.Category.ID != category) continue;
                }
                if (branch > 0)
                {
                    if (item.Order.Station != null && item.Order.Station.IDSucursal != branch) continue;
                }

                var exist = products.FirstOrDefault(s => s.ProductId == item.Product.ID);
                if (exist == null)
                {
                    products.Add(new TopSaleProductModel()
                    {
                        ProductId = item.Product.ID,
                        ProductName = item.Product.Name,
                        ProductImage = item.Product.Photo,
                        Cost = item.Product.ProductCost,
                        Qty = item.Qty
                    });
                }
                else
                {
                    exist.Qty += item.Qty;
                }
            }

            return (products.OrderByDescending(s => s.Qty).Take(6).ToList());
        }

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

		[HttpPost]
		public JsonResult GetProfileInfo()
		{
            var userName = User.Identity.GetUserName();
            var logUser = _dbContext.User.FirstOrDefault(u => u.Username == userName);
            UserBasicInfo userLog = new UserBasicInfo();
            userLog.FullName = logUser.FullName;
            userLog.ProfileImage = logUser.ProfileImage;
            userLog.Role = User.Identity.GetRole();

			return Json(userLog);
        }

		[HttpPost]
		public JsonResult GetSalesTotal(string from, string to)
		{
			var toDate = DateTime.Now;
			if (!string.IsNullOrEmpty(to))
			{
				try
				{
					toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}
			var fromDate = DateTime.Now;
			if (!string.IsNullOrEmpty(from))
			{
				try
				{
					fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}

			//var items = _dbContext.OrderItems.Include(s => s.Product).ThenInclude(s => s.Category).Include(s => s.Taxes).Where(s => s.Status == OrderItemStatus.Paid && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date);

			//var refunds = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.PaymentDate.Date >= fromDate.Date && s.PaymentDate.Date <= toDate.Date && s.Type == TransactionType.Refund).OrderByDescending(s => s.PaymentDate).ToList();

			decimal totalSales = 0;
			decimal totalTaxes = 0;
			decimal totalPropinas = 0;
			//foreach (var item in items)
			//{
			//	totalSales += item.SubTotal;
			//	foreach (var t in item.Taxes)
			//	{
			//		if (!t.IsExempt)
			//		{
			//			totalTaxes += t.Amount;
			//		}					
			//	}
			//}

			//foreach(var refund in refunds)
			//{
			//	if (refund.Order != null)
			//	{
			//		totalSales -= refund.Order.SubTotal;
			//		totalTaxes -= refund.Order.Tax;
			//	}
			//}


			var trans = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.PaymentDate.Date >= fromDate.Date && s.PaymentDate.Date <= toDate.Date).OrderByDescending(s => s.PaymentDate).ToList();

			var lstOrder = new List<Order>();

			foreach(var tran in trans)
			{
				if (tran.Order != null && !lstOrder.Contains(tran.Order))
				{
					lstOrder.Add(tran.Order);

                    if (tran.Type == TransactionType.Refund)
                    {
                        totalSales -= tran.Order.SubTotal - tran.Order.Discount;
                        totalTaxes -= tran.Order.Tax;
						totalPropinas -= tran.Order.Propina;
                    }
                    else
					{
                        totalSales += tran.Order.SubTotal - tran.Order.Discount;
                        totalTaxes += tran.Order.Tax;
                        totalPropinas += tran.Order.Propina;
                    }
				}
			}
			

            return Json(new { TotalSales = totalSales, TotalSalesTax = totalTaxes, TotalPropina = totalPropinas });
		}

		[HttpPost]
		public JsonResult GetPurchaseTotal(string from, string to)
		{
			var toDate = DateTime.Now;
			if (!string.IsNullOrEmpty(to))
			{
				try
				{
					toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}
			var fromDate = DateTime.Now;
			if (!string.IsNullOrEmpty(from))
			{
				try
				{
					fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}
			var purchaseOrders = _dbContext.PurchaseOrders.Where(s => s.OrderTime.Date >= fromDate.Date && s.OrderTime.Date <= toDate.Date && s.Status == PurchaseOrderStatus.Received).ToList();

			decimal totalPurchases = 0;
			decimal totalTaxes = 0;
			foreach (var item in purchaseOrders)
			{
				totalPurchases += item.Total;
				totalTaxes += item.TaxTotal;
			}

			return Json(new { TotalPurchase = totalPurchases - totalTaxes, TotalPurchaseTax = totalTaxes });
		}

		[HttpPost]
		public JsonResult GetTopSalesProducts(int category = 0, int branch = 0)
		{
			var items = _dbContext.OrderItems.Include(s=>s.Order).ThenInclude(s=>s.Station).Include(s => s.Product).ThenInclude(s=>s.Category).Where(s => s.Status == OrderItemStatus.Paid).ToList();

			var products = new List<TopSaleProductModel>();

			foreach(var item in items)
			{
				if (category > 0)
				{
					if (item.Product.Category.ID != category) continue;
				}
				if (branch > 0)
				{
					if (item.Order.Station != null && item.Order.Station.IDSucursal != branch) continue;
				}

				var exist = products.FirstOrDefault(s => s.ProductId == item.Product.ID);
				if (exist == null)
				{
					products.Add(new TopSaleProductModel()
					{
						ProductId = item.Product.ID,
						ProductName = item.Product.Name,
						ProductImage = item.Product.Photo,
						Cost = item.Product.ProductCost,
						Qty = item.Qty
					});
				}
				else
				{
					exist.Qty += item.Qty;
				}
			}

			products = products.OrderByDescending(s => s.Qty).ToList();
			return Json(products);
		}

		[HttpPost]
		public JsonResult GetInventoryTotal()
		{
			var stocks = _dbContext.WarehouseStocks.Include(s => s.Warehouse).Where(s=>s.Warehouse.IsActive).ToList();
			var stockTotal = new List<InventoryTotal>();
			decimal total = 0;
			foreach (var stock in stocks)
			{
				decimal subtotal = 0;
				var sub = new InventoryStockSub();
				try
				{
					if (stock.ItemType == ItemType.Article)
					{
						var article = _dbContext.Articles.Include(s => s.Items).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
						if (article == null) continue;
						var units = article.Items.OrderBy(s => s.Number).ToList();
						subtotal = stock.Qty * units[0].Cost;
					}
					else
					{
						var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
						if (subRecipe == null) continue;
						var units = subRecipe.ItemUnits.OrderBy(s => s.Number).ToList();
						subtotal = stock.Qty * units[0].Cost;
					}
				}
				catch { }
				

				var exist = stockTotal.FirstOrDefault(s => s.Warehouse == stock.Warehouse.WarehouseName);
				if (exist != null)
				{
					exist.Total += subtotal;
					exist.Qty += stock.Qty;
				}
				else
				{
					exist = new InventoryTotal();
					exist.Warehouse = stock.Warehouse.WarehouseName;
					exist.Total = subtotal;
					exist.Qty = stock.Qty;

					stockTotal.Add(exist);
				}
				total += subtotal;
			}

			return Json(new { inventoryTotal = total, inventoryStock = stockTotal});
		}

		public JsonResult GetStockAlerts()
		{
			var result = new List<StockAlertModel>();

			var stocks = _dbContext.WarehouseStocks.Include(s => s.Warehouse).Where(s => s.Warehouse.IsActive && s.ItemType == ItemType.Article).ToList();

			foreach (var stock in stocks)
			{
				var article = _dbContext.Articles.Include(s => s.Items).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
				if (article != null)
				{
					var units = article.Items.ToList();
					var firstunit = article.Items.FirstOrDefault(s => s.Number == 1);
					if (firstunit == null) { continue; }

					var exist = result.FirstOrDefault(s => s.ArticleID == article.ID);
					if (exist != null)
					{
						exist.Stock += stock.Qty;
					}
					else
					{
						var min = AlfaHelper.ConvertQtyToBase((decimal)article.MinimumQuantity, article.MaximumUnit, units);

						result.Add(new StockAlertModel()
						{
							ArticleID = article.ID,
							Name = article.Name,
							Minimum = min,
							Stock = stock.Qty,
							Date = DateTime.Now,
							UnitName = firstunit.Name
						});
					}
				}
			}

			return Json(result);
		}
	}

	public class InventoryTotal
	{
		public string Warehouse { get; set; }
		public decimal Qty { get; set; }
		public decimal Total { get; set; }
	}

	public class TopSaleProductModel
	{
		public long ProductId { get; set; }
		public string ProductName { get; set; }
		public string ProductImage { get; set; }
		public decimal Qty { get; set; }
		public decimal Cost { get; set; }
	}

	public class StockAlertModel
	{
		public long ArticleID { get; set; }
		public string Name { get; set; }
		public decimal Minimum { get; set; }
		public string UnitName { get; set; }
		public decimal Stock { get; set; }
		public DateTime Date { get; set; }
	}
}