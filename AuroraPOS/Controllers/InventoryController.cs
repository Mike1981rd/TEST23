using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.Services;
using AuroraPOS.ViewModels;
using iText.Barcodes;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.POIFS.Properties;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using static SkiaSharp.HarfBuzz.SKShaper;
using Image = iText.Layout.Element.Image;

namespace AuroraPOS.Controllers
{
	[Authorize]
    public class InventoryController : BaseController
	{
		private readonly IWebHostEnvironment _env;
		private readonly AppDbContext _dbContext;
		private readonly IUploadService _uploadService;
        private readonly IHttpContextAccessor _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public InventoryController(ExtendedAppDbContext dbContext, IUploadService uploadService, IWebHostEnvironment env, IHttpContextAccessor context, IWebHostEnvironment hostingEnvironment)
		{
			_dbContext = dbContext._context;
            _context = context;
            _uploadService = uploadService;
			_env = env;
			_hostingEnvironment = hostingEnvironment;
		}

		#region Warehouse
		// warehouse
		[Authorize(Policy ="Permission.INVENTORY.Warehouse")]
		public IActionResult WareHouseList()
		{
			return View();
		}
		[Authorize(Policy = "Permission.INVENTORY.StockHistory")]
		public IActionResult Stock()
		{
			return View();
		}

		[HttpPost]
        
        public JsonResult GetAllActiveWarehouses()
		{
			var suppliers = _dbContext.Warehouses.Where(s => s.IsActive).ToList();

			return Json(suppliers);
		}
		
		[HttpPost]
       
        public async Task<IActionResult> GetWarehouseList()
		{
			try
			{
				var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
				// Skiping number of Rows count  
				var start = Request.Form["start"].FirstOrDefault();
				// Paging Length 10,20  
				var length = Request.Form["length"].FirstOrDefault();
				// Sort Column Name  
				var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
				// Sort Column Direction ( asc ,desc)  
				var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
				// Search Value from (Search box)  
				var searchValue = Request.Form["search[value]"].FirstOrDefault();

				//Paging Size (10,20,50,100)  
				int pageSize = length != null ? Convert.ToInt32(length) : 0;
				int skip = start != null ? Convert.ToInt32(start) : 0;
				int recordsTotal = 0;

				// Getting all Customer data  
				var customerData = (from s in _dbContext.Warehouses
									select s);

				//Sorting
				if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				{
					try
					{
                        customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
					catch { }
					
				}

                var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
                var status = Request.Form["columns[1][search][value]"].FirstOrDefault();

                ////Search  
                if (!string.IsNullOrEmpty(all))
				{
                    all = all.Trim().ToLower();
                    customerData = customerData.Where(m => m.WarehouseName.ToLower().Contains(all) || m.Email.ToLower().Contains(all) || m.PhoneNumber.ToLower().Contains(all));
				}
				if (string.IsNullOrEmpty(status) || status == "1")
				{
					customerData = customerData.Where(s => s.IsActive);
				}
				else
				{
					customerData = customerData.Where(s => s.IsActive == false);
				}
				//total number of rows count   
				recordsTotal = customerData.Count();
				//Paging   
				var data = customerData.Skip(skip).ToList();
				if (pageSize != -1)
				{
					data = data.Take(pageSize).ToList();
				}
				//Returning Json Data  
				return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

			}
			catch (Exception ex)
			{
				throw;
			}
		}

		[HttpPost]
        [Authorize(Policy = "Permission.INVENTORY.Warehouse")]
        public JsonResult AddWarehouse(Warehouse request)
		{
			var existing = _dbContext.Warehouses.FirstOrDefault(x => x.WarehouseName == request.WarehouseName);
			if (existing != null)
			{
				return Json(new { status = 1 });
			}
			_dbContext.Warehouses.Add(request);
			_dbContext.SaveChanges();

			return Json(new { status = 0 });
		}

		[HttpPost]
        [Authorize(Policy = "Permission.INVENTORY.Warehouse")]
        public JsonResult EditWarehouse(Warehouse request)
		{
			var existing = _dbContext.Warehouses.FirstOrDefault(x => x.ID == request.ID);
			if (existing != null)
			{

				existing.WarehouseName = request.WarehouseName;
				existing.Email = request.Email;
				existing.PhoneNumber = request.PhoneNumber;
				existing.IsActive = request.IsActive;
				_dbContext.SaveChanges();
				return Json(new { status = 0 });
			}
			else
			{
				_dbContext.Warehouses.Add(request);
				_dbContext.SaveChanges();
				return Json(new { status = 0 });
			}
		}

		[HttpPost]
        [Authorize(Policy = "Permission.INVENTORY.Warehouse")]
        public JsonResult DeleteWarehouse(long warehouseId)
		{
			var existing = _dbContext.Warehouses.FirstOrDefault(x => x.ID == warehouseId);
			if (existing != null)
			{
				_dbContext.Warehouses.Remove(existing);
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}

		#endregion

		#region Supplier
		// supplier
		public IActionResult SupplierList()
		{
			return View();
		}

		public IActionResult AddSupplier(long supplierId = 0)
		{
			if (supplierId != 0)
			{
				var supplier = _dbContext.Suppliers.FirstOrDefault(s=>s.ID == supplierId);
				if (supplier != null)
				{
					return View(supplier);
				}

			}
			return View(new Supplier());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddSupplier([FromForm] Supplier supplier)
		{
			if (supplier != null)
			{
				var existing = _dbContext.Suppliers.FirstOrDefault(s => s.ID == supplier.ID);
				if (existing != null)
				{
					existing.Name= supplier.Name;
					existing.RNC= supplier.RNC;
					existing.IsActive = supplier.IsActive;
					existing.Avatar = supplier.Avatar;
					existing.DeliveryTime = supplier.DeliveryTime;
					var other = _dbContext.Suppliers.FirstOrDefault(s=> s.ID != supplier.ID && s.Name == supplier.Name);
					if (other != null)
					{
						ModelState.AddModelError("Name", "The other supplier with same name exists!");

						return View(supplier);
					}
					var other1 = _dbContext.Suppliers.FirstOrDefault(s => s.ID != supplier.ID && s.RNC == supplier.RNC);
					if (other1 != null)
					{
						ModelState.AddModelError("RNC", "The other supplier with same RNC exists!");

						return View(supplier);
					}
					existing.PhoneNumber= supplier.PhoneNumber;
					existing.Email= supplier.Email;
					existing.Country= supplier.Country;
					existing.City = supplier.City;
					existing.Seller = supplier.Seller;
					existing.CellPhone = supplier.CellPhone;
					existing.IsFormalSupplier = supplier.IsFormalSupplier;
					existing.IsTaxIncluded = supplier.IsTaxIncluded;
					existing.Direction = supplier.Direction;
				}
				else
				{
					var other = _dbContext.Suppliers.FirstOrDefault(s => s.Name == supplier.Name);
					if (other != null)
					{
						ModelState.AddModelError("Name", "The other supplier with same name exists!");

						return View(supplier);
					}
					var other1 = _dbContext.Suppliers.FirstOrDefault(s => s.RNC == supplier.RNC);
					if (other1 != null)
					{
						ModelState.AddModelError("RNC", "The other supplier with same RNC exists!");

						return View(supplier);
					}
					_dbContext.Suppliers.Add(supplier);
				}
				_dbContext.SaveChanges();
			}
			
			return RedirectToAction("SupplierList");
		}

		[HttpPost]
		public async Task<JsonResult> EditSupplier([FromForm] Supplier supplier)
		{
			if (supplier != null)
			{
				var existing = _dbContext.Suppliers.FirstOrDefault(s => s.ID == supplier.ID);
				if (existing != null)
				{
					existing.Name = supplier.Name;
					existing.RNC = supplier.RNC;
					existing.IsActive = supplier.IsActive;
					var other = _dbContext.Suppliers.FirstOrDefault(s => s.ID != supplier.ID && s.Name == supplier.Name);
					if (other != null)
					{
						return Json(new { status = 2 });
					}
					var other1 = _dbContext.Suppliers.FirstOrDefault(s => s.ID != supplier.ID && s.RNC == supplier.RNC);
					if (other1 != null)
					{
						return Json(new { status = 3 });
					}
					existing.PhoneNumber = supplier.PhoneNumber;
					existing.Email = supplier.Email;
					existing.DeliveryTime = supplier.DeliveryTime;
					existing.Country = supplier.Country;
					existing.City = supplier.City;
					existing.Seller = supplier.Seller;
					existing.CellPhone = supplier.CellPhone;
					existing.IsFormalSupplier = supplier.IsFormalSupplier;
					existing.IsTaxIncluded = supplier.IsTaxIncluded;
					existing.Avatar = supplier.Avatar;
					existing.Direction = supplier.Direction;
				}
				else
				{
					var other = _dbContext.Suppliers.FirstOrDefault(s => s.Name == supplier.Name);
					if (other != null)
					{
						return Json(new { status = 2 });
					}
					var other1 = _dbContext.Suppliers.FirstOrDefault(s => s.RNC == supplier.RNC);
					if (other1 != null)
					{
						return Json(new { status = 3 });
					}
					_dbContext.Suppliers.Add(supplier);
				}
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0, id = supplier.ID });
		}

		[HttpPost]
		public IActionResult GetSupplierList()
		{
			try
			{
				var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
				// Skiping number of Rows count  
				var start = Request.Form["start"].FirstOrDefault();
				// Paging Length 10,20  
				var length = Request.Form["length"].FirstOrDefault();
				// Sort Column Name  
				var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
				// Sort Column Direction ( asc ,desc)  
				var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
				// Search Value from (Search box)  
				var searchValue = Request.Form["search[value]"].FirstOrDefault();

				//Paging Size (10,20,50,100)  
				int pageSize = length != null ? Convert.ToInt32(length) : 0;
				int skip = start != null ? Convert.ToInt32(start) : 0;
				int recordsTotal = 0;

				// Getting all Customer data  
				var customerData = (from s in _dbContext.Suppliers		
									select s);

				//Sorting
				if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				{
					try
					{
                        customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
					catch { }
					
				}
				
					customerData = customerData.OrderByDescending(s => s.CreatedDate);
				
                ////Search
                var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
                var status = Request.Form["columns[1][search][value]"].FirstOrDefault();

                if (!string.IsNullOrEmpty(all))
				{
                    all = all.Trim().ToLower();
                    customerData = customerData.Where(m => m.Name.ToLower().Contains(all) || m.RNC.ToLower().Contains(all) || m.PhoneNumber.ToLower().Contains(all) || m.Direction.ToLower().Contains(all) || m.Seller.ToLower().Contains(all));
				}
				
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.Trim().ToLower();
                    customerData = customerData.Where(m => m.Name.ToLower().Contains(searchValue) || m.RNC.ToLower().Contains(searchValue) || m.PhoneNumber.ToLower().Contains(searchValue) || m.Direction.ToLower().Contains(searchValue) || m.Seller.ToLower().Contains(searchValue));
                }
                if (string.IsNullOrEmpty(status) || status == "1")
				{
					customerData = customerData.Where(s => s.IsActive);
				}
				else
				{
					customerData = customerData.Where(s => s.IsActive == false);
				}
				//total number of rows count   
				recordsTotal = customerData.Count();
				//Paging   
				var data = customerData.Skip(skip).ToList();
				if (pageSize != -1)
				{
					data = data.Take(pageSize).ToList();
				}
				//Returning Json Data  
				return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

			}
			catch (Exception ex)
			{
				throw;
			}
		}

		[HttpPost]
		public JsonResult GetAllActiveSuppliers()
		{
			var suppliers = _dbContext.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();

			return Json(suppliers);
		}

		[HttpPost]
		public JsonResult DeleteSupplier(long supplierId)
		{
			var existing = _dbContext.Suppliers.FirstOrDefault(x => x.ID == supplierId);
			if (existing != null)
			{
				_dbContext.Suppliers.Remove(existing);
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}

		#endregion

		#region Article
		// articles
		public IActionResult ArticleList()
		{
			return View();
		}

        [HttpPost]
        public IActionResult GetArticleList(long supplierID = 0)
        {
            try
            {
                var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                // Skiping number of Rows count  
                var start = Request.Form["start"].FirstOrDefault();
                // Paging Length 10,20  
                var length = Request.Form["length"].FirstOrDefault();
                // Sort Column Name  
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                // Sort Column Direction ( asc ,desc)  
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                // Search Value from (Search box)  
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                //Paging Size (10,20,50,100)  
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

				// Getting all Customer data  
				var customerData = _dbContext.Articles.Include(s => s.Category).Include(s => s.Brand).Include(s => s.Suppliers).Include(s=>s.Items).Include(s => s.SubCategory).Include(s => s.Tax).OrderByDescending(s=>s.CreatedDate).Select(s => new ArticleViewModel
				{
					ID = s.ID,
					Name = s.Name,
					CategoryName = s.Category == null ? "": s.Category.Name,
					CategoryId = s.Category == null? 0: s.Category.ID,
					SubCategoryId = s.SubCategory == null? 0: s.SubCategory.ID,
					BrandId = s.Brand == null?0: s.Brand.ID,
					SubCategoryName = s.SubCategory== null?"": s.SubCategory.Name,
					Tax = s.Tax == null?"" : s.Tax.TaxName,
					IsActive = s.IsActive,
					Performance = s.Performance,
					Suppliers = s.Suppliers == null? null: s.Suppliers.ToList(),
					Barcode = "", 
					Items = s.Items == null ?null : s.Items.ToList(),
					Brand = s.Brand == null ? "": s.Brand.Name,
					MinimumQty = s.MinimumQuantity,
					MaximumQty = s.MaximumQuantity,	
					Photo = null //s.Photo
				});


			
				//Sorting
				if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				{
					try
					{
                        customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
					catch { }
				}

				var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
				var brand = Request.Form["columns[1][search][value]"].FirstOrDefault();
				var category = Request.Form["columns[2][search][value]"].FirstOrDefault();
				var subcategory = Request.Form["columns[3][search][value]"].FirstOrDefault();
				var barcode = Request.Form["columns[4][search][value]"].FirstOrDefault();
				var status = Request.Form["columns[5][search][value]"].FirstOrDefault();
				var supplier = Request.Form["columns[6][search][value]"].FirstOrDefault();

				var result = new List<ArticleViewModel>();
				if (!string.IsNullOrEmpty(barcode))
				{
					var ret = customerData.ToList();
					foreach(var r in ret)
					{
						if (r.Items != null)
						{
							var units = r.Items.FirstOrDefault(s => s.CodeBar == barcode);
							if (units != null)
							{
								result.Add(r);
							}
						}				

					}
					
				}
				else
				{
					////Search  
					if (!string.IsNullOrEmpty(all))
					{
						all = all.Trim().ToLower();
						customerData = customerData.Where(m => m.Name.ToLower().Contains(all) || m.CategoryName.ToLower().Contains(all) || m.SubCategoryName.ToLower().Contains(all) || m.Brand.ToLower().Contains(all));
					}
					////Search  
					if (!string.IsNullOrEmpty(searchValue))
					{
						all = searchValue.Trim().ToLower();
						customerData = customerData.Where(m => m.Name.ToLower().Contains(all) || m.CategoryName.ToLower().Contains(all) || m.SubCategoryName.ToLower().Contains(all) || m.Brand.ToLower().Contains(all));
					}

					if (!string.IsNullOrEmpty(brand))
					{
						customerData = customerData.Where(s => "" + s.BrandId == brand);
					}
					if (!string.IsNullOrEmpty(category))
					{
						customerData = customerData.Where(s => "" + s.CategoryId == category);
					}
					if (!string.IsNullOrEmpty(subcategory))
					{
						customerData = customerData.Where(s => "" + s.SubCategoryId == subcategory);
					}
                    if (string.IsNullOrEmpty(status) || status == "1")
                    {
                        customerData = customerData.Where(s => s.IsActive);
                    }
					else
					{
						customerData = customerData.Where(s => s.IsActive == false);
					}
					
                    result = customerData.ToList();
					if (!string.IsNullOrEmpty(supplier))
					{
						result = result.Where(s => s.Suppliers.Any(s => "" + s.ID == supplier)).ToList();
					}
				}
				
				if (supplierID > 0 && string.IsNullOrEmpty(barcode))
				{
					result = result.Where(s => s.Suppliers.Any(s => s.ID == supplierID)).ToList();
				}
				//total number of rows count   
				recordsTotal = result.Count();
                //Paging   
                var data = result.Skip(skip).ToList();
                if (pageSize != -1)
                {
                    data = data.Take(pageSize).ToList();
                }

                //Obtenemos las urls de las imagenes
                var request = _context.HttpContext.Request;
                var _baseURL = $"{request.Scheme}://{request.Host}";
                if (data != null && data.Any())
                {
                    foreach (var item in data)
                    {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "article", item.ID.ToString() + ".png");
                        if (System.IO.File.Exists(pathFile))
                        {
                            var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

                            item.Photo = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "article", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second); ;
                        }
                        else
                        {
							item.Photo = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "article", "empty.png");
                        }
                    }
                }

                //Returning Json Data  
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                throw;
            }
        }

		[HttpPost]
		public IActionResult GetActiveArticleList(long supplierID = 0)
		{
			try
			{
				var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
				// Skiping number of Rows count  
				var start = Request.Form["start"].FirstOrDefault();
				// Paging Length 10,20  
				var length = Request.Form["length"].FirstOrDefault();
				// Sort Column Name  
				var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
				// Sort Column Direction ( asc ,desc)  
				var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
				// Search Value from (Search box)  
				var searchValue = Request.Form["search[value]"].FirstOrDefault();

				//Paging Size (10,20,50,100)  
				int pageSize = length != null ? Convert.ToInt32(length) : 0;
				int skip = start != null ? Convert.ToInt32(start) : 0;
				int recordsTotal = 0;

				// Getting all Customer data  
				var customerData = _dbContext.Articles.Include(s => s.Category).Include(s => s.Brand).Include(s => s.Suppliers).Include(s => s.Items).Include(s => s.SubCategory).Include(s => s.Tax).OrderByDescending(s => s.CreatedDate).Where(s => s.IsActive).Select(s => new ArticleViewModel
				{
					ID = s.ID,
					Name = s.Name,
					CategoryName = s.Category == null ? "" : s.Category.Name,
					CategoryId = s.Category == null ? 0 : s.Category.ID,
					SubCategoryId = s.SubCategory == null ? 0 : s.SubCategory.ID,
					BrandId = s.Brand == null ? 0 : s.Brand.ID,
					SubCategoryName = s.SubCategory == null ? "" : s.SubCategory.Name,
					Tax = s.Tax == null ? "" : s.Tax.TaxName,
					IsActive = s.IsActive,
					Performance = s.Performance,
					Suppliers = s.Suppliers == null ? null : s.Suppliers.ToList(),
					Barcode = "",
					Items = s.Items == null ? null : s.Items.ToList(),
					Brand = s.Brand == null ? "" : s.Brand.Name,
					MinimumQty = s.MinimumQuantity,
					MaximumQty = s.MaximumQuantity,
					Photo = null //s.Photo
				});



				//Sorting
				if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				{
					try
					{
						customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
					}
					catch { }
				}

				var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
				var brand = Request.Form["columns[1][search][value]"].FirstOrDefault();
				var category = Request.Form["columns[2][search][value]"].FirstOrDefault();
				var subcategory = Request.Form["columns[3][search][value]"].FirstOrDefault();
				var barcode = Request.Form["columns[4][search][value]"].FirstOrDefault();
				var status = Request.Form["columns[5][search][value]"].FirstOrDefault();
				var supplier = Request.Form["columns[6][search][value]"].FirstOrDefault();

				var result = new List<ArticleViewModel>();
				if (!string.IsNullOrEmpty(barcode))
				{
					var ret = customerData.ToList();
					foreach (var r in ret)
					{
						if (r.Items != null)
						{
							var units = r.Items.FirstOrDefault(s => s.CodeBar == barcode);
							if (units != null)
							{
								result.Add(r);
							}
						}

					}

				}
				else
				{
					////Search  
					if (!string.IsNullOrEmpty(all))
					{
						all = all.Trim().ToLower();
						customerData = customerData.Where(m => m.Name.ToLower().Contains(all) || m.CategoryName.ToLower().Contains(all) || m.SubCategoryName.ToLower().Contains(all) || m.Brand.ToLower().Contains(all));
					}
					////Search  
					if (!string.IsNullOrEmpty(searchValue))
					{
						all = searchValue.Trim().ToLower();
						customerData = customerData.Where(m => m.Name.ToLower().Contains(all) || m.CategoryName.ToLower().Contains(all) || m.SubCategoryName.ToLower().Contains(all) || m.Brand.ToLower().Contains(all));
					}

					if (!string.IsNullOrEmpty(brand))
					{
						customerData = customerData.Where(s => "" + s.BrandId == brand);
					}
					if (!string.IsNullOrEmpty(category))
					{
						customerData = customerData.Where(s => "" + s.CategoryId == category);
					}
					if (!string.IsNullOrEmpty(subcategory))
					{
						customerData = customerData.Where(s => "" + s.SubCategoryId == subcategory);
					}
					if (string.IsNullOrEmpty(status) || status == "1")
					{
						customerData = customerData.Where(s => s.IsActive);
					}
					else
					{
						customerData = customerData.Where(s => s.IsActive == false);
					}

					result = customerData.ToList();
					if (!string.IsNullOrEmpty(supplier))
					{
						result = result.Where(s => s.Suppliers.Any(s => "" + s.ID == supplier)).ToList();
					}
				}

				if (supplierID > 0 && string.IsNullOrEmpty(barcode))
				{
					result = result.Where(s => s.Suppliers.Any(s => s.ID == supplierID)).ToList();
				}
				//total number of rows count   
				recordsTotal = result.Count();
				//Paging   
				var data = result.Skip(skip).ToList();
				if (pageSize != -1)
				{
					data = data.Take(pageSize).ToList();
				}

				//Obtenemos las urls de las imagenes
				var request = _context.HttpContext.Request;
				var _baseURL = $"{request.Scheme}://{request.Host}";
				if (data != null && data.Any())
				{
					foreach (var item in data)
					{
						string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "article", item.ID.ToString() + ".png");
						if (System.IO.File.Exists(pathFile))
						{
							var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

							item.Photo = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "article", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second); ;
						}
						else
						{
							item.Photo = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "article", "empty.png");
						}
					}
				}

				//Returning Json Data  
				return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

			}
			catch (Exception ex)
			{
				throw;
			}
		}

		[HttpPost]
        public IActionResult GetWarehouseArticleList(long warehouseID = 0, long? articuloID = null, ItemType type = ItemType.Article)
        {
            try
            {
	            if (articuloID != null)
	            {
		            var resultAux = new List<WarehouseStockArticleViewModel>();
					var stocksAux = _dbContext.WarehouseStocks.Include(s=>s.Warehouse).Where(s => s.Warehouse.ID == warehouseID).ToList();
					foreach(var stock in stocksAux)
					{
						if (stock.ItemType == ItemType.Article && type == ItemType.Article)
						{
	                        var article = _dbContext.Articles.Include(s=>s.Brand).Include(s => s.Category).Include(s => s.Brand).Include(s => s.SubCategory).Include(s => s.Items).FirstOrDefault(s =>s.IsActive && s.ID == stock.ItemId);
							if (article == null) continue;
	                        var unit = article.Items.FirstOrDefault(s => s.Number == 1);
	                        var item = new WarehouseStockArticleViewModel()
	                        {
	                            ID = stock.ItemId,
	                            ItemId = stock.ItemId,
	                            Name = article.Name,
	                            ItemType = stock.ItemType,
								Brand = article.Brand?.Name,
	                            Category = article.Category?.Name,
	                            SubCategory = article.SubCategory == null ? "" : article.SubCategory.Name,
	                            Qty = stock.Qty.ToString("0.0000"),
	                            UnitName = unit.Name
	                        };

	                        resultAux.Add(item);
	                    }
						else if (stock.ItemType == ItemType.SubRecipe && type == ItemType.SubRecipe)
						{
	                        var subRecipe = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.ItemUnits).FirstOrDefault(s => s.IsActive && s.ID == stock.ItemId);
							if (subRecipe == null) continue;
	                        var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == 1);
	                        var item = new WarehouseStockArticleViewModel()
	                        {
	                            ID = stock.ItemId,
	                            ItemId = stock.ItemId,
	                            Name = subRecipe.Name,
								ItemType = stock.ItemType,
								Brand = "",
	                            Category = subRecipe.Category?.Name,
	                            SubCategory = subRecipe.SubCategory == null ? "" : subRecipe.SubCategory.Name,
	                            Qty = stock.Qty.ToString("0.0000"),
	                            UnitName = unit.Name
	                        };

	                        resultAux.Add(item);
	                    }
						
					}
				
		            var objResult = resultAux.Where(s => s.ID == articuloID).First();
		            return Json(objResult);
	            }
	            
                var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                // Skiping number of Rows count  
                var start = Request.Form["start"].FirstOrDefault();
                // Paging Length 10,20  
                var length = Request.Form["length"].FirstOrDefault();
                // Sort Column Name  
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                // Sort Column Direction ( asc ,desc)  
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                // Search Value from (Search box)  
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                //Paging Size (10,20,50,100)  
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

				// Getting all Customer data  
				var result = new List<WarehouseStockArticleViewModel>();
				var stocks = _dbContext.WarehouseStocks.Include(s=>s.Warehouse).Where(s => s.Warehouse.ID == warehouseID).ToList();
				foreach(var stock in stocks)
				{
					if (stock.ItemType == ItemType.Article)
					{
                        var article = _dbContext.Articles.Include(s=>s.Brand).Include(s => s.Category).Include(s => s.Brand).Include(s => s.SubCategory).Include(s => s.Items).FirstOrDefault(s =>s.IsActive && s.ID == stock.ItemId);
						if (article == null) continue;
						var unit = article.Items.FirstOrDefault(s => s.Number == 1);
                        var item = new WarehouseStockArticleViewModel()
                        {
                            ID = stock.ItemId,
                            ItemId = stock.ItemId,
                            Name = article.Name,
                            ItemType = stock.ItemType,
							Brand = article.Brand?.Name,
                            Category = article.Category?.Name,
                            SubCategory = article.SubCategory == null ? "" : article.SubCategory.Name,
                            Qty = stock.Qty.ToString("0.0000"),
                            UnitName = unit.Name
                        };

                        result.Add(item);
                    }
					else
					{
                        var subRecipe = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.ItemUnits).FirstOrDefault(s => s.IsActive && s.ID == stock.ItemId);
						if (subRecipe == null) continue;
						var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == 1);
                        var item = new WarehouseStockArticleViewModel()
                        {
                            ID = stock.ItemId,
                            ItemId = stock.ItemId,
                            Name = subRecipe.Name,
							Brand = "",
							ItemType = stock.ItemType,
                            Category = subRecipe.Category?.Name,
                            SubCategory = subRecipe.SubCategory == null ? "" : subRecipe.SubCategory.Name,
                            Qty = stock.Qty.ToString("0.0000"),
                            UnitName = unit.Name
                        };

                        result.Add(item);
                    }
				}

                //Sorting
                ////Search  
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.Trim().ToLower();
                    result = result.Where(m => m.Name.ToLower().Contains(searchValue) || m.Category.ToLower().Contains(searchValue) || m.SubCategory.ToLower().Contains(searchValue)).ToList();
                }
				                
                //total number of rows count   
                recordsTotal = result.Count();
                //Paging   
                var data = result.Skip(skip).ToList();
                if (pageSize != -1)
                {
                    data = data.Take(pageSize).ToList();
                }

                //Returning Json Data  
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public IActionResult AddArticle(long articleID = 0)
		{
			ViewBag.Groups = _dbContext.Groups.ToList();
			ViewBag.PrinterChannels = _dbContext.PrinterChannels.Where(s => s.IsActive).ToList();            
            ViewBag.Taxes = _dbContext.Taxs.Where(s => s.IsActive).ToList();
            ViewBag.ArticleID = articleID;
			var wstocks = new List<WarehouseStock>();
			var products = new List<Product>();
			var phistory = new List<PurchaseHistoryViewModel>();
			if (articleID > 0)
			{
				var article = _dbContext.Articles.Include(s => s.SubCategory).Include(s=>s.Items).FirstOrDefault(s => s.ID == articleID);
				if (article != null )
				{
					if (article.SubCategory != null)
						ViewBag.SubCategory = article.SubCategory.ID;
					ViewBag.MaxUnit = article.MaximumUnit;
					ViewBag.MinUnit = article.MinimumUnit;
					ViewBag.DefaultUnit = article.DefaultUnitNum;
					ViewBag.ScannerUnit = article.ScannerUnit;
				}

				wstocks = _dbContext.WarehouseStocks.Include(s=>s.Warehouse).Where(s => s.ItemId == article.ID && s.ItemType == ItemType.Article && s.Warehouse.IsActive).ToList();		
				var Allproducts = _dbContext.Products.Include(s=>s.Category).Include(s=>s.RecipeItems).ToList();
				foreach(var p in Allproducts)
				{
					var item = p.RecipeItems.Where(s => s.Type == ItemType.Article && s.ItemID == articleID);
					if (item != null && item.Count() > 0)
					{
						products.Add(p);
					}
				}
				var units = article.Items.OrderBy(s=>s.Number).ToList();

				var purchaseHistories = _dbContext.PurchaseOrderItems.Include(s=>s.PurchaseOrder).ThenInclude(s=>s.Supplier).Include(s=>s.Item).Where(s => s.Item.ID == articleID).OrderByDescending(s => s.CreatedDate).ToList();
				foreach(var p in purchaseHistories)
				{
					var unit = units.FirstOrDefault(s => s.Number == p.UnitNum);
					if (unit == null) continue;
					phistory.Add(new PurchaseHistoryViewModel()
					{
						PurchaseDate = p.CreatedDate,
						Supplier = p.PurchaseOrder.Supplier.Name,
						Qty = p.Qty,
						Unit = unit.Name,
						Cost = p.UnitCost,
						Total = p.Qty * p.UnitCost,						
					});
                }
			}

			ViewBag.Stocks = wstocks;
			ViewBag.Products = products;
			ViewBag.Categories = _dbContext.Categories.Where(c => c.IsActive).ToList();
			ViewBag.PurchaseOrderHistory = phistory;

			CheckSystemBrand();
            ViewBag.Brands = _dbContext.Brands.Where(c => c.IsActive).ToList();
			ViewBag.Taxes = _dbContext.Taxs.Where(c => c.IsActive && c.IsInArticle).ToList();

			return View();
		}

		private void CheckSystemBrand()
		{
			var brand = _dbContext.Brands.FirstOrDefault(s => s.IsSystemDefault);
			if (brand == null)
			{
				_dbContext.Brands.Add(new Brand()
				{
					Name = "Default",
					IsSystemDefault = true,
				});

				_dbContext.SaveChanges();
			}
		}

		[HttpGet]
		public JsonResult GetArticle(long articleID)
		{
			var article = _dbContext.Articles.Include(s => s.Suppliers).Include(s=>s.Tax).Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Brand).Include(s => s.Items.OrderBy(s=>s.Number)).FirstOrDefault(s => s.ID == articleID);

            //Obtenemos la URL de la imagen del archivo            
            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "article", article.ID.ToString() + ".png");
            var request = _context.HttpContext.Request;
            var _baseURL = $"{request.Scheme}://{request.Host}";
            if (System.IO.File.Exists(pathFile))
            {
                var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

                article.Photo = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "article", article.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
            }
            else
            {
				article.Photo = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "article", "empty.png");
            }

            return Json(article);
		}

        [HttpGet]
        public JsonResult GetArticleForPurchaseOrder(long articleID, long supplierId)
        {
            var article = _dbContext.Articles.Include(s => s.Suppliers).Include(s => s.Tax).Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Brand).Include(s => s.Items.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == articleID);

            //Obtenemos la URL de la imagen del archivo            
            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "article", article.ID.ToString() + ".png");
            var request = _context.HttpContext.Request;
            var _baseURL = $"{request.Scheme}://{request.Host}";
            if (System.IO.File.Exists(pathFile))
            {
                var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

                article.Photo = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "article", article.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
            }
            else
            {
                article.Photo = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "article", "empty.png");
            }

            // get latest price of purchase order
            var purchaseHistories = _dbContext.PurchaseOrderItems.Include(s => s.PurchaseOrder).ThenInclude(s => s.Supplier).Include(s => s.Item).Where(s => s.Item.ID == articleID && s.PurchaseOrder.Supplier.ID == supplierId).OrderByDescending(s => s.CreatedDate).ToList();
			var lastCost = 0.0m;
			if (purchaseHistories.Count > 0)
			{
				var last = purchaseHistories[0];
				lastCost = last.UnitCost;
				var lastUnitNum = last.UnitNum;

				foreach(var item in article.Items)
				{
					if (item.Number == lastUnitNum)
					{
						item.Cost = lastCost;
					}
				}
			}

            return Json(new { article, lastCost });
        }

        [HttpPost]
		public JsonResult GetConvertedQty([FromBody] ArticleQtyConvertionModel model)
		{
			var articleUnits = _dbContext.Articles.Include(s => s.Items).FirstOrDefault(s => s.ID == model.ArticleID);

			if (articleUnits != null)
			{
				var units = articleUnits.Items.OrderBy(s => s.Number);

				if (model.CurrentNumber > model.OriginalNumber)
				{
					var qty = model.Qty;
					var qty1 = model.Qty1;
					decimal cost = 0;
					for(int i = model.CurrentNumber; i > model.OriginalNumber; i--)
					{
						var unit = units.FirstOrDefault(s => s.Number == i);
						qty *= unit.Rate;
						qty1 *= unit.Rate;
						cost = qty* unit.Cost;
					}
					return Json(new { status = 0, cost = cost, qty = qty.ToString("0.00"), qty1 = qty1.ToString("0.00") }); 
				}
				else
				{
					decimal cost = 0;
					var qty = model.Qty;
					var qty1 = model.Qty1;
					for (int i = model.OriginalNumber; i > model.CurrentNumber;i--)
					{
						var unit = units.FirstOrDefault(s => s.Number == i);
						qty /= unit.Rate;
						qty1 /= unit.Rate;
						cost = qty* unit.Cost;
					}
					return Json(new { status = 0, cost = cost, qty = qty.ToString("0.00"), qty1 = qty1.ToString("0.00") });
				}
			}
			return Json(new { status = 0, qty= model.Qty, cost =  0, qty1 = model.Qty1});

		}
        
		[HttpPost]
        public JsonResult GetConvertedQtyForSubRecipe([FromBody] ArticleQtyConvertionModel model)
        {
            var articleUnits = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == model.ArticleID);

            if (articleUnits != null)
            {
                var units = articleUnits.ItemUnits.OrderBy(s => s.Number);

                if (model.CurrentNumber > model.OriginalNumber)
                {
                    var qty = model.Qty;
                    var qty1 = model.Qty1;
                    for (int i = model.CurrentNumber; i > model.OriginalNumber; i--)
                    {
                        var unit = units.FirstOrDefault(s => s.Number == i);
                        qty *= unit.Rate;
                        qty1 *= unit.Rate;
                    }
                    return Json(new { status = 0, qty = qty.ToString("0.00"), qty1 = qty1.ToString("0.00") });
                }
                else
                {
                    var qty = model.Qty;
                    var qty1 = model.Qty1;
                    for (int i = model.OriginalNumber; i > model.CurrentNumber; i--)
                    {
                        var unit = units.FirstOrDefault(s => s.Number == i);
                        qty /= unit.Rate;
                        qty1 /= unit.Rate;
                    }
                    return Json(new { status = 0, qty = qty.ToString("0.00"), qty1 = qty1.ToString("0.00") });
                }
            }
            return Json(new { status = 0, qty = model.Qty, qty1 = model.Qty1 });

        }

        [HttpPost]
		public JsonResult EditArticle([FromBody] ArticleCreateViewModel model)
		{
			if (model == null)
			{
				return Json(new { status = 1 });
			}

			if (model.ID > 0)
			{
				var article = _dbContext.Articles.Include(s => s.Suppliers).Include(s=>s.Tax).Include(s => s.SubCategory).Include(s => s.Brand).Include(s => s.Items).FirstOrDefault(s => s.ID == model.ID);
				if (article == null)
				{
					return Json(new { status = 1 });
				}

				//var other = _dbContext.Articles.FirstOrDefault(s => s.ID != article.ID && s.Name == model.Name);
				//if (other != null)
				//{
				//	return Json(new { status = 2 });
				//}

				article.Name = model.Name;
				article.IsActive = model.IsActive;
				article.Performance = model.Performance;
				article.MinimumQuantity = model.MinQty;
				article.MaximumQuantity = model.MaxQty;
				article.MinimumUnit = model.MinUnitID;
				article.MaximumUnit = model.MaxUnitID;
				article.DefaultUnitNum = model.DefaultUnitID;
				article.ScannerUnit = model.ScannerUnit;
				article.Barcode = model.Barcode;
				article.DepleteCondition = (DepleteCondition) model.DepleteCondition;
				article.SaftyStock =model.SaftyStock;
				article.PrimarySupplier = model.PrimarySupplier;
				//article.Photo = model.Photo;

                //Guardamos la imagen del archivo
                if (!string.IsNullOrEmpty(model.ImageUpload) && !model.ImageUpload.Contains("http:") && !model.ImageUpload.Contains("https:"))
                {
                    string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "article");

                    if (!Directory.Exists(pathFile))
                    {
                        Directory.CreateDirectory(pathFile);
                    }

                    pathFile = Path.Combine(pathFile, model.ID.ToString() + ".png");

                    model.ImageUpload = model.ImageUpload.Replace('-', '+');
                    model.ImageUpload = model.ImageUpload.Replace('_', '/');
                    model.ImageUpload = model.ImageUpload.Replace("data:image/jpeg;base64,", string.Empty);
                    model.ImageUpload = model.ImageUpload.Replace("data:image/png;base64,", string.Empty);
                    byte[] imageByteArray = Convert.FromBase64String(model.ImageUpload);
                    System.IO.File.WriteAllBytes(pathFile, imageByteArray);

                }
                else if (string.IsNullOrEmpty(model.ImageUpload))
                {
                    try
                    {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "article");
                        pathFile = Path.Combine(pathFile, model.ID.ToString() + ".png");
                        System.IO.File.Delete(pathFile);
                    }
                    catch { }
                }

                var category = _dbContext.Categories.FirstOrDefault(s => s.ID == model.CategoryID);
				article.Category = category;
				var subcategory = _dbContext.SubCategories.FirstOrDefault(s => s.ID == model.SubCategoryID);
				article.SubCategory = subcategory;
				if (model.BrandID == 0)
				{
                    var brand = _dbContext.Brands.FirstOrDefault(s => s.IsSystemDefault);
                    article.Brand = brand;
                }
				else
				{
                    var brand = _dbContext.Brands.FirstOrDefault(s => s.ID == model.BrandID);
                    article.Brand = brand;
                }

				
				var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == model.TaxID);
				article.Tax = tax;

				
				if (model.Suppliers != null && model.Suppliers.Count > 0)
				{
					if (article.Suppliers == null) article.Suppliers = new List<Supplier>();
					article.Suppliers.Clear();
					foreach (var ss in model.Suppliers)
					{
						
						var supplier = _dbContext.Suppliers.FirstOrDefault(s => s.ID == ss);
						if (supplier != null)
						{
							article.Suppliers.Add(supplier);
						}
											
					}
				}

				if (model.ItemUnits != null && model.ItemUnits.Count > 0)
				{
					if (article.Items == null) article.Items = new List<ItemUnit>();
					else
					{
                        foreach (var item in article.Items)
                        {
                            _dbContext.ItemUnits.Remove(item);
                        }
                        article.Items.Clear();
                    }
				

					foreach(var item in model.ItemUnits)
					{
						article.Items.Add(item);
					}
				}

				_dbContext.SaveChanges();
				return Json(new { status = 0, id=article.ID });
			}
			else
			{
				var article = new InventoryItem();

				article.Name = model.Name;
				article.IsActive = model.IsActive;
				article.Performance = model.Performance;
				article.MinimumQuantity = model.MinQty;
				article.MaximumQuantity = model.MaxQty;
				article.MinimumUnit = model.MinUnitID;
				article.MaximumUnit = model.MaxUnitID;
				article.DefaultUnitNum = model.DefaultUnitID;
				article.ScannerUnit = model.ScannerUnit;
				article.DepleteCondition = (DepleteCondition)model.DepleteCondition;
				article.PrimarySupplier = model.PrimarySupplier;
				article.SaftyStock = model.SaftyStock;
				//article.Photo = model.Photo;
				article.Barcode = model.Barcode;
                

                var category = _dbContext.Categories.FirstOrDefault(s => s.ID == model.CategoryID);
				article.Category = category;
				var subcategory = _dbContext.SubCategories.FirstOrDefault(s => s.ID == model.SubCategoryID);
				article.SubCategory = subcategory;
				var brand = _dbContext.Brands.FirstOrDefault(s => s.ID == model.BrandID);
				article.Brand = brand;
				var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == model.TaxID);
				article.Tax = tax;
				
				
				if (model.Suppliers != null && model.Suppliers.Count > 0)
				{
					article.Suppliers= new List<Supplier>();
					foreach (var ss in model.Suppliers)
					{
						var supplier = _dbContext.Suppliers.FirstOrDefault(s => s.ID == ss);
						if (supplier != null)
						{
							article.Suppliers.Add(supplier);
						}
					}
				}
				_dbContext.Articles.Add(article);
				_dbContext.SaveChanges();

                //Guardamos la imagen del archivo
                if (!string.IsNullOrEmpty(model.ImageUpload) && !model.ImageUpload.Contains("http:") && !model.ImageUpload.Contains("https:"))
                {
                    string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "article");

                    if (!Directory.Exists(pathFile))
                    {
                        Directory.CreateDirectory(pathFile);
                    }

                    pathFile = Path.Combine(pathFile, article.ID.ToString() + ".png");

                    model.ImageUpload = model.ImageUpload.Replace('-', '+');
                    model.ImageUpload = model.ImageUpload.Replace('_', '/');
                    model.ImageUpload = model.ImageUpload.Replace("data:image/jpeg;base64,", string.Empty);
                    model.ImageUpload = model.ImageUpload.Replace("data:image/png;base64,", string.Empty);
                    byte[] imageByteArray = Convert.FromBase64String(model.ImageUpload);
                    System.IO.File.WriteAllBytes(pathFile, imageByteArray);

                }
                else if (string.IsNullOrEmpty(model.ImageUpload))
                {
                    try
                    {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "article");
                        pathFile = Path.Combine(pathFile, article.ID.ToString() + ".png");
                        System.IO.File.Delete(pathFile);
                    }
                    catch { }
                }

                if (model.ItemUnits != null && model.ItemUnits.Count > 0)
				{
					article.Items = new List<ItemUnit>();
			
					foreach (var item in model.ItemUnits)
					{
						article.Items.Add(item);
					}
				}

				_dbContext.SaveChanges();
				return Json(new { status = 0, id = article.ID });
			}
		}

		[HttpPost]
		public JsonResult GenerateArticleBarcode(long articleID)
		{
			var product = _dbContext.Articles.FirstOrDefault(s => s.ID == articleID);
			if (product != null)
			{
				if (string.IsNullOrEmpty(product.Barcode))
				{
					var barcode = DateTime.Today.ToString("yyMMdd") + "2" + product.ID.ToString().PadLeft(6, '0');

					product.Barcode = barcode;

					_dbContext.SaveChanges();

					return Json(new { status = 0, barcode = barcode });
				}
			}
			return Json(new { status = 1 });
		}

		[HttpPost]
		public JsonResult GenerateSubrecipeBarcode(long subRecipeID)
		{
			var product = _dbContext.SubRecipes.FirstOrDefault(s => s.ID == subRecipeID);
			if (product != null)
			{
				if (string.IsNullOrEmpty(product.Barcode))
				{
					var barcode = DateTime.Today.ToString("yyMMdd") + "3" + product.ID.ToString().PadLeft(6, '0');

					product.Barcode = barcode;

					_dbContext.SaveChanges();

					return Json(new { status = 0, barcode = barcode });
				}
			}
			return Json(new { status = 1 });
		}


		[HttpPost]
		public JsonResult PrintArticleLabel(ProductPrintLabelRequest request)
		{
			var product = _dbContext.Articles.Include(s=>s.Brand).FirstOrDefault(s => s.ID == request.ProductID);
			if (product != null)
			{
				var label = GenerateArticleLabel(product, request);
				return Json(new { status = 0, label = label });
			}

			return Json(new { status = 1 });
		}
		[HttpPost]
		public JsonResult CalculateArticleMinMax(long articleID)
		{
			var product = _dbContext.Articles.Include(s => s.Suppliers).FirstOrDefault(s => s.ID == articleID);
			if (product != null)
			{
				var supplier = product.Suppliers.FirstOrDefault(s => s.ID == product.PrimarySupplier);
				if (supplier == null)
				{
					return Json(new { status = 2 });
				}

				var lastday = DateTime.Now;
				var lastweekday = lastday.AddDays(-30);

				var stocks = _dbContext.WarehouseStockChangeHistory.Include(s=>s.Warehouse).Where(s =>s.Warehouse.IsActive && s.ItemType == ItemType.Article && s.ReasonType == StockChangeReason.Kitchen && s.ItemId == product.ID && s.CreatedDate.Date >= lastweekday.Date && s.CreatedDate.Date <= lastday.Date).ToList();

				if (stocks.Any())
				{
					decimal totalDepletion = 0;
					foreach(var s in stocks)
					{
						totalDepletion += s.Qty;
					}

					var dailyDepletion = Math.Abs(totalDepletion) / 30.0m;

					var security = dailyDepletion * product.SaftyStock / 100.0m;

					var min = Math.Round(dailyDepletion * supplier.DeliveryTime, 2);
					var max = Math.Round((dailyDepletion * supplier.DeliveryTime + security) * 2.0m, 2);

					product.MinimumQuantity = (double)min;
					product.MaximumQuantity = (double)max;

					_dbContext.SaveChanges();

					return Json(new { status = 0, min, max });
				}
			}

			return Json(new { status = 1 });
		}
        [HttpPost]
        public JsonResult CalculateSubrecipeMinMax(long subrecipeId)
        {
            var product = _dbContext.SubRecipes.FirstOrDefault(s => s.ID == subrecipeId);
            if (product != null)
			{ 
                var lastday = DateTime.Now;
                var lastweekday = lastday.AddDays(-30);

                var stocks = _dbContext.WarehouseStockChangeHistory.Include(s => s.Warehouse).Where(s => s.Warehouse.IsActive && s.ItemType == ItemType.Article && s.ReasonType == StockChangeReason.Kitchen && s.ItemId == product.ID && s.CreatedDate.Date >= lastweekday.Date && s.CreatedDate.Date <= lastday.Date).ToList();

                if (stocks.Any())
                {
                    decimal totalDepletion = 0;
                    foreach (var s in stocks)
                    {
                        totalDepletion += s.Qty;
                    }

                    var dailyDepletion = Math.Abs(totalDepletion) / 30.0m;

                    var security = dailyDepletion * product.SaftyStock / 100.0m;

					decimal defaultDeliveryTime = 1;

                    var min = Math.Round(dailyDepletion * defaultDeliveryTime, 2);
                    var max = Math.Round((dailyDepletion * defaultDeliveryTime + security) * 2.0m, 2);

                    product.MinimumQuantity = min;
                    product.MaximumQuantity = max;

                    _dbContext.SaveChanges();

                    return Json(new { status = 0, min, max });
                }
            }

            return Json(new { status = 1 });
        }
        private string GenerateArticleLabel(InventoryItem product, ProductPrintLabelRequest request)
		{
			string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
			var uniqueFileName = "ProductLabel_" + "_" + DateTime.Now.Ticks + ".pdf";
			var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
			var uploadsUrl = "/temp/" + uniqueFileName;
			var fontSize = 12;
			int width = 3 * 72, height = 3 * 72;
			int marginY = 20, marginX = 10;
			if (request.Dimension == "2x3")
			{
				marginY = 10;
				height = 2 * 72;
				width = 3 * 72;
                fontSize = 10;
            }
			else if (request.Dimension == "3x2")
			{
				height = 3 * 72;
				width = 2 * 72;
			}
			else if (request.Dimension == "2x4")
			{
                marginY = 10;
                height = 2 * 72;
				width = 4 * 72;
			}
			else if (request.Dimension == "2x2")
			{
                marginY = 10;
                height = 2 * 72;
				width = 2 * 72;
				fontSize = 10;
			}
			if (request.Copies <= 0) request.Copies = 1;
			using (var writer = new PdfWriter(uploadsFile))
			{
				var pdf = new PdfDocument(writer);
				var doc = new iText.Layout.Document(pdf, new iText.Kernel.Geom.PageSize(width, height));
				doc.SetMargins(marginY, marginX, marginY, marginX);
				for (int i = 0; i < request.Copies; i++)
				{
					switch (request.Dimension)
					{
						case "2x2":
							{
                                Paragraph header = new Paragraph(product.Name)
                              .SetTextAlignment(TextAlignment.CENTER)
                              .SetMultipliedLeading(0.5f)
                              .SetFontSize(12);
                                doc.Add(header);
                                Paragraph brandlabel = new Paragraph("Brand : " + product.Brand.Name)
                                    .SetMultipliedLeading(1.0f)
                                           .SetFontSize(fontSize);
                                doc.Add(brandlabel);
                                Paragraph packingdatelabel = new Paragraph("Packing Date : " + request.PackingDate.ToString("dd/MM/yyyy"))
                                    .SetMultipliedLeading(1.0f)
                                           .SetFontSize(fontSize);
                                doc.Add(packingdatelabel);
                                Paragraph expiredatelabel = new Paragraph("Expire Date : " + request.ExpireDate.ToString("dd/MM/yyyy"))
                                    .SetMultipliedLeading(1.0f)
                                       .SetFontSize(fontSize);
                                doc.Add(expiredatelabel);
                            }
                           

                            break;
                        case "2x3":
                            {
                                Paragraph header = new Paragraph(product.Name)
                              .SetTextAlignment(TextAlignment.CENTER)
                              .SetMultipliedLeading(0.5f)
                              .SetFontSize(12);
                                doc.Add(header);
                                Paragraph brandlabel = new Paragraph("Brand : " + product.Brand.Name)
                                    .SetMultipliedLeading(1.0f)
                                           .SetFontSize(fontSize);
                                doc.Add(brandlabel);
                                Paragraph packingdatelabel = new Paragraph("Packing Date : " + request.PackingDate.ToString("dd/MM/yyyy"))
                                    .SetMultipliedLeading(1.0f)
                                           .SetFontSize(fontSize);
                                doc.Add(packingdatelabel);
                                Paragraph expiredatelabel = new Paragraph("Expire Date : " + request.ExpireDate.ToString("dd/MM/yyyy"))
                                    .SetMultipliedLeading(1.0f)
                                       .SetFontSize(fontSize);
                                doc.Add(expiredatelabel);
                            }
                            break;
                        case "2x4":
                            {
                                Paragraph header = new Paragraph(product.Name)
                              .SetTextAlignment(TextAlignment.CENTER)
                              .SetMultipliedLeading(0.5f)
                              .SetFontSize(12);
                                doc.Add(header);
                                Paragraph brandlabel = new Paragraph("Brand : " + product.Brand.Name)
                                    .SetMultipliedLeading(0.5f)
                                           .SetFontSize(fontSize);
                                doc.Add(brandlabel);
                                Paragraph packingdatelabel = new Paragraph("Packing Date : " + request.PackingDate.ToString("dd/MM/yyyy"))
                                    .SetMultipliedLeading(0.5f)
                                           .SetFontSize(fontSize);
                                doc.Add(packingdatelabel);
                                Paragraph expiredatelabel = new Paragraph("Expire Date : " + request.ExpireDate.ToString("dd/MM/yyyy"))
                                    .SetMultipliedLeading(0.5f)
                                       .SetFontSize(fontSize);
                                doc.Add(expiredatelabel);
                            }
                            break;
                        case "3x2":
                            {
                                Paragraph header = new Paragraph(product.Name)
                              .SetTextAlignment(TextAlignment.CENTER)
                              .SetMultipliedLeading(0.5f)
                              .SetFontSize(12);
                                doc.Add(header);
                                Paragraph brandlabel = new Paragraph("Brand : " + product.Brand.Name)
                                    .SetMultipliedLeading(1.0f)
                                           .SetFontSize(fontSize);
                                doc.Add(brandlabel);
                                Paragraph packingdatelabel = new Paragraph("Packing Date : " + request.PackingDate.ToString("dd/MM/yyyy"))
                                    .SetMultipliedLeading(1.0f)
                                           .SetFontSize(fontSize);
                                doc.Add(packingdatelabel);
                                Paragraph expiredatelabel = new Paragraph("Expire Date : " + request.ExpireDate.ToString("dd/MM/yyyy"))
                                    .SetMultipliedLeading(1.0f)
                                       .SetFontSize(fontSize);
                                doc.Add(expiredatelabel);
                            }
                            break;
                        case "3x3":
                            {
                                Paragraph header = new Paragraph(product.Name)
                              .SetTextAlignment(TextAlignment.CENTER)
                              .SetMultipliedLeading(1.0f)
                              .SetFontSize(15);
                                doc.Add(header);
                                Paragraph brandlabel = new Paragraph("Brand : " + product.Brand.Name)
                                    .SetMultipliedLeading(1.0f)
                                           .SetFontSize(fontSize);
                                doc.Add(brandlabel);
                                Paragraph packingdatelabel = new Paragraph("Packing Date : " + request.PackingDate.ToString("dd/MM/yyyy"))
                                    .SetMultipliedLeading(1.0f)
                                           .SetFontSize(fontSize);
                                doc.Add(packingdatelabel);
                                Paragraph expiredatelabel = new Paragraph("Expire Date : " + request.ExpireDate.ToString("dd/MM/yyyy"))
                                    .SetMultipliedLeading(1.0f)
                                       .SetFontSize(fontSize);
                                doc.Add(expiredatelabel);
                            }
                            break;
                    }

					Barcode128 code128 = new Barcode128(pdf);
					code128.SetCode(product.Barcode);

					//Here's how to add barcode to PDF with IText7
					var barcodeImg = new iText.Layout.Element.Image(code128.CreateFormXObject(pdf));
					barcodeImg.SetFixedPosition(10, marginY);
					barcodeImg.SetWidth(width - 20);

					barcodeImg.SetHeight(height / 3);

					doc.Add(barcodeImg);
					AreaBreak aB = new AreaBreak();
					if (i < request.Copies - 1)
						doc.Add(aB);
				}

				doc.Close();
			}

			return uploadsUrl;
		}

        [HttpPost]
        public JsonResult PrintSubRecipeItems(ProductPrintLabelRequest request)
        {
            var product = _dbContext.SubRecipes.Include(s=>s.Items).FirstOrDefault(s => s.ID == request.ProductID);
            if (product != null)
            {
                string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
                var uniqueFileName = "Subrecipe_" + "_" + product.ID + ".pdf";
                var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
                var uploadsUrl = "/temp/" + uniqueFileName;
                var paperSize = iText.Kernel.Geom.PageSize.A4;

                using (var writer = new PdfWriter(uploadsFile))
                {
                    var pdf = new PdfDocument(writer);
                    var doc = new iText.Layout.Document(pdf);
                    // REPORT HEADER
                    {
                        string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                        var store = _dbContext.Preferences.First();
                        if (store != null)
                        {
                            var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                            headertable.SetWidth(UnitValue.CreatePercentValue(100));
                            headertable.SetFixedLayout();

                            var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                            var time = "Fecha: " + DateTime.Today.ToString("dd/MM/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                            var cellh1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));

                            Cell cellh2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                            Image img = AlfaHelper.GetLogo(store.Logo);
                            if (img != null)
                            {
                                cellh2.Add(img.ScaleToFit(100, 60));

                            }
                            var cellh3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                            headertable.AddCell(cellh1);
                            headertable.AddCell(cellh2);
                            headertable.AddCell(cellh3);

                            doc.Add(headertable);
                        }
                    }
                    Paragraph header = new Paragraph("Recipe Name : " + product.Name)
                                   .SetTextAlignment(TextAlignment.LEFT)
                                   .SetFontSize(15);
                    doc.Add(header);

                    Paragraph header1 = new Paragraph("Total Costo : $" + product.Total.ToString("N2", CultureInfo.InvariantCulture))
                                   .SetTextAlignment(TextAlignment.LEFT)
                                   .SetFontSize(15);
                    doc.Add(header1);
                    var table = new iText.Layout.Element.Table(new float[] { 2, 1, 1, 1, 1, 1 });
                    table.SetWidth(UnitValue.CreatePercentValue(100));

                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Ingredientes").SetFontSize(12));
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("Brand").SetFontSize(12));
                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Candidad").SetFontSize(12));
                    var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Unidad").SetFontSize(12));
                    var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Costo Unidad").SetFontSize(12));
                    var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Costo Total").SetFontSize(12));
                  
                    table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6);

                    foreach (var d in product.Items)
                    {
                        var subRecipeItem = new SubRecipeItemViewModel(d);

                        if (d.IsArticle)
                        {
                            var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == d.ItemID);
                            subRecipeItem.Article = article;
							var unit = article.Items.FirstOrDefault(s => s.Number == d.UnitNum);

                            var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(article.Name).SetFontSize(11));
                            var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(article.Brand.Name).SetFontSize(11));
                            var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.Qty).SetFontSize(11));
                            var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + unit.Name).SetFontSize(11));
                            var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + unit.Cost).SetFontSize(11));
                            var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + (d.Qty * unit.Cost).ToString("N2", CultureInfo.InvariantCulture)).SetFontSize(11));
                         
                            table.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25).AddCell(cell26);

                        }
                        else
                        {
                            var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == d.ItemID);
                            subRecipeItem.SubRecipe = subRecipe;
                            var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == d.UnitNum);

                            var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(subRecipe.Name).SetFontSize(11));
                            var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Sub recipe").SetFontSize(11));
                            var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.Qty).SetFontSize(11));
                            var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + unit.Name).SetFontSize(11));
                            var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + unit.Cost.ToString("N2", CultureInfo.InvariantCulture)).SetFontSize(11));
                            var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + (d.Qty * unit.Cost).ToString("N2", CultureInfo.InvariantCulture)).SetFontSize(11));
             
                            table.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25).AddCell(cell26);
                        }
                    }

                    doc.Add(table);
                    doc.Close();
                }

                return Json(new { status = 0, label = uploadsUrl });
            }

            return Json(new { status = 1 });
        }

        [HttpPost]
		public JsonResult PrintSubRecipeLabel(ProductPrintLabelRequest request)
		{
			var product = _dbContext.SubRecipes.FirstOrDefault(s => s.ID == request.ProductID);
			if (product != null)
			{
				var label = GenerateSubRecipeLabel(product, request);
				return Json(new { status = 0, label = label });
			}

			return Json(new { status = 1 });
		}

		private string GenerateSubRecipeLabel(SubRecipe product, ProductPrintLabelRequest request)
		{
			string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
			var uniqueFileName = "ProductLabel_" + "_" + DateTime.Now.Ticks + ".pdf";
			var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
			var uploadsUrl = "/temp/" + uniqueFileName;

			int width = 3 * 72, height = 3 * 72;
			int fontSize = 12;
			int marginX = 10, marginY = 20;
			if (request.Dimension == "2x3")
			{
				height = 2 * 72;
				width = 3 * 72;
				fontSize = 10;
				marginY = 10;
			}
			else if (request.Dimension == "3x2")
			{
				height = 3 * 72;
				width = 2 * 72;
			}
			else if (request.Dimension == "2x4")
			{
				height = 2 * 72;
				width = 4 * 72;
                fontSize = 10;
                marginY = 10;
				marginX = 20;
            }
			else if (request.Dimension == "2x2")
			{
				height = 2 * 72;
				width = 2 * 72;
                fontSize = 10;
                marginY = 10;
            }
			if (request.Copies <= 0) request.Copies = 1;
			using (var writer = new PdfWriter(uploadsFile))
			{
				var pdf = new PdfDocument(writer);
				var doc = new iText.Layout.Document(pdf, new iText.Kernel.Geom.PageSize(width, height));
				doc.SetMargins(marginY, 10, marginY, 10);
				for (int i = 0; i < request.Copies; i++)
				{
					
					switch (request.Dimension)
					{
						case "2x2":
							{
                                Paragraph header = new Paragraph(product.Name)
                               .SetTextAlignment(TextAlignment.CENTER)
							   .SetMultipliedLeading(0.8f)
                               .SetFontSize(12);
                                doc.Add(header);

                                Paragraph packingdatelabel = new Paragraph("Packing Date : " + request.PackingDate.ToString("dd/MM/yyyy"))
                                           .SetFontSize(fontSize);
                                doc.Add(packingdatelabel);

                                Paragraph expiredatelabel = new Paragraph("Expire Date : " + request.ExpireDate.ToString("dd/MM/yyyy"))
                                           .SetFontSize(fontSize);
                                doc.Add(expiredatelabel);
                            }

                            break;
                        case "2x3":
                            {
                                Paragraph header = new Paragraph(product.Name)
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetMultipliedLeading(0.5f)
                               .SetFontSize(12);
                                doc.Add(header);

                                Paragraph packingdatelabel = new Paragraph("Packing Date : " + request.PackingDate.ToString("dd/MM/yyyy"))
                                           .SetFontSize(fontSize);
                                doc.Add(packingdatelabel);

                                Paragraph expiredatelabel = new Paragraph("Expire Date : " + request.ExpireDate.ToString("dd/MM/yyyy"))
                                           .SetFontSize(fontSize);
                                doc.Add(expiredatelabel);
                            }
                            break;
                        case "2x4":
                            {
                                Paragraph header = new Paragraph(product.Name)
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetMultipliedLeading(0.5f)
                               .SetFontSize(12);
                                doc.Add(header);

                                Paragraph packingdatelabel = new Paragraph("Packing Date : " + request.PackingDate.ToString("dd/MM/yyyy"))
                                           .SetFontSize(fontSize);
                                doc.Add(packingdatelabel);

                                Paragraph expiredatelabel = new Paragraph("Expire Date : " + request.ExpireDate.ToString("dd/MM/yyyy"))
                                           .SetFontSize(fontSize);
                                doc.Add(expiredatelabel);
                            }
                            break;
                        case "3x2":
                            {
                                Paragraph header = new Paragraph(product.Name)
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetMultipliedLeading(1.0f)
                               .SetFontSize(12);
                                doc.Add(header);

                                Paragraph packingdatelabel = new Paragraph("Packing Date : " + request.PackingDate.ToString("dd/MM/yyyy"))
									.SetMultipliedLeading(1.0f)
                                           .SetFontSize(fontSize);
                                doc.Add(packingdatelabel);

                                Paragraph expiredatelabel = new Paragraph("Expire Date : " + request.ExpireDate.ToString("dd/MM/yyyy"))
                                    .SetMultipliedLeading(1.0f)
                                           .SetFontSize(fontSize);

                                doc.Add(expiredatelabel);
                            }
                            break;
                        case "3x3":
                            {
                                Paragraph header = new Paragraph(product.Name)
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetMultipliedLeading(1.0f)
                               .SetFontSize(15);
                                doc.Add(header);

                                Paragraph packingdatelabel = new Paragraph("Packing Date : " + request.PackingDate.ToString("dd/MM/yyyy"))
                                           .SetFontSize(fontSize);
                                doc.Add(packingdatelabel);

                                Paragraph expiredatelabel = new Paragraph("Expire Date : " + request.ExpireDate.ToString("dd/MM/yyyy"))
                                           .SetFontSize(fontSize);
                                doc.Add(expiredatelabel);
                            }
                            break;
                    }


                    Barcode128 code128 = new Barcode128(pdf);
					code128.SetCode(product.Barcode);


					//Here's how to add barcode to PDF with IText7
					var barcodeImg = new iText.Layout.Element.Image(code128.CreateFormXObject(pdf));
					barcodeImg.SetFixedPosition(marginX, marginY);
					barcodeImg.SetWidth(width - marginX * 2);

					barcodeImg.SetHeight(height / 3);

					doc.Add(barcodeImg);
					AreaBreak aB = new AreaBreak();
					if (i < request.Copies - 1)
						doc.Add(aB);
				}

				doc.Close();
			}

			return uploadsUrl;
		}

		#endregion

		#region Brand

		// brand

		[HttpPost]
		public JsonResult GetAllBrands()
		{
			return Json(_dbContext.Brands.Where(s=>!s.IsSystemDefault).ToList());
		}

		[HttpPost]
		public IActionResult GetBrandList()
		{
			try
			{
				var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
				// Skiping number of Rows count  
				var start = Request.Form["start"].FirstOrDefault();
				// Paging Length 10,20  
				var length = Request.Form["length"].FirstOrDefault();
				// Sort Column Name  
				var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
				// Sort Column Direction ( asc ,desc)  
				var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
				// Search Value from (Search box)  
				var searchValue = Request.Form["search[value]"].FirstOrDefault();

				//Paging Size (10,20,50,100)  
				int pageSize = length != null ? Convert.ToInt32(length) : 0;
				int skip = start != null ? Convert.ToInt32(start) : 0;
				int recordsTotal = 0;

				// Getting all Customer data  
				var customerData = (from s in _dbContext.Brands
									where !s.IsSystemDefault
									select s);

				//Sorting
				if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				{
					try
					{
                        customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
					catch { }
					
				}
				////Search  
				if (!string.IsNullOrEmpty(searchValue))
				{
					searchValue = searchValue.Trim().ToLower();
					customerData = customerData.Where(m => m.Name.ToLower().Contains(searchValue) );
				}

				//total number of rows count   
				recordsTotal = customerData.Count();
				//Paging   
				var data = customerData.Skip(skip).ToList();
				if (pageSize != -1)
				{
					data = data.Take(pageSize).ToList();
				}
				//Returning Json Data  
				return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

			}
			catch (Exception ex)
			{
				throw;
			}
		}

		[HttpPost]
		public JsonResult EditBrand(Brand request)
		{
			try
			{
				var existing = _dbContext.Brands.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					var otherexisting = _dbContext.Brands.FirstOrDefault(x => x.ID != request.ID && x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					existing.Name = request.Name;
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}
				else
				{
					var otherexisting = _dbContext.Brands.FirstOrDefault(x => x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					_dbContext.Brands.Add(request);
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}

			}
			catch { }

			return Json(new { status = 1 });

		}

		[HttpPost]
		public JsonResult DeleteBrand(long brandId)
		{
			var existing = _dbContext.Brands.FirstOrDefault(x => x.ID == brandId);
			if (existing != null)
			{
				_dbContext.Brands.Remove(existing);
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}


		#endregion

		#region Unit

		// unit

		[HttpPost]
		public JsonResult GetAllUnits()
		{
			return Json(_dbContext.Units.ToList());
		}

		[HttpPost]
		public IActionResult GetUnitList()
		{
			try
			{
				var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
				// Skiping number of Rows count  
				var start = Request.Form["start"].FirstOrDefault();
				// Paging Length 10,20  
				var length = Request.Form["length"].FirstOrDefault();
				// Sort Column Name  
				var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
				// Sort Column Direction ( asc ,desc)  
				var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
				// Search Value from (Search box)  
				var searchValue = Request.Form["search[value]"].FirstOrDefault();

				//Paging Size (10,20,50,100)  
				int pageSize = length != null ? Convert.ToInt32(length) : 0;
				int skip = start != null ? Convert.ToInt32(start) : 0;
				int recordsTotal = 0;

				// Getting all Customer data  
				var customerData = (from s in _dbContext.Units
									select s);

				//Sorting
				if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				{
					try
					{
                        customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
					catch { }
					
				}
				////Search  
				if (!string.IsNullOrEmpty(searchValue))
				{
					searchValue = searchValue.Trim().ToLower();
					customerData = customerData.Where(m => m.Name.ToLower().Contains(searchValue));
				}

				//total number of rows count   
				recordsTotal = customerData.Count();
				//Paging   
				var data = customerData.Skip(skip).ToList();
				if (pageSize != -1)
				{
					data = data.Take(pageSize).ToList();
				}
				//Returning Json Data  
				return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

			}
			catch (Exception ex)
			{
				throw;
			}
		}

		[HttpPost]
		public JsonResult EditUnit(Unit request)
		{
			try
			{
				var existing = _dbContext.Units.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					var otherexisting = _dbContext.Units.FirstOrDefault(x => x.ID != request.ID && x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					existing.Name = request.Name;
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}
				else
				{
					var otherexisting = _dbContext.Units.FirstOrDefault(x => x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					_dbContext.Units.Add(request);
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}

			}
			catch { }

			return Json(new { status = 1 });

		}

		[HttpPost]
		public JsonResult DeleteUnit(long unitId)
		{
			var existing = _dbContext.Units.FirstOrDefault(x => x.ID == unitId);
			if (existing != null)
			{
				_dbContext.Units.Remove(existing);
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}

		#endregion

		#region Purchase Order
		// purchase order
		public IActionResult PurchaseOrderList()
		{
			return View();
		}

		[HttpPost]
		public IActionResult GetPurchaseOrderList()
		{
			try
			{
				var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
				// Skiping number of Rows count  
				var start = Request.Form["start"].FirstOrDefault();
				// Paging Length 10,20  
				var length = Request.Form["length"].FirstOrDefault();
				// Sort Column Name  
				var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
				// Sort Column Direction ( asc ,desc)  
				var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
				// Search Value from (Search box)  
				var searchValue = Request.Form["search[value]"].FirstOrDefault();

				//Paging Size (10,20,50,100)  
				int pageSize = length != null ? Convert.ToInt32(length) : 0;
				int skip = start != null ? Convert.ToInt32(start) : 0;
				int recordsTotal = 0;

				// Getting all Customer data  
				var customerData = _dbContext.PurchaseOrders.Include(s => s.Supplier).Include(s=>s.Warehouse).OrderByDescending (s=>s.OrderTime).Select(s => new
				{
					s.ID,
					SupplierId = s.Supplier.ID,
					Supplier = s.Supplier.Name,
					Warehouse = s.Warehouse.WarehouseName,
					NCF = s.NCF,
					Status = s.Status,
					RealOrderDate = s.OrderTime,
					OrderDate = s.OrderTime.ToShortDateString(),
					SubTotal = s.SubTotal,
					Total = s.Total,
				});
				//Sorting
				if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				{
					try
					{
						customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
					}
					catch { }

				}

				var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
				var datefrom = Request.Form["columns[1][search][value]"].FirstOrDefault();
				var supplier = Request.Form["columns[2][search][value]"].FirstOrDefault();
				var status = Request.Form["columns[3][search][value]"].FirstOrDefault();
				var dateto = Request.Form["columns[4][search][value]"].FirstOrDefault();

				////Search  
				if (!string.IsNullOrEmpty(all))
				{
					all = all.Trim().ToLower();
					customerData = customerData.Where(m => m.Warehouse.ToLower().Contains(all) || m.Supplier.ToLower().Contains(all) || m.NCF.ToLower().Contains(all));
				}

				if (!string.IsNullOrEmpty(supplier))
				{
					customerData = customerData.Where(s => "" + s.SupplierId == supplier);
				}
				if (!string.IsNullOrEmpty(status))
				{
					customerData = customerData.Where(s => "" + s.Status == status);
				}
				if (!string.IsNullOrEmpty(datefrom))
				{
					try
					{
						var orderDate = DateTime.ParseExact(datefrom, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						customerData = customerData.Where(s => s.RealOrderDate.Date >= orderDate.Date);
					}
					catch { }
					
				}
                if (!string.IsNullOrEmpty(dateto))
                {
                    try
                    {
                        var orderDate = DateTime.ParseExact(dateto, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        customerData = customerData.Where(s => s.RealOrderDate.Date <= orderDate.Date);
                    }
                    catch { }

                }
                //total number of rows count   
                recordsTotal = customerData.Count();
				//Paging   
				var data = customerData.Skip(skip).ToList();
				if (pageSize != -1)
				{
					data = data.Take(pageSize).ToList();
				}
				//Returning Json Data  
				return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

			}
			catch (Exception ex)
			{
				throw;
			}
		}

        [HttpPost]
		public JsonResult DownloadPurchaseOrderExcel(string search, string supplier, string datefrom, string dateto, string status)
		{
			try
			{
				var customerData = _dbContext.PurchaseOrders.Include(s => s.Supplier).Include(s => s.Warehouse).OrderByDescending(s => s.OrderTime).Select(s => new
				{
					s.ID,
					SupplierId = s.Supplier.ID,
					Supplier = s.Supplier.Name,
					Warehouse = s.Warehouse.WarehouseName,
					NCF = s.NCF,
					Status = s.Status,
					RealOrderDate = s.OrderTime,
					OrderDate = s.OrderTime.ToShortDateString(),
					SubTotal = s.SubTotal,
					Total = s.Total,
				});
				////Search  
				if (!string.IsNullOrEmpty(search))
				{
					search = search.Trim().ToLower();
					customerData = customerData.Where(m => m.Warehouse.ToLower().Contains(search) || m.Supplier.ToLower().Contains(search) || m.NCF.ToLower().Contains(search));
				}

				if (!string.IsNullOrEmpty(supplier))
				{
					customerData = customerData.Where(s => "" + s.SupplierId == supplier);
				}
				if (!string.IsNullOrEmpty(status))
				{
					customerData = customerData.Where(s => "" + s.Status == status);
				}
				if (!string.IsNullOrEmpty(datefrom))
				{
					try
					{
						var orderDate = DateTime.ParseExact(datefrom, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						customerData = customerData.Where(s => s.RealOrderDate.Date >= orderDate.Date);
					}
					catch { }

				}
				if (!string.IsNullOrEmpty(dateto))
				{
					try
					{
						var orderDate = DateTime.ParseExact(dateto, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						customerData = customerData.Where(s => s.RealOrderDate.Date <= orderDate.Date);
					}
					catch { }

				}

				var purchaseOrders = customerData.ToList();

				if (purchaseOrders.Count == 0)
				{
					return Json(new { status = 1, message = "There are no items to export." });
				}

				var csvBuilder = new StringBuilder();

				csvBuilder.AppendLine("Purchase Order List");
				if (!string.IsNullOrEmpty(search))
				{
					csvBuilder.AppendLine("Search Text: ," + search);
				}
				if (!string.IsNullOrEmpty(supplier))
				{
					csvBuilder.AppendLine("Supplier: ," + purchaseOrders[0].Supplier);
				}
				if (!string.IsNullOrEmpty(datefrom))
				{
					csvBuilder.AppendLine("From : ," + datefrom);
				}
				if (!string.IsNullOrEmpty(dateto))
				{
					csvBuilder.AppendLine("To : ," + dateto);
				}
				if (!string.IsNullOrEmpty(status))
				{
					csvBuilder.AppendLine("Status : ," + purchaseOrders[0].Status);
				}

				var header = "ID #,Supplier,Warehouse,NCF,Date,Sub Total, Grand Total, Status";

				csvBuilder.AppendLine(header);

				foreach (var temp in purchaseOrders)
				{

					var line = $"\"{temp.ID}\",\"{temp.Supplier}\",{temp.Warehouse},{temp.NCF},{temp.OrderDate},\"{temp.SubTotal.ToString("0.00")}\",\"{temp.Total.ToString("0.00")}\",{temp.Status}";
					csvBuilder.AppendLine(line);
				}

				string tempFolder = Path.Combine(_env.WebRootPath, "temp");
				var uniqueFileName = "PurchaseOrder_" + DateTime.Now.Ticks + ".csv";
				var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
				if (!Directory.Exists(tempFolder))
				{
					Directory.CreateDirectory(tempFolder);
				}

				var uploadsUrl = "/temp/" + uniqueFileName;

				System.IO.File.WriteAllText(uploadsFile, csvBuilder.ToString(), System.Text.Encoding.UTF8);

				return Json(new { status = 0, result = uploadsUrl });
			}
			catch (Exception ex)
			{
				return Json(new { status = 1, message = ex.Message });
			}

		}

		[HttpPost]
		public JsonResult DownloadPurchaseOrderPDF(string search, string supplier, string datefrom, string dateto, string status)
		{
			string tempFolder = Path.Combine(_env.WebRootPath, "temp");

			var customerData = _dbContext.PurchaseOrders.Include(s => s.Supplier).Include(s => s.Warehouse).OrderByDescending(s => s.OrderTime).Select(s => new
			{
				s.ID,
				SupplierId = s.Supplier.ID,
				Supplier = s.Supplier.Name,
				Warehouse = s.Warehouse.WarehouseName,
				NCF = s.NCF,
				Status = s.Status,
				RealOrderDate = s.OrderTime,
				OrderDate = s.OrderTime.ToShortDateString(),
				SubTotal = s.SubTotal,
				Total = s.Total,
			});
			////Search  
			if (!string.IsNullOrEmpty(search))
			{
				search = search.Trim().ToLower();
				customerData = customerData.Where(m => m.Warehouse.ToLower().Contains(search) || m.Supplier.ToLower().Contains(search) || m.NCF.ToLower().Contains(search));
			}

			if (!string.IsNullOrEmpty(supplier))
			{
				customerData = customerData.Where(s => "" + s.SupplierId == supplier);
			}
			if (!string.IsNullOrEmpty(status))
			{
				customerData = customerData.Where(s => "" + s.Status == status);
			}
			if (!string.IsNullOrEmpty(datefrom))
			{
				try
				{
					var orderDate = DateTime.ParseExact(datefrom, "dd-MM-yyyy", CultureInfo.InvariantCulture);
					customerData = customerData.Where(s => s.RealOrderDate.Date >= orderDate.Date);
				}
				catch { }

			}
			if (!string.IsNullOrEmpty(dateto))
			{
				try
				{
					var orderDate = DateTime.ParseExact(dateto, "dd-MM-yyyy", CultureInfo.InvariantCulture);
					customerData = customerData.Where(s => s.RealOrderDate.Date <= orderDate.Date);
				}
				catch { }

			}

			var purchaseOrders = customerData.ToList();


			if (purchaseOrders.Count == 0)
			{
				return Json(new { status = 1 });
			}
			var uniqueFileName = "PurchaseOrder_" + "_" + DateTime.Now.Ticks + ".pdf";
			var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
			var uploadsUrl = "/temp/" + uniqueFileName;
			var paperSize = iText.Kernel.Geom.PageSize.A4;
			{
				using (var writer = new PdfWriter(uploadsFile))
				{
					var pdf = new PdfDocument(writer);
					var doc = new iText.Layout.Document(pdf);

					Paragraph header = new Paragraph("Purchase Order List")
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(20);
					doc.Add(header);

					if (!string.IsNullOrEmpty(search))
					{
						Paragraph subheader = new Paragraph("Search Text: " + search)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}

					if (!string.IsNullOrEmpty(supplier))
					{
						Paragraph subheader = new Paragraph("Supplier: " + purchaseOrders[0].Supplier)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					
					if (!string.IsNullOrEmpty(datefrom))
					{
						Paragraph subheader = new Paragraph("Date From: " + datefrom)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					if (!string.IsNullOrEmpty(dateto))
					{
						Paragraph subheader = new Paragraph("Date To: " + dateto)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					if (!string.IsNullOrEmpty(status))
					{
						Paragraph subheader = new Paragraph("Status: " + purchaseOrders[0].Status)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					// Table
					doc.Add(new Paragraph(""));
					var table = new Table(new float[] { 1, 3, 3, 2, 2, 2, 2, 2 });
					table.SetWidth(UnitValue.CreatePercentValue(100));
					// headers
					var cell1 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("ID #"));
					var cell2 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Supplier"));
					var cell3 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Warehouse"));
					var cell4 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("NCF"));
					var cell5 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Date"));
					var cell6 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Sub Total"));
					var cell7 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Grand Total"));
					var cell8 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Status"));
					table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell7).AddCell(cell8);

					foreach (var p in purchaseOrders)
					{
						var cell11 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.ID));
						var cell12 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.Supplier));
						var cell13 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.Warehouse));
						var cell14 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.NCF));
						var cell15 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.OrderDate));
						var cell16 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.SubTotal.ToString("$0.00", CultureInfo.InvariantCulture)));
						var cell17 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Total.ToString("$0.00", CultureInfo.InvariantCulture)));
						var cell18 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Status));

						table.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16).AddCell(cell17).AddCell(cell18);
					}
					doc.Add(table);

					doc.Add(new Paragraph(""));
					doc.Close();
				}
			}

			return Json(new { status = 0, url = uploadsUrl });
		}

		[HttpPost]
		public JsonResult DownloadPurchaseOrderReport(long PurchaseOrderID)
		{
            var purchaseOrder = _dbContext.PurchaseOrders.Include(s => s.Supplier).Include(s => s.Warehouse).Include(s => s.Items).ThenInclude(s=>s.Tax).Include(s => s.Items).ThenInclude(x => x.Item).ThenInclude(s=>s.Brand).Include(s => s.Items).ThenInclude(x => x.Item).ThenInclude(s => s.Items).FirstOrDefault(s => s.ID == PurchaseOrderID);

			var supplier = purchaseOrder.Supplier;

            string tempFolder = Path.Combine(_env.WebRootPath, "temp");
            var uniqueFileName = "PurchaseOrder_" + PurchaseOrderID + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;
            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
				var store = _dbContext.Preferences.First();
				{
					
					if (store != null)
					{
						var headertable = new Table(new float[] { 4, 1, 4 });
						headertable.SetWidth(UnitValue.CreatePercentValue(100));
						headertable.SetFixedLayout();

						var info = store.Name + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
						var time = "Fecha: " + DateTime.Today.ToString("dd/MM/yyyy");

						var cellh1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));
						Cell cellh2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
						var img = AlfaHelper.GetLogo(store.Logo);
						if (img != null)
						{
							cellh2.Add(img.ScaleToFit(100, 60));

						}

						var cellh3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

						headertable.AddCell(cellh1);
						headertable.AddCell(cellh2);
						headertable.AddCell(cellh3);

						doc.Add(headertable);
					}
				}
				SolidLine line = new SolidLine(2f);
				line.SetColor(ColorConstants.BLACK);
				LineSeparator ls = new LineSeparator(line);
				ls.SetWidth(UnitValue.CreatePercentValue(100));
				ls.SetMarginTop(5);

				doc.Add(ls);

				var table = new Table(new float[] { 6, 3, 2 });
                table.SetWidth(UnitValue.CreatePercentValue(100));
				table.SetFixedLayout();
                {
                    var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT);
					cell11.Add(new Paragraph(store.Name).SetBold().SetFontColor(ColorConstants.DARK_GRAY).SetFontSize(10));
					cell11.Add(new Paragraph(""));
					cell11.Add(new Paragraph(store.Company).SetFontSize(10));
					cell11.Add(new Paragraph(store.Address1).SetFontSize(10));
					cell11.Add(new Paragraph(store.City + ", " + store.State + ", " + store.PostalCode).SetFontSize(10));
					cell11.Add(new Paragraph(store.Country).SetFontSize(10));
										
					var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT);
					cell12.Add(new Paragraph("PURCHASE ORDER # ").SetBold().SetFontColor(ColorConstants.DARK_GRAY).SetFontSize(10));
					cell12.Add(new Paragraph("PURCHASE ORDER DATE ").SetBold().SetFontColor(ColorConstants.DARK_GRAY).SetFontSize(10));

					var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);
					cell13.Add(new Paragraph(purchaseOrder.ID.ToString("000000")).SetFontSize(10));
					cell13.Add(new Paragraph(purchaseOrder.OrderTime.ToString("dd-MM-yyyy")).SetFontSize(10));

					table.AddCell(cell11).AddCell(cell12).AddCell(cell13);
				}
              

				doc.Add(table);
				doc.Add(new Paragraph());
				doc.Add(new Paragraph());
				doc.Add(new Paragraph());

				var table1 = new Table(new float[] { 1, 3, 2, 2 });
				table1.SetWidth(UnitValue.CreatePercentValue(100));
				table1.SetFixedLayout();
				{
					var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);
					cell11.Add(new Paragraph("VENDOR:").SetBold().SetFontColor(ColorConstants.DARK_GRAY).SetFontSize(9));

					var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT);
					cell12.Add(new Paragraph("" +supplier.Name).SetFontSize(10));
					cell12.Add(new Paragraph("" +supplier.Direction).SetFontSize(10));
					cell12.Add(new Paragraph("" +supplier.City ).SetFontSize(10));
					cell12.Add(new Paragraph("" +supplier.Country).SetFontSize(10));


					var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);
					cell13.Add(new Paragraph("SHIP TO:").SetBold().SetFontColor(ColorConstants.DARK_GRAY).SetFontSize(9));

					var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT);
					cell14.Add(new Paragraph("" + store.Company).SetFontSize(10));
					cell14.Add(new Paragraph("" + store.Address1).SetFontSize(10));
					cell14.Add(new Paragraph("" + store.City + ", " + store.State + ", " + store.PostalCode).SetFontSize(10));
					cell14.Add(new Paragraph("" + store.Country).SetFontSize(10));
					table1.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14);
				}
				doc.Add(table1);
				doc.Add(new Paragraph());
				doc.Add(new Paragraph());
				doc.Add(new Paragraph());
		

				var itable = new Table(new float[] { 3,3,1,1,1,1,1,1 });
                itable.SetWidth(UnitValue.CreatePercentValue(100));
                var cell1 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Artículo").SetFontSize(10));
                var cell2 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Marca").SetFontSize(10));
                var cell3 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("QTY").SetFontSize(10));
                var cell4 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Unidad").SetFontSize(10));
                var cell5 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Precio").SetFontSize(10));
                var cell6 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Importe").SetFontSize(10));
                var cell8 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Monto Impuesto").SetFontSize(10));
                var cell9 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Total neto").SetFontSize(10));
				itable.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell8).AddCell(cell9);

                decimal allSubTotal = 0;
                decimal allTotal = 0;
                decimal allTax = 0;
                decimal allTaxable = 0;
                decimal allExtento = 0;
                decimal allDiscount = 0;
				bool isTaxable = purchaseOrder.Supplier.IsTaxIncluded;

				var taxitems = new List<TaxTempItem>();
                foreach (var item in purchaseOrder.Items)
				{
                    var unit = item.Item.Items.FirstOrDefault(s => s.Number == item.UnitNum);

					decimal discount = item.Discount;
					decimal tax = item.TaxRate;

					var subTotal = item.Qty * item.UnitCost;
					var taxable = subTotal * 100 / (100 + item.TaxRate);

					decimal subvalue = 0;
					if (!isTaxable)
					{
						if (purchaseOrder.Discount > 0)
						{
							if (purchaseOrder.IsDiscountPercent)
							{
                                discount = subTotal * purchaseOrder.Discount / 100;
                            }
							else
							{
								discount = purchaseOrder.Discount;
							}
						}
						else
						{
							if (purchaseOrder.IsDiscountPercentItems)
							{
								discount = subTotal * discount / 100;
							}
						}
						subvalue = subTotal - discount;
					}
					else
					{
                        if (purchaseOrder.Discount > 0)
                        {
                            if (purchaseOrder.IsDiscountPercent)
                            {
                                discount = taxable * purchaseOrder.Discount / 100;
                            }
                            else
                            {
                                discount = purchaseOrder.Discount;
                            }
                        }
                        else
                        {
                            if (purchaseOrder.IsDiscountPercentItems)
                            {
                                discount = taxable * discount / 100;
                            }
                        }
                        subvalue = taxable - discount;
                    }
					decimal taxAmount = 0;
					if (item.Tax != null)
					{
                        taxAmount = (taxable - discount) * (decimal)item.Tax.TaxValue / 100;
                        if (!isTaxable)
                        {
                            taxAmount = subvalue * (decimal)item.Tax.TaxValue / 100;
                        }

						var exist = taxitems.FirstOrDefault(s => s.Name == item.Tax.TaxName);
						if (exist == null)
						{
							taxitems.Add(new TaxTempItem()
							{
								Name = item.Tax.TaxName,
								Value = "" + item.Tax.TaxValue,
								Amount = taxAmount
							});
						}
						else
						{
							exist.Amount += taxAmount;
						}
                    }

                    if (item.Tax.TaxValue > 0)
                    {
                        if (isTaxable)
                        {
                            allTaxable += taxable;
                        }
                        else
                        {
                            allTaxable += subTotal;
                        }

                    }
                    else
                    {
                        if (isTaxable)
                        {
                            allExtento += taxable;
                        }
                        else
                        {
                            allExtento += subTotal;
                        }

                    }

                    var total = subvalue + taxAmount;

                    var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(item.Item.Name).SetFontSize(9));
                    var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(item.Item.Brand.Name).SetFontSize(9));
                    var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + item.Qty).SetFontSize(9));
                    var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(unit.Name).SetFontSize(9));
                    var cell15 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(item.UnitCost.ToString("0.0000", CultureInfo.InvariantCulture)).SetFontSize(9));
                    var cell16 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(subvalue.ToString("$0.00", CultureInfo.InvariantCulture)).SetFontSize(9));
                    var cell17 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(taxAmount.ToString("$0.00", CultureInfo.InvariantCulture)).SetFontSize(9));
                    var cell19 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(total.ToString("$0.00", CultureInfo.InvariantCulture)).SetFontSize(9));
                    itable.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16).AddCell(cell17).AddCell(cell19);
                }



				doc.Add(itable);
				doc.Add(new Paragraph());
				doc.Add(new Paragraph());
				doc.Add(new Paragraph());

				var stable = new Table(new float[] { 1,1 }).SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                stable.SetWidth(UnitValue.CreatePercentValue(50));
				{
                    var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Base impnible").SetFontSize(10).SetFontColor(ColorConstants.GRAY));
                    var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(allTaxable.ToString("$0.00", CultureInfo.InvariantCulture)).SetFontSize(10));
                    stable.AddCell(cell11).AddCell(cell12);
                }
                {
                    var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Monto extento").SetFontColor(ColorConstants.GRAY).SetFontSize(10));
                    var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(allExtento.ToString("$0.00", CultureInfo.InvariantCulture)).SetFontSize(10));
                    stable.AddCell(cell11).AddCell(cell12);
                }
                {
                    var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Transporte").SetFontColor(ColorConstants.GRAY).SetFontSize(10));
                    var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(purchaseOrder.Shipping.ToString("$0.00", CultureInfo.InvariantCulture)).SetFontSize(10));
                    stable.AddCell(cell11).AddCell(cell12);
                }
                {
                    var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Descuento").SetFontColor(ColorConstants.GRAY).SetFontSize(10));
                    var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(purchaseOrder.DiscountTotal.ToString("$0.00", CultureInfo.InvariantCulture)).SetFontSize(10));
                    stable.AddCell(cell11).AddCell(cell12);
                }
                {
                    var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Monto Bruto").SetFontColor(ColorConstants.GRAY).SetFontSize(10));
                    var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(purchaseOrder.SubTotal.ToString("$0.00", CultureInfo.InvariantCulture)).SetFontSize(10));
                    stable.AddCell(cell11).AddCell(cell12);
                }
                {
                    var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Impuesto").SetFontColor(ColorConstants.GRAY).SetFontSize(10));
                    var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(purchaseOrder.TaxTotal.ToString("$0.00", CultureInfo.InvariantCulture)).SetFontSize(10));
                    stable.AddCell(cell11).AddCell(cell12);
                }
                {
                    var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Total neto").SetFontSize(10));
                    var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(purchaseOrder.Total.ToString("$0.00", CultureInfo.InvariantCulture)).SetFontSize(10));
                    stable.AddCell(cell11).AddCell(cell12);
                }

				stable.SetFixedPosition(250, 50, 250);
				doc.Add(stable);
                //doc.Add(new Paragraph(purchaseOrder.Description));



                doc.Close();
            }

            return Json(new { status = 0, url = uploadsUrl });
        }

        [HttpPost]
        public JsonResult DownloadMoveArticleReport(int MoveArticleID)
        {
            var moveArticle = _dbContext.MoveArticles.Include(s => s.Items).Include(s => s.FromWarehouse).Include(s => s.ToWarehouse).FirstOrDefault(s => s.ID == MoveArticleID);


            string tempFolder = Path.Combine(_env.WebRootPath, "temp");
            var uniqueFileName = "PurchaseOrder_" + MoveArticleID + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;
            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
                var store = _dbContext.Preferences.First();
                {

                    if (store != null)
                    {
                        var headertable = new Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();

                        var info = store.Name + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                        var time = "Fecha: " + DateTime.Today.ToString("dd/MM/yyyy");

                        var cellh1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));
                        Cell cellh2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        var img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cellh2.Add(img.ScaleToFit(100, 60));

                        }

                        var cellh3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cellh1);
                        headertable.AddCell(cellh2);
                        headertable.AddCell(cellh3);

                        doc.Add(headertable);
                    }
                }
                SolidLine line = new SolidLine(2f);
                line.SetColor(ColorConstants.BLACK);
                LineSeparator ls = new LineSeparator(line);
                ls.SetWidth(UnitValue.CreatePercentValue(100));
                ls.SetMarginTop(5);

                doc.Add(ls);

                var table = new Table(new float[] { 6, 3, 2 });
                table.SetWidth(UnitValue.CreatePercentValue(100));
                table.SetFixedLayout();
                {
                    var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT);
                    cell11.Add(new Paragraph(store.Name).SetBold().SetFontColor(ColorConstants.DARK_GRAY).SetFontSize(10));
                    cell11.Add(new Paragraph(""));
                    cell11.Add(new Paragraph(store.Company).SetFontSize(10));
                    cell11.Add(new Paragraph(store.Address1).SetFontSize(10));
                    cell11.Add(new Paragraph(store.City + ", " + store.State + ", " + store.PostalCode).SetFontSize(10));
                    cell11.Add(new Paragraph(store.Country).SetFontSize(10));

                    var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT);
                    cell12.Add(new Paragraph("TRANSFERENCIA # ").SetBold().SetFontColor(ColorConstants.DARK_GRAY).SetFontSize(10));
                    cell12.Add(new Paragraph("TRANSFERENCIA DATE ").SetBold().SetFontColor(ColorConstants.DARK_GRAY).SetFontSize(10));

                    var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);
                    cell13.Add(new Paragraph(moveArticle.ID.ToString("000000")).SetFontSize(10));
                    cell13.Add(new Paragraph(moveArticle.MoveDate.ToString("dd-MM-yyyy")).SetFontSize(10));

                    table.AddCell(cell11).AddCell(cell12).AddCell(cell13);
                }


                doc.Add(table);
                doc.Add(new Paragraph());
                doc.Add(new Paragraph());
                doc.Add(new Paragraph());

                var table1 = new Table(new float[] { 1, 3, 2, 2 });
                table1.SetWidth(UnitValue.CreatePercentValue(100));
                table1.SetFixedLayout();
                {
                    var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);
                    cell11.Add(new Paragraph("FROM:").SetBold().SetFontColor(ColorConstants.DARK_GRAY).SetFontSize(9));

                    var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT);
                    cell12.Add(new Paragraph("" + moveArticle.FromWarehouse.WarehouseName).SetFontSize(10));

                    var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);
                    cell13.Add(new Paragraph("TO:").SetBold().SetFontColor(ColorConstants.DARK_GRAY).SetFontSize(9));

                    var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT);
                    cell14.Add(new Paragraph("" + moveArticle.ToWarehouse.WarehouseName).SetFontSize(10));
                    table1.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14);
                }
                doc.Add(table1);
                doc.Add(new Paragraph());
                doc.Add(new Paragraph());
                doc.Add(new Paragraph());


                var itable = new Table(new float[] { 3, 2, 2, 2, 2, 2 });
                itable.SetWidth(UnitValue.CreatePercentValue(100));
                var cell1 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Artículo").SetFontSize(10));
                var cell2 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Type").SetFontSize(10));
                var cell3 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Marca").SetFontSize(10));
                var cell4 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Move QTY").SetFontSize(10));
                var cell5 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Unidad").SetFontSize(10));
				var cell6 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).Add(new Paragraph("Total").SetFontSize(10));
				itable.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6);

                decimal allSubTotal = 0;
                decimal allTotal = 0;
                decimal allTax = 0;
                decimal allTaxable = 0;
                decimal allExtento = 0;
                decimal allDiscount = 0;

                foreach (var item in moveArticle.Items)
                {
					if (item.ItemType == ItemType.Article)
					{

						var article = _dbContext.Articles.Include(s=>s.Brand).Include(s=>s.Items).FirstOrDefault(s => s.ID == item.ItemID);
                        var unit = article.Items.FirstOrDefault(s => s.Number == item.UnitNum);
						var cost = item.Qty * unit.Cost;
						allTotal += cost;
                        var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(article.Name).SetFontSize(9));
                        var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Article").SetFontSize(9));
                        var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(article.Brand?.Name).SetFontSize(9));
                        var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + item.Qty).SetFontSize(9));
                        var cell15 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(unit.Name).SetFontSize(9));
                        var cell16 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + cost.ToString("N2", CultureInfo.InvariantCulture)).SetFontSize(9));
                        itable.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16);
                    }
					else
					{
                        var subrecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == item.ItemID);
                        var unit = subrecipe.ItemUnits.FirstOrDefault(s => s.Number == item.UnitNum);
						var cost = item.Qty * unit.Cost;
						allTotal += cost;

						var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(subrecipe.Name).SetFontSize(9));
                        var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Sub Recipe").SetFontSize(9));
                        var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("").SetFontSize(9));
                        var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + item.Qty).SetFontSize(9));
                        var cell15 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(unit.Name).SetFontSize(9));
						var cell16 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + cost.ToString("N2", CultureInfo.InvariantCulture)).SetFontSize(9));
						itable.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16); 
                    }
                }

				var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("").SetFontSize(9));
				var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("").SetFontSize(9));
				var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("").SetFontSize(9));
				var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("").SetFontSize(9));
				var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Total").SetFontSize(12));
				var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + allTotal.ToString("N2", CultureInfo.InvariantCulture)).SetFontSize(12));
				itable.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25).AddCell(cell26);

				doc.Add(itable);
                doc.Add(new Paragraph());
                doc.Add(new Paragraph());
                doc.Add(new Paragraph("" + moveArticle.Description));

                doc.Close();
            }

            return Json(new { status = 0, url = uploadsUrl });
        }

        public IActionResult AddPurchaseOrder(long purchaseOrderID = 0)
		{
			var purchaseOrder = _dbContext.PurchaseOrders.FirstOrDefault(s=>s.ID == purchaseOrderID);
			ViewBag.PurchaseOrderID = purchaseOrderID;
			ViewBag.OrderDate = DateTime.Now.ToString("dd-MM-yyyy");
			ViewBag.Status = PurchaseOrderStatus.None;
			if (purchaseOrder != null)
			{
				ViewBag.PurchaseOrderID = purchaseOrderID;
                ViewBag.OrderDate = purchaseOrder.OrderTime.ToString("dd-MM-yyyy");
				ViewBag.Status = purchaseOrder.Status;
            }

            return View();
        }

		public JsonResult GetPurchaseOrder(long purchaseOrderID)
		{
			var purchaseOrder = _dbContext.PurchaseOrders.Include(s => s.Supplier).Include(s => s.Warehouse).Include(s => s.Items).ThenInclude(x=>x.Item).FirstOrDefault(s => s.ID == purchaseOrderID);

			return Json(purchaseOrder);
		}

		public JsonResult GetPurchaseOrderItem(long itemID)
		{
			var item = _dbContext.PurchaseOrderItems.Include(s=>s.Item).ThenInclude(x=>x.Items).Include(s=>s.Item).ThenInclude(x=>x.Brand).Include(s=>s.Tax).FirstOrDefault(s=>s.ID == itemID); 
			return Json(item);
		}

        [HttpPost]
        public JsonResult EditPurchaseOrder([FromBody] PurchaseOrderCreateModel model)
        {
            try
            {
                long porderID = 0;
                var existing = _dbContext.PurchaseOrders.FirstOrDefault(s => s.ID == model.PurchaseOrderId);
                if (existing == null)
                {
                    var newPurchaseOrder = new PurchaseOrder();
                    var supplier = _dbContext.Suppliers.FirstOrDefault(s => s.ID == model.SupplierID);
                    var warehouse = _dbContext.Warehouses.FirstOrDefault(s => s.ID == model.WarehouseID);
                    if (supplier.IsFormalSupplier && string.IsNullOrEmpty(model.NCF) && model.OrderStatus == 4)
                    {
                        return Json(new { status = 3, message = "The NCF should be input." });
                    }
                    if (!string.IsNullOrEmpty(model.NCF))
                    {
                        var other = _dbContext.PurchaseOrders.FirstOrDefault(s => s.ID != model.PurchaseOrderId && s.NCF == model.NCF);
                        if (other != null)
                        {
                            return Json(new { status = 2, message = "The NCF should be unique." });
                        }
                    }

                    newPurchaseOrder.Supplier = supplier;
                    newPurchaseOrder.Warehouse = warehouse;
                    newPurchaseOrder.NCF = model.NCF;
                    newPurchaseOrder.Term = model.Term;
                    newPurchaseOrder.OrderTime = DateTime.Now;
                    newPurchaseOrder.IsDiscountPercentItems = model.DiscountType == "%";
                    newPurchaseOrder.IsDiscountPercent = model.DiscountTypeTotal == "%";
                    newPurchaseOrder.Status = PurchaseOrderStatus.Pending;
                    newPurchaseOrder.Description = model.Description;
                    newPurchaseOrder.Shipping = model.Shipping;
                    newPurchaseOrder.Discount = model.Discount;
                    newPurchaseOrder.TaxTotal = model.TaxTotal;
                    newPurchaseOrder.SubTotal = model.SubTotal;
                    newPurchaseOrder.Total = model.Total;
                    newPurchaseOrder.DiscountAmount = model.DiscountAmount;
                    newPurchaseOrder.DiscountPercent = model.DiscountPercent;
                    newPurchaseOrder.DiscountTotal = model.DiscountTotal;

                    if (model.Items.Count > 0)
                    {
                        newPurchaseOrder.Items = new List<PurchaseOrderItem>();

                        foreach (var item in model.Items)
                        {
                            var article = _dbContext.Articles.FirstOrDefault(s => s.ID == item.ArticleID);
                            if (article != null)
                            {
                                var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == item.TaxID);

                                var pItem = new PurchaseOrderItem();
                                pItem.UnitCost = item.UnitPrice;
                                pItem.Qty = item.QTY;
                                pItem.Item = article;
                                pItem.UnitNum = item.UnitNum;
                                pItem.Tax = tax;
                                pItem.Discount = item.DiscountAmount;
                                pItem.TaxRate = (decimal)tax.TaxValue;

                                newPurchaseOrder.Items.Add(pItem);
                            }

                        }
                    }

                    _dbContext.PurchaseOrders.Add(newPurchaseOrder);
                    _dbContext.SaveChanges();



                    return Json(new { status = 0, newPurchaseOrder.ID });
                }
                else
                {
                    if (existing.Status == PurchaseOrderStatus.Received)
                    {
                        return Json(new { status = 1 });
                    }


                    var supplier = _dbContext.Suppliers.FirstOrDefault(s => s.ID == model.SupplierID);
                    var warehouse = _dbContext.Warehouses.FirstOrDefault(s => s.ID == model.WarehouseID);
                    if (supplier != null && supplier.IsFormalSupplier && string.IsNullOrEmpty(model.NCF) && model.OrderStatus == 4)
                    {
                        return Json(new { status = 3, message = "The NCF should be input." });
                    }
                    if (!string.IsNullOrEmpty(model.NCF))
                    {
                        var other = _dbContext.PurchaseOrders.FirstOrDefault(s => s.NCF == model.NCF && s.ID != model.PurchaseOrderId);
                        if (other != null)
                        {
                            return Json(new { status = 2, message = "The NCF should be unique." });
                        }
                    }

                    existing.Supplier = supplier;
                    existing.Warehouse = warehouse;
                    existing.NCF = model.NCF;
                    existing.Term = model.Term;
                    existing.IsDiscountPercentItems = model.DiscountType == "%";
                    existing.IsDiscountPercent = model.DiscountTypeTotal == "%";
                    existing.Description = model.Description;
                    existing.Shipping = model.Shipping;
                    existing.Discount = model.Discount;
                    existing.DiscountAmount = model.DiscountAmount;
                    existing.DiscountPercent = model.DiscountPercent;
                    existing.DiscountTotal = model.DiscountTotal;

                    existing.TaxTotal = model.TaxTotal;
                    existing.SubTotal = model.SubTotal;
                    existing.Total = model.Total;

                    if (model.Items.Count > 0)
                    {
                        if (existing.Items == null)
                            existing.Items = new List<PurchaseOrderItem>();

                        foreach (var item in model.Items)
                        {
                            var existItem = _dbContext.PurchaseOrderItems.FirstOrDefault(s => s.ID == item.ItemID);
                            if (existItem != null)
                            {
                                var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == item.TaxID);

                                existItem.UnitCost = item.UnitPrice;
                                existItem.Qty = item.QTY;
                                existItem.UnitNum = item.UnitNum;
                                existItem.Tax = tax;
                                existItem.TaxRate = (decimal)tax.TaxValue;
                                existItem.Discount = item.DiscountAmount;
                            }
                            else
                            {
                                var article = _dbContext.Articles.FirstOrDefault(s => s.ID == item.ArticleID);
                                if (article != null)
                                {
                                    var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == item.TaxID);

                                    var pItem = new PurchaseOrderItem();
                                    pItem.UnitCost = item.UnitPrice;
                                    pItem.Qty = item.QTY;
                                    pItem.Item = article;
                                    pItem.UnitNum = item.UnitNum;
                                    pItem.Tax = tax;
                                    pItem.Discount = item.DiscountAmount;
                                    pItem.TaxRate = (decimal)tax.TaxValue;

                                    existing.Items.Add(pItem);
                                }
                            }



                        }
                    }
                    _dbContext.SaveChanges();
                    return Json(new { status = 0, existing.ID });
                }
            }
            catch
            {
                return Json(new { status = 1 });
            }
        }

        [HttpPost]
		public JsonResult UpdatePurchaseOrderStatus ([FromBody]PurchaseOrderStatusUpdateModel model)
		{
			var purchaseOrder = _dbContext.PurchaseOrders.Include(s=>s.Supplier).Include(s=>s.Warehouse).Include(s => s.Items).ThenInclude(s => s.Item).Include(s=>s.Items).Include(s => s.Items).ThenInclude(s => s.Tax).FirstOrDefault(s => s.ID == model.ID);
			if (purchaseOrder != null)
			{
				purchaseOrder.Status = (PurchaseOrderStatus)model.Status;

				if (purchaseOrder.Status == PurchaseOrderStatus.Received)
				{
					if (purchaseOrder.Supplier.IsFormalSupplier && string.IsNullOrEmpty(purchaseOrder.NCF))
					{
						return Json(new { status = 2 });
					}
					if (purchaseOrder.Warehouse != null)
					{
						foreach(var item in purchaseOrder.Items)
						{
                           
							try
							{
								decimal subTotal = item.Qty * item.UnitCost;

								decimal taxable = subTotal * 100.0m / (100.0m);
								if (item.Tax != null)
								{
									taxable = subTotal * 100.0m / (100.0m + (decimal)item.Tax.TaxValue);
								}

								

								decimal amount = 0;
								if (!purchaseOrder.Supplier.IsTaxIncluded)
								{
									decimal discount = 0;
									if (purchaseOrder.Discount > 0)
									{
										if (purchaseOrder.IsDiscountPercent)
										{
											discount = subTotal * purchaseOrder.Discount / 100;  // G2
										}
									}
									else
									{
										if (purchaseOrder.IsDiscountPercentItems)
										{
											discount = subTotal * item.Discount / 100;  // G2
										}
										else
										{
											discount = item.Discount;
										}
									}
									amount = subTotal - discount;
								}
								else
								{
									decimal discount = 0;
									if (purchaseOrder.Discount > 0)
									{
										if (purchaseOrder.IsDiscountPercent)
										{
											discount = taxable * purchaseOrder.Discount / 100;  // G2
										}
										
									}
									else
									{
										if (purchaseOrder.IsDiscountPercentItems)
										{
											discount = taxable * item.Discount / 100;  // G2
										}
										else
										{
											discount = item.Discount;
										}
									}

									amount = taxable - discount;
								}

								var newPrice = amount / item.Qty;
								
								var realrates = new List<decimal>();
								var article = _dbContext.Articles.Include(s => s.Items).FirstOrDefault(s => s.ID == item.Item.ID);
								var units = article.Items.OrderBy(s => s.Number).ToList();

								var baseQty = ConvertQtyToBase(item.Qty, item.UnitNum, units);
                                
                                //var avgCost = GetAverageCost(article, units[0].Cost, baseQty, newPrice);
								int i = 0;
								decimal rate = 0;
								foreach (var unit in units)
								{
									if (i == 0)
									{
										realrates.Add(unit.Rate);
										rate = unit.Rate;
									}
									else
									{
										var realrate = rate * unit.Rate;
										realrates.Add(realrate);
										rate = realrate;
									}

									i++;
								}
								var firstNewPrice = realrates[item.UnitNum - 1] / realrates[0] * newPrice;
                                var avgCost = GetAverageCost(article, units[0].Cost, baseQty, firstNewPrice);

                                var firstPrice = avgCost;
								i = 0;
								foreach (var unit in units)
								{
									unit.Cost = Math.Round(firstPrice / realrates[i], 4);
									i++;
								}
								
								var existingWarehouseStock = _dbContext.WarehouseStocks.FirstOrDefault(s => s.Warehouse == purchaseOrder.Warehouse && s.ItemType == ItemType.Article && s.ItemId == item.Item.ID);

                                var warehouseHistoryItem = new WarehouseStockChangeHistory();
                                warehouseHistoryItem.Warehouse = purchaseOrder.Warehouse;
                                warehouseHistoryItem.Price = firstPrice;
                                warehouseHistoryItem.ItemId = item.Item.ID;
                                warehouseHistoryItem.ItemType = ItemType.Article;
								warehouseHistoryItem.BeforeBalance = existingWarehouseStock == null ? 0 : existingWarehouseStock.Qty;
								warehouseHistoryItem.AfterBalance = existingWarehouseStock == null ? baseQty : existingWarehouseStock.Qty + baseQty;
                                warehouseHistoryItem.Qty = baseQty;
                                warehouseHistoryItem.UnitNum = 1;
                                warehouseHistoryItem.ReasonType = StockChangeReason.PurchaseOrder;
                                warehouseHistoryItem.ReasonId = purchaseOrder.ID;

                                _dbContext.WarehouseStockChangeHistory.Add(warehouseHistoryItem);

                                if (existingWarehouseStock == null)
								{
									existingWarehouseStock = new WarehouseStock();
									existingWarehouseStock.Warehouse = purchaseOrder.Warehouse;
									existingWarehouseStock.ItemId = article.ID;
									existingWarehouseStock.ItemType = ItemType.Article;
									existingWarehouseStock.Qty = baseQty;

									_dbContext.WarehouseStocks.Add(existingWarehouseStock);
									_dbContext.SaveChanges();
								}
								else
								{
                                    existingWarehouseStock.Qty += baseQty;
                                    _dbContext.SaveChanges();
                                }
                            }
							catch { }
						}
                    }
				}	

				_dbContext.SaveChanges();

				return Json(new { status = 0 });
            }
			return Json(new {status = 1});
		}

		private decimal GetAverageCost(InventoryItem item, decimal prevCost, decimal Qty, decimal Cost)
		{
			decimal avgCost = Cost;

			var changeItems = _dbContext.WarehouseStockChangeHistory.Where(s => s.ItemType == ItemType.Article && s.ItemId == item.ID).ToList() ;
			if (changeItems.Count > 0)
			{
				var sum = changeItems.Sum(s => s.Qty);
				if (sum > 0)
				{
					var total = prevCost * sum + Qty * Cost;
					avgCost = total / (Qty + sum);
				}			
			}

			return avgCost;			
		}

		private decimal ConvertQtyToBase(decimal originQty, int UnitNum, List<ItemUnit> Units)
		{
			if (UnitNum <= 1) return originQty;

			try
			{
                var realrates = new List<decimal>();
                var units = Units.OrderBy(s => s.Number).ToList();
                int i = 0;
                decimal rate = 0;
                foreach (var unit in units)
                {
                    if (i == 0)
                    {
                        realrates.Add(unit.Rate);
                        rate = unit.Rate;
                    }
                    else
                    {
                        var realrate = rate * unit.Rate;
                        realrates.Add(realrate);
                        rate = realrate;
                    }

                    i++;
                }

				return originQty / realrates[UnitNum - 1] * realrates[0];
            }
            catch { }
			return originQty;
		}


		#endregion

		#region Sub Recipe
		//[HttpPost]
		//public JsonResult GetLastPrice(long articleID)
		//{
		//	var lastPriceItem = _dbContext.PurchaseOrderItems.Include(s=>s.Item).FirstOrDefault(x=>x.Item.ID == articleID);
		//	if (lastPriceItem != null)
		//	{
		//		return Json(new { status = 0, unitID = lastPriceItem.Unit.UnitID });
		//	}

		//	return Json(new { status = 1 });
		//}

		public IActionResult SubRecipeList()
		{
			return View();
		}

		public IActionResult AddSubRecipe(long subRecipeID = 0)
		{
			ViewBag.Groups = _dbContext.Groups.ToList();
			ViewBag.PrinterChannels = _dbContext.PrinterChannels.Where(s => s.IsActive).ToList();
			ViewBag.SubRecipeID = subRecipeID;

			var stocks = new List<WarehouseStock>();
			var products = new List<Product>();

			if (subRecipeID > 0)
			{
				var subRecipe = _dbContext.SubRecipes.Include(s => s.SubCategory).FirstOrDefault(s => s.ID == subRecipeID);
				if (subRecipe != null)
				{
					if (subRecipe.SubCategory != null)
						ViewBag.SubCategory = subRecipe.SubCategory.ID;
					ViewBag.MaxUnit = subRecipe.MaximumUnit;
					ViewBag.MinUnit = subRecipe.MinimumUnit;
					ViewBag.SubRecipeUnit = subRecipe.UnitNumber;

					stocks = _dbContext.WarehouseStocks.Include(s=>s.Warehouse).Where(s => s.ItemId == subRecipe.ID && s.ItemType == ItemType.SubRecipe).ToList();
				}

				var Allproducts = _dbContext.Products.Include(s => s.Category).Include(s => s.RecipeItems).ToList();
				foreach (var p in Allproducts)
				{
					var item = p.RecipeItems.Where(s => s.Type == ItemType.SubRecipe && s.ItemID == subRecipeID);
					if (item != null && item.Count() > 0)
					{
						products.Add(p);
					}
				}
			}

			ViewBag.Products = products;
			ViewBag.Stocks = stocks;
			ViewBag.Categories = _dbContext.Categories.Where(c => c.IsActive).ToList();
			ViewBag.Taxes = _dbContext.Taxs.Where(c => c.IsActive).ToList();

			return View();
		}
		public JsonResult GetAllActiveArticles()
		{
			var articles = _dbContext.Articles.Where(s => s.IsActive).ToList();
			return Json(articles);
		}

		public JsonResult GetAllActiveSubRecipes()
		{
            var subRecipes = _dbContext.SubRecipes.Where(s => s.IsActive).ToList();
			return Json(subRecipes);
        }

		[HttpPost]
		public IActionResult GetSubRecipeList(long currentID = 0)
		{
			try
			{
				var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
				// Skiping number of Rows count  
				var start = Request.Form["start"].FirstOrDefault();
				// Paging Length 10,20  
				var length = Request.Form["length"].FirstOrDefault();
				// Sort Column Name  
				var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
				// Sort Column Direction ( asc ,desc)  
				var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
				// Search Value from (Search box)  
				var searchValue = Request.Form["search[value]"].FirstOrDefault();

				//Paging Size (10,20,50,100)  
				int pageSize = length != null ? Convert.ToInt32(length) : 0;
				int skip = start != null ? Convert.ToInt32(start) : 0;
				int recordsTotal = 0;

				// Getting all Customer data  
				var customerData = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Where(s=>s.ID != currentID).Select(s => new
				{
					s.ID,
					s.Name,
					Category = s.Category == null?"":s.Category.Name,
					CategoryId = s.Category == null?0:s.Category.ID,
					SubCategoryId = s.SubCategory == null?0:s.SubCategory.ID,
					SubCategory =s.SubCategory == null?"": s.SubCategory.Name,
					s.IsActive,
					Total = s.Total.ToString("0.00", CultureInfo.InvariantCulture),
				}).Where(s=>s.Name.ToUpper().Contains(searchValue.ToUpper()) || s.Category.ToUpper().Contains(searchValue.ToUpper()));

				//Sorting
				if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				{
					try
					{
                        customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
					catch { }
					
				}

				var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
				var category = Request.Form["columns[1][search][value]"].FirstOrDefault();
				var subcategory = Request.Form["columns[2][search][value]"].FirstOrDefault();
				var status = Request.Form["columns[3][search][value]"].FirstOrDefault();

				////Search  
				if (!string.IsNullOrEmpty(all))
				{
					all = all.Trim().ToLower();
					customerData = customerData.Where(m => m.Name.ToLower().Contains(all) || m.Category.ToLower().Contains(all) || m.SubCategory.ToLower().Contains(all));
				}
				if (!string.IsNullOrEmpty(category))
				{
					customerData = customerData.Where(s => "" + s.CategoryId == category);
				}
				if (!string.IsNullOrEmpty(subcategory))
				{
					customerData = customerData.Where(s => "" + s.SubCategoryId == subcategory);
				}
				if (string.IsNullOrEmpty(status) || status == "1")
				{
					customerData = customerData.Where(s => s.IsActive);
				}
				else
				{
					customerData = customerData.Where(s => s.IsActive == false);
				}
				//total number of rows count   
				recordsTotal = customerData.Count();
				//Paging   
				var data = customerData.Skip(skip).ToList();
				if (pageSize != -1)
				{
					data = data.Take(pageSize).ToList();
				}
				//Returning Json Data  
				return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

			}
			catch (Exception ex)
			{
				throw;
			}
		}


		[HttpPost]
		public JsonResult DownloadSubRecipeExcel(string search, string category, string subcategory)
		{
			try
			{
				var customerData = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Select(s => new
				{
					s.ID,
					s.Name,
					Category = s.Category.Name,
					CategoryId = s.Category.ID,
					SubCategoryId = s.SubCategory.ID,
					SubCategory = s.SubCategory.Name,
					s.IsActive,
					Total = s.Total.ToString("0.00", CultureInfo.InvariantCulture),
				});
				////Search  
				if (!string.IsNullOrEmpty(search))
				{
					search = search.Trim().ToLower();
					customerData = customerData.Where(m => m.Name.ToLower().Contains(search) || m.Category.ToLower().Contains(search) || m.SubCategory.ToLower().Contains(search));
				}
				if (!string.IsNullOrEmpty(category))
				{
					customerData = customerData.Where(s => "" + s.CategoryId == category);
				}
				if (!string.IsNullOrEmpty(subcategory))
				{
					customerData = customerData.Where(s => "" + s.SubCategoryId == subcategory);
				}
				var subRecipes = customerData.ToList();

				if (subRecipes.Count() == 0)
				{
					return Json(new { status = 1, message = "There are no items to export." });
				}

				var csvBuilder = new StringBuilder();

				csvBuilder.AppendLine("Sub Recipe List");
				if (!string.IsNullOrEmpty(search))
				{
					csvBuilder.AppendLine("Search Text: ," + search);
				}
				if (!string.IsNullOrEmpty(category))
				{
					csvBuilder.AppendLine("Category: ," + subRecipes[0].Category);
				}
				if (!string.IsNullOrEmpty(subcategory))
				{
					csvBuilder.AppendLine("Sub Category : ," + subRecipes[0].SubCategory);
				}
				
				var header = "ID #,Name,Category,Sub Category,Active";

				csvBuilder.AppendLine(header);

				foreach (var temp in subRecipes)
				{

					var line = $"\"{temp.ID}\",\"{temp.Name}\",{temp.Category},{temp.SubCategory},{temp.IsActive}";
					csvBuilder.AppendLine(line);
				}

				string tempFolder = Path.Combine(_env.WebRootPath, "temp");
				var uniqueFileName = "SubRecipe_" + DateTime.Now.Ticks + ".csv";
				var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
				if (!Directory.Exists(tempFolder))
				{
					Directory.CreateDirectory(tempFolder);
				}

				var uploadsUrl = "/temp/" + uniqueFileName;

				System.IO.File.WriteAllText(uploadsFile, csvBuilder.ToString(), System.Text.Encoding.UTF8);

				return Json(new { status = 0, result = uploadsUrl });
			}
			catch (Exception ex)
			{
				return Json(new { status = 1, message = ex.Message });
			}

		}

		[HttpPost]
		public JsonResult DownloadSubRecipePDF(string search, string category, string subcategory)
		{
			string tempFolder = Path.Combine(_env.WebRootPath, "temp");

			var customerData = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Select(s => new
			{
				s.ID,
				s.Name,
				Category = s.Category.Name,
				CategoryId = s.Category.ID,
				SubCategoryId = s.SubCategory.ID,
				SubCategory = s.SubCategory.Name,
				s.IsActive,
				Total = s.Total.ToString("0.00", CultureInfo.InvariantCulture),
			});
			////Search  
			if (!string.IsNullOrEmpty(search))
			{
				search = search.Trim().ToLower();
				customerData = customerData.Where(m => m.Name.ToLower().Contains(search) || m.Category.ToLower().Contains(search) || m.SubCategory.ToLower().Contains(search));
			}
			if (!string.IsNullOrEmpty(category))
			{
				customerData = customerData.Where(s => "" + s.CategoryId == category);
			}
			if (!string.IsNullOrEmpty(subcategory))
			{
				customerData = customerData.Where(s => "" + s.SubCategoryId == subcategory);
			}
			var subRecipes = customerData.ToList();

			if (subRecipes.Count == 0)
			{
				return Json(new { status = 1 });
			}
			var uniqueFileName = "SubRecipe_" + "_" + DateTime.Now.Ticks + ".pdf";
			var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
			var uploadsUrl = "/temp/" + uniqueFileName;
			var paperSize = iText.Kernel.Geom.PageSize.A4;
			{
				using (var writer = new PdfWriter(uploadsFile))
				{
					var pdf = new PdfDocument(writer);
					var doc = new iText.Layout.Document(pdf);

					Paragraph header = new Paragraph("Sub Recipe List")
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(20);
					doc.Add(header);

					if (!string.IsNullOrEmpty(search))
					{
						Paragraph subheader = new Paragraph("Search Text: " + search)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}

					if (!string.IsNullOrEmpty(category))
					{
						Paragraph subheader = new Paragraph("Category: " + subRecipes[0].Category)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					if (!string.IsNullOrEmpty(subcategory))
					{
						Paragraph subheader = new Paragraph("Sub Category: " + subRecipes[0].SubCategory)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}

					// Table
					doc.Add(new Paragraph(""));
					var table = new Table(new float[] { 1, 3, 3, 3, 2 });
					table.SetWidth(UnitValue.CreatePercentValue(100));
					// headers
					var cell1 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("ID #"));
					var cell2 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Name"));
					var cell3 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Category"));
					var cell4 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Sub Category"));
					var cell5 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Active"));
					table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5);

					foreach (var p in subRecipes)
					{
						var cell11 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.ID));
						var cell12 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.Name));
						var cell13 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.Category));
						var cell14 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.SubCategory));
						var cell15 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.IsActive));

						table.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15);
					}
					doc.Add(table);

					doc.Add(new Paragraph(""));
					doc.Close();
				}
			}

			return Json(new { status = 0, url = uploadsUrl });
		}


		[HttpGet]
        public JsonResult GetSubRecipe(long subRecipeID)
        {
            var subrecipe = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Items).Include(s=>s.ItemUnits.OrderBy(s=>s.Number)).FirstOrDefault(s => s.ID == subRecipeID);

            return Json(subrecipe);
        }
        
		public JsonResult GetSubRecipeItem(long itemID)
        {
            var item = _dbContext.SubRecipeItems.FirstOrDefault(s => s.ID == itemID);

			var subRecipeItem = new SubRecipeItemViewModel(item);
			
			if (item.IsArticle)
			{
				var article = _dbContext.Articles.Include(s=>s.Items.OrderBy(s=>s.Number)).Include(s=>s.Brand).FirstOrDefault(s => s.ID == item.ItemID);
				subRecipeItem.Article = article;
			}
			else
			{
				var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s=>s.Number)).FirstOrDefault(s => s.ID == item.ItemID);
				subRecipeItem.SubRecipe = subRecipe;
			}


            return Json(subRecipeItem);
        }

        [HttpPost]
		public JsonResult EditSubRecipe([FromBody] SubRecipeCreateModel model)
		{
			try
			{
				var existing = _dbContext.SubRecipes.Include(s=>s.Category).Include(s=>s.SubCategory).Include(s=>s.ItemUnits).Include(s=>s.Items).FirstOrDefault(s => s.ID == model.ID);
				if (existing == null)
				{
					var other = _dbContext.SubRecipes.FirstOrDefault(s => s.Name == model.Name);
					if (other != null)
					{
						return Json(new { status = 2 });
					}
					var newSubRecipe = new SubRecipe();
					var category = _dbContext.Categories.FirstOrDefault(s => s.ID == model.CategoryID);
					var subCategory = _dbContext.SubCategories.FirstOrDefault(s => s.ID == model.SubCategoryID);

					newSubRecipe.Category = category;
					newSubRecipe.SubCategory = subCategory;
					newSubRecipe.Name = model.Name;

					newSubRecipe.IsActive = model.IsActive;
					newSubRecipe.MinimumQuantity = model.Minimum;
					newSubRecipe.MinimumUnit = model.MinUnitID;
					newSubRecipe.MaximumQuantity = model.Maximum;
					newSubRecipe.MaximumUnit = model.MaxUnitID;
					newSubRecipe.UnitNumber = model.UnitNumber;
					newSubRecipe.SaftyStock = model.SaftyStock;
					newSubRecipe.Total = model.Total;
					newSubRecipe.Barcode = model.Barcode;
				
					if (model.Items.Count > 0)
					{
						newSubRecipe.Items = new List<SubRecipeItem>();

						foreach (var item in model.Items)
						{
							if (item.IsArticle)
							{
                                var article = _dbContext.Articles.Include(s=>s.Items).FirstOrDefault(s => s.ID == item.ArticleID);
                                if (article != null)
                                {
                                    var unit = _dbContext.ItemUnits.FirstOrDefault(s => s.ID == item.UnitID);

                                    var pItem = new SubRecipeItem();
                                    pItem.UnitCost = item.UnitPrice;
									pItem.FirstQty = item.FirstQty;
                                    pItem.Qty = item.QTY;
                                    pItem.ItemID = item.ArticleID;
                                    pItem.UnitNum = item.UnitID;
									pItem.IsArticle = true;
                                    newSubRecipe.Items.Add(pItem);
                                }
                            }
							else
							{
                                var subrecipe = _dbContext.SubRecipes.Include(s=>s.ItemUnits).FirstOrDefault(s => s.ID == item.ArticleID);
                                if (subrecipe != null)
                                {
                                    var pItem = new SubRecipeItem();
                                    pItem.UnitCost = item.UnitPrice;
									pItem.FirstQty = item.FirstQty;
                                    pItem.Qty = item.QTY;
                                    pItem.ItemID = item.ArticleID;
                                    pItem.UnitNum = item.UnitID;
                                    pItem.IsArticle = false;
                                    newSubRecipe.Items.Add(pItem);
                                }
                            }

						}
					}

					if (model.ItemUnits != null && model.ItemUnits.Count > 0)
					{
						newSubRecipe.ItemUnits = new List<ItemUnit>();
						int index = 0;
						foreach (var item in model.ItemUnits)
						{
							if (index == 4) break;
							newSubRecipe.ItemUnits.Add(item);
							index++;
						}
					}

					_dbContext.SubRecipes.Add(newSubRecipe);
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}
				else
				{
					var category = _dbContext.Categories.FirstOrDefault(s => s.ID == model.CategoryID);
					var subCategory = _dbContext.SubCategories.FirstOrDefault(s => s.ID == model.SubCategoryID);

					var other = _dbContext.SubRecipes.FirstOrDefault(s => s.ID != existing.ID && s.Name == model.Name);
					if (other != null)
					{
						return Json(new { status = 2 });
					}

					existing.Category = category;
					existing.SubCategory = subCategory;
					existing.Name = model.Name;
					existing.IsActive = model.IsActive;
					existing.MinimumQuantity = model.Minimum;
					existing.MinimumUnit = model.MinUnitID;
					existing.MaximumQuantity = model.Maximum;
					existing.MaximumUnit = model.MaxUnitID;
					existing.UnitNumber = model.UnitNumber;
					existing.SaftyStock = model.SaftyStock;
					existing.Total = model.Total;
					existing.Barcode = model.Barcode;

					if (model.Items.Count > 0)
					{
						if (existing.Items == null)
							existing.Items = new List<SubRecipeItem>();
						else
						{
                            foreach (var item in existing.Items)
                            {
                                _dbContext.SubRecipeItems.Remove(item);
                            }
                            existing.Items.Clear();
						}
						foreach (var item in model.Items)
						{
							if (item.IsArticle)
							{
                                var article = _dbContext.Articles.Include(s=>s.Items).FirstOrDefault(s => s.ID == item.ArticleID);
                                if (article != null)
                                {
                                    var pItem = new SubRecipeItem();
                                    pItem.UnitCost = item.UnitPrice;
                                    pItem.Qty = item.QTY;
                                    pItem.FirstQty = item.FirstQty;
                                    pItem.ItemID = item.ArticleID;
                                    pItem.UnitNum = item.UnitID;
									pItem.IsArticle = true;
                                    existing.Items.Add(pItem);
                                }
                            }
							else
							{
                                var subreipe = _dbContext.SubRecipes.Include(s=>s.ItemUnits).FirstOrDefault(s => s.ID == item.ArticleID);
                                if (subreipe != null)
                                {
                                    var pItem = new SubRecipeItem();
									pItem.ItemID = item.ArticleID;
                                    pItem.UnitCost = item.UnitPrice;
                                    pItem.FirstQty = item.FirstQty;
                                    pItem.Qty = item.QTY;
                                    pItem.UnitNum = item.UnitID;
									pItem.IsArticle = false;

                                    existing.Items.Add(pItem);
                                }
                            }
							
						}
					}
				
					if (model.ItemUnits != null && model.ItemUnits.Count > 0)
					{
						if (existing.ItemUnits == null) 
							existing.ItemUnits = new List<ItemUnit>();
						else
						{
							foreach(var item in existing.ItemUnits)
							{
								_dbContext.ItemUnits.Remove(item);
							}
							existing.ItemUnits.Clear();
							
						}
						int index = 0;
						foreach (var item in model.ItemUnits)
						{
							if (index == 4) break;
							existing.ItemUnits.Add(item);
							index++;
						}
					}

					_dbContext.SaveChanges();

					return Json(new { status = 0 });
				}
			}
			catch (Exception ex)
			{
				return Json(new { status = 1 });
			}
		}

		#endregion

		#region Production
		public IActionResult ProductionList()
		{
			return View();
		}

		public IActionResult AddProduction(long productionID = 0)
		{
			ViewBag.Status = ProductionStatus.None;
			ViewBag.ProductionID = productionID;
			ViewBag.ProductionDate = DateTime.Now.ToString("dd-MM-yyyy");
			ViewBag.UnitNum = 0;
			if (productionID > 0)
			{
				var production = _dbContext.SubRecipeProductions.FirstOrDefault(s => s.ID == productionID);
				ViewBag.ProductionDate = production.ProductionDate.ToString("dd-MM-yyyy");
				ViewBag.UnitNum = production.UnitNum;
                ViewBag.Status = production.Status;
            }

			return View();
		}
        
		[HttpPost]
        public IActionResult GetProductionList()
        {
            try
            {
                var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                // Skiping number of Rows count  
                var start = Request.Form["start"].FirstOrDefault();
                // Paging Length 10,20  
                var length = Request.Form["length"].FirstOrDefault();
                // Sort Column Name  
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                // Sort Column Direction ( asc ,desc)  
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                // Search Value from (Search box)  
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                //Paging Size (10,20,50,100)  
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

				// Getting all Customer data  
				var customerData = _dbContext.SubRecipeProductions.Include(s => s.Warehouse).Include(s => s.SubRecipe).ThenInclude(s => s.ItemUnits).OrderByDescending(s => s.ID).Select(s => new ProductionViewModel()
                {
					ID = s.ID,
					WarehouseName = s.Warehouse.WarehouseName,
					WarehouseId = s.Warehouse.ID,
					SubRecipeName = s.SubRecipe.Name,
					SubRecipe = s.SubRecipe,
					UnitNumber = s.UnitNum,
					UnitName = "",
					Qty = s.Qty,
					Status = s.Status,
				}); ;

                //Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
					try
					{
                        customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
					catch { }
                   
                }

				var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
				var warehouse = Request.Form["columns[1][search][value]"].FirstOrDefault();
				////Search  
				if (!string.IsNullOrEmpty(all))
                {
					all = all.Trim().ToLower();
                    customerData = customerData.Where(m => m.WarehouseName.ToLower().Contains(all) || m.SubRecipeName.ToLower().Contains(all));
                }
				if (!string.IsNullOrEmpty(warehouse))
				{
					customerData = customerData.Where(s => "" + s.WarehouseId == warehouse);
				}

				//total number of rows count   
				recordsTotal = customerData.Count();
                //Paging   
                var data = customerData.Skip(skip).ToList();
                if (pageSize != -1)
                {
                    data = data.Take(pageSize).ToList();
                }

				foreach (var d in data)
				{
					var unit = d.SubRecipe.ItemUnits.FirstOrDefault(s => s.Number == d.UnitNumber);
					if (unit != null)
					{
						d.UnitName = unit.Name;
					}
				}
                //Returning Json Data  
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                throw;
            }
        }

		[HttpPost]
		public JsonResult DownloadProductionExcel(string search, string warehouse)
		{
			try
			{
				var customerData = _dbContext.SubRecipeProductions.Include(s => s.Warehouse).Include(s => s.SubRecipe).ThenInclude(s => s.ItemUnits).OrderByDescending(s => s.ID).Select(s => new ProductionViewModel()
				{
					ID = s.ID,
					WarehouseName = s.Warehouse.WarehouseName,
					WarehouseId = s.Warehouse.ID,
					SubRecipeName = s.SubRecipe.Name,
					SubRecipe = s.SubRecipe,
					UnitNumber = s.UnitNum,
					UnitName = "",
					Qty = s.Qty,
					Status = s.Status,
				}); 

				////Search  
				if (!string.IsNullOrEmpty(search))
				{
					search = search.Trim().ToLower();
					customerData = customerData.Where(m => m.WarehouseName.ToLower().Contains(search) || m.SubRecipeName.ToLower().Contains(search));
				}
				if (!string.IsNullOrEmpty(warehouse))
				{
					customerData = customerData.Where(s => "" + s.WarehouseId == warehouse);
				}

				var products = customerData.ToList();

				foreach (var d in products)
				{
					var unit = d.SubRecipe.ItemUnits.FirstOrDefault(s => s.Number == d.UnitNumber);
					if (unit != null)
					{
						d.UnitName = unit.Name;
					}
				}

				var csvBuilder = new StringBuilder();

				csvBuilder.AppendLine("Production List");
				csvBuilder.AppendLine("Search Text: ," + search);
				csvBuilder.AppendLine("Warehouse: ," + warehouse);

				var header = "ID #,Warehouse,Sub Recipe,Qty,Unit,Status";

				csvBuilder.AppendLine(header);

				foreach (var temp in products)
				{

					var line = $"\"{temp.ID}\",\"{temp.WarehouseName}\",{temp.SubRecipeName},\"{temp.Qty}\",{temp.UnitName},{temp.Status}";
					csvBuilder.AppendLine(line);
				}

				string tempFolder = Path.Combine(_env.WebRootPath, "temp");
				var uniqueFileName = "Productions_" + DateTime.Now.Ticks + ".csv";
				var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
				if (!Directory.Exists(tempFolder))
				{
					Directory.CreateDirectory(tempFolder);
				}
				
				var uploadsUrl = "/temp/" + uniqueFileName;

				System.IO.File.WriteAllText(uploadsFile, csvBuilder.ToString(), System.Text.Encoding.UTF8);

				return Json(new { status = 0, result = uploadsUrl });
			}
			catch (Exception ex)
			{
				return Json(new { status = 1, message = ex.Message });
			}

		}

		[HttpPost]
		public JsonResult DownloadProductionPDF(string search, string warehouse)
		{
			string tempFolder = Path.Combine(_env.WebRootPath, "temp");

			var customerData = _dbContext.SubRecipeProductions.Include(s => s.Warehouse).Include(s => s.SubRecipe).ThenInclude(s => s.ItemUnits).OrderByDescending(s => s.ID).Select(s => new ProductionViewModel()
			{
				ID = s.ID,
				WarehouseName = s.Warehouse.WarehouseName,
				WarehouseId = s.Warehouse.ID,
				SubRecipeName = s.SubRecipe.Name,
				SubRecipe = s.SubRecipe,
				UnitNumber = s.UnitNum,
				UnitName = "",
				Qty = s.Qty,
				Status = s.Status,
			});

			////Search  
			if (!string.IsNullOrEmpty(search))
			{
				search = search.Trim().ToLower();
				customerData = customerData.Where(m => m.WarehouseName.ToLower().Contains(search) || m.SubRecipeName.ToLower().Contains(search));
			}
			if (!string.IsNullOrEmpty(warehouse))
			{
				customerData = customerData.Where(s => "" + s.WarehouseId == warehouse);
			}

			var products = customerData.ToList();

			foreach (var d in products)
			{
				var unit = d.SubRecipe.ItemUnits.FirstOrDefault(s => s.Number == d.UnitNumber);
				if (unit != null)
				{
					d.UnitName = unit.Name;
				}
			}

			if (products.Count == 0)
			{
				return Json(new { status = 1 });
			}
			var uniqueFileName = "Productions_" + "_" + DateTime.Now.Ticks + ".pdf";
			var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
			var uploadsUrl = "/temp/" + uniqueFileName;
			var paperSize = iText.Kernel.Geom.PageSize.A4;
			{
				using (var writer = new PdfWriter(uploadsFile))
				{
					var pdf = new PdfDocument(writer);
					var doc = new iText.Layout.Document(pdf);

					Paragraph header = new Paragraph("Production List")
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(20);
					doc.Add(header);

					if (!string.IsNullOrEmpty(search))
					{
						Paragraph subheader = new Paragraph("Search Text: " + search)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}

					if (!string.IsNullOrEmpty(warehouse))
					{
						Paragraph subheader = new Paragraph("Warehouse: " + products[0].WarehouseName)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}

					// Table
					doc.Add(new Paragraph(""));
					var table = new Table(new float[] { 1, 3, 3, 1, 2, 1});
					table.SetWidth(UnitValue.CreatePercentValue(100));
					// headers
					var cell1 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("ID #"));
					var cell2 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Warehouse"));
					var cell3 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Sub Recipe"));
					var cell4 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Qty"));
					var cell5 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Unit"));
					var cell6 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Status"));
					table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6);

					foreach(var p in products)
					{
						var cell11 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.ID));
						var cell12 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.WarehouseName));
						var cell13 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.SubRecipeName));
						var cell14 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Qty));
						var cell15 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.UnitName));
						var cell16 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Status));

						table.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16);
					}
					doc.Add(table);

					doc.Add(new Paragraph(""));
					doc.Close();
				}
				
				
			}

			return Json(new { status = 0, url = uploadsUrl });
		}

		public JsonResult GetProduction(long productionID)
		{
			var production = _dbContext.SubRecipeProductions.Include(s => s.Warehouse).Include(s => s.SubRecipe).FirstOrDefault(s => s.ID == productionID);
			return Json(production);
		}

        public JsonResult DownloadProduction(long productionID)
        {
            var production = _dbContext.SubRecipeProductions.Include(s => s.Warehouse).Include(s => s.SubRecipe).FirstOrDefault(s => s.ID == productionID);
            var subrecipe = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Items).Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == production.SubRecipe.ID);

			var punit = subrecipe.ItemUnits.FirstOrDefault(s => s.Number == production.UnitNum);

            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "VoidProducts_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;

            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
                // REPORT HEADER
                {
                    string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                    var store = _dbContext.Preferences.First();
                    if (store != null)
                    {
                        var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();

                        var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                        var time = "Fecha: " + DateTime.Today.ToString("MM/dd/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));
                        Cell cell2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        Image img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cell2.Add(img.ScaleToFit(100, 60));

                        }

                        var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cell1);
                        headertable.AddCell(cell2);
                        headertable.AddCell(cell3);

                        doc.Add(headertable);
                    }
                }

                Paragraph header = new Paragraph("Production #" + production.ID)
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(25);
                doc.Add(header);

				{
                    var prodtable = new iText.Layout.Element.Table(new float[] { 1, 4 });
                    prodtable.SetWidth(UnitValue.CreatePercentValue(100));

					{
                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Almacén  :").SetFontSize(12));
                        var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(production.Warehouse.WarehouseName).SetFontSize(12));
						prodtable.AddCell(cell1).AddCell(cell2);
                    }
                    {
                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Date      :").SetFontSize(12));
                        var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(production.ProductionDate.ToString("dd-MM-yyyy")).SetFontSize(12));
                        prodtable.AddCell(cell1).AddCell(cell2);
                    }
                    {
                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Sub Recipe :").SetFontSize(12));
                        var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(production.SubRecipe.Name).SetFontSize(12));
                        prodtable.AddCell(cell1).AddCell(cell2);
                    }
                    {
                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("QTY       :").SetFontSize(12));
                        var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + production.Qty + " " + punit.Name).SetFontSize(12));
                        prodtable.AddCell(cell1).AddCell(cell2);
                    }
                    {
                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Result QTY :").SetFontSize(12));
                        var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + production.EndQty).SetFontSize(12));
                        prodtable.AddCell(cell1).AddCell(cell2);
                    }
                    {
                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Description :").SetFontSize(12));
                        var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(production.Description).SetFontSize(12));
                        prodtable.AddCell(cell1).AddCell(cell2);
                    }
                    doc.Add(prodtable);
					doc.Add(new Paragraph());
					doc.Add(new Paragraph());
                }

                {
                    var prodtable = new iText.Layout.Element.Table(new float[] { 2, 1, 1, 1, 1, 1, 1, 1 });
                    prodtable.SetWidth(UnitValue.CreatePercentValue(100));

                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Nombre").SetFontSize(12));
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Tibo").SetFontSize(12));
                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Marca").SetFontSize(12));
                    var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Actuacion(%)").SetFontSize(12));
                    var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Qty").SetFontSize(12));
                    var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Unidad").SetFontSize(12));
                    var cell7 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Precio($)").SetFontSize(12));
                    var cell8 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Subtotal($)").SetFontSize(12));

                    prodtable.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell7).AddCell(cell8);

					decimal allSubTotal = 0;
                    foreach (var p in subrecipe.Items)
                    {
                        if (p.IsArticle)
                        {
                            var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == p.ItemID);
							var unit = article.Items.FirstOrDefault(s => s.Number == p.UnitNum);
							var price = unit.Price;
							var performance = article.Performance;
							if (performance <= 0) performance = 100;

							decimal itemPrice = price * (decimal)performance / 100.0m;
							decimal subTotal = itemPrice * p.Qty;
							allSubTotal += subTotal;

                            var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(article.Name).SetFontSize(11));
                            var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Article").SetFontSize(11));
                            var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(article.Brand.Name).SetFontSize(11));
                            var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + article.Performance).SetFontSize(11));
                            var cell15 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Qty).SetFontSize(11));
                            var cell16 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(unit.Name).SetFontSize(11));
                            var cell17 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + itemPrice).SetFontSize(11));
                            var cell18 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + subTotal).SetFontSize(11));
                            prodtable.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16).AddCell(cell17).AddCell(cell18);


                        }
                        else
                        {
                            var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == p.ItemID);
                            var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == p.UnitNum);
                            var price = unit.Price;

                            decimal itemPrice = price ;
                            decimal subTotal = itemPrice * p.Qty;
                            allSubTotal += subTotal;

                            var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(subRecipe.Name).SetFontSize(11));
                            var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Sub Recipe").SetFontSize(11));
                            var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("").SetFontSize(11));
                            var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("").SetFontSize(11));
                            var cell15 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Qty).SetFontSize(11));
                            var cell16 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(unit.Name).SetFontSize(11));
                            var cell17 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + itemPrice).SetFontSize(11));
                            var cell18 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + subTotal).SetFontSize(11));
                            prodtable.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16).AddCell(cell17).AddCell(cell18);
                        }
                        
                    }

                    doc.Add(prodtable);
                }

                doc.Close();

            }
            return Json(new { status = 0, url = uploadsUrl });
        }

        public JsonResult EditProduction([FromBody] ProductionCreateModel model)
		{
			try
			{
                var existing = _dbContext.SubRecipeProductions.FirstOrDefault(s => s.ID == model.ID);
                if (existing == null)
                {
                    existing = new SubRecipeProduction();

                    var warehouse = _dbContext.Warehouses.FirstOrDefault(s => s.ID == model.WarehouseID);
                    var subRecipe = _dbContext.SubRecipes.FirstOrDefault(s => s.ID == model.SubRecipeID);


                    existing.Warehouse = warehouse;
                    existing.SubRecipe = subRecipe;
                    existing.UnitNum = model.UnitNum;
                    existing.Description = model.Description;
                    existing.Qty = model.Qty;
                    existing.EndQty = model.EndQty;
				
                    existing.Status = ProductionStatus.Pending;
                    existing.ProductionDate = DateTime.Now;
                    try
                    {
                        var productionDate = DateTime.ParseExact(model.ProductionDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        existing.ProductionDate = productionDate;
                    }
                    catch { }
                    _dbContext.SubRecipeProductions.Add(existing);
                }
                else
                {
                    var warehouse = _dbContext.Warehouses.FirstOrDefault(s => s.ID == model.WarehouseID);
                    var subRecipe = _dbContext.SubRecipes.FirstOrDefault(s => s.ID == model.SubRecipeID);


                    existing.Warehouse = warehouse;
                    existing.SubRecipe = subRecipe;
                    existing.UnitNum = model.UnitNum;
                    existing.Description = model.Description;
                    existing.ProductionDate = DateTime.Now;
                    try
                    {
                        var productionDate = DateTime.ParseExact(model.ProductionDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        existing.ProductionDate = productionDate;
                    }
                    catch { }
                    existing.Qty = model.Qty;
					existing.EndQty = model.EndQty;
					existing.Status = ProductionStatus.Pending;
                }

                _dbContext.SaveChanges();
                return Json(new { status = 0, existing.ID });
            }
			catch { }
		
            return Json(new { status = 1 });
		}

        [HttpPost]
        public JsonResult UpdateProductionStatus([FromBody] StatusUpdateModel model)
        {
            var production = _dbContext.SubRecipeProductions.Include(s=>s.Warehouse).Include(s=>s.SubRecipe).ThenInclude(s=>s.Items).Include(s => s.SubRecipe).ThenInclude(s => s.ItemUnits).FirstOrDefault(s => s.ID == model.ID);
            if (production != null)
            {
				if ((int)production.Status < model.Status)
				{
                    production.Status = (ProductionStatus)model.Status;
                }
				_dbContext.SaveChanges();
                if (production.Status == ProductionStatus.Completed)
                {
					var factor = production.Qty / production.EndQty;
					var baseQty = ConvertQtyToBase(production.Qty, production.UnitNum, production.SubRecipe.ItemUnits.ToList());
					foreach(var item in production.SubRecipe.Items)
					{
						var qty = item.Qty * baseQty;
						if (factor > 0)
						{
							item.FirstQty = item.FirstQty * factor;
							item.Qty = item.Qty * factor;
						}
						if (item.IsArticle)
						{
							var article = _dbContext.Articles.Include(s=>s.Items).FirstOrDefault(s => s.ID == item.ItemID);

							UpdateStockOfArticle(article, -qty, item.UnitNum, production.Warehouse, StockChangeReason.Production, production.ID);
						}
						else
						{
                            var subrecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == item.ItemID);

                            UpdateStockOfSubRecipe(subrecipe, -qty, item.UnitNum, production.Warehouse, StockChangeReason.Production, production.ID);
                           
						}
						_dbContext.SaveChanges();
					}
					{
                        var mainsubrecipe = production.SubRecipe;
                        UpdateStockOfSubRecipe(mainsubrecipe, production.EndQty, production.UnitNum, production.Warehouse, StockChangeReason.Production, production.ID);
                        
						_dbContext.SaveChanges();
					}

					foreach(var item in production.SubRecipe.ItemUnits)
					{
						var newCost = item.Cost * factor;
						item.Cost = newCost;
					}
                    _dbContext.SaveChanges();
                }

                return Json(new { status = 0 });
            }
            return Json(new { status = 1 });
        }

		#endregion

		#region Move Article
		public IActionResult MoveArticleList()
		{
			return View();
		}

		public IActionResult AddMoveArticle(long moveArticleID)
		{
            ViewBag.MoveDate = DateTime.Now.ToString("dd-MM-yyyy");
            ViewBag.MoveArticleID = 0;
            ViewBag.Status = MoveArticleStatus.None;

            if (moveArticleID > 0)
            {
                var move = _dbContext.MoveArticles.FirstOrDefault(s => s.ID == moveArticleID);
                if (move != null)
                {
                    ViewBag.MoveDate = move.MoveDate.ToString("dd-MM-yyyy");
                    ViewBag.MoveArticleID = move.ID;
                    ViewBag.Status = move.Status;
                }
            }
            return View();
		}

		public JsonResult GetStockItem(long warehouseId, long itemID, int itemtype, decimal qty, int unitNum)
		{
			var stockitem = _dbContext.WarehouseStocks.FirstOrDefault(s=>s.Warehouse.ID== warehouseId && s.ItemId == itemID && s.ItemType == (ItemType)itemtype);

			return Json(new { stockitem, qty, unitNum, itemtype });
		}

        [HttpPost]
        public IActionResult GetMoveArticleList()
        {
            try
            {
                var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                // Skiping number of Rows count  
                var start = Request.Form["start"].FirstOrDefault();
                // Paging Length 10,20  
                var length = Request.Form["length"].FirstOrDefault();
                // Sort Column Name  
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                // Sort Column Direction ( asc ,desc)  
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                // Search Value from (Search box)  
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                //Paging Size (10,20,50,100)  
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                // Getting all Customer data  
                var customerData = _dbContext.MoveArticles.Include(s => s.FromWarehouse).Include(s => s.ToWarehouse).OrderByDescending(s => s.MoveDate).Select(s => new
                {
                    ID = s.ID,
                    FromWarehouse = s.FromWarehouse.WarehouseName,
					FromWarehousId = s.FromWarehouse.ID,
                    ToWarehouse = s.ToWarehouse.WarehouseName,
					ToWarehouseId = s.ToWarehouse.ID,
                    Price = s.TotalPrice,
                    MoveDate = s.MoveDate.ToString("dd-MM-yyyy"),
					RealMoveDate = s.MoveDate,
                    Status = s.Status,
                }) ;

                //Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
					try
					{
                        customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
					catch { }
                    
                }

				var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
				var warehousefrom = Request.Form["columns[1][search][value]"].FirstOrDefault();
				var warehouseto = Request.Form["columns[2][search][value]"].FirstOrDefault();
				var datefrom = Request.Form["columns[3][search][value]"].FirstOrDefault();
				var dateto = Request.Form["columns[4][search][value]"].FirstOrDefault();
				var status = Request.Form["columns[5][search][value]"].FirstOrDefault();

				////Search  
				if (!string.IsNullOrEmpty(all))
                {
					all = all.Trim().ToLower();
                    customerData = customerData.Where(m => m.FromWarehouse.ToLower().Contains(all) || m.ToWarehouse.ToLower().Contains(all));
                }
				if (!string.IsNullOrEmpty(warehousefrom))
				{
					customerData = customerData.Where(s => "" + s.FromWarehousId == warehousefrom);
				}
				if (!string.IsNullOrEmpty(warehouseto))
				{
					customerData = customerData.Where(s => "" + s.ToWarehouseId == warehouseto);
				}
				if (!string.IsNullOrEmpty(status))
				{
					customerData = customerData.Where(s => "" + s.Status == status);
				}

				if (!string.IsNullOrEmpty(datefrom))
				{
					try
					{
						var orderDate = DateTime.ParseExact(datefrom, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						customerData = customerData.Where(s => s.RealMoveDate.Date >= orderDate.Date);
					}
					catch { }

				}
				if (!string.IsNullOrEmpty(dateto))
				{
					try
					{
						var orderDate = DateTime.ParseExact(dateto, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						customerData = customerData.Where(s => s.RealMoveDate.Date <= orderDate.Date);
					}
					catch { }

				}
				//total number of rows count   
				recordsTotal = customerData.Count();
                //Paging   
                var data = customerData.Skip(skip).OrderByDescending(s=>s.ID).ToList();
                if (pageSize != -1)
                {
                    data = data.Take(pageSize).ToList();
                }


                //Returning Json Data  
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                throw;
            }
        }


		[HttpPost]
		public JsonResult DownloadMoveArticleExcel(string search, string warehousefrom, string warehouseto, string datefrom, string dateto, string status)
		{
			try
			{
				var customerData = _dbContext.MoveArticles.Include(s => s.FromWarehouse).Include(s => s.ToWarehouse).OrderByDescending(s => s.MoveDate).Select(s => new
				{
					ID = s.ID,
					FromWarehouse = s.FromWarehouse.WarehouseName,
					FromWarehousId = s.FromWarehouse.ID,
					ToWarehouse = s.ToWarehouse.WarehouseName,
					ToWarehouseId = s.ToWarehouse.ID,
					Price = s.TotalPrice,
					MoveDate = s.MoveDate.ToString("dd-MM-yyyy"),
					RealMoveDate = s.MoveDate,
					Status = s.Status,
				});
				////Search  
				if (!string.IsNullOrEmpty(search))
				{
					search = search.Trim().ToLower();
					customerData = customerData.Where(m => m.FromWarehouse.ToLower().Contains(search) || m.ToWarehouse.ToLower().Contains(search));
				}
				if (!string.IsNullOrEmpty(warehousefrom))
				{
					customerData = customerData.Where(s => "" + s.FromWarehousId == warehousefrom);
				}
				if (!string.IsNullOrEmpty(warehouseto))
				{
					customerData = customerData.Where(s => "" + s.ToWarehouseId == warehouseto);
				}
				if (!string.IsNullOrEmpty(status))
				{
					customerData = customerData.Where(s => "" + s.Status == status);
				}

				if (!string.IsNullOrEmpty(datefrom))
				{
					try
					{
						var orderDate = DateTime.ParseExact(datefrom, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						customerData = customerData.Where(s => s.RealMoveDate.Date >= orderDate.Date);
					}
					catch { }

				}
				if (!string.IsNullOrEmpty(dateto))
				{
					try
					{
						var orderDate = DateTime.ParseExact(dateto, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						customerData = customerData.Where(s => s.RealMoveDate.Date <= orderDate.Date);
					}
					catch { }

				}

				var movearticles = customerData.ToList();

				if (movearticles.Count == 0)
				{
					return Json(new { status = 1, message = "There are no items to export." });
				}

				var csvBuilder = new StringBuilder();

				csvBuilder.AppendLine("Transfer List");
				if (!string.IsNullOrEmpty(search))
				{
					csvBuilder.AppendLine("Search Text: ," + search);
				}
				if (!string.IsNullOrEmpty(warehousefrom))
				{
					csvBuilder.AppendLine("Source Warehouse: ," + movearticles[0].FromWarehouse);
				}
				if (!string.IsNullOrEmpty(warehouseto))
				{
					csvBuilder.AppendLine("Target Warehouse: ," + movearticles[0].ToWarehouse);
				}
				if (!string.IsNullOrEmpty(datefrom))
				{
					csvBuilder.AppendLine("From : ," + datefrom);
				}
				if (!string.IsNullOrEmpty(dateto))
				{
					csvBuilder.AppendLine("To : ," + dateto);
				}
				if (!string.IsNullOrEmpty(status))
				{
					csvBuilder.AppendLine("Status : ," + movearticles[0].Status);
				}

				var header = "ID #,Source Warehouse,Target Warehouse,Transfer Date,Status";

				csvBuilder.AppendLine(header);

				foreach (var temp in movearticles)
				{

					var line = $"\"{temp.ID}\",\"{temp.FromWarehouse}\",{temp.ToWarehouse},{temp.MoveDate},{temp.Status}";
					csvBuilder.AppendLine(line);
				}

				string tempFolder = Path.Combine(_env.WebRootPath, "temp");
				var uniqueFileName = "Transfer_" + DateTime.Now.Ticks + ".csv";
				var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
				if (!Directory.Exists(tempFolder))
				{
					Directory.CreateDirectory(tempFolder);
				}

				var uploadsUrl = "/temp/" + uniqueFileName;

				System.IO.File.WriteAllText(uploadsFile, csvBuilder.ToString(), System.Text.Encoding.UTF8);

				return Json(new { status = 0, result = uploadsUrl });
			}
			catch (Exception ex)
			{
				return Json(new { status = 1, message = ex.Message });
			}

		}

		[HttpPost]
		public JsonResult DownloadMoveArticlePDF(string search, string warehousefrom, string warehouseto, string datefrom, string dateto, string status)
		{
			string tempFolder = Path.Combine(_env.WebRootPath, "temp");

			var customerData = _dbContext.MoveArticles.Include(s => s.FromWarehouse).Include(s => s.ToWarehouse).OrderByDescending(s => s.MoveDate).Select(s => new
			{
				ID = s.ID,
				FromWarehouse = s.FromWarehouse.WarehouseName,
				FromWarehousId = s.FromWarehouse.ID,
				ToWarehouse = s.ToWarehouse.WarehouseName,
				ToWarehouseId = s.ToWarehouse.ID,
				Price = s.TotalPrice,
				MoveDate = s.MoveDate.ToString("dd-MM-yyyy"),
				RealMoveDate = s.MoveDate,
				Status = s.Status,
			});
			////Search  
			if (!string.IsNullOrEmpty(search))
			{
				search = search.Trim().ToLower();
				customerData = customerData.Where(m => m.FromWarehouse.ToLower().Contains(search) || m.ToWarehouse.ToLower().Contains(search));
			}
			if (!string.IsNullOrEmpty(warehousefrom))
			{
				customerData = customerData.Where(s => "" + s.FromWarehousId == warehousefrom);
			}
			if (!string.IsNullOrEmpty(warehouseto))
			{
				customerData = customerData.Where(s => "" + s.ToWarehouseId == warehouseto);
			}
			if (!string.IsNullOrEmpty(status))
			{
				customerData = customerData.Where(s => "" + s.Status == status);
			}

			if (!string.IsNullOrEmpty(datefrom))
			{
				try
				{
					var orderDate = DateTime.ParseExact(datefrom, "dd-MM-yyyy", CultureInfo.InvariantCulture);
					customerData = customerData.Where(s => s.RealMoveDate.Date >= orderDate.Date);
				}
				catch { }

			}
			if (!string.IsNullOrEmpty(dateto))
			{
				try
				{
					var orderDate = DateTime.ParseExact(dateto, "dd-MM-yyyy", CultureInfo.InvariantCulture);
					customerData = customerData.Where(s => s.RealMoveDate.Date <= orderDate.Date);
				}
				catch { }

			}

			var movearticles = customerData.ToList();


			if (movearticles.Count == 0)
			{
				return Json(new { status = 1 });
			}
			var uniqueFileName = "Transfer_" + "_" + DateTime.Now.Ticks + ".pdf";
			var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
			var uploadsUrl = "/temp/" + uniqueFileName;
			var paperSize = iText.Kernel.Geom.PageSize.A4;
			{
				using (var writer = new PdfWriter(uploadsFile))
				{
					var pdf = new PdfDocument(writer);
					var doc = new iText.Layout.Document(pdf);

					Paragraph header = new Paragraph("Transfer List")
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(20);
					doc.Add(header);

					if (!string.IsNullOrEmpty(search))
					{
						Paragraph subheader = new Paragraph("Search Text: " + search)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}

					if (!string.IsNullOrEmpty(warehousefrom))
					{
						Paragraph subheader = new Paragraph("Source Warehouse: " + movearticles[0].FromWarehouse)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					if (!string.IsNullOrEmpty(warehouseto))
					{
						Paragraph subheader = new Paragraph("Target Warehouse: " + movearticles[0].ToWarehouse)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					if (!string.IsNullOrEmpty(datefrom))
					{
						Paragraph subheader = new Paragraph("Date From: " + datefrom)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					if (!string.IsNullOrEmpty(dateto))
					{
						Paragraph subheader = new Paragraph("Date To: " + dateto)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					if (!string.IsNullOrEmpty(status))
					{
						Paragraph subheader = new Paragraph("Status: " + movearticles[0].Status)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					// Table
					doc.Add(new Paragraph(""));
					var table = new Table(new float[] { 1, 3, 3, 2, 2 });
					table.SetWidth(UnitValue.CreatePercentValue(100));
					// headers
					var cell1 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("ID #"));
					var cell2 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Source Warehouse"));
					var cell3 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Target Warehouse"));
					var cell4 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Date"));
					var cell6 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Status"));
					table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell6);

					foreach (var p in movearticles)
					{
						var cell11 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.ID));
						var cell12 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.FromWarehouse));
						var cell13 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.ToWarehouse));
						var cell14 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.MoveDate));
						var cell16 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Status));

						table.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell16);
					}
					doc.Add(table);

					doc.Add(new Paragraph(""));
					doc.Close();
				}


			}

			return Json(new { status = 0, url = uploadsUrl });
		}


		public JsonResult GetMoveArticle(long moveArticleID)
        {
            var production = _dbContext.MoveArticles.Include(s => s.FromWarehouse).Include(s => s.ToWarehouse).Include(s => s.Items).FirstOrDefault(s => s.ID == moveArticleID);
            return Json(production);
        }

        public JsonResult EditMoveArticle([FromBody] MoveArticleCreateModel model)
        {
            try
            {
                var existing = _dbContext.MoveArticles.Include(s => s.Items).FirstOrDefault(s => s.ID == model.ID);
                if (existing == null)
                {
                    existing = new MoveArticle();

                    var from = _dbContext.Warehouses.FirstOrDefault(s => s.ID == model.FromWarehouseID);
                    var to = _dbContext.Warehouses.FirstOrDefault(s => s.ID == model.ToWarehouseID);

					existing.FromWarehouse = from;
					existing.ToWarehouse = to;
					existing.Description = model.Description;
					existing.Status = MoveArticleStatus.Pending;
                    existing.MoveDate = DateTime.Now;
                    try
                    {
                        var moveDate = DateTime.ParseExact(model.MoveDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        existing.MoveDate = moveDate;
                    }
                    catch { }
                    existing.Items = new List<MoveItem>();
					decimal totalPrice = 0;
					foreach(var item in model.MoveItems)
					{
						var moveItem = new MoveItem();
						moveItem.ItemID = item.ItemID;
						moveItem.UnitNum = item.UnitNum;
						moveItem.Qty = item.Qty;
						moveItem.ItemType = item.IsArticle ? ItemType.Article : ItemType.SubRecipe;
						if (moveItem.ItemType == ItemType.Article)
						{
							var article = _dbContext.Articles.Include(s => s.Category).Include(s => s.Brand).Include(s => s.SubCategory).Include(s => s.Items).FirstOrDefault(s => s.ID == item.ItemID);
							var unit = article.Items.FirstOrDefault(s => s.Number == moveItem.UnitNum);

							totalPrice += item.Qty * unit.Cost;
						}
						else if (moveItem.ItemType == ItemType.SubRecipe)
						{
							var subRecipe = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == item.ItemID);
							var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == item.UnitNum);

							totalPrice += item.Qty * unit.Cost;
						}
						existing.TotalPrice = totalPrice;
						existing.Items.Add(moveItem);
					}
            
                    _dbContext.MoveArticles.Add(existing);
                }
                else
                {

                    var from = _dbContext.Warehouses.FirstOrDefault(s => s.ID == model.FromWarehouseID);
                    var to = _dbContext.Warehouses.FirstOrDefault(s => s.ID == model.ToWarehouseID);

                    existing.FromWarehouse = from;
                    existing.ToWarehouse = to;
                    existing.MoveDate = DateTime.Now;
                    try
                    {
                        var moveDate = DateTime.ParseExact(model.MoveDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        existing.MoveDate = moveDate;
                    }
                    catch { }
                    existing.Description = model.Description;

					if (existing.Items != null && existing.Items.Count > 0)
					{
						foreach(var item in existing.Items)
						{
							_dbContext.Remove(item);
						}
						existing.Items.Clear();
					}
					else
					{
						existing.Items = new List<MoveItem>();
					}
					decimal totalPrice = 0;
                    foreach (var item in model.MoveItems)
                    {
                        var moveItem = new MoveItem();
                        moveItem.ItemID = item.ItemID;
                        moveItem.UnitNum = item.UnitNum;
                        moveItem.Qty = item.Qty;
                        moveItem.ItemType = item.IsArticle ? ItemType.Article : ItemType.SubRecipe;
						if (moveItem.ItemType == ItemType.Article)
						{
							var article = _dbContext.Articles.Include(s => s.Category).Include(s => s.Brand).Include(s => s.SubCategory).Include(s => s.Items).FirstOrDefault(s => s.ID == item.ItemID);
							var unit = article.Items.FirstOrDefault(s => s.Number == moveItem.UnitNum);

							totalPrice += item.Qty * unit.Cost;
						}
						else if (moveItem.ItemType == ItemType.SubRecipe)
						{
							var subRecipe = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == item.ItemID);
							var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == item.UnitNum);

							totalPrice += item.Qty * unit.Cost;
						}

                        existing.Items.Add(moveItem);
                    }
					existing.TotalPrice = totalPrice;
                    
                }

                _dbContext.SaveChanges();
                return Json(new { status = 0, existing.ID });
            }
            catch { }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult UpdateMoveArticleStatus([FromBody] StatusUpdateModel model)
        {
            var move = _dbContext.MoveArticles.Include(s => s.FromWarehouse).Include(s => s.ToWarehouse).Include(s => s.Items).FirstOrDefault(s => s.ID == model.ID);
            if (move != null)
            {
                if ((int)move.Status < model.Status)
                {
                    move.Status = (MoveArticleStatus)model.Status;
                }
                _dbContext.SaveChanges();
                if (move.Status == MoveArticleStatus.Moved)
                {
                    // move
                    foreach(var item in move.Items)
					{
						if (item.ItemType == ItemType.Article)
						{
							var article = _dbContext.Articles.Include(s=>s.Items).FirstOrDefault(s => s.ID == item.ItemID);

                            UpdateStockOfArticle(article, -item.Qty, item.UnitNum, move.FromWarehouse, StockChangeReason.Move, move.ID);
                            UpdateStockOfArticle(article, item.Qty, item.UnitNum, move.ToWarehouse, StockChangeReason.Move, move.ID);
						}
						else
						{
                            var subresipe = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == item.ItemID);

                            UpdateStockOfSubRecipe(subresipe, -item.Qty, item.UnitNum, move.FromWarehouse, StockChangeReason.Move, move.ID);
                            UpdateStockOfSubRecipe(subresipe, item.Qty, item.UnitNum, move.ToWarehouse, StockChangeReason.Move, move.ID);
                        }
						_dbContext.SaveChanges();
					}
                }

                return Json(new { status = 0 });
            }
            return Json(new { status = 1 });
        }

		private void UpdateStockOfArticle(InventoryItem item, decimal qty, int unitNum, Warehouse warehouse, StockChangeReason reason, long reasonId)
		{
			decimal baseQty = ConvertQtyToBase(qty, unitNum, item.Items.ToList());
			
            var existingWarehouseStock = _dbContext.WarehouseStocks.FirstOrDefault(s => s.Warehouse == warehouse && s.ItemType == ItemType.Article && s.ItemId == item.ID);

            var warehouseHistoryItem = new WarehouseStockChangeHistory();
            warehouseHistoryItem.Warehouse = warehouse;
            warehouseHistoryItem.Price = 0;
            warehouseHistoryItem.ItemId = item.ID;
            warehouseHistoryItem.ItemType = ItemType.Article;
            warehouseHistoryItem.BeforeBalance = existingWarehouseStock == null ? 0 : existingWarehouseStock.Qty;
            warehouseHistoryItem.AfterBalance = existingWarehouseStock == null ? baseQty : existingWarehouseStock.Qty + baseQty;
            warehouseHistoryItem.Qty = baseQty;
            warehouseHistoryItem.UnitNum = 1;
            warehouseHistoryItem.ReasonType = reason;
            warehouseHistoryItem.ReasonId = reasonId;

            _dbContext.WarehouseStockChangeHistory.Add(warehouseHistoryItem);
            if (existingWarehouseStock == null)
            {
                existingWarehouseStock = new WarehouseStock();
                existingWarehouseStock.Warehouse = warehouse;
                existingWarehouseStock.ItemId = item.ID;
                existingWarehouseStock.ItemType = ItemType.Article;
                existingWarehouseStock.Qty = baseQty;

                _dbContext.WarehouseStocks.Add(existingWarehouseStock);
            }
            else
            {
                existingWarehouseStock.Qty += baseQty;
            }
        }

        private void UpdateStockOfSubRecipe(SubRecipe item, decimal qty, int unitNum, Warehouse warehouse, StockChangeReason reason, long reasonId)
        {
            decimal baseQty = ConvertQtyToBase(qty, unitNum, item.ItemUnits.ToList());

            var existingWarehouseStock = _dbContext.WarehouseStocks.FirstOrDefault(s => s.Warehouse == warehouse && s.ItemType == ItemType.SubRecipe && s.ItemId == item.ID);

            var warehouseHistoryItem = new WarehouseStockChangeHistory();
            warehouseHistoryItem.Warehouse = warehouse;
            warehouseHistoryItem.Price = 0;
            warehouseHistoryItem.ItemId = item.ID;
            warehouseHistoryItem.ItemType = ItemType.SubRecipe;
            warehouseHistoryItem.BeforeBalance = existingWarehouseStock == null ? 0 : existingWarehouseStock.Qty;
            warehouseHistoryItem.AfterBalance = existingWarehouseStock == null ? baseQty : existingWarehouseStock.Qty + baseQty;
            warehouseHistoryItem.Qty = baseQty;
            warehouseHistoryItem.UnitNum = 1;
            warehouseHistoryItem.ReasonType = reason;
            warehouseHistoryItem.ReasonId = reasonId;

            _dbContext.WarehouseStockChangeHistory.Add(warehouseHistoryItem);
            if (existingWarehouseStock == null)
            {
                existingWarehouseStock = new WarehouseStock();
                existingWarehouseStock.Warehouse = warehouse;
                existingWarehouseStock.ItemId = item.ID;
                existingWarehouseStock.ItemType = ItemType.SubRecipe;
                existingWarehouseStock.Qty = baseQty;

                _dbContext.WarehouseStocks.Add(existingWarehouseStock);
            }
            else
            {
                existingWarehouseStock.Qty += baseQty;
            }
        }

        #endregion

        #region Damage Article

        public IActionResult DamageArticleList()
		{
			return View();
		}

		public IActionResult AddDamageArticle(long damageArticleID)
		{
            ViewBag.DamageDate = DateTime.Now.ToString("dd-MM-yyyy");
			ViewBag.DamageArticleID = 0;
			ViewBag.Status = DamageArticleStatus.None;
			if (damageArticleID > 0)
			{
				var damage = _dbContext.DamageArticles.FirstOrDefault(s=>s.ID == damageArticleID);
				if (damage != null)
				{
					ViewBag.DamageDate = damage.DamageDate.ToString("dd-MM-yyyy");
					ViewBag.DamageArticleID = damage.ID;
                    ViewBag.Status = damage.Status;

                }
			}
            return View();
		}

        [HttpPost]
        public IActionResult GetDamageArticleList()
        {
            try
            {
                var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                // Skiping number of Rows count  
                var start = Request.Form["start"].FirstOrDefault();
                // Paging Length 10,20  
                var length = Request.Form["length"].FirstOrDefault();
                // Sort Column Name  
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                // Sort Column Direction ( asc ,desc)  
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                // Search Value from (Search box)  
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                //Paging Size (10,20,50,100)  
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                // Getting all Customer data  
                var customerData = _dbContext.DamageArticles.Include(s => s.Warehouse).Include(s => s.Article).Include(s=>s.SubRecipe).OrderByDescending(s => s.DamageDate).Select(s => new
                {
                    ID = s.ID,
                    WarehouseName = s.Warehouse.WarehouseName,
					WarehouseId = s.Warehouse.ID,
                    Name = s.ItemType == ItemType.Article? s.Article.Name : s.SubRecipe.Name,
					Price = s.Price,
                    UnitName = s.UnitName,
					DamageDate = s.DamageDate.ToString("dd-MM-yyyy"),
					RealDamageDate = s.DamageDate,
                    Qty = s.Qty,
                    Status = s.Status,
                }); ;

                //Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				{
					try
					{
						customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
					}
					catch { }
                    
                }

				var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
				var datefrom = Request.Form["columns[1][search][value]"].FirstOrDefault();
				var warehouse = Request.Form["columns[2][search][value]"].FirstOrDefault();
				var status = Request.Form["columns[3][search][value]"].FirstOrDefault();
				var dateto = Request.Form["columns[4][search][value]"].FirstOrDefault();

				////Search  
				if (!string.IsNullOrEmpty(all))
                {
					all = all.Trim().ToLower();
                    customerData = customerData.Where(m => m.WarehouseName.ToLower().Contains(all) || m.Name.ToLower().Contains(all));
                }

				if (!string.IsNullOrEmpty(warehouse))
				{
					customerData = customerData.Where(s => "" + s.WarehouseId == warehouse);
				}
				if (!string.IsNullOrEmpty(status))
				{
					customerData = customerData.Where(s => "" + s.Status == status);
				}
				if (!string.IsNullOrEmpty(datefrom))
				{
					try
					{
						var orderDate = DateTime.ParseExact(datefrom, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						customerData = customerData.Where(s => s.RealDamageDate.Date >= orderDate.Date);
					}
					catch { }

				}
				if (!string.IsNullOrEmpty(dateto))
				{
					try
					{
						var orderDate = DateTime.ParseExact(dateto, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						customerData = customerData.Where(s => s.RealDamageDate.Date <= orderDate.Date);
					}
					catch { }

				}

				//total number of rows count   
				recordsTotal = customerData.Count();
                //Paging   
                var data = customerData.Skip(skip).ToList();
                if (pageSize != -1)
                {
                    data = data.Take(pageSize).ToList();
                }

               
                //Returning Json Data  
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                throw;
            }
        }

		[HttpPost]
		public JsonResult CopyArticle(long articleId)
		{
			var model = _dbContext.Articles.Include(s => s.Suppliers).Include(s => s.Tax).Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Brand).Include(s => s.Items.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == articleId);

			if (model != null)
			{
				var accountItems = _dbContext.AccountItems.Include(s => s.Account).Where(s => s.ItemID == articleId && s.Target == AccountingTarget.Article).ToList();

				var article = new InventoryItem();

				article.Name = model.Name + " - Copy";
				article.IsActive = model.IsActive;
				article.Performance = model.Performance;
				article.MinimumQuantity = model.MinimumQuantity;
				article.MaximumQuantity = model.MaximumQuantity;
				article.MinimumUnit = model.MinimumUnit;
				article.MaximumUnit = model.MaximumUnit;
				article.DefaultUnitNum = model.DefaultUnitNum;
				article.ScannerUnit = model.ScannerUnit;
				article.DepleteCondition = (DepleteCondition)model.DepleteCondition;
				article.Photo = model.Photo;
				article.Barcode = model.Barcode;

				if (model.Category != null)
				{
					var category = _dbContext.Categories.FirstOrDefault(s => s.ID == model.Category.ID);
					article.Category = category;
				}
				
				if (model.SubCategory != null)
				{
					var subcategory = _dbContext.SubCategories.FirstOrDefault(s => s.ID == model.SubCategory.ID);
					article.SubCategory = subcategory;
				}
			
				if (model.Brand != null)
				{
					var brand = _dbContext.Brands.FirstOrDefault(s => s.ID == model.Brand.ID);
					article.Brand = brand;
				}
				if (model.Tax != null)
				{
					var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == model.Tax.ID);
					article.Tax = tax;

				}


				if (model.Suppliers != null && model.Suppliers.Count > 0)
				{
					article.Suppliers = new List<Supplier>();
					foreach (var ss in model.Suppliers)
					{
						article.Suppliers.Add(ss);
					}
				}
				_dbContext.Articles.Add(article);
				_dbContext.SaveChanges();

				article.Items = new List<ItemUnit>();

				foreach (var item in model.Items)
				{					
					article.Items.Add(new ItemUnit()
					{
						CodeBar = item.CodeBar,
						Cost = item.Cost,
						Name = item.Name,
						UnitID = item.UnitID,
						Number = item.Number,
						Price = item.Price,
						PayItem = item.PayItem,
						Rate = item.Rate,
						Unit = item.Unit,					
					});
				}

				_dbContext.SaveChanges();

				return Json(new { status = 0 });
			}
			return Json(new { status = 1 });
		}


		[HttpPost]
		public JsonResult DownloadDamageArticleExcel(string search, string warehouse, string datefrom, string dateto, string status)
		{
			try
			{
				// Getting all Customer data  
				var customerData = _dbContext.DamageArticles.Include(s => s.Warehouse).Include(s => s.Article).Include(s => s.SubRecipe).OrderByDescending(s => s.DamageDate).Select(s => new
				{
					ID = s.ID,
					WarehouseName = s.Warehouse.WarehouseName,
					WarehouseId = s.Warehouse.ID,
					Name = s.ItemType == ItemType.Article ? s.Article.Name : s.SubRecipe.Name,
					Price = s.Price,
					UnitName = s.UnitName,
					DamageDate = s.DamageDate.ToString("dd-MM-yyyy"),
					RealDamageDate = s.DamageDate,
					Qty = s.Qty,
					Status = s.Status,
				}); 
							
				////Search  
				if (!string.IsNullOrEmpty(search))
				{
					search = search.Trim().ToLower();
					customerData = customerData.Where(m => m.WarehouseName.ToLower().Contains(search) || m.Name.ToLower().Contains(search));
				}

				if (!string.IsNullOrEmpty(warehouse))
				{
					customerData = customerData.Where(s => "" + s.WarehouseId == warehouse);
				}
				if (!string.IsNullOrEmpty(status))
				{
					customerData = customerData.Where(s => "" + s.Status == status);
				}
				if (!string.IsNullOrEmpty(datefrom))
				{
					try
					{
						var orderDate = DateTime.ParseExact(datefrom, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						customerData = customerData.Where(s => s.RealDamageDate.Date >= orderDate.Date);
					}
					catch { }

				}
				if (!string.IsNullOrEmpty(dateto))
				{
					try
					{
						var orderDate = DateTime.ParseExact(dateto, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						customerData = customerData.Where(s => s.RealDamageDate.Date <= orderDate.Date);
					}
					catch { }

				}

				var damagearticles = customerData.ToList();

				if (damagearticles.Count == 0)
				{
					return Json(new { status = 1, message = "There are no items to export." });
				}

				var csvBuilder = new StringBuilder();

				csvBuilder.AppendLine("Damage Article List");
				if (!string.IsNullOrEmpty(search))
				{
					csvBuilder.AppendLine("Search Text: ," + search);
				}
				if (!string.IsNullOrEmpty(warehouse))
				{
					csvBuilder.AppendLine("Warehouse: ," + damagearticles[0].WarehouseName);
				}			
				if (!string.IsNullOrEmpty(datefrom))
				{
					csvBuilder.AppendLine("From : ," + datefrom);
				}
				if (!string.IsNullOrEmpty(dateto))
				{
					csvBuilder.AppendLine("To : ," + dateto);
				}
				if (!string.IsNullOrEmpty(status))
				{
					csvBuilder.AppendLine("Status : ," + damagearticles[0].Status);
				}

				var header = "ID #,Warehouse,Name, Date,Qty,Unit,Status";

				csvBuilder.AppendLine(header);

				foreach (var temp in damagearticles)
				{

					var line = $"\"{temp.ID}\",\"{temp.WarehouseName}\",{temp.Name},{temp.DamageDate},\"{temp.Qty}\",{temp.UnitName},{temp.Status}";
					csvBuilder.AppendLine(line);
				}

				string tempFolder = Path.Combine(_env.WebRootPath, "temp");
				var uniqueFileName = "MoveArticle_" + DateTime.Now.Ticks + ".csv";
				var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
				if (!Directory.Exists(tempFolder))
				{
					Directory.CreateDirectory(tempFolder);
				}

				var uploadsUrl = "/temp/" + uniqueFileName;

				System.IO.File.WriteAllText(uploadsFile, csvBuilder.ToString(), System.Text.Encoding.UTF8);

				return Json(new { status = 0, result = uploadsUrl });
			}
			catch (Exception ex)
			{
				return Json(new { status = 1, message = ex.Message });
			}

		}

		[HttpPost]
		public JsonResult DownloadDamageArticlePDF(string search, string warehouse, string datefrom, string dateto, string status)
		{
			string tempFolder = Path.Combine(_env.WebRootPath, "temp");
			var customerData = _dbContext.DamageArticles.Include(s => s.Warehouse).Include(s => s.Article).Include(s => s.SubRecipe).OrderByDescending(s => s.DamageDate).Select(s => new
			{
				ID = s.ID,
				WarehouseName = s.Warehouse.WarehouseName,
				WarehouseId = s.Warehouse.ID,
				Name = s.ItemType == ItemType.Article ? s.Article.Name : s.SubRecipe.Name,
				Price = s.Price,
				UnitName = s.UnitName,
				DamageDate = s.DamageDate.ToString("dd-MM-yyyy"),
				RealDamageDate = s.DamageDate,
				Qty = s.Qty,
				Status = s.Status,
			});

			////Search  
			if (!string.IsNullOrEmpty(search))
			{
				search = search.Trim().ToLower();
				customerData = customerData.Where(m => m.WarehouseName.ToLower().Contains(search) || m.Name.ToLower().Contains(search));
			}

			if (!string.IsNullOrEmpty(warehouse))
			{
				customerData = customerData.Where(s => "" + s.WarehouseId == warehouse);
			}
			if (!string.IsNullOrEmpty(status))
			{
				customerData = customerData.Where(s => "" + s.Status == status);
			}
			if (!string.IsNullOrEmpty(datefrom))
			{
				try
				{
					var orderDate = DateTime.ParseExact(datefrom, "dd-MM-yyyy", CultureInfo.InvariantCulture);
					customerData = customerData.Where(s => s.RealDamageDate.Date >= orderDate.Date);
				}
				catch { }

			}
			if (!string.IsNullOrEmpty(dateto))
			{
				try
				{
					var orderDate = DateTime.ParseExact(dateto, "dd-MM-yyyy", CultureInfo.InvariantCulture);
					customerData = customerData.Where(s => s.RealDamageDate.Date <= orderDate.Date);
				}
				catch { }

			}

			var damagearticles = customerData.ToList();


			if (damagearticles.Count == 0)
			{
				return Json(new { status = 1 });
			}
			var uniqueFileName = "DamageArticle_" + "_" + DateTime.Now.Ticks + ".pdf";
			var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
			var uploadsUrl = "/temp/" + uniqueFileName;
			var paperSize = iText.Kernel.Geom.PageSize.A4;
			{
				using (var writer = new PdfWriter(uploadsFile))
				{
					var pdf = new PdfDocument(writer);
					var doc = new iText.Layout.Document(pdf);

					Paragraph header = new Paragraph("Damage Article List")
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(20);
					doc.Add(header);

					if (!string.IsNullOrEmpty(search))
					{
						Paragraph subheader = new Paragraph("Search Text: " + search)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}

					if (!string.IsNullOrEmpty(warehouse))
					{
						Paragraph subheader = new Paragraph("Warehouse: " + damagearticles[0].WarehouseName)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
				
					if (!string.IsNullOrEmpty(datefrom))
					{
						Paragraph subheader = new Paragraph("Date From: " + datefrom)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					if (!string.IsNullOrEmpty(dateto))
					{
						Paragraph subheader = new Paragraph("Date To: " + dateto)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					if (!string.IsNullOrEmpty(status))
					{
						Paragraph subheader = new Paragraph("Status: " + damagearticles[0].Status)
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(15);
						doc.Add(subheader);
					}
					// Table
					doc.Add(new Paragraph(""));
					var table = new Table(new float[] { 1, 3, 3, 2, 2, 2, 2 });
					table.SetWidth(UnitValue.CreatePercentValue(100));
					// headers
					var cell1 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("ID #"));
					var cell2 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Warehouse"));
					var cell3 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Name"));
					var cell4 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Date"));
					var cell5 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Qty"));
					var cell6 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Unit"));
					var cell7 = new Cell(1, 1).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Status"));
					table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell7);

					foreach (var p in damagearticles)
					{
						var cell11 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.ID));
						var cell12 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.WarehouseName));
						var cell13 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Name));
						var cell14 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.DamageDate));
						var cell15 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Qty));
						var cell16 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.UnitName));
						var cell17 = new Cell(1, 1).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Status));

						table.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16).AddCell(cell17);
					}
					doc.Add(table);

					doc.Add(new Paragraph(""));
					doc.Close();
				}


			}

			return Json(new { status = 0, url = uploadsUrl });
		}

		public JsonResult GetDamageArticle(long damageArticleID)
        {
            var production = _dbContext.DamageArticles.Include(s => s.Warehouse).Include(s => s.Article).Include(s=>s.SubRecipe).Include(s=>s.Photos).FirstOrDefault(s => s.ID == damageArticleID);
            return Json(production);
        }

        public JsonResult EditDamageArticle([FromBody] DamageArticleCreateModel model)
        {
            try
            {
                var existing = _dbContext.DamageArticles.Include(s=>s.Photos).FirstOrDefault(s => s.ID == model.ID);
                if (existing == null)
                {
                    existing = new DamageArticle();

                    var warehouse = _dbContext.Warehouses.FirstOrDefault(s => s.ID == model.WarehouseID);
					if (model.IsArticle)
					{
                        var article = _dbContext.Articles.Include(s => s.Items).FirstOrDefault(s => s.ID == model.ItemID);

                        var unit = article.Items.FirstOrDefault(s => s.Number == model.UnitNum);
                        var baseQty = ConvertQtyToBase(model.Qty, model.UnitNum, article.Items.ToList());
                        var price = baseQty * unit.Cost;
						existing.Article = article;
                        existing.Price = price;
                        existing.UnitName = unit.Name;
                        existing.ItemType = ItemType.Article;
                    }
					else
					{
                        var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == model.ItemID);

                        var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == model.UnitNum);
                        var baseQty = ConvertQtyToBase(model.Qty, model.UnitNum, subRecipe.ItemUnits.ToList());
                        var price = baseQty * unit.Cost;
						existing.SubRecipe = subRecipe;
                        existing.Price = price;
                        existing.UnitName = unit.Name;
						existing.ItemType = ItemType.SubRecipe;
                    }
                 
                    existing.Warehouse = warehouse;
                    existing.UnitNum = model.UnitNum;
                    existing.Description = model.Description;
                    existing.Qty = model.Qty;
					existing.DamageDate = DateTime.Now;
                    try
					{
						var damageDate = DateTime.ParseExact(model.DamageDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        existing.DamageDate = damageDate;
                    }
					catch { }
                    
                    existing.Status = DamageArticleStatus.Pending;
					if (model.Images != null && model.Images.Count > 0)
					{
						existing.Photos = new List<GeneralMedia>();

						foreach(var item in model.Images)
						{
							var image = new GeneralMedia();
							image.IsURL = false;
							image.Src = item;
							image.DestEntity = MediaDestination.DamagedArticle;
							image.Type = MediaType.Image;
							
							existing.Photos.Add(image);
						}
					}
                    _dbContext.DamageArticles.Add(existing);
                }
                else
                {
                    var warehouse = _dbContext.Warehouses.FirstOrDefault(s => s.ID == model.WarehouseID);
                    if (model.IsArticle)
                    {
                        var article = _dbContext.Articles.Include(s => s.Items).FirstOrDefault(s => s.ID == model.ItemID);

                        var unit = article.Items.FirstOrDefault(s => s.Number == model.UnitNum);
                        var baseQty = ConvertQtyToBase(model.Qty, model.UnitNum, article.Items.ToList());
                        var price = baseQty * unit.Cost;
                        existing.Article = article;
                        existing.Price = price;
                        existing.UnitName = unit.Name;
                        existing.ItemType = ItemType.Article;
                    }
                    else
                    {
                        var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == model.ItemID);

                        var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == model.UnitNum);
                        var baseQty = ConvertQtyToBase(model.Qty, model.UnitNum, subRecipe.ItemUnits.ToList());
                        var price = baseQty * unit.Cost;
                        existing.SubRecipe = subRecipe;
                        existing.Price = price;
                        existing.UnitName = unit.Name;
                        existing.ItemType = ItemType.SubRecipe;
                    }
                    existing.Warehouse = warehouse;
                    existing.UnitNum = model.UnitNum;
                    existing.Description = model.Description;
                    existing.Qty = model.Qty;
                    existing.DamageDate = DateTime.Now;
                    try
                    {
                        var damageDate = DateTime.ParseExact(model.DamageDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        existing.DamageDate = damageDate;
                    }
                    catch { }

                    if (model.Images != null && model.Images.Count > 0)
                    {
						if (existing.Photos!= null && existing.Photos.Count > 0)
						{
							foreach(var photo in existing.Photos)
							{
								_dbContext.Medias.Remove(photo);
                            }
							existing.Photos.Clear();
						}
						else
						{
                            existing.Photos = new List<GeneralMedia>();
                        }                      

                        foreach (var item in model.Images)
                        {
                            var image = new GeneralMedia();
                            image.IsURL = false;
                            image.Src = item;
                            image.DestEntity = MediaDestination.DamagedArticle;
                            image.Type = MediaType.Image;

                            existing.Photos.Add(image);
                        }
                    }
                }

                _dbContext.SaveChanges();
                return Json(new { status = 0, existing.ID });
            }
            catch { }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult UpdateDamageArticleStatus([FromBody] StatusUpdateModel model)
        {
            var damage = _dbContext.DamageArticles.Include(s => s.Warehouse).Include(s => s.Article).ThenInclude(s => s.Items).Include(s=>s.SubRecipe).ThenInclude(s=>s.ItemUnits).FirstOrDefault(s => s.ID == model.ID);
            if (damage != null)
            {
                if ((int)damage.Status < model.Status)
                {
                    damage.Status = (DamageArticleStatus)model.Status;
                }
                _dbContext.SaveChanges();
                if (damage.Status == DamageArticleStatus.Confirmed)
                {
                    if (damage.ItemType == ItemType.Article)
                    {
						UpdateStockOfArticle(damage.Article, -damage.Qty, damage.UnitNum, damage.Warehouse, StockChangeReason.Damage, damage.ID);
                    }
                    else
                    {
                        UpdateStockOfSubRecipe(damage.SubRecipe, -damage.Qty, damage.UnitNum, damage.Warehouse, StockChangeReason.Damage, damage.ID);
                    }
					_dbContext.SaveChanges();
				}

                return Json(new { status = 0 });
            }
            return Json(new { status = 1 });
        }


		#endregion

		#region physical count

		public IActionResult ManagePhysicalCount()
		{
			return View();
		}

        [HttpPost]
        public IActionResult GetWarehouseStockList(long warehouseID = 0)
        {
	        int cual = 0;
            try
            {
                var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                // Skiping number of Rows count  
                var start = Request.Form["start"].FirstOrDefault();
                // Paging Length 10,20  
                var length = Request.Form["length"].FirstOrDefault();
                // Sort Column Name  
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                // Sort Column Direction ( asc ,desc)  
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                // Search Value from (Search box)  
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                //Paging Size (10,20,50,100)  
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                // Getting all Customer data  
                var customerData = _dbContext.WarehouseStocks.Include(s=>s.Warehouse).Where(s=>s.Warehouse.ID == warehouseID).OrderByDescending(s=>s.UpdatedBy).ToList();
				var result = new List<PhysicalCountViewModel>();


                var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
                var category = Request.Form["columns[1][search][value]"].FirstOrDefault();
                var subcategory = Request.Form["columns[2][search][value]"].FirstOrDefault();
                var barcode = Request.Form["columns[3][search][value]"].FirstOrDefault();
				if (category == "null") category = "";
				if (subcategory == "null") subcategory = "";
				if (barcode == "null") barcode = "";
				
                foreach (var c in customerData)
                {
	                cual = cual + 1;
					if (c.ItemType == ItemType.Article)
					{
						var article = _dbContext.Articles.Include(s=>s.Brand).Include(s=>s.Category).Include(s=>s.SubCategory).Include(s=>s.Items).FirstOrDefault(s => s.ID == c.ItemId);
						if (!article.IsActive) continue;
						var units = article.Items.OrderBy(s => s.Number).ToList();
                        var article_barcode = "";
						foreach(var u in units)
						{
							if (!string.IsNullOrEmpty(u.CodeBar))
							{
								article_barcode = u.CodeBar;
							}
						}
						var item = new PhysicalCountViewModel()
						{
							ItemID = article.ID,
							Name = article.Name,
							Type = "Article",
							Brand = article.Brand?.Name,
							//Category = article.Category.Name,
							//CategoryID = article.Category.ID,
							Category = article.Category?.Name,
							CategoryID = article.Category == null ? 0 : article.Category.ID,
							SubCategory = article.SubCategory == null? "" : article.SubCategory.Name,
							SubCategoryID = article.SubCategory == null ? 0: article.SubCategory.ID,
							Barcode = article_barcode,
							Units = units,
                            ItemType= c.ItemType,
							StockQty = c.Qty
                        };
						result.Add(item);
					}
					else
					{
                        var subrecipe = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == c.ItemId);
						if (!subrecipe.IsActive) continue;
                        var item = new PhysicalCountViewModel()
                        {
                            ItemID = subrecipe.ID,
                            Name = subrecipe.Name,
                            Brand = "",
                            Type = "Sub Recipe",
                            Category = subrecipe.Category.Name,
                            CategoryID = subrecipe.Category.ID,
                            SubCategory = subrecipe.SubCategory == null ? "" : subrecipe.SubCategory.Name,
                            SubCategoryID = subrecipe.SubCategory == null ? 0 : subrecipe.SubCategory.ID,
                            Units = subrecipe.ItemUnits.OrderBy(s => s.Number).ToList(),
                            ItemType = c.ItemType,
                            StockQty = c.Qty
                        };
                        result.Add(item);
                    }
				}

				if (!string.IsNullOrEmpty(barcode))
				{
                    result = result.Where(s => "" + s.Barcode == barcode).ToList();
                }
				else
				{
                    ////Search  
                    if (!string.IsNullOrEmpty(all))
                    {
                        all = all.Trim().ToLower();
                        result = result.Where(m => m.Name.ToLower().Contains(all) || m.Category.ToLower().Contains(all) || m.SubCategory.ToLower().Contains(all) || m.Brand.ToLower().Contains(all)).ToList();
                    }

                    if (!string.IsNullOrEmpty(category))
                    {
                        result = result.Where(s => "" + s.CategoryID == category).ToList();
                    }
                    if (!string.IsNullOrEmpty(subcategory))
                    {
                        result = result.Where(s => "" + s.SubCategoryID == subcategory).ToList();
                    }

                }

                //total number of rows count   
                recordsTotal = result.Count();
                //Paging   
                var data = result.Skip(skip).ToList();
                if (pageSize != -1)
                {
                    data = data.Take(pageSize).ToList();
                }

                //Returning Json Data  
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                throw;
            }
        }

		[HttpPost]
		public JsonResult GetCategoryTotal(long warehouseID, int categoryID)
		{
			decimal total = 0;
			if (warehouseID > 0 && categoryID > 0)
			{
                var customerData = _dbContext.WarehouseStocks.Include(s => s.Warehouse).Where(s => s.Warehouse.ID == warehouseID).OrderByDescending(s => s.UpdatedBy).ToList();

                foreach (var c in customerData)
                {                  
                    if (c.ItemType == ItemType.Article)
                    {
                        var article = _dbContext.Articles.Include(s => s.Brand).Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Items).FirstOrDefault(s => s.ID == c.ItemId);
                        var units = article.Items.OrderBy(s => s.Number).ToList();
                                            
						if (article.Category != null && article.Category.ID == categoryID)
						{
							total += c.Qty * units[0].Cost;
						}
                    }
                    else
                    {
                        var subrecipe = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == c.ItemId);
						var units = subrecipe.ItemUnits.OrderBy(s => s.Number).ToList();
                        if (subrecipe.Category != null && subrecipe.Category.ID == categoryID)
                        {
                            total += c.Qty * units[0].Cost;
                        }
                    }
                }
            }

            return Json(new { status = 0, total = Math.Round(total, 2) });
		}


		[HttpPost]
		public JsonResult GetWarehouseStockByBarcode(long warehouseID, string barcode)
		{
            var stocks = _dbContext.WarehouseStocks.Where(s => s.Warehouse.ID == warehouseID).ToList();
			foreach(var stock in stocks)
			{
				if (stock.ItemType == ItemType.Article)
				{
                    var article = _dbContext.Articles.Include(s => s.Brand).Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Items).FirstOrDefault(s => s.ID == stock.ItemId && s.Barcode == barcode);
					if (article != null)
                    {
                        var item = new PhysicalCountViewModel()
                        {
                            ItemID = article.ID,
                            Name = article.Name,
                            Type = "Article",
                            Brand = article.Brand?.Name,
                            Category = article.Category.Name,
                            CategoryID = article.Category.ID,
                            SubCategory = article.SubCategory == null ? "" : article.SubCategory.Name,
                            SubCategoryID = article.SubCategory == null ? 0 : article.SubCategory.ID,
                            Units = article.Items.OrderBy(s => s.Number).ToList(),
                            ItemType = stock.ItemType,
                            StockQty = stock.Qty
                        }; 
						var itemstocks = _dbContext.WarehouseStocks.Include(s => s.Warehouse).Where(s => s.ItemId == article.ID && s.ItemType == ItemType.Article).ToList();

                        var warehouses = new List<Warehouse>();
                        foreach (var itemstock in itemstocks)
                        {
                            warehouses.Add(itemstock.Warehouse);
                        }
                        item.Warehouses = warehouses;
                        return Json(new { status = 0, item });
                    }
                }
				else if (stock.ItemType == ItemType.SubRecipe)
				{
                    var subrecipe = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.ItemUnits).FirstOrDefault(s =>  s.ID == stock.ItemId && s.Barcode == barcode);
					if (subrecipe != null)
                    {
                        var item = new PhysicalCountViewModel()
                        {
                            ItemID = subrecipe.ID,
                            Name = subrecipe.Name,
                            Type = "Sub Recipe",
                            Brand = "",
                            Category = subrecipe.Category.Name,
                            CategoryID = subrecipe.Category.ID,
                            SubCategory = subrecipe.SubCategory == null ? "" : subrecipe.SubCategory.Name,
                            SubCategoryID = subrecipe.SubCategory == null ? 0 : subrecipe.SubCategory.ID,
                            Units = subrecipe.ItemUnits.OrderBy(s => s.Number).ToList(),
                            ItemType = stock.ItemType,
                            StockQty = stock.Qty
                        };

                        var itemstocks = _dbContext.WarehouseStocks.Include(s => s.Warehouse).Where(s => s.ItemId == subrecipe.ID && s.ItemType == ItemType.Article).ToList();

                        var warehouses = new List<Warehouse>();
                        foreach (var itemstock in itemstocks)
                        {
                            warehouses.Add(itemstock.Warehouse);
                        }
						item.Warehouses = warehouses;
                        return Json(new { status = 0, item });
                    }
                }               
				
            }

			return Json(new { status = 1 });
		}


        [HttpPost]
        public JsonResult GetArticleByBarcode(long warehouseID, string barcode)
        {          
            var article = _dbContext.Articles.Include(s => s.Brand).Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Items).FirstOrDefault(s => s.Barcode == barcode);
			if (article == null) return Json(new { status = 2 });
            var stocks = _dbContext.WarehouseStocks.Where(s => s.Warehouse.ID == warehouseID && s.ItemId == article.ID &&  s.ItemType == ItemType.Article).ToList();
			if (stocks == null)
				return Json(new { status = 2 });

			var itemstocks = _dbContext.WarehouseStocks.Include(s=>s.Warehouse).Where(s=>s.ItemId == article.ID && s.ItemType == ItemType.Article).ToList();

			var warehouses = new List<Warehouse>();
			foreach(var itemstock in itemstocks)
			{
				warehouses.Add(itemstock.Warehouse);
			}

            if (article != null)
            {
				var item = new PhysicalCountViewModel()
				{
					ItemID = article.ID,
					Name = article.Name,
					Type = "Article",
					Brand = article.Brand?.Name,
					Category = article.Category.Name,
					CategoryID = article.Category.ID,
					SubCategory = article.SubCategory == null ? "" : article.SubCategory.Name,
					SubCategoryID = article.SubCategory == null ? 0 : article.SubCategory.ID,
					Units = article.Items.OrderBy(s => s.Number).ToList(),
					ScannerUnit = article.ScannerUnit,
                    ItemType = ItemType.Article,
                    StockQty = 0,
					Warehouses = warehouses
                };
                return Json(new { status = 0, item });
            }

            return Json(new { status = 1 });
        }

		[HttpPost]
		public JsonResult GetScannerUpdatedQty(ScannerRequest request)
		{
			var response = new ScannerResponse();

            var stock = _dbContext.WarehouseStocks.Include(s => s.Warehouse).FirstOrDefault(s => s.Warehouse.ID == request.WarehouseID && s.ItemId == request.ArticleID && s.ItemType == request.ItemType);
			if (request.ItemType == ItemType.Article)
			{
                var article = _dbContext.Articles.Include(s => s.Items).FirstOrDefault(s => s.ID == request.ArticleID);

                if (stock != null)
                {
                    //if (stock.Qty < (decimal)0.00001)
                    //{
                    //    response.StockQty = 0;
                    //}
                    //else
                    {
                        response.StockQty = stock.Qty;
                    }

                }

                if (request.Mode == 1) // new qty
                {
                    ItemUnit selectedUnit = null;
                    if (article != null && request.UnitNum >= 1)
                    {
                        selectedUnit = article?.Items.FirstOrDefault(s => s.Number == request.UnitNum);
                        var qty = response.StockQty;
                        for (int i = 1; i <= request.UnitNum; i++)
                        {
                            var unit = article?.Items.FirstOrDefault(s => s.Number == i);
                            qty *= unit.Rate;
                        }

                        //if (qty < (decimal)0.00001)
                        //{
                        //    response.StockQty = 0;
                        //}
                        //else
                        {
                            response.StockQty = qty;
                        }

                    }
                    if (request.Qty == 0)
                    {
                        response.FutureQty = 0;
                    }
                    else
                    {
                        response.FutureQty = request.Qty;
                        if (selectedUnit != null && selectedUnit.PayItem > 0)
                        {
                            response.FutureQty = request.Qty - selectedUnit.PayItem;
                        }
                    }
                    response.Difference = response.FutureQty - response.StockQty;
                }
                else
                {
                    ItemUnit selectedUnit = null;
                    if (article != null && request.UnitNum >= 1)
                    {
                        selectedUnit = article?.Items.FirstOrDefault(s => s.Number == request.UnitNum);
                        var qty = response.StockQty;
                        for (int i = 1; i <= request.UnitNum; i++)
                        {
                            var unit = article?.Items.FirstOrDefault(s => s.Number == i);
                            qty *= unit.Rate;
                        }

                        response.StockQty = qty;
                    }
                    if (request.Qty == 0)
                    {
                        response.FutureQty = 0;
                        response.Difference = 0;
                    }
                    else
                    {
                        response.FutureQty = response.StockQty + request.Qty;
                        if (selectedUnit != null && selectedUnit.PayItem > 0)
                        {
                            response.FutureQty = response.FutureQty - selectedUnit.PayItem;
                        }
                    }
                    response.Difference = response.FutureQty - response.StockQty;
                }

            }
            else if (request.ItemType == ItemType.SubRecipe)
			{
                var subrecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == request.ArticleID);

                if (stock != null)
                {
                    //if (stock.Qty < (decimal)0.00001)
                    //{
                    //    response.StockQty = 0;
                    //}
                    //else
                    {
                        response.StockQty = stock.Qty;
                    }

                }

                if (request.Mode == 1) // new qty
                {
                    ItemUnit selectedUnit = null;
                    if (subrecipe != null && request.UnitNum >= 1)
                    {
                        selectedUnit = subrecipe?.ItemUnits.FirstOrDefault(s => s.Number == request.UnitNum);
                        var qty = response.StockQty;
                        for (int i = 1; i <= request.UnitNum; i++)
                        {
                            var unit = subrecipe?.ItemUnits.FirstOrDefault(s => s.Number == i);
                            qty *= unit.Rate;
                        }

                        //if (qty < (decimal)0.00001)
                        //{
                        //    response.StockQty = 0;
                        //}
                        //else
                        {
                            response.StockQty = qty;
                        }

                    }
                    if (request.Qty == 0)
                    {
                        response.FutureQty = 0;
                        response.Difference = 0;
                    }
                    else
                    {
                        response.FutureQty = request.Qty;
                        if (selectedUnit != null && selectedUnit.PayItem > 0)
                        {
                            response.FutureQty = request.Qty - selectedUnit.PayItem;
                        }
                    }
                    response.Difference = response.FutureQty - response.StockQty;
                }
                else
                {
                    ItemUnit selectedUnit = null;
                    if (subrecipe != null && request.UnitNum >= 1)
                    {
                        selectedUnit = subrecipe?.ItemUnits.FirstOrDefault(s => s.Number == request.UnitNum);
                        var qty = response.StockQty;
                        for (int i = 1; i <= request.UnitNum; i++)
                        {
                            var unit = subrecipe?.ItemUnits.FirstOrDefault(s => s.Number == i);
                            qty *= unit.Rate;
                        }

                        response.StockQty = qty;
                    }
                    if (request.Qty == 0)
                    {
                        response.FutureQty = 0;
                    }
                    else
                    {
                        response.FutureQty = response.StockQty + request.Qty;
                        if (selectedUnit != null && selectedUnit.PayItem > 0)
                        {
                            response.FutureQty = response.FutureQty - selectedUnit.PayItem;
                        }

                        
                    }

                    response.Difference = response.FutureQty - response.StockQty;
                }

            }


            return Json(response);
		}

        [HttpPost]
		public JsonResult UpdateInventory([FromBody] PhysicalCountUpdateModel model)
		{
            if (Math.Abs(model.Difference) < 0.0001m)
            {
                return Json(new { status = 0 });
            }
            var stock = _dbContext.WarehouseStocks.Include(s=>s.Warehouse).FirstOrDefault(s=>s.Warehouse.ID == model.WarehouseID && s.ItemId == model.ItemID && s.ItemType == model.ItemType);
			if (stock != null)
			{
				
				if (stock.ItemType == ItemType.Article)
				{
					var article = _dbContext.Articles.Include(s=>s.Items).FirstOrDefault(s => s.ID == stock.ItemId);
					var units = article.Items.OrderBy(s=>s.Number).ToList();
					var lastNum = units.Last().Number;

					if (model.UnitID != null)
					{
						lastNum = model.UnitID.Value;
					}

					/*if (units[lastNum - 1].PayItem > 0)
					{
						model.Difference = model.Difference - units[lastNum - 1].PayItem;	
					}*/

					UpdateStockOfArticle(article, model.Difference, lastNum, stock.Warehouse, StockChangeReason.PhysicalCheck, stock.ID);
                }
				else
				{
                    var subrecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == stock.ItemId);
                    var units = subrecipe.ItemUnits.OrderBy(s => s.Number).ToList();
                    var lastNum = units.Last().Number;
                    if (model.UnitID != null)
                    {
                        lastNum = model.UnitID.Value;
                    }

                    UpdateStockOfSubRecipe(subrecipe, model.Difference, lastNum, stock.Warehouse, StockChangeReason.PhysicalCheck, stock.ID);
                }

				_dbContext.SaveChanges();
				return Json(new { status = 0 });
			}
			else
			{
				var warehouse = _dbContext.Warehouses.FirstOrDefault(s => s.ID == model.WarehouseID);
                var article = _dbContext.Articles.Include(s => s.Items).FirstOrDefault(s => s.ID == model.ItemID);
                var units = article.Items.OrderBy(s => s.Number).ToList();
                var lastNum = units.Last().Number;
                UpdateStockOfArticle(article, model.Difference, lastNum, warehouse, StockChangeReason.PhysicalCheck, 0);
            }
			return Json(new { status = 1 });
		}


        [HttpPost]
        public JsonResult UpdateInventoryList([FromBody] List<PhysicalCountUpdateModel> models)
        {
			foreach(var model in models)
			{
                var stock = _dbContext.WarehouseStocks.Include(s => s.Warehouse).FirstOrDefault(s => s.Warehouse.ID == model.WarehouseID && s.ItemId == model.ItemID && s.ItemType == model.ItemType);
                if (stock != null)
                {
                    if (Math.Abs(model.Difference) > 0.0001m)
                    {
                        if (stock.ItemType == ItemType.Article)
                        {
                            var article = _dbContext.Articles.Include(s => s.Items).FirstOrDefault(s => s.ID == stock.ItemId);
                            var units = article.Items.OrderBy(s => s.Number).ToList();
                            var lastNum = units.Last().Number;
                            
                            UpdateStockOfArticle(article, model.Difference, lastNum, stock.Warehouse, StockChangeReason.PhysicalCheck, stock.ID);
                        }
                        else
                        {
                            var subrecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == stock.ItemId);
                            var units = subrecipe.ItemUnits.OrderBy(s => s.Number).ToList();
                            var lastNum = units.Last().Number;
                            UpdateStockOfSubRecipe(subrecipe, model.Difference, lastNum, stock.Warehouse, StockChangeReason.PhysicalCheck, stock.ID);
                        }
                        _dbContext.SaveChanges();
                    }
                }
            }

            return Json(new { status = 0 });
        }
		#endregion

		[HttpPost]
		[Authorize(Policy = "Permission.INVENTORY.StockHistory")]
		public JsonResult GetStockHistoryByFilter(StockHistoryRequest request)
		{
			var stocks = _dbContext.WarehouseStockChangeHistory.Include(s=>s.Warehouse).OrderBy(s => s.CreatedDate).ToList();
			var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(request.ToDate))
            {
                try
                {
                    toDate = DateTime.ParseExact(request.ToDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
					//stocks = stocks.Where(s=>s.CreatedDate.Date <= toDate.Date).ToList();
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(request.FromDate))
            {
                try
                {
                    fromDate = DateTime.ParseExact(request.FromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
					//stocks = stocks.Where(s => s.CreatedDate.Date >= fromDate.Date).ToList();
				}
                catch { }
            }
			
			if (request.WarehouseID > 0)
			{
				var warehouse = _dbContext.Warehouses.FirstOrDefault(s => s.ID == request.WarehouseID);
				if (warehouse != null)
				{
					stocks = stocks.Where(s => s.Warehouse == warehouse).ToList();
				}
			}

			if (request.Reason > 0)
			{
				stocks = stocks.Where(s=>s.ReasonType == (StockChangeReason)request.Reason).ToList();
			}

			if (request.ItemType >= 0)
			{
				var type = request.ItemType;
				stocks = stocks.Where(s => s.ItemType == (ItemType)type).ToList();
			}

			if (request.ItemID > 0)
			{
				stocks = stocks.Where(s=>s.ItemId == request.ItemID).ToList();
			}

			var result = new List<StockViewModel>();

			foreach(var stock in stocks)
			{
				var currentstock = _dbContext.WarehouseStocks.FirstOrDefault(s => s.Warehouse == stock.Warehouse && s.ItemId == stock.ItemId && s.ItemType == stock.ItemType);
				var exist = result.FirstOrDefault(s => s.ItemID == stock.ItemId && s.ItemType == stock.ItemType && s.Warehouse == stock.Warehouse.WarehouseName);
				if (exist == null)
				{
					exist = new StockViewModel()
					{
						
						ItemID = stock.ItemId,
						ItemType = stock.ItemType,
						Warehouse = stock.Warehouse.WarehouseName,
						StockQty = currentstock==null ? 0:currentstock.Qty
					};
					result.Add(exist);
                }

				var isVisible = stock.CreatedDate.Date >= fromDate.Date && stock.CreatedDate.Date <= toDate.Date;

				exist.History.Add(new StockHistoryViewModel()
				{
					ItemID = stock.ItemId,
					IsVisible = isVisible,
					ChangeDate = stock.CreatedDate,
					ChangeAt = stock.CreatedDate.ToString("MM/dd/yyyy hh:mm tt"),
					ChangeBy = stock.CreatedBy,
					Reason = stock.ReasonType.ToString(),
					Qty = stock.Qty,
					Unit = stock.UnitNum,
				});

                if (stock.ItemType == ItemType.Article)
				{
					var article = _dbContext.Articles.Include(s=>s.Items).FirstOrDefault(s => s.ID == stock.ItemId);
					if (article != null)
					{
                        exist.ItemName = article.Name;
                        exist.Units = article.Items.OrderBy(s=>s.Number).ToList();
					}
						
				}
				else if (stock.ItemType == ItemType.SubRecipe)
				{
					var subrecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == stock.ItemId);
					if (subrecipe != null)
					{
                        exist.ItemName = subrecipe.Name;
                        exist.Units = subrecipe.ItemUnits.OrderBy(s => s.Number).ToList();
					}
				}
			}

			foreach(var stock in result)
			{
				if (stock.Units == null || stock.Units.Count == 0) continue;
				decimal total = 0;
				decimal totalCost = 0;
				foreach(var item in stock.History)
				{
					var baseQty = ConvertQtyToBase(item.Qty, item.Unit, stock.Units);
					var baseUnit = stock.Units.FirstOrDefault(s => s.Number == 1);
					item.Qty = Math.Round(baseQty, 4);
					item.Unit = 1;
					item.Cost = Math.Abs(Math.Round( baseUnit.Cost * baseQty, 2));
					item.BaseQty = baseQty;
					total += baseQty;
					totalCost += baseUnit.Cost * baseQty;
					item.Balance = total;
				}

				stock.TotalQty = total;
				stock.TotalCost = Math.Round(totalCost, 2);
			}

            return Json(result);
		}

		public JsonResult CreatePurchaseOrder([FromBody] ParPurchaseOrderRequest request)
		{
			if (request.Items != null && request.Items.Count > 0)
			{
				var newPurchaseOrder = new PurchaseOrder();
				var supplier = _dbContext.Suppliers.FirstOrDefault(s => s.ID == request.SupplierID);

				if (supplier != null)
					newPurchaseOrder.Supplier = supplier;

				var warehouse = _dbContext.Warehouses.FirstOrDefault(s => s.ID == request.WarehouseID);
				if (warehouse != null)
					newPurchaseOrder.Warehouse = warehouse;

				newPurchaseOrder.NCF = "";
				newPurchaseOrder.Term = "";
				newPurchaseOrder.OrderTime = DateTime.Now;
				newPurchaseOrder.IsDiscountPercentItems = true;
				newPurchaseOrder.IsDiscountPercent = true;
				newPurchaseOrder.Status = PurchaseOrderStatus.Pending;
				newPurchaseOrder.Description = "";
				newPurchaseOrder.Shipping = 0;
				newPurchaseOrder.Discount = 0;
				newPurchaseOrder.TaxTotal = 0;
				newPurchaseOrder.SubTotal = 0;
				newPurchaseOrder.Total = 0;
				newPurchaseOrder.DiscountAmount = 0;
				newPurchaseOrder.DiscountPercent = 0;
				newPurchaseOrder.DiscountTotal = 0;

				{
					newPurchaseOrder.Items = new List<PurchaseOrderItem>();

					foreach (var item in request.Items)
					{
						var article = _dbContext.Articles.Include(s=>s.Items).FirstOrDefault(s => s.ID == item.ArticleID);
						if (article != null)
						{
							var unit = article.Items.FirstOrDefault(s => s.Number == item.UnitNum);

							var pItem = new PurchaseOrderItem();
							pItem.UnitCost = unit.Cost;
							pItem.Qty = Math.Round(item.Qty, 4);
							pItem.Item = article;
							pItem.UnitNum = item.UnitNum;

							newPurchaseOrder.Items.Add(pItem);
						}

					}
				}

				_dbContext.PurchaseOrders.Add(newPurchaseOrder);
				//try
				//{
					_dbContext.SaveChanges();
					return Json(new { status = 0, id = newPurchaseOrder.ID });
				//}
				//catch { }
			}

			return Json(new { status = 1 });
		}

    }

	public class TaxTempItem
	{
		public string Name { get; set; }
		public string Value { get; set; }
		public decimal Amount { get; set; }
	}

	public class ArticleViewModel
	{
		public long ID { get; set; }
		public string Name { get; set; }
		public long BrandId { get; set; }
		public string CategoryName { get; set; }
		public long CategoryId { get; set; }
		public long SubCategoryId { get; set; } 
		public string SubCategoryName { get; set; }
		public string Tax { get; set; }
		public bool IsActive { get; set; }
		public double Performance { get; set; }
		public List<Supplier> Suppliers { get; set; }
		public string Barcode { get; set; }
		public string Brand { get; set; }
		public double MinimumQty { get; set; }
		public double MaximumQty { get; set; }	
		public string Photo { get; set; }
		public List<ItemUnit> Items { get; set; }
	}

	public class ScannerRequest
	{
		public int WarehouseID { get; set; }
		public decimal Qty { get; set; }
		public int UnitNum { get; set; }
		public long ArticleID { get; set; }
		public int Mode { get; set; }
		public ItemType ItemType { get; set; }
 	}

	public class ScannerResponse
	{
		public decimal StockQty { get; set; }
		public decimal FutureQty { get; set; }
		public decimal Difference { get; set; }
	}

	public class StockHistoryRequest
	{
		public string FromDate { get; set; }
		public string ToDate { get; set; }
		public int WarehouseID { get; set; }
		public int ItemType { get; set; }
		public int Reason { get; set; }
        public long ItemID { get; set; }
    }

	public class StockViewModel
	{
		public string Warehouse { get; set; }
		public long ItemID { get; set; }
		public string ItemName { get; set; }
		public ItemType ItemType { get; set; }
		public decimal TotalQty { get; set; }
		public decimal TotalCost { get; set; }
		public decimal StockQty { get; set; }
	
        public List<ItemUnit> Units { get; set; }
        public List<StockHistoryViewModel> History { get; set; }
		public StockViewModel()
		{
			Units = new List<ItemUnit>();
			History = new List<StockHistoryViewModel>();
		}
	}

	public class StockHistoryViewModel
	{
		public bool IsVisible { get; set; } = true;
		public DateTime ChangeDate { get; set; }
		public string ChangeAt { get; set; }
		public long ItemID { get; set; }
		public int Unit { get; set; }
		public decimal Cost { get; set; }
		public decimal Qty { get; set; }
		public decimal BaseQty { get; set; }
		public string ChangeBy { get; set; }
		public string Reason { get; set; }
		public decimal Balance { get; set; }
	}	

	public class ParPurchaseOrderRequest
	{
		public long SupplierID { get; set; }
		public long WarehouseID { get; set; }
		public List<ParPurchaseOrderModel> Items { get; set; }
	}
	public class ParPurchaseOrderModel
	{
		public long ArticleID { get; set; }
		public int UnitNum { get; set; }
		public decimal Qty { get; set; }
	}


}
