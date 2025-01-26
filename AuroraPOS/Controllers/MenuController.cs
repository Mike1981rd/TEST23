using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.Globalization;
using Newtonsoft.Json;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Barcodes;
using AuroraPOS.Services;
using iText.Kernel.Colors;
using iText.Layout.Borders;
using AuroraPOS.Core;
using AuroraPOS.ModelsJWT;

namespace AuroraPOS.Controllers
{
    [Authorize]
    public class MenuController : BaseController
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        private readonly IUserService _userService;

        public MenuController(ExtendedAppDbContext dbContext, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor context, IUserService userService)
        {
            _dbContext = dbContext._context;
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _userService = userService;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region Group
        //group
        public IActionResult GroupList()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetGroupList()
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
                var customerData = (from s in _dbContext.Groups
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
                    customerData = customerData.Where(m => m.GroupName.ToLower().Contains(all) || m.Note.ToLower().Contains(all));
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
        public JsonResult EditGroup(Group request)
        {
            try
            {
                var existing = _dbContext.Groups.FirstOrDefault(x => x.ID == request.ID);
                if (existing != null)
                {
                    var otherexisting = _dbContext.Groups.FirstOrDefault(x => x.ID != request.ID && x.GroupName == request.GroupName);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    existing.GroupName = request.GroupName;
                    existing.Note = request.Note;
                    existing.IsActive = request.IsActive;
                    _dbContext.SaveChanges();
                    return Json(new { status = 0, id = existing.ID });
                }
                else
                {
                    var otherexisting = _dbContext.Groups.FirstOrDefault(x => x.GroupName == request.GroupName);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    _dbContext.Groups.Add(request);
                    _dbContext.SaveChanges();
                    return Json(new { status = 0, id = request.ID });
                }

            }
            catch { }

            return Json(new { status = 1 });
           
        }

        [HttpPost]
        public JsonResult DeleteGroup(long groupId)
        {
            var existing = _dbContext.Groups.FirstOrDefault(x => x.ID == groupId);
            if (existing != null)
            {
                _dbContext.Groups.Remove(existing);
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }
        #endregion

        #region Category
        // category
        public IActionResult CategoryList()
		{
			return View();
		}

		public IActionResult AddCategory(long categoryId = 0)
		{
			var model = new CategoryViewModel();
			ViewBag.Groups = _dbContext.Groups.ToList();
			ViewBag.Taxes = _dbContext.Taxs.Where(s => s.IsActive).ToList();
			ViewBag.Propinas = _dbContext.Propinas.Where(s => s.IsActive).ToList();
			ViewBag.PrinterChannels = _dbContext.PrinterChannels.Where(s => s.IsActive).ToList();
			ViewBag.Courses = _dbContext.Courses.Where(s => s.IsActive).ToList();

			if (categoryId != 0)
			{
				var category = _dbContext.Categories.Include(s=>s.Questions).Include(s=>s.Group).Include(s=>s.Taxs).Include(s=>s.Propinas).Include(s=>s.PrinterChannels).FirstOrDefault(s => s.ID == categoryId);
				if (category != null)
				{
					model.ID = category.ID;
					model.Name = category.Name;
					model.Plato= category.Plato;
					model.GroupID = category.Group.ID;
                    model.CourseID = category.CourseID;
                    if (category.Taxs != null)
                    {
						model.Taxes = category.Taxs.Select(s => s.ID).ToList();
					}
                    else
                    {
						model.Taxes = new List<long>();
					}
					if (category.Propinas != null)
					{
						model.Propinas = category.Propinas.Select(s => s.ID).ToList();
					}
					else
					{
						model.Propinas = new List<long>();
					}
					if (category.PrinterChannels != null)
                    {
						model.PrinterChannels = category.PrinterChannels.Select(s => s.ID).ToList();
					}
                    else
                    {
                        model.PrinterChannels = new List<long>();
                    }
                    if (category.Questions != null)
                    {
                        model.Questions = category.Questions.Select(s => new CategoryQuestionModel()
                        {
                            ID = s.ID,
                            Name = s.Name,
                            DisplayOrder = s.DisplayOrder,
                            Type = s.IsForced ? "Forced Question" : "Optional Modifier"
                        }).ToList();
                    }
                    
					return View(model);
				}
			}

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult AddCategory([FromForm] CategoryViewModel category)
		{
			if (!ModelState.IsValid)
			{
				ViewBag.Groups = _dbContext.Groups.ToList();
				ViewBag.Taxes = _dbContext.Taxs.Where(s => s.IsActive).ToList();
				ViewBag.Propinas = _dbContext.Propinas.Where(s => s.IsActive).ToList();
				ViewBag.PrinterChannels = _dbContext.PrinterChannels.Where(s => s.IsActive).ToList();
				ViewBag.Courses = _dbContext.Courses.Where(s => s.IsActive).ToList();
				return View(category);
			}
			if (category != null)
			{
				var existing = _dbContext.Categories.Include(s=>s.Taxs).Include(s => s.Propinas).Include(s=>s.PrinterChannels).FirstOrDefault(s => s.ID == category.ID);
				if (existing != null)
				{
					var other = _dbContext.Categories.FirstOrDefault(s => s.ID != category.ID && s.Name == category.Name);
					if (other != null)
					{
						ModelState.AddModelError("Name", "The category name should be unique!");
                        ViewBag.Groups = _dbContext.Groups.ToList();
                        ViewBag.Taxes = _dbContext.Taxs.Where(s => s.IsActive).ToList();
						ViewBag.Propinas = _dbContext.Propinas.Where(s => s.IsActive).ToList();
						ViewBag.PrinterChannels = _dbContext.PrinterChannels.Where(s => s.IsActive).ToList();
						ViewBag.Courses = _dbContext.Courses.Where(s => s.IsActive).ToList();
						return View(category);
					}

					existing.Name = category.Name;
					existing.Plato = category.Plato;
					existing.IsActive= category.IsActive;
                    existing.CourseID = category.CourseID;
					var group = _dbContext.Groups.FirstOrDefault(s => s.ID == category.GroupID);
					if (group != null)
						existing.Group = group;

                    if (category.Taxes != null)
                    {
                        existing.Taxs = new List<Tax>();
                        foreach (var t in category.Taxes)
                        {
                            var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == t);
                            if (tax != null)
                            {
                                existing.Taxs.Add(tax);
                            }
                        }
                    }

					if (category.Propinas != null)
					{
						existing.Propinas = new List<Propina>();
						foreach (var t in category.Propinas)
						{
							var p = _dbContext.Propinas.FirstOrDefault(s => s.ID == t);
							if (p != null)
							{
								existing.Propinas.Add(p);
							}
						}
					}

					if (category.PrinterChannels != null)
                    {
                        existing.PrinterChannels = new List<PrinterChannel>();
                        foreach (var t in category.PrinterChannels)
                        {
                            var tax = _dbContext.PrinterChannels.FirstOrDefault(s => s.ID == t);
                            if (tax != null)
                            {
                                existing.PrinterChannels.Add(tax);
                            }
                        }
                    }
					existing.Questions = new List<Question>();
					//if (category.Questions != null)
					//{
					//	foreach (var question in category.Questions)
					//	{
					//		var q = _dbContext.Questions.FirstOrDefault(s => s.ID == question.ID);
					//		q.DisplayOrder = question.DisplayOrder;
					//		existing.Questions.Add(q);
					//	}
					//}
					_dbContext.SaveChanges();
					
				}
				else
				{
					var other = _dbContext.Categories.FirstOrDefault(s => s.Name == category.Name);
					if (other != null)
					{
						ModelState.AddModelError("Name", "The category name should be unique!");
                        ViewBag.Groups = _dbContext.Groups.ToList();
                        ViewBag.Taxes = _dbContext.Taxs.Where(s => s.IsActive).ToList();
						ViewBag.Propinas = _dbContext.Propinas.Where(s => s.IsActive).ToList();
						ViewBag.PrinterChannels = _dbContext.PrinterChannels.Where(s => s.IsActive).ToList();
						ViewBag.Courses = _dbContext.Courses.Where(s => s.IsActive).ToList();
						return View(category);
					}

					existing = new Category();
					existing.Name = category.Name;
					existing.Plato = category.Plato;
					existing.IsActive= category.IsActive;
                    existing.CourseID = category.CourseID;
					var group = _dbContext.Groups.FirstOrDefault(s => s.ID == category.GroupID);
					if (group != null)
						existing.Group = group;

					
                    if (category.Taxes!= null)
                    {
                        existing.Taxs = new List<Tax>();
                        foreach (var t in category.Taxes)
                        {
                            var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == t);
                            if (tax != null)
                            {
                                existing.Taxs.Add(tax);
                            }
                        }
                    }


					if (category.Propinas != null)
					{
						existing.Propinas = new List<Propina>();
						foreach (var t in category.Propinas)
						{
							var p = _dbContext.Propinas.FirstOrDefault(s => s.ID == t);
							if (p != null)
							{
								existing.Propinas.Add(p);
							}
						}
					}


					if (category.PrinterChannels!= null)
                    {
                        existing.PrinterChannels = new List<PrinterChannel>();
                        foreach (var t in category.PrinterChannels)
                        {
                            var tax = _dbContext.PrinterChannels.FirstOrDefault(s => s.ID == t);
                            if (tax != null)
                            {
                                existing.PrinterChannels.Add(tax);
                            }
                        }
                    }

					existing.Questions = new List<Question>();
					//if (category.Questions != null)
					//{
					//	foreach (var question in category.Questions)
					//	{
					//		var q = _dbContext.Questions.FirstOrDefault(s => s.ID == question.ID);
					//		q.DisplayOrder = question.DisplayOrder;
					//		existing.Questions.Add(q);
					//	}
					//}
					_dbContext.Categories.Add(existing);
					_dbContext.SaveChanges();

                    
				}
                return Json(new { status = 0, id = existing.ID });
			}

			return Json(new { status = 1 });
		}

		[HttpPost]
		public JsonResult EditCategory([FromForm] CategoryViewModel category)
		{
			try
			{				
				if (category != null)
				{
					var existing = _dbContext.Categories.Include(s=>s.Questions).Include(s => s.Taxs).Include(s => s.PrinterChannels).Include(s=>s.Propinas).FirstOrDefault(s => s.ID == category.ID);
					if (existing != null)
					{
						var other = _dbContext.Categories.FirstOrDefault(s => s.ID != category.ID && s.Name == category.Name);
						if (other != null)
						{							
							return Json(new { status = 2 });
						}

						existing.Name = category.Name;
						existing.Plato = category.Plato;
						existing.IsActive = category.IsActive;
                        existing.CourseID = category.CourseID;
						var group = _dbContext.Groups.FirstOrDefault(s => s.ID == category.GroupID);
						if (group != null)
							existing.Group = group;

                        if (existing.Taxs != null)
						    existing.Taxs.Clear();
                        else
                            existing.Taxs = new List<Tax>();
                        if (category.Taxes != null)
                        {
							foreach (var t in category.Taxes)
							{
								var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == t);
								if (tax != null)
								{
									existing.Taxs.Add(tax);
								}
							}
						}
						if (existing.Propinas != null)
							existing.Propinas.Clear();
						else
							existing.Propinas = new List<Propina>();
						if (category.Propinas != null)
						{
							foreach (var t in category.Propinas)
							{
								var tax = _dbContext.Propinas.FirstOrDefault(s => s.ID == t);
								if (tax != null)
								{
									existing.Propinas.Add(tax);
								}
							}
						}

						if (existing.PrinterChannels != null)
						    existing.PrinterChannels.Clear();
                        else
                        {
							existing.PrinterChannels = new List<PrinterChannel>();

						}
                        if (category.PrinterChannels != null)
                        {
							foreach (var t in category.PrinterChannels)
							{
								var tax = _dbContext.PrinterChannels.FirstOrDefault(s => s.ID == t);
								if (tax != null)
								{
									existing.PrinterChannels.Add(tax);
								}
							}
						}
						if (existing.Questions != null)
							existing.Questions.Clear();
						else
						{
							existing.Questions = new List<Question>();
						}
						if (category.Questions != null)
						{
							foreach (var question in category.Questions)
							{
								var q = _dbContext.Questions.FirstOrDefault(s => s.ID == question.ID);
								q.DisplayOrder = question.DisplayOrder;
								existing.Questions.Add(q);
							}
						}
						_dbContext.SaveChanges();

					}
					else
					{
						var other = _dbContext.Categories.FirstOrDefault(s => s.Name == category.Name);
						if (other != null)
						{							
							return Json(new { status = 2});
						}

						existing = new Category();
						existing.Name = category.Name;
						existing.Plato = category.Plato;
						existing.IsActive = category.IsActive;
                        existing.CourseID = category.CourseID;
						var group = _dbContext.Groups.FirstOrDefault(s => s.ID == category.GroupID);
						if (group != null)
							existing.Group = group;

						existing.Taxs = new List<Tax>();
                        if (category.Taxes != null)
                        {
							foreach (var t in category.Taxes)
							{
								var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == t);
								if (tax != null)
								{
									existing.Taxs.Add(tax);
								}
							}

						}
						existing.Propinas = new List<Propina>();
						if (category.Propinas != null)
						{
							foreach (var t in category.Propinas)
							{
								var tax = _dbContext.Propinas.FirstOrDefault(s => s.ID == t);
								if (tax != null)
								{
									existing.Propinas.Add(tax);
								}
							}

						}
						existing.PrinterChannels = new List<PrinterChannel>();
                        if (category.PrinterChannels != null)
                        {
							foreach (var t in category.PrinterChannels)
							{
								var tax = _dbContext.PrinterChannels.FirstOrDefault(s => s.ID == t);
								if (tax != null)
								{
									existing.PrinterChannels.Add(tax);
								}
							}
						}
						existing.Questions = new List<Question>();
						if (category.Questions != null)
						{
							foreach (var question in category.Questions)
							{
								var q = _dbContext.Questions.FirstOrDefault(s => s.ID == question.ID);
								q.DisplayOrder = question.DisplayOrder;
								existing.Questions.Add(q);
							}
						}
						_dbContext.Categories.Add(existing);
						_dbContext.SaveChanges();
					}
					return Json(new { status = 0, id= existing.ID });
				}
			}
			catch { }

			return Json(new { status = 1 });

		}

		[HttpPost]
        public JsonResult GetAllActiveCategoryList()
        {
            var response = new MenuResponse();
            var menu = new MenuCore(_userService, _dbContext, _context);

            try
            {
                response.Valor = menu.GetAllActiveCategoryList();
                response.Success = true;
                return Json(response);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Error = ex.Message;
                return Json(response);
            }
        }

        [HttpPost]
        public JsonResult GetCategory(long categoryID)
        {
			var existing = _dbContext.Categories.Include(s => s.Questions.OrderBy(s=>s.DisplayOrder)).Include(s => s.Taxs).Include(s => s.PrinterChannels).Include(s => s.Propinas).FirstOrDefault(s => s.ID == categoryID);
            return Json(existing);
		}

		[HttpPost]
		public IActionResult GetCategoryList()
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
                var customerData = _dbContext.Categories.Include(s => s.Taxs).Include(s => s.PrinterChannels).Include(s => s.Group).OrderByDescending(s=>s.CreatedDate).Select(s => new CategoryViewModel()
                {
                    ID = s.ID,
                    Name = s.Name,
                    Plato = s.Plato,
                    IsActive = s.IsActive,
                    GroupID = s.Group.ID,
                    GroupName = s.Group.GroupName,
                    CourseID = s.CourseID,
                    Taxes = s.Taxs==null?new List<long>():s.Taxs.Select(x => x.ID).ToList(),
                    Propinas = s.Propinas == null?new List<long>():s.Propinas.Select(x => x.ID).ToList(),
                    PrinterChannels = s.PrinterChannels == null? new List<long>() : s.PrinterChannels.Select(x => x.ID).ToList()

                }).ToList();

                ////Sorting
                //if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                //{
                //	customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                //}
                ////Search  
                
                var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
                var status = Request.Form["columns[1][search][value]"].FirstOrDefault();
                
                if (!string.IsNullOrEmpty(all))
                {
                    all = all.Trim().ToLower();
                    customerData = customerData.Where(m => m.Name.ToLower().Contains(all) || m.GroupName.ToLower().Contains(all)).ToList();
				}
                else
                {
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        searchValue = searchValue.ToLower();
						customerData = customerData.Where(m => m.Name.ToLower().Contains(searchValue) || m.GroupName.ToLower().Contains(searchValue)).ToList();
					}
                }
				if (string.IsNullOrEmpty(status) || status == "1")
				{
					customerData = customerData.Where(s => s.IsActive).ToList();
				}
				else
				{
					customerData = customerData.Where(s => s.IsActive == false).ToList();
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
        public JsonResult GetAllCategories()
        {
            var suppliers = _dbContext.Categories.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();

            return Json(suppliers);
        }

        [HttpPost]
		public JsonResult DeleteCategory(long categoryId)
		{
			var existing = _dbContext.Categories.FirstOrDefault(x => x.ID == categoryId);
			if (existing != null)
			{
                existing.IsDeleted = true;
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}
        #endregion

        #region Sub Category
        // sub category

        public IActionResult SubCategoryList()
        {
            var categories = _dbContext.Categories.Where(s=>s.IsActive).ToList();
            ViewBag.Categories = categories;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetSubCategoryList(long categoryID = 0)
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

                var result = new List<SubCategoryViewModel>();
                // Getting all Customer data  
                var customerData = _dbContext.SubCategories.Include(x => x.Category).ToList();
                foreach(var c in customerData)
                {
                    var model = new SubCategoryViewModel();
                    model.ID = c.ID;
                    model.Name = c.Name;
                    model.Description = c.Description;
                    model.CategoryID = c.Category.ID;
                    model.IsActive = c.IsActive;
                    model.CategoryName = c.Category.Name;
                    result.Add(model);
                }

                if (categoryID != 0)
                {
                    result = result.Where(s=>s.CategoryID == categoryID).ToList();
                }
                //Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    try
                    {
                        result = result.AsQueryable().OrderBy(sortColumn + " " + sortColumnDirection).ToList();
                    }
                    catch { }
                }
                ////Search  
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.Trim().ToLower();
                    result = result.Where(m => m.Name.ToLower().Contains(searchValue) || m.CategoryName.ToLower().Contains(searchValue) || (m.Description != null && m.Description.ToLower().Contains(searchValue))).ToList();
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
        public JsonResult GetAllSubCategories(long categoryID)
        {
            var subCategories = _dbContext.Categories.Include(s=>s.SubCategories).FirstOrDefault(s=>s.ID == categoryID).SubCategories.Where(s=>s.IsActive).ToList();

            return Json(subCategories);
        }
        [HttpPost]
        public JsonResult EditSubCategory(SubCategoryViewModel request)
        {
            try
            {
                var existing = _dbContext.SubCategories.FirstOrDefault(x => x.ID == request.ID);
                if (existing != null)
                {
                    var otherexisting = _dbContext.SubCategories.FirstOrDefault(x => x.ID != request.ID && x.Name == request.Name);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    existing.Name = request.Name;
                    existing.Description = request.Description;
                    existing.IsActive = request.IsActive;

                    var category = _dbContext.Categories.FirstOrDefault(x=>x.ID == request.CategoryID);
                    if (category != null)
                    {
                        existing.Category = category;
                    }

                    _dbContext.SaveChanges();
                    return Json(new { status = 0 });
                }
                else
                {
                    var otherexisting = _dbContext.SubCategories.FirstOrDefault(x => x.Name == request.Name);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    existing = new SubCategory();
                    existing.Name = request.Name;
                    existing.Description = request.Description;
                    existing.IsActive = request.IsActive;

                    var category = _dbContext.Categories.FirstOrDefault(x => x.ID == request.CategoryID);
                    if (category != null)
                    {
                        existing.Category = category;
                    }

                    _dbContext.SubCategories.Add(existing);
                    _dbContext.SaveChanges();
                    return Json(new { status = 0 });
                }

            }
            catch { }

            return Json(new { status = 1 });

        }

        [HttpPost]
        public JsonResult DeleteSubCategory(long subcategoryId)
        {
            var existing = _dbContext.SubCategories.FirstOrDefault(x => x.ID == subcategoryId);
            if (existing != null)
            {
                _dbContext.SubCategories.Remove(existing);
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }
        #endregion

        #region Product
        // product

        public IActionResult ProductList()
        {
            return View();
        }

        public IActionResult AddProduct(long productId = 0)
        {
            ViewBag.PrinterChannels = _dbContext.PrinterChannels.Where(s => s.IsActive).ToList();
            ViewBag.Taxes = _dbContext.Taxs.Where(s => s.IsActive).ToList();
            ViewBag.Propinas = _dbContext.Propinas.Where(s => s.IsActive).ToList();
            ViewBag.Categories = _dbContext.Categories.Where(c => c.IsActive).ToList();
            ViewBag.Courses = _dbContext.Courses.Where(c => c.IsActive).ToList();
            ViewBag.ProductID = 0;
            ViewBag.SubCategory = "";
            if (productId > 0)
            {
                var product = _dbContext.Products.Include(s=>s.SubCategory).FirstOrDefault(x => x.ID == productId);
                if (product != null)
                {
                    ViewBag.ProductId = product.ID;
                    if (product.SubCategory !=null)
                    {
                        ViewBag.SubCategory = "" + product.SubCategory.ID;
                    }
                    
                }
            }


            return View();
        }

        public JsonResult GetProduct(long productID)
        {
            var product = _dbContext.Products.Include(s=>s.Category).Include(s=>s.SubCategory).Include(s=>s.Taxes).Include(s=>s.Propinas).Include(s=>s.PrinterChannels).Include(s=>s.RecipeItems).Include(s=>s.ServingSizes).Include(s=>s.Questions.OrderBy(s=>s.DisplayOrder)).FirstOrDefault(s=>s.ID == productID);

            //Obtenemos la URL de la imagen del archivo            
            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "product", product.ID.ToString() + ".png");
            var request = _context.HttpContext.Request;
            var _baseURL = $"https://{request.Host}";
            if (System.IO.File.Exists(pathFile))
            {
                var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

                product.Photo = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", product.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
            }
            else
            {
                product.Photo = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "paymentmethod", "empty.png");
            }

            return Json(product);
        }

        [HttpPost]
        public JsonResult GetProducts([FromBody] List<long> products)
        {
            var result = new List<Product>();
            foreach(var p in products)
            {
                var product = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions.OrderBy(s => s.DisplayOrder)).FirstOrDefault(s => s.ID == p);
                result.Add(product);
            }
            return Json(result);
        }

		public JsonResult GetMenuProduct(long productID)
		{
			var product = _dbContext.MenuProducts.Include(s=>s.Product).ThenInclude(s => s.ServingSizes).FirstOrDefault(s => s.ID == productID);

			return Json(product.Product);
		}
		public JsonResult GetProductRecipeItem(long itemID)
        {
            var item = _dbContext.ProductItems.FirstOrDefault(s => s.ID == itemID);

            var subRecipeItem = new ProductRecipeItemViewModel(item);

            if (item.Type == ItemType.Article)
            {
                var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == item.ItemID);
                subRecipeItem.Article = article;
            }
            else if (item.Type == ItemType.Product)
            {
                var product = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s=>s.ServingSizes).FirstOrDefault(s => s.ID == item.ItemID);
                subRecipeItem.Product = product;
            }
            else
            {
                var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == item.ItemID);
                subRecipeItem.SubRecipe = subRecipe;
            }


            return Json(subRecipeItem);
        }

        public JsonResult GetProductSavingSizes(long productID)
        {
	        var result = new List<ProductServingSizeViewModel>();

	        var product = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes)
		        .Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems)
		        .Include(s => s.ServingSizes).Include(s => s.Questions).FirstOrDefault(s => s.ID == productID);

	        foreach (var ss in product.ServingSizes)
	        {
		        var ret = new ProductServingSizeViewModel(ss);

		        var items = product.RecipeItems.Where(s => s.ServingSizeID == ss.ServingSizeID);
		        foreach (var item in items)
		        {
			        var subRecipeItem = new ProductRecipeItemViewModel(item);
			        if (item.Type == ItemType.Article)
			        {
				        var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number))
					        .Include(s => s.Brand).FirstOrDefault(s => s.ID == item.ItemID);
				        subRecipeItem.Article = article;
			        }
			        else if (item.Type == ItemType.Product)
			        {
				        var product1 = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory)
					        .Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels)
					        .Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes)
					        .FirstOrDefault(s => s.ID == item.ItemID);
				        subRecipeItem.Product = product1;
			        }
			        else
			        {
				        var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number))
					        .FirstOrDefault(s => s.ID == item.ItemID);
				        subRecipeItem.SubRecipe = subRecipe;
			        }

			        ret.items.Add(subRecipeItem);
		        }

		        result.Add(ret);
	        }



        return Json(result );
        }
        
        private List<Answer> GetPreselectAnswers(long productID, long servingSizeID)
        {
            var result = new List<Answer>();
            var product = _dbContext.Products.Include(s => s.Questions).ThenInclude(s => s.Answers).ThenInclude(s => s.Product).ThenInclude(s => s.Category).Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions).FirstOrDefault(s => s.ID == productID);

            foreach (var objQuestion in product.Questions)
            {
                foreach (var objAnswer in objQuestion.Answers)
                {
                    if (objAnswer.IsPreSelected && objAnswer.ServingSizeID == servingSizeID)
                    {                            
                        result.Add(objAnswer);
                    }
                }
            }

            return result;
        }

        public JsonResult GetProductsQuestions(long productID, string questions = "")
        {
            var result = new List<Product>();

            if (string.IsNullOrEmpty(questions))
            {
	            var product = _dbContext.Products.Include(s=>s.Questions).ThenInclude(s=>s.Answers).ThenInclude(s=>s.Product).ThenInclude(s=>s.Category).Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions).FirstOrDefault(s => s.ID == productID);

	            foreach (var objQuestion in product.Questions)
	            {
		            foreach (var objAnswer in objQuestion.Answers)
		            {
			            if (objAnswer.IsPreSelected)
			            {
				            result.Add(objAnswer.Product);
			            }
		            }
	            }
            }
            else
            {
	            var lstQuestions = questions.Split("|");

	            foreach (var strQuestion in lstQuestions)
	            {
		            var question = _dbContext.Questions.Include(s=>s.Answers).ThenInclude(s=>s.Product).ThenInclude(s=>s.Category).FirstOrDefault(s => s.ID == long.Parse(strQuestion));
		            
			            foreach (var objAnswer in question.Answers)
			            {
				            if (objAnswer.IsPreSelected)
				            {
					            result.Add(objAnswer.Product);
				            }
			            }
		            
	            }
	            
	            
            }

            
            
            
            return Json(result);
        }
        public JsonResult GetProductRecipeItemsOfServingSize(long productID, long id)
        {
            var product = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions).FirstOrDefault(s => s.ID == productID);

            var servingSizeItem = _dbContext.ProductServingSize.FirstOrDefault(s => s.ID == id);

            var items = product.RecipeItems.Where(s => s.ServingSizeID == servingSizeItem.ServingSizeID);

            var result = new List<ProductRecipeItemViewModel>();
            foreach(var item in items)
            {
                var subRecipeItem = new ProductRecipeItemViewModel(item);
                if (item.Type == ItemType.Article)
                {
                    var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == item.ItemID);
                    subRecipeItem.Article = article;
                }
                else if (item.Type == ItemType.Product)
                {
                    var product1 = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == item.ItemID);
                    subRecipeItem.Product = product1;
                }
                else
                {
                    var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == item.ItemID);
                    subRecipeItem.SubRecipe = subRecipe;
                }
                result.Add(subRecipeItem);
            }

            return Json(result);
        }

        [AllowAnonymous]
        public IActionResult UpdateProductCost()
        {
            var products = _dbContext.Products.Include(s=>s.RecipeItems).ToList();
            foreach(var p in  products)
            {
                decimal total = 0;
                foreach(var item in p.RecipeItems)
                {
                    try
                    {
						if (item.Type == ItemType.Article)
						{
							var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == item.ItemID);
							var unit = article.Items.FirstOrDefault(s => s.Number == item.UnitNum);
							var itemPrice = unit.Cost * (decimal)article.Performance / 100.0m;
							total += item.Qty * itemPrice;
						}
						else
						{
							var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == item.ItemID);
							var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == item.UnitNum);
							total += item.Qty * unit.Cost;
						}
					}
                    catch { }
                    
                }

                p.ProductCost = total;
            }
            _dbContext.SaveChanges();

            return View();
        }
        [HttpPost]
        public JsonResult GetProductList()
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
                var result = _dbContext.Products.Include(x => x.Category).Include(s=>s.SubCategory).OrderByDescending(s=>s.CreatedDate).Select(s=>new ProductFiltered
                {
                    ID = s.ID,
                    Name =s.Name,
                    Printer = s.Printer,
                    Photo = s.Photo,
                    CategoryName = s.Category.Name,
                    CategoryId = s.Category.ID,
                    Barcode = s.Barcode,
                    IsActive = s.IsActive,

                });
              
                
                //Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    try
                    {
						result = result.AsQueryable().OrderBy(sortColumn + " " + sortColumnDirection);
					}
                    catch { }
                   
                }

				var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
				var category = Request.Form["columns[1][search][value]"].FirstOrDefault();
				var barcode = Request.Form["columns[2][search][value]"].FirstOrDefault();
				var status = Request.Form["columns[3][search][value]"].FirstOrDefault();
                if (!string.IsNullOrEmpty(barcode))
                {
					result = result.Where(s => "" + s.Barcode == barcode);
				}
                else
                {
					////Search  
					if (!string.IsNullOrEmpty(all))
					{
						all = all.Trim().ToLower();
						result = result.Where(m => m.Name.ToLower().Contains(all) || m.CategoryName.ToLower().Contains(all));
					}

					if (!string.IsNullOrEmpty(category))
					{
						result = result.Where(s => "" + s.CategoryId == category);
					}
					if ( status == "1")
					{
						result = result.Where(s => s.IsActive);
					}
					else
					{
						if (status == "0")
						{
							result = result.Where(s => s.IsActive == false);	
						}
						
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

                //Obtenemos las urls de las imagenes
                var request = _context.HttpContext.Request;
                var _baseURL = $"https://{request.Host}";
                if (data != null && data.Any())
                {
                    foreach (var item in data)
                    {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "product", item.ID.ToString() + ".png");
                        if (System.IO.File.Exists(pathFile))
                        {
                            var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

                            item.Photo = (string?) Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second); ;
                        }
                        else
                        {
                            item.Photo = null; //Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", "empty.png");
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
        public JsonResult EditProduct([FromBody] ProductCreateModel model)
        {
            try
            {
                long productID = model.ID;
                if (model != null)
                {
                    var existing = _dbContext.Products.Include(s => s.Taxes).Include(s=>s.Propinas).Include(s => s.PrinterChannels).Include(s=>s.RecipeItems).Include(s=>s.Questions).Include(s=>s.ServingSizes).FirstOrDefault(s => s.ID == model.ID);
                    if (existing != null)
                    {
                        existing.Name = model.Name;
                        existing.Printer = model.PrinterName;
                        existing.IsActive = model.IsActive;
                        var category = _dbContext.Categories.FirstOrDefault(s => s.ID == model.CategoryId);
                        existing.Category = category;
                        var subCategory = _dbContext.SubCategories.FirstOrDefault(s=>s.ID == model.SubCategoryId);
                        existing.SubCategory = subCategory;
                        existing.ProductCost = model.ProductCost;
                        existing.Barcode = model.Barcode;
                        existing.CourseID = model.CourseID;
                        existing.HasServingSize = model.HasServingSize;
                        existing.Description = model.Description;

                        if (!string.IsNullOrEmpty(model.Barcode))
                        {
							var other = _dbContext.Products.FirstOrDefault(s => s.ID != model.ID && s.Barcode == model.Barcode);
							if (other != null)
							{
								return Json(new { status = 2 });
							}
						}
                      

                        //existing.Photo = model.Photo;


                        //Guardamos la imagen del archivo
                        if (!string.IsNullOrEmpty(model.ImageUpload) && !model.ImageUpload.Contains("http:") && !model.ImageUpload.Contains("https:"))
                        {
                            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "product");

                            if (!Directory.Exists(pathFile))
                            {
                                Directory.CreateDirectory(pathFile);
                            }

                            pathFile = Path.Combine(pathFile, existing.ID.ToString() + ".png");

                            model.ImageUpload = model.ImageUpload.Replace('-', '+');
                            model.ImageUpload = model.ImageUpload.Replace('_', '/');
                            model.ImageUpload = model.ImageUpload.Replace("data:image/jpeg;base64,", string.Empty);
                            model.ImageUpload = model.ImageUpload.Replace("data:image/png;base64,", string.Empty);
                            byte[] imageByteArray = Convert.FromBase64String(model.ImageUpload);
                            System.IO.File.WriteAllBytes(pathFile, imageByteArray);

                        }else if (string.IsNullOrEmpty(model.ImageUpload)) {
                            try { 
                            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "product");                           
                            pathFile = Path.Combine(pathFile, existing.ID.ToString() + ".png");
                            System.IO.File.Delete(pathFile);
                            }
                            catch { }
                        }
                        

                        existing.Price = new List<decimal>() { model.Price1, model.Price2, model.Price3, model.Price4, model.Price5, model.Price6, model.Price7, model.Price8 };
                        if (string.IsNullOrEmpty(model.BackColor))
                        {
                            existing.BackColor = "#000000";
                        }
                        else
                        {
                            existing.BackColor = model.BackColor;
                        }
                        if (string.IsNullOrEmpty(model.TextColor))
                        {
                            existing.TextColor = "#FFFFFF";
                        }
                        else
                        {
                            existing.TextColor = model.TextColor;
                        }
                        if (existing.Taxes != null)
                         
                            existing.Taxes = new List<Tax>();
                        if (model.Taxes != null)
                        {
                            foreach (var t in model.Taxes)
                            {
                                var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == t);
                                if (tax != null)
                                {
                                    existing.Taxes.Add(tax);
                                }
                            }
                        }

                        if (existing.Propinas != null)
                            existing.Propinas = new List<Propina>();
                        if (model.Propinas != null)
                        {
                            foreach (var t in model.Propinas)
                            {
                                var tax = _dbContext.Propinas.FirstOrDefault(s => s.ID == t);
                                if (tax != null)
                                {
                                    existing.Propinas.Add(tax);
                                }
                            }
                        }

                        if (existing.PrinterChannels != null)
                           
                            existing.PrinterChannels = new List<PrinterChannel>();

                      
                        if (model.PrinterChannels != null)
                        {
                            foreach (var t in model.PrinterChannels)
                            {
                                var pchanel = _dbContext.PrinterChannels.FirstOrDefault(s => s.ID == t);
                                if (pchanel != null)
                                {
                                    existing.PrinterChannels.Add(pchanel);
                                }
                            }
                        }

                       
                        {
                            if (existing.RecipeItems!= null)
                            {
                                foreach (var item in existing.RecipeItems)
                                {
                                    _dbContext.ProductItems.Remove(item);
                                }
                                existing.RecipeItems.Clear();
                            }
                            existing.RecipeItems = new List<ProductRecipeItem>();
                            if (model.Recipes != null)
                            {
                                foreach (var recipe in model.Recipes)
                                {
                                    var nrecipe = new ProductRecipeItem();
                                    nrecipe.ItemID = recipe.ItemId;
                                    nrecipe.Qty = recipe.Qty;
                                    nrecipe.UnitNum = recipe.UnitNum;
                                    nrecipe.Type = (ItemType)recipe.Type;
                                    nrecipe.ServingSizeID = recipe.ServingSizeID;

                                    existing.RecipeItems.Add(nrecipe);
                                }
                            }
                           
                        }
                        {
                            if (existing.ServingSizes != null)
                            {
                                foreach (var item in existing.ServingSizes)
                                {
                                    _dbContext.ProductServingSize.Remove(item);
                                }
                                existing.ServingSizes.Clear();
                            }
                            existing.ServingSizes = new List<ProductServingSize>();
                            if (model.ProductServingSizeItems != null)
                            {
                                var servingSizes = _dbContext.ServingSize.ToList();
                                foreach (var recipe in model.ProductServingSizeItems)
                                {
                                    var servingSize = servingSizes.FirstOrDefault(s => s.ID == recipe.ServingSizeID);
                                    var nrecipe = new ProductServingSize();
                                    nrecipe.ServingSizeID = recipe.ServingSizeID;
                                    nrecipe.IsDefault = recipe.IsDefault;
                                    nrecipe.Order = recipe.Order;
                                    nrecipe.ServingSizeName = servingSize?.Name;
                                    nrecipe.Price = new List<decimal>() { recipe.Price1, recipe.Price2, recipe.Price3, recipe.Price4, recipe.Price5, recipe.Price6, recipe.Price7, recipe.Price8 };
                                    nrecipe.Cost = recipe.ProductCost;

                                    existing.ServingSizes.Add(nrecipe);
                                }
                            }

                        }

                        existing.Questions = new List<Question>();
                        if (model.Questions!= null)
                        {
                            foreach(var question in model.Questions)
                            {
                                var q = _dbContext.Questions.FirstOrDefault(s => s.ID == question.ID);
                                q.DisplayOrder = question.DisplayOrder;
                                existing.Questions.Add(q);
                            }
                        }
                        _dbContext.SaveChanges();

                    }
                    else
					{
                        if (!string.IsNullOrEmpty(model.Barcode))
                        {
                            var other = _dbContext.Products.FirstOrDefault(s => s.Barcode == model.Barcode);
                            if (other != null)
                            {
                                return Json(new { status = 2 });
                            }
                        }
                        existing = new Product();
                       
                        existing.Name = model.Name;
                        existing.Printer = model.PrinterName;
                        existing.IsActive = model.IsActive;
                        var category = _dbContext.Categories.FirstOrDefault(s => s.ID == model.CategoryId);
                        existing.Category = category;
                        var subCategory = _dbContext.SubCategories.FirstOrDefault(s => s.ID == model.SubCategoryId);
                        existing.SubCategory = subCategory;
                        existing.ProductCost = model.ProductCost;
						existing.Barcode = model.Barcode;
						//existing.Photo = model.Photo;

                        

                        existing.CourseID = model.CourseID;
                        existing.HasServingSize = model.HasServingSize;
                        existing.Price = new List<decimal>() { model.Price1, model.Price2, model.Price3, model.Price4, model.Price5, model.Price6, model.Price7, model.Price8 };
                        if (string.IsNullOrEmpty(model.BackColor))
                        {
                            existing.BackColor = "#000000";
                        }
                        else
                        {
                            existing.BackColor = model.BackColor;
                        }
                        if (string.IsNullOrEmpty(model.TextColor))
                        {
                            existing.TextColor = "#FFFFFF";
                        }
                        else
                        {
                            existing.TextColor = model.TextColor;
                        }
                        if (existing.Taxes != null)
                            existing.Taxes.Clear();
                        else
                            existing.Taxes = new List<Tax>();
                        if (model.Taxes != null)
                        {
                            foreach (var t in model.Taxes)
                            {
                                var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == t);
                                if (tax != null)
                                {
                                    existing.Taxes.Add(tax);
                                }
                            }
                        }
                        if (existing.Propinas != null)
                            existing.Propinas.Clear();
                        else
                            existing.Propinas = new List<Propina>();
                        if (model.Propinas != null)
                        {
                            foreach (var t in model.Propinas)
                            {
                                var tax = _dbContext.Propinas.FirstOrDefault(s => s.ID == t);
                                if (tax != null)
                                {
                                    existing.Propinas.Add(tax);
                                }
                            }
                        }

                        if (existing.PrinterChannels != null)
                            existing.PrinterChannels.Clear();
                        else
                        {
                            existing.PrinterChannels = new List<PrinterChannel>();

                        }
                        if (model.PrinterChannels != null)
                        {
                            foreach (var t in model.PrinterChannels)
                            {
                                var pchanel = _dbContext.PrinterChannels.FirstOrDefault(s => s.ID == t);
                                if (pchanel != null)
                                {
                                    existing.PrinterChannels.Add(pchanel);
                                }
                            }
                        }

                        if (model.Recipes != null)
                        {
                            if (existing.RecipeItems != null)
                            {
                                foreach (var item in existing.RecipeItems)
                                {
                                    _dbContext.ProductItems.Remove(item);
                                }
                                existing.RecipeItems.Clear();
                            }
                            existing.RecipeItems = new List<ProductRecipeItem>();
                            foreach (var recipe in model.Recipes)
                            {
                                var nrecipe = new ProductRecipeItem();
                                nrecipe.ItemID = recipe.ItemId;
                                nrecipe.Qty = recipe.Qty;
                                nrecipe.UnitNum = recipe.UnitNum;
                                nrecipe.Type = (ItemType)recipe.Type;
                                nrecipe.ServingSizeID = recipe.ServingSizeID;

                                existing.RecipeItems.Add(nrecipe);
                            }
                        }
                        if (model.HasServingSize)
                        {
                            if (existing.ServingSizes != null)
                            {
                                foreach (var item in existing.ServingSizes)
                                {
                                    _dbContext.ProductServingSize.Remove(item);
                                }
                                existing.ServingSizes.Clear();
                            }
                            existing.ServingSizes = new List<ProductServingSize>();
                            if (model.ProductServingSizeItems != null)
                            {
                                var servingSizes = _dbContext.ServingSize.ToList();
                                foreach (var recipe in model.ProductServingSizeItems)
                                {
                                    var servingSize = servingSizes.FirstOrDefault(s => s.ID == recipe.ServingSizeID);
                                    var nrecipe = new ProductServingSize();
                                    nrecipe.ServingSizeID = recipe.ServingSizeID;
                                    nrecipe.IsDefault = recipe.IsDefault;
                                    nrecipe.Order = recipe.Order;
                                    nrecipe.ServingSizeName = servingSize?.Name;
                                    nrecipe.Price = new List<decimal>() { recipe.Price1, recipe.Price2, recipe.Price3, recipe.Price4, recipe.Price5, recipe.Price6, recipe.Price7, recipe.Price8 };
                                    nrecipe.Cost = recipe.ProductCost;

                                    existing.ServingSizes.Add(nrecipe);
                                }
                            }

                        }

                        existing.Questions = new List<Question>();
                        if (model.Questions != null)
                        {
                            foreach (var question in model.Questions)
                            {
                                var q = _dbContext.Questions.FirstOrDefault(s => s.ID == question.ID);
                                q.DisplayOrder = question.DisplayOrder;
                                existing.Questions.Add(q);
                            }
                        }
                        _dbContext.Products.Add(existing);
                        _dbContext.SaveChanges();
                        productID = existing.ID;


                        //Guardamos la imagen del archivo
                        if (!string.IsNullOrEmpty(model.ImageUpload) && !model.ImageUpload.Contains("http:") && !model.ImageUpload.Contains("https:"))
                        {
                            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "product");

                            if (!Directory.Exists(pathFile))
                            {
                                Directory.CreateDirectory(pathFile);
                            }

                            pathFile = Path.Combine(pathFile, existing.ID.ToString() + ".png");

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
                                string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "product");
                                pathFile = Path.Combine(pathFile, existing.ID.ToString() + ".png");
                                System.IO.File.Delete(pathFile);
                            }
                            catch { }
                        }
                    }
                }

                return Json(new { status = 0, id = productID });
            }
            catch (Exception ex){ }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult GenerateBarcode(long productID)
        {
            var product = _dbContext.Products.FirstOrDefault(s => s.ID == productID);
            if (product != null)
            {
                if (string.IsNullOrEmpty(product.Barcode))
                {
                    var barcode = DateTime.Today.ToString("yyMMdd") + "1" + product.ID.ToString().PadLeft(6, '0');

                    product.Barcode = barcode;

                    _dbContext.SaveChanges();

                    return Json(new { status = 0, barcode = barcode });
                }
            }
            return Json(new { status = 1});
        }

        [HttpPost]
        public JsonResult PrintProductRecipe(ProductPrintRecipeRequest request)
        {
            var product = _dbContext.Products.Include(s=>s.RecipeItems).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == request.ProductID);
            if (product != null)
            {
                var items = product.RecipeItems.Where(s=>s.ServingSizeID == request.ServingSizeID && s.Qty > 0);
                var servingSize = product.ServingSizes.FirstOrDefault(s => s.ServingSizeID == request.ServingSizeID);

                var answers = GetPreselectAnswers(product.ID, request.ServingSizeID);

                string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
                var uniqueFileName = "Product_Recipe_" + "_" + product.ID + ".pdf";
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
                    var prodName = product.Name;
                    if (request.ServingSizeID > 0 && servingSize != null)
                    {
                        prodName += "(" + servingSize.ServingSizeName + ")";
                    }

                    Paragraph header = new Paragraph("Product Name : " + prodName)
                                   .SetTextAlignment(TextAlignment.LEFT)
                                   .SetFontSize(15);
                    doc.Add(header);

                 
                    var table = new iText.Layout.Element.Table(new float[] { 2, 1, 1, 1, 1, 1, 1 });
                    table.SetWidth(UnitValue.CreatePercentValue(100));

                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Ingredientes").SetFontSize(12));
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Type").SetFontSize(12));
                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("Brand").SetFontSize(12));
                    var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Candidad").SetFontSize(12));
                    var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Unidad").SetFontSize(12));
                    var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Costo Unidad").SetFontSize(12));
                    var cell7 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Costo Total").SetFontSize(12));

                    table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell7);

                    decimal total = 0;
                    foreach (var d in items)
                    {

                        var subRecipeItem = new ProductRecipeItemViewModel(d);

                        if (d.Type == ItemType.Article)
                        {
                            var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == d.ItemID);
                            if (article == null) continue;
                            var unit = article.Items.FirstOrDefault(s => s.Number == d.UnitNum);

                            var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(article.Name).SetFontSize(11));
                            var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Article").SetFontSize(11));
                            var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(article.Brand.Name).SetFontSize(11));
                            var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + Math.Round(d.Qty, 4)).SetFontSize(11));
                            var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + unit.Name).SetFontSize(11));
                            var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + unit.Cost).SetFontSize(11));
                            var cell27 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + (d.Qty * unit.Cost).ToString("N2", CultureInfo.InvariantCulture)).SetFontSize(11));

                            total += d.Qty * unit.Cost;

                            table.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25).AddCell(cell26).AddCell(cell27);
                        }
                        else if (d.Type == ItemType.Product)
                        {
                            var product1 = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == d.ItemID);
                            var cost = product1.ProductCost;
                            var serving = product1.ServingSizes.FirstOrDefault(s => s.ServingSizeID == d.UnitNum);
                            var prodName1 = product1.Name;
                            if (serving != null)
                            {
                                prodName1 += "(" + serving.ServingSizeName + ")";
                                cost = serving.Cost;
                            }

                            var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(prodName1).SetFontSize(11));
                            var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Product").SetFontSize(11));
                            var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("").SetFontSize(11));
                            var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + Math.Round(d.Qty, 4)).SetFontSize(11));
                            var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("").SetFontSize(11));
                            var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + cost).SetFontSize(11));
                            var cell27 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + (d.Qty * cost).ToString("N2", CultureInfo.InvariantCulture)).SetFontSize(11));

                            total += d.Qty * cost;

                            table.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25).AddCell(cell26).AddCell(cell27);

                        }
                        else
                        {
                            var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == d.ItemID);
                            var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == d.UnitNum);

                            var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(subRecipe.Name).SetFontSize(11));
                            var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Sub Recipe").SetFontSize(11));
                            var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("").SetFontSize(11));
                            var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + Math.Round(d.Qty, 4)).SetFontSize(11));
                            var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + unit.Name).SetFontSize(11));
                            var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + unit.Cost).SetFontSize(11));
                            var cell27 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + (d.Qty * unit.Cost).ToString("N2", CultureInfo.InvariantCulture)).SetFontSize(11));

                            total += d.Qty * unit.Cost;

                            table.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25).AddCell(cell26).AddCell(cell27);

                        }

                    }

                    foreach (var d in answers)
                    {
                        var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(d.Product.Name).SetFontSize(11));
                        var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Product").SetFontSize(11));
                        var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(d.Product.Category.Name).SetFontSize(11));
                        var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + Math.Round(d.Qty, 4)).SetFontSize(11));
                        var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Preselected").SetFontSize(11));
                        var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("").SetFontSize(11));
                        var cell27 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.Product.ProductCost.ToString("N2", CultureInfo.InvariantCulture)).SetFontSize(11));

                        total += d.Product.ProductCost;

                        table.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25).AddCell(cell26).AddCell(cell27);

                    }
                    Paragraph header1 = new Paragraph("Total Costo : $" + total.ToString("N2", CultureInfo.InvariantCulture))
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetFontSize(15);
                    doc.Add(header1);
                    doc.Add(table);
                    doc.Close();
                }

                return Json(new { status = 0, label = uploadsUrl });
            }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult PrintProductLabel (ProductPrintLabelRequest request)
        {
            var product = _dbContext.Products.FirstOrDefault(s => s.ID == request.ProductID);
            if (product != null)
            {
                var label = GenerateProductLabel(product, request);
                return Json(new { status = 0, label = label });
            }
                
            return Json(new { status = 1 });
        }

        private string GenerateProductLabel(Product product, ProductPrintLabelRequest request)
        {
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "ProductLabel_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;

            int width = 3 * 72, height = 3 * 72;

            if (request.Dimension == "2x3")
            {
                height = 2 * 72;
                width = 3 * 72;
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
            }
            else if (request.Dimension == "xx2")
            {
                height = 2 * 72;
                width = 2 * 72;
            }
            if (request.Copies <= 0) request.Copies = 1;
            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf, new iText.Kernel.Geom.PageSize(width, height));
                doc.SetMargins(20, 10, 20, 10);
                for (int i = 0; i < request.Copies; i++) 
                {
                    Paragraph header = new Paragraph(product.Name)
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(15);
                    doc.Add(header);
                    Paragraph packingdatelabel = new Paragraph("Packing Date : " + request.PackingDate.ToString("dd/MM/yyyy"))
                               .SetFontSize(12);
                    doc.Add(packingdatelabel);
                   

                    Barcode128 code128 = new Barcode128(pdf);
                    code128.SetCode(product.Barcode);


                    //Here's how to add barcode to PDF with IText7
                    var barcodeImg = new iText.Layout.Element.Image(code128.CreateFormXObject(pdf));
                    barcodeImg.SetFixedPosition(10, 20);
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
        public JsonResult CopyProduct(long productID)
        {
			var product = _dbContext.Products.Include(s=>s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID ==productID);
            if (product != null)
            {
                var accountItems = _dbContext.AccountItems.Include(s=>s.Account).Where(s=>s.ItemID == productID && s.Target == AccountingTarget.Product).ToList();

				var existing = new Product();

				existing.Name = product.Name + " - Copy";
				existing.Printer = product.Printer;
				existing.IsActive = product.IsActive;
				existing.Category = product.Category;
				existing.SubCategory = product.SubCategory;
				existing.ProductCost = product.ProductCost;
				existing.Barcode = "";
				existing.Photo = product.Photo;
				existing.CourseID = product.CourseID;
				existing.HasServingSize = product.HasServingSize;
				existing.Price = product.Price;
                existing.BackColor = product.BackColor;
				existing.TextColor = product.TextColor;
				existing.Taxes = new List<Tax>();
				if (product.Taxes != null)
				{
					foreach (var t in product.Taxes)
					{
						var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == t.ID);
						if (tax != null)
						{
							existing.Taxes.Add(tax);
						}
					}
				}
				
				existing.Propinas = new List<Propina>();
				if (product.Propinas != null)
				{
					foreach (var t in product.Propinas)
					{
						var tax = _dbContext.Propinas.FirstOrDefault(s => s.ID == t.ID);
						if (tax != null)
						{
							existing.Propinas.Add(tax);
						}
					}
				}
				
				existing.PrinterChannels = new List<PrinterChannel>();
				if (product.PrinterChannels != null)
				{
					foreach (var t in product.PrinterChannels)
					{
						var pchanel = _dbContext.PrinterChannels.FirstOrDefault(s => s.ID == t.ID);
						if (pchanel != null)
						{
							existing.PrinterChannels.Add(pchanel);
						}
					}
				}

				if (product.RecipeItems != null)
				{
					
					existing.RecipeItems = new List<ProductRecipeItem>();
					foreach (var recipe in product.RecipeItems)
					{
						var nrecipe = new ProductRecipeItem();
						nrecipe.ItemID = recipe.ItemID;
						nrecipe.Qty = recipe.Qty;
						nrecipe.UnitNum = recipe.UnitNum;
						nrecipe.Type = (ItemType)recipe.Type;
						nrecipe.ServingSizeID = recipe.ServingSizeID;

						existing.RecipeItems.Add(nrecipe);
					}
				}
				if (product.HasServingSize)
				{
					existing.ServingSizes = new List<ProductServingSize>();
					if (product.ServingSizes != null)
					{
						var servingSizes = _dbContext.ServingSize.ToList();
						foreach (var recipe in product.ServingSizes)
						{
							var servingSize = servingSizes.FirstOrDefault(s => s.ID == recipe.ServingSizeID);
							var nrecipe = new ProductServingSize();
							nrecipe.ServingSizeID = recipe.ServingSizeID;
							nrecipe.IsDefault = recipe.IsDefault;
							nrecipe.Order = recipe.Order;
							nrecipe.ServingSizeName = recipe.ServingSizeName;
							nrecipe.Price = recipe.Price;
							nrecipe.Cost = recipe.Cost;

							existing.ServingSizes.Add(nrecipe);
						}
					}

				}

				existing.Questions = new List<Question>();
				if (product.Questions != null)
				{
					foreach (var question in product.Questions)
					{
						var q = _dbContext.Questions.FirstOrDefault(s => s.ID == question.ID);
						q.DisplayOrder = question.DisplayOrder;
						existing.Questions.Add(q);
					}
				}
				_dbContext.Products.Add(existing);
				_dbContext.SaveChanges();
				
                if (accountItems.Count > 0)
                {
                    foreach(var item in accountItems)
                    {
                        _dbContext.AccountItems.Add(new AccountItem
                        {
                            Order = item.Order,
                            Account = item.Account,
                            ItemID = existing.ID,
                            Target = item.Target,
                        });
                    }

                    _dbContext.SaveChanges();
                }
                
                
                return Json(new { status = 0 });
			}
			return Json(new { status = 1 });
		}

		[HttpPost]
		public JsonResult AddProductSimple([FromBody] ProductCreateModel model)
		{
			try
			{
				if (model != null)
				{
					var existing = new Product();

					existing.Name = model.Name;
					existing.Printer = model.PrinterName;
					existing.IsActive = model.IsActive;
					var category = _dbContext.Categories.FirstOrDefault(s => s.ID == model.CategoryId);
					existing.Category = category;
					existing.ProductCost = model.ProductCost;
					existing.Barcode = model.Barcode;
                    existing.Photo = "";
					model.Price2 = model.Price3 = model.Price4 = model.Price5 = model.Price6 = model.Price7 = model.Price8 = model.Price1;
					existing.Price = new List<decimal>() { model.Price1, model.Price2, model.Price3, model.Price4, model.Price5, model.Price6, model.Price7, model.Price8 };
					existing.BackColor = "#000000";
					existing.TextColor = "#FFFFFF";
					
					existing.Taxes = new List<Tax>();
					if (model.Taxes != null)
					{
						foreach (var t in model.Taxes)
						{
							var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == t);
							if (tax != null)
							{
								existing.Taxes.Add(tax);
							}
						}
					}
                    existing.Propinas = new List<Propina>();
                    if (model.Propinas != null)
                    {
                        foreach (var t in model.Propinas)
                        {
                            var tax = _dbContext.Propinas.FirstOrDefault(s => s.ID == t);
                            if (tax != null)
                            {
                                existing.Propinas.Add(tax);
                            }
                        }
                    }

                    existing.PrinterChannels = new List<PrinterChannel>();
					if (model.PrinterChannels != null)
					{
						foreach (var t in model.PrinterChannels)
						{
							var pchanel = _dbContext.PrinterChannels.FirstOrDefault(s => s.ID == t);
							if (pchanel != null)
							{
								existing.PrinterChannels.Add(pchanel);
							}
						}
					}

					if (model.Recipes != null)
					{
						if (existing.RecipeItems != null)
						{
							foreach (var item in existing.RecipeItems)
							{
								_dbContext.ProductItems.Remove(item);
							}
							existing.RecipeItems.Clear();
						}
						existing.RecipeItems = new List<ProductRecipeItem>();
						foreach (var recipe in model.Recipes)
						{
							var nrecipe = new ProductRecipeItem();
							nrecipe.ItemID = recipe.ItemId;
							nrecipe.Qty = recipe.Qty;
							nrecipe.UnitNum = recipe.UnitNum;
							nrecipe.Type = (ItemType)recipe.Type;
                            nrecipe.ServingSizeID = recipe.ServingSizeID;

							existing.RecipeItems.Add(nrecipe);
						}
					}
					existing.Questions = new List<Question>();
						
					_dbContext.Products.Add(existing);
					_dbContext.SaveChanges();
					
				}

				return Json(new { status = 0 });

			}
			catch (Exception ex) { }

			return Json(new { status = 1 });
		}

        public class ProductFiltered {

            public long ID { get; set; }
            public string Name { get; set; }
            public string Printer { get; set; }
            public string Photo { get; set; }
            public string CategoryName { get; set; }
            public long CategoryId { get; set; }
            public string Barcode { get; set; }
            public bool IsActive { get; set; }            
        }

		#endregion

		#region Question
		[HttpPost]
        public JsonResult GetQuestionList()
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
                var result = _dbContext.Questions.Include(x => x.Answers).Select(s => new QuestionViewModel()
                {
                    ID = s.ID,
                    IsForced = s.IsForced,
                    Name = s.Name,
                    Type = s.IsForced ? "Forced Question" : "Optional Modifier",
                    Answers = s.Answers.Count(),
                    IsActive= s.IsActive,
                }).ToList() ;


                //Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    try
                    {
                        result = result.AsQueryable().OrderBy(sortColumn + " " + sortColumnDirection).ToList();
                    }
                    catch { }
                   
                }
                ////Search  
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.Trim().ToLower();
                    result = result.Where(m => m.Name.ToLower().Contains(searchValue) ).ToList();
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
        public JsonResult EditQuestion([FromBody] Question question)
        {
            try
            {
                var existing = _dbContext.Questions.Include(s => s.Answers).Include(s => s.SmartButtons).FirstOrDefault(s => s.ID == question.ID);
                if (existing != null)
                {
                    existing.Name = question.Name;
                    existing.MaxAnswer = question.MaxAnswer;
                    existing.MinAnswer = question.MinAnswer;
                    existing.FreeChoice = question.FreeChoice;
                    existing.IsActive = question.IsActive;
                    existing.IsForced = question.IsForced;
                    existing.IsAutomatic = question.IsAutomatic;

                    if (existing.Answers != null && existing.Answers.Count > 0)
                    {
                        foreach (var answer in existing.Answers)
                        {
                            _dbContext.Answers.Remove(answer);
                        }
                    }

                    existing.Answers = new List<Answer>();
                    if (question.Answers != null)
                    {
                        foreach (var answer in question.Answers)
                        {
                            var nanswer = new Answer();
                            nanswer.Order = answer.Order;
                            var product = _dbContext.Products.FirstOrDefault(s => s.ID == answer.Product.ID);
                            nanswer.Product = product;
                            nanswer.PriceType = answer.PriceType;
                            nanswer.FixedPrice = answer.FixedPrice;
                            nanswer.RollPrice = answer.RollPrice;
                            nanswer.HasQty = answer.HasQty;
                            nanswer.HasDivisor = answer.HasDivisor;
                            nanswer.IsPreSelected = answer.IsPreSelected;
                            nanswer.ForcedQuestionID = answer.ForcedQuestionID;
                            nanswer.MatchSize = answer.MatchSize;
                            nanswer.Comentario = answer.Comentario;
                            nanswer.ServingSizeID = answer.ServingSizeID;

                            existing.Answers.Add(nanswer);
                        }
                    }

                    existing.SmartButtons = new List<SmartButtonItem>();
                    if (question.SmartButtons != null)
                    {
                        foreach (var btn in question.SmartButtons)
                        {
                            var nbtn = new SmartButtonItem();
                            nbtn.Order = btn.Order;
                            var smartBtn = _dbContext.SmartButtons.FirstOrDefault(s => s.ID == btn.Button.ID);
                            nbtn.Button = smartBtn;
                            existing.SmartButtons.Add(nbtn);
                        }
                    }

                    _dbContext.SaveChanges();
                }
                else
                {
                    existing = new Question();
                    existing.Name = question.Name;
                    existing.MaxAnswer = question.MaxAnswer;
                    existing.MinAnswer = question.MinAnswer;
                    existing.FreeChoice = question.FreeChoice;
                    existing.IsActive = question.IsActive;
                    existing.IsForced = question.IsForced;
                    existing.IsAutomatic = question.IsAutomatic;

                    existing.Answers = new List<Answer>();
                    if (question.Answers != null)
                    {
                        foreach (var answer in question.Answers)
                        {
                            var nanswer = new Answer();
                            nanswer.Order = answer.Order;
                            var product = _dbContext.Products.FirstOrDefault(s => s.ID == answer.Product.ID);
                            nanswer.Product = product;
                            nanswer.PriceType = answer.PriceType;
                            nanswer.FixedPrice = answer.FixedPrice;
                            nanswer.RollPrice = answer.RollPrice;
                            nanswer.HasQty = answer.HasQty;
                            nanswer.HasDivisor = answer.HasDivisor;
                            nanswer.IsPreSelected = answer.IsPreSelected;
                            nanswer.ForcedQuestionID = answer.ForcedQuestionID;
                            nanswer.MatchSize = answer.MatchSize;
                            nanswer.Comentario = answer.Comentario;
                            nanswer.ServingSizeID = answer.ServingSizeID;

                            existing.Answers.Add(nanswer);
                        }
                    }

                    existing.SmartButtons = new List<SmartButtonItem>();
                    if (question.SmartButtons != null)
                    {
                        foreach (var btn in question.SmartButtons)
                        {
                            var nbtn = new SmartButtonItem();
                            nbtn.Order = btn.Order;
                            var smartBtn = _dbContext.SmartButtons.FirstOrDefault(s => s.ID == btn.Button.ID);
                            nbtn.Button = smartBtn;
                            existing.SmartButtons.Add(nbtn);
                        }
                    }

                    _dbContext.Questions.Add(existing);
                    _dbContext.SaveChanges();
                }
                return Json(new { status = 0 });
            }
            catch { }

            return Json(new { status = 1 });
        }

        public JsonResult GetQuestion(long questionId)
        {
            var question = _dbContext.Questions.Include(s=>s.Answers).ThenInclude(s=>s.Product).ThenInclude(s=>s.Questions).Include(s=>s.SmartButtons).ThenInclude(s=>s.Button).FirstOrDefault(s => s.ID == questionId);

            question.Answers = question.Answers.OrderBy(s=>s.Order).ToList();
            question.SmartButtons = question.SmartButtons.OrderBy(s=>s.Order).ToList();            
            
            return Json(question);
        }
        #endregion

        #region Smart Button
        [HttpPost]
        public JsonResult GetSmartButtonList()
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
                var result = _dbContext.SmartButtons.ToList();


                //Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    try
                    {
                        result = result.AsQueryable().OrderBy(sortColumn + " " + sortColumnDirection).ToList();
                    }
                    catch { }
                }
                ////Search  
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.Trim().ToLower();
                    result = result.Where(m => m.Name.ToLower().Contains(searchValue)).ToList();
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
        public JsonResult EditSmartButton([FromForm] SmartButton smartButton)
        {
            try
            {
                var existing = _dbContext.SmartButtons.FirstOrDefault(s => s.ID == smartButton.ID);
                if (existing != null)
                {
                    existing.Name = smartButton.Name;
                    existing.IsApplyPrice = smartButton.IsApplyPrice;
                    existing.IsAfterText = smartButton.IsAfterText;
                }
                else
                {
                    smartButton.IsActive = true;
                    _dbContext.SmartButtons.Add(smartButton);
                }
                _dbContext.SaveChanges();

                return Json(new { status = 0 });
            }
            catch
            {

            }
            return Json(new { status = 1 });
        }
        #endregion

        #region Menu
        public IActionResult MenuEditor()
        {
            return View();
        }
        // menu
        public JsonResult GetMenuList()
        {
            var menus = _dbContext.Menus.Where(s=>!s.IsDeleted).ToList();

            return Json(menus);
        }

        [HttpPost]
        public JsonResult EditMenu([FromForm] Menu menu)
        {
            var existing = _dbContext.Menus.Include(s=>s.Groups).FirstOrDefault(s => s.ID == menu.ID);
            if (existing != null)
            {
                existing.Name = menu.Name;
                existing.Description = menu.Description;
                _dbContext.SaveChanges();
            }
            else
            {
                existing = new Menu();
                existing.Name = menu.Name;
                existing.Description = menu.Description;
                existing.Mode = menu.Mode;
                _dbContext.Menus.Add(existing);
                _dbContext.SaveChanges();
                if (menu.Mode == MenuMode.Kiosk)
                {
                    var group = new MenuGroup();
                    group.Name = "Kiosk";
                    group.BackColor = "#000000";
                    group.TextColor = "#FFFFFF";
                    group.Order = 1;
                    if (menu.Groups == null)
                        existing.Groups = new List<MenuGroup>();
                    existing.Groups.Add(group);
                    _dbContext.SaveChanges();
                }
                
            }

            return Json(new { status = 0, menuId= existing.ID });
        }

        [HttpPost]
        public JsonResult DeleteMenu(long menuID)
        {
            var menu = _dbContext.Menus.FirstOrDefault(s => s.ID == menuID);
            if (menu != null)
            {
                menu.IsDeleted = true;
                _dbContext.SaveChanges();
            }
            return Json(new { status = 0 }) ;
        }
        #endregion

        #region Menu Group
        // group
        public JsonResult GetMenuGroupList(long menuId)
        {
            var menu = _dbContext.Menus.Include(s=>s.Groups).FirstOrDefault(s=>s.ID == menuId);

            var groups = menu.Groups.OrderBy(s =>s.Order).ToList();

            return Json(groups);
        }

        [HttpPost]
        public JsonResult EditMenuGroup([FromQuery]int menuId, [FromForm] MenuGroup group)
        {
            try
            {
                var menu = _dbContext.Menus.Include(s=>s.Groups).FirstOrDefault(s => s.ID == menuId);

                var existing = _dbContext.MenuGroups.FirstOrDefault(s => s.ID == group.ID);
                if (existing != null)
                {
                    existing.Name = group.Name;
                    existing.BackColor = group.BackColor;
                    existing.TextColor = group.TextColor;

                    _dbContext.SaveChanges();
                }
                else
                {
                    existing = new MenuGroup();
                    existing.Name = group.Name;
                    
                    if (string.IsNullOrEmpty(group.BackColor))
                    {
                        existing.BackColor = "#000000";
                    }
                    else
                    {
                        existing.BackColor = group.BackColor;
                    }
                    if (string.IsNullOrEmpty(group.TextColor))
                    {
                        existing.TextColor = "#FFFFFF";
                    }
                    else
                    {
                        existing.TextColor = group.TextColor;
                    }
                    existing.Order = group.Order;
                    menu.Groups.Add(existing);
                    _dbContext.SaveChanges();
                }

                return Json(new { status = 0, groupId = existing.ID });
            }
            catch { }
            return Json(new { status = 1 });
            
        }

        [HttpPost]
        public JsonResult DeleteMenuGroup(long menuId, long groupId)
        {
            var menu = _dbContext.Menus.Include(s=>s.Groups).FirstOrDefault(s => s.ID == menuId);
            if (menu != null)
            {
                var group = menu.Groups.FirstOrDefault(s=>s.ID == groupId);
                if (group != null)
                {
                    menu.Groups.Remove(group);
                    _dbContext.SaveChanges();
                }
            }
            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult UpdateMenuGroupOrders([FromBody]List<MenuGroup> menuGroups)
        {
            foreach(var group in menuGroups)
            {
                var g = _dbContext.MenuGroups.FirstOrDefault(s => s.ID == group.ID);
                g.Order = group.Order;
            }
            _dbContext.SaveChanges();
            return Json(new { status = 0 });
        }
        #endregion

        #region Menu Category
        // category
        public JsonResult GetMenuCategoryList(long groupId)
        {
            var group = _dbContext.MenuGroups.Include(s => s.Categories).FirstOrDefault(s => s.ID == groupId);
            if (group != null)
            {
                var categories = group.Categories.OrderBy(s => s.Order).ToList();
                foreach (var category in categories)
                {

                }

                return Json(categories);
            }
            return Json(new List<MenuCategory>() );
        }

        [HttpPost]
        public JsonResult EditMenuCategory([FromQuery] int groupId,[FromQuery]int menuId, [FromForm] MenuCategory category)
        {
            try
            {
                var menu = _dbContext.Menus.FirstOrDefault(s => s.ID == menuId);
                var group = _dbContext.MenuGroups.Include(s => s.Categories).FirstOrDefault(s => s.ID == groupId);

                var existing = _dbContext.MenuCategories.Include(s=>s.SubCategories).FirstOrDefault(s => s.ID == category.ID);
                if (existing != null)
                {
                    existing.Name = category.Name;
                    existing.BackColor = category.BackColor;
                    existing.TextColor = category.TextColor;

                    _dbContext.SaveChanges();
                }
                else
                {
                    existing = new MenuCategory();
                    existing.Name = category.Name;
                    if (string.IsNullOrEmpty(category.BackColor))
                    {
                        existing.BackColor = "#000000";
                    }
                    else
                    {
                        existing.BackColor = category.BackColor;
                    }
                    if (string.IsNullOrEmpty(category.TextColor))
                    {
                        existing.TextColor = "#FFFFFF";
                    }
                    else
                    {
                        existing.TextColor = category.TextColor;
                    }
                    
                    existing.Order = category.Order;
                    group.Categories.Add(existing);
                    _dbContext.SaveChanges();

                    if (menu.Mode == MenuMode.Kiosk)
                    {
                        existing.SubCategories = new List<MenuSubCategory>() { 
                            new MenuSubCategory() {
                                Name="Kiosk",
                                Order = 1,
                                BackColor = "#000000",
                                TextColor = "#FFFFFF"
                            } 
                        };
                        _dbContext.SaveChanges();
                    }
                    else
                    {
						existing.SubCategories = new List<MenuSubCategory>() {
							new MenuSubCategory() {
								Name=category.Name,
								Order = 1,
								BackColor = "#000000",
								TextColor = "#FFFFFF",
							}
						};
						_dbContext.SaveChanges();
					}
                }

                return Json(new { status = 0, categoryId = existing.ID });
            }
            catch { }
            return Json(new { status = 1 });

        }

        [HttpPost]
        public JsonResult DeleteMenuCategory(long groupId, long categoryId)
        {
            var group = _dbContext.MenuGroups.Include(s => s.Categories).FirstOrDefault(s => s.ID == groupId);
            if (group != null)
            {
                var category = group.Categories.FirstOrDefault(s => s.ID == categoryId);
                if (category != null)
                {
                    group.Categories.Remove(category);
                    _dbContext.SaveChanges();
                }
            }
            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult UpdateMenuCategoryOrders([FromBody] List<MenuCategory> menuCategoris)
        {
            foreach (var category in menuCategoris)
            {
                var g = _dbContext.MenuCategories.FirstOrDefault(s => s.ID == category.ID);
                g.Order = category.Order;
            }
            _dbContext.SaveChanges();
            return Json(new { status = 0 });
        }
        #endregion

        #region Menu Sub Category
        // sub category
        public JsonResult GetMenuSubCategoryList(long categoryId)
        {
            var group = _dbContext.MenuCategories.Include(s => s.SubCategories).FirstOrDefault(s => s.ID == categoryId);

            var subcategories = group.SubCategories.OrderBy(s => s.Order).ToList();

            return Json(subcategories);
        }

        [HttpPost]
        public JsonResult EditMenuSubCategory([FromQuery] int categoryId, [FromForm] MenuSubCategory subCategory)
        {
            try
            {
                var category = _dbContext.MenuCategories.Include(s => s.SubCategories).FirstOrDefault(s => s.ID == categoryId);

                var existing = _dbContext.MenuSubCategoris.FirstOrDefault(s => s.ID == subCategory.ID);
                if (existing != null)
                {
                    existing.Name = subCategory.Name;
                    existing.BackColor = subCategory.BackColor;
                    existing.TextColor = subCategory.TextColor;

                    _dbContext.SaveChanges();
                }
                else
                {
                    existing = new MenuSubCategory();
                    existing.Name = subCategory.Name;
                    if (string.IsNullOrEmpty(subCategory.BackColor))
                    {
                        existing.BackColor = "#000000";
                    }
                    else
                    {
                        existing.BackColor = subCategory.BackColor;
                    }
                    if (string.IsNullOrEmpty(subCategory.TextColor))
                    {
                        existing.TextColor = "#FFFFFF";
                    }
                    else
                    {
                        existing.TextColor = subCategory.TextColor;
                    }
                    existing.Order = subCategory.Order;
                    category.SubCategories.Add(existing);
                    _dbContext.SaveChanges();
                }

                return Json(new { status = 0, subCategoryId = existing.ID });
            }
            catch { }
            return Json(new { status = 1 });

        }

        [HttpPost]
        public JsonResult DeleteMenuSubCategory(long categoryId, long subCategoryId)
        {
            var category = _dbContext.MenuCategories.Include(s => s.SubCategories).FirstOrDefault(s => s.ID == categoryId);
            if (category != null)
            {
                var subcategory = category.SubCategories.FirstOrDefault(s => s.ID == subCategoryId);
                if (subcategory != null)
                {
                    category.SubCategories.Remove(subcategory);
                    _dbContext.SaveChanges();
                }
            }
            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult UpdateMenuSubCategoryOrders([FromBody] List<MenuSubCategory> menuSubCategoris)
        {
            foreach (var category in menuSubCategoris)
            {
                var g = _dbContext.MenuSubCategoris.FirstOrDefault(s => s.ID == category.ID);
                g.Order = category.Order;
            }
            _dbContext.SaveChanges();
            return Json(new { status = 0 });
        }
        #endregion

        #region Menu Product
        // menu product 
        public JsonResult GetMenuProductList(long subCategoryId)
        {
            var group = _dbContext.MenuSubCategoris.Include(s => s.Products).ThenInclude(s=>s.Product).FirstOrDefault(s => s.ID == subCategoryId);

            var products = group.Products.OrderBy(s => s.Order).ToList();

            return Json(products);
        }

        [HttpPost]
        public JsonResult EditMenuProduct([FromQuery] long groupId, [FromQuery] long categoryId, [FromQuery] long subCategoryId, [FromQuery] long productId, [FromForm] MenuProduct product)
        {
            try
            {
                var subcategory = _dbContext.MenuSubCategoris.Include(s => s.Products).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == subCategoryId);
                var existing = subcategory.Products.FirstOrDefault(s => s.Product.ID == productId);
                if (existing != null)
                {
                    existing.GroupID = groupId;
                    existing.CategoryID = categoryId;
                    existing.SubCategoryID = subCategoryId;
                    return Json(new { status = 0, productId = existing.ID });
                }
                else
                {
                    var nproduct = _dbContext.Products.FirstOrDefault(s => s.ID == productId);
                    existing = new MenuProduct();
                    existing.Product = nproduct;
                    existing.GroupID = groupId;
                    existing.CategoryID = categoryId;
                    existing.SubCategoryID = subCategoryId;
                    existing.Order = product.Order;
                    if (subcategory.Products == null) subcategory.Products = new List<MenuProduct>();
                    subcategory.Products.Add(existing);
                    _dbContext.SaveChanges();
                }

                return Json(new { status = 0, productId = existing.ID });
            }
            catch { }
            return Json(new { status = 1 });

        }

        [HttpPost]
        public JsonResult AddMenuProductFromCategory([FromQuery] long groupId, [FromQuery] long categoryId, [FromQuery] long subCategoryId, [FromQuery] long productCategoryId, [FromForm] MenuProduct product)
        {
            try
            {
                var category = _dbContext.Categories.FirstOrDefault(s => s.ID == productCategoryId);
                var products = _dbContext.Products.Where(s => s.Category == category && s.IsActive).ToList();
                var subcategory = _dbContext.MenuSubCategoris.Include(s => s.Products).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == subCategoryId);
                var index = product.Order;
                foreach(var p in products)
                {
                    var existing = subcategory.Products.FirstOrDefault(s => s.Product.ID == p.ID);
                    if (existing == null)
                    {                        
                        existing = new MenuProduct();
                        existing.Product = p;
                        existing.GroupID = groupId;
                        existing.CategoryID = categoryId;
                        existing.SubCategoryID = subCategoryId;
                        existing.Order = index;
                        if (subcategory.Products == null) subcategory.Products = new List<MenuProduct>();
                        subcategory.Products.Add(existing);
                        index++;
                    }
                }
                _dbContext.SaveChanges();

                return Json(new { status = 0 });
            }
            catch { }
            return Json(new { status = 1 });

        }

        [HttpPost]
        public JsonResult DeleteMenuProduct(long subCategoryId, long productId)
        {
            var category = _dbContext.MenuSubCategoris.Include(s => s.Products).FirstOrDefault(s => s.ID == subCategoryId);
            if (category != null)
            {
                var product = category.Products.FirstOrDefault(s => s.ID == productId);
                if (product != null)
                {
                    category.Products.Remove(product);
                    _dbContext.SaveChanges();
                }
            }
            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult UpdateMenuProductOrders([FromQuery] long groupId, [FromQuery] long categoryId, [FromQuery] long subCategoryId, [FromBody] List<MenuProduct> menuProducts)
        {
            foreach (var product in menuProducts)
            {
                var g = _dbContext.MenuProducts.FirstOrDefault(s => s.ID == product.ID);
                g.GroupID = groupId;
                g.CategoryID = categoryId;
                g.SubCategoryID = subCategoryId;
                g.Order = product.Order;
            }
            _dbContext.SaveChanges();
            return Json(new { status = 0 });
        }
        public IActionResult UpdateDB()
        {
            UpdateMeneProduct();
            return View();
        }
        private void UpdateMeneProduct()
        {
            var menus = _dbContext.Menus.Include(s=>s.Groups).ThenInclude(s=>s.Categories).ThenInclude(s=>s.SubCategories).ThenInclude(s=>s.Products).ToList();
            foreach(var menu in menus)
            {
                foreach(var group in menu.Groups)
                {
                    if (group.Categories != null)
                    {
                        foreach (var categoory in group.Categories)
                        {
                            if (categoory.SubCategories != null)
                                foreach (var sub in categoory.SubCategories)
                                {
                                    if (sub.Products != null)
                                        foreach (var product in sub.Products)
                                        {
                                            product.GroupID = group.ID;
                                            product.CategoryID = categoory.ID;
                                            product.SubCategoryID = sub.ID;
                                        }
                                }
                        }
                    }
                  
                }
            }
            _dbContext.SaveChanges();
        }

        #endregion

        #region Static product
        public JsonResult GetMenuStaticProductList(long menuId)
        {
            var products = _dbContext.MenuProducts.Include(s => s.Product).Where(s => s.MenuID == menuId).OrderBy(s => s.Order).ToList();

            return Json(products);
        }

        [HttpPost]
        public JsonResult DeleteMenuStaticProduct(long productId)
        {
            var product = _dbContext.MenuProducts.FirstOrDefault(s => s.ID == productId);
            if (product != null)
            {
                _dbContext.MenuProducts.Remove(product);
                _dbContext.SaveChanges();
            }
            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult EditMenuStaticProduct([FromQuery] long menuId, long productId, [FromForm] MenuProduct product)
        {
            try
            {

                var nproduct = _dbContext.Products.FirstOrDefault(s => s.ID == productId);
                var existing = new MenuProduct();
                existing.Product = nproduct;
                existing.GroupID = 0;
                existing.CategoryID = 0;
                existing.SubCategoryID = 0;
                existing.MenuID = menuId;
                existing.Order = product.Order;
                _dbContext.MenuProducts.Add(existing);

                _dbContext.SaveChanges();

                return Json(new { status = 0, productId = existing.ID });
            }
            catch { }
            return Json(new { status = 1 });

        }

        [HttpPost]
        public JsonResult UpdateMenuStaticProductOrders([FromBody] List<MenuProduct> menuProducts)
        {
            foreach (var product in menuProducts)
            {
                var g = _dbContext.MenuProducts.FirstOrDefault(s => s.ID == product.ID);
                g.Order = product.Order;
            }
            _dbContext.SaveChanges();
            return Json(new { status = 0 });
        }

        #endregion
        #region Map Table Editor
        public IActionResult MapTableEditor()
        {
            var imgdir = Path.Combine( _hostingEnvironment.WebRootPath, "vendor/img", "areas");
            var getFiles = Directory.GetFiles(imgdir);
            var backgroundGalleries = new List<string>();
            foreach (var file in getFiles)
            {
                backgroundGalleries.Add("/vendor/img/areas/" + Path.GetFileName(file));
            }

            var objimgdir = Path.Combine(_hostingEnvironment.WebRootPath, "vendor/img", "areaobjects");
            var getObjFiles = Directory.GetFiles(objimgdir);
            var objectGalleries = new List<string>();
            foreach (var file in getObjFiles)
            {
                objectGalleries.Add("/vendor/img/areaobjects/" + Path.GetFileName(file));
            }

            ViewBag.BackgroundImages = backgroundGalleries;
            ViewBag.ObjectImages = objectGalleries;
            return View();
        }
        public JsonResult GetArea(long areaID)
        {
            var area = _dbContext.Areas.Include(s=>s.AreaObjects).FirstOrDefault(s => s.ID == areaID);

            //Obtenemos la URL de la imagen del archivo       
            
            //string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "area", area.ID.ToString() + ".png");
            string pathFile = Environment.CurrentDirectory + "/wwwroot" + "/localfiles/" + Request.Cookies["db"] + "/area/" + area.ID.ToString() + ".png";
            var request = _context.HttpContext.Request;
            var _baseURL = $"https://{request.Host}";
            if (System.IO.File.Exists(pathFile))
            {
                var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

                area.BackImage = _baseURL + "/localfiles/" + Request.Cookies["db"] + "/area/" + area.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second;
            }
            else
            {
                area.BackImage = _baseURL + "/localfiles/" + Request.Cookies["db"] + "/area/" + "empty.png";
            }

            return Json(area);
        }

        public JsonResult GetAreaList()
        {
            var areas = _dbContext.Areas.Where(s => !s.IsDeleted).ToList();

            //Obtenemos las urls de las imagenes
            var request = _context.HttpContext.Request;
            var _baseURL = $"https://{request.Host}";
            if (areas != null && areas.Any())
            {
                foreach (var item in areas)
                {
                    string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "area", item.ID.ToString() + ".png");
                    if (System.IO.File.Exists(pathFile))
                    {
                        var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

                        item.BackImage = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "area", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second); ;
                    }
                    else
                    {
                        item.BackImage = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "area", "empty.png");
                    }
                }
            }

            return Json(areas);
        }

		[HttpPost]
		public IActionResult GetAreas()
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
				var customerData = _dbContext.Areas.Where(s => !s.IsDeleted).Select(s => new
				{
					s.ID,
					Name = s.Name,
					s.IsActive,
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
        public JsonResult EditArea([FromForm] AreaCreateModel area)
        {
            var existing = _dbContext.Areas.FirstOrDefault(s => s.ID == area.ID);
            if (existing != null)
            {
                existing.Name = area.Name;
                existing.Width = area.Width;
                existing.Height = area.Height;
                existing.BackColor= area.BackColor;
                //existing.BackImage  = area.BackImage;

                //Guardamos la imagen del archivo
                if (!string.IsNullOrEmpty(area.ImageUpload) && !area.ImageUpload.Contains("http:") && !area.ImageUpload.Contains("https:"))
                {
                    string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "area");

                    if (!Directory.Exists(pathFile))
                    {
                        Directory.CreateDirectory(pathFile);
                    }

                    pathFile = Path.Combine(pathFile, area.ID.ToString() + ".png");

                    area.ImageUpload = area.ImageUpload.Replace('-', '+');
                    area.ImageUpload = area.ImageUpload.Replace('_', '/');
                    area.ImageUpload = area.ImageUpload.Replace("data:image/jpeg;base64,", string.Empty);
                    area.ImageUpload = area.ImageUpload.Replace("data:image/png;base64,", string.Empty);
                    byte[] imageByteArray = Convert.FromBase64String(area.ImageUpload);
                    System.IO.File.WriteAllBytes(pathFile, imageByteArray);

                }
                else if (string.IsNullOrEmpty(area.ImageUpload))
                {
                    try
                    {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "area");
                        pathFile = Path.Combine(pathFile, area.ID.ToString() + ".png");
                        System.IO.File.Delete(pathFile);
                    }
                    catch { }
                }

                _dbContext.SaveChanges();
            }
            else
            {
                existing = new Area();
                existing.Name = area.Name;
                existing.Width = area.Width;
                existing.Height = area.Height;
                existing.BackColor = area.BackColor;
                //existing.BackImage = area.BackImage;

                

                _dbContext.Areas.Add(existing);
                _dbContext.SaveChanges();

                //Guardamos la imagen del archivo
                if (!string.IsNullOrEmpty(area.ImageUpload) && !area.ImageUpload.Contains("http:") && !area.ImageUpload.Contains("https:"))
                {
                    string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "area");

                    if (!Directory.Exists(pathFile))
                    {
                        Directory.CreateDirectory(pathFile);
                    }

                    pathFile = Path.Combine(pathFile, existing.ID.ToString() + ".png");

                    area.ImageUpload = area.ImageUpload.Replace('-', '+');
                    area.ImageUpload = area.ImageUpload.Replace('_', '/');
                    area.ImageUpload = area.ImageUpload.Replace("data:image/jpeg;base64,", string.Empty);
                    area.ImageUpload = area.ImageUpload.Replace("data:image/png;base64,", string.Empty);
                    byte[] imageByteArray = Convert.FromBase64String(area.ImageUpload);
                    System.IO.File.WriteAllBytes(pathFile, imageByteArray);

                }
                else if (string.IsNullOrEmpty(area.ImageUpload))
                {
                    try
                    {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "area");
                        pathFile = Path.Combine(pathFile, existing.ID.ToString() + ".png");
                        System.IO.File.Delete(pathFile);
                    }
                    catch { }
                }
            }

            return Json(new { status = 0, areaId = existing.ID });
        }


        [HttpPost]
        public JsonResult EditAreaObject([FromQuery]long areaID, [FromBody] AreaObject areaObj)
        {
            var existing = _dbContext.Areas.FirstOrDefault(s => s.ID == areaID);
            bool isNew = false;
            if (existing != null)
            {
                if (existing.Width <= areaObj.Width || existing.Height <= areaObj.Height || areaObj.PositionX > existing.Width - areaObj.Width || areaObj.PositionY > existing.Height - areaObj.Height) {
                    return Json(new { status = 2 });
                }



                var existingObj = _dbContext.AreaObjects.FirstOrDefault(s => s.ID == areaObj.ID);
                if (existingObj != null)
                {
                    existingObj.Name = areaObj.Name;
                    existingObj.Width = areaObj.Width;
                    existingObj.Height = areaObj.Height;
                    existingObj.PositionX = areaObj.PositionX;
                    existingObj.PositionY = areaObj.PositionY;
                    existingObj.BackColor = areaObj.BackColor;  
                    existingObj.TextColor = areaObj.TextColor;  
                    //existingObj.BackImage = areaObj.BackImage;
                    existingObj.Shape = areaObj.Shape;
                    existingObj.ObjectType= areaObj.ObjectType;
                    existingObj.Radius= areaObj.Radius;

                    //Guardamos la imagen del archivo
                    if (!string.IsNullOrEmpty(areaObj.ImageUpload) && !areaObj.ImageUpload.Contains("http:") && !areaObj.ImageUpload.Contains("https:"))
                    {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "areaobject");

                        if (!Directory.Exists(pathFile))
                        {
                            Directory.CreateDirectory(pathFile);
                        }

                        pathFile = Path.Combine(pathFile, areaObj.ID.ToString() + ".png");

                        areaObj.ImageUpload = areaObj.ImageUpload.Replace('-', '+');
                        areaObj.ImageUpload = areaObj.ImageUpload.Replace('_', '/');
                        areaObj.ImageUpload = areaObj.ImageUpload.Replace("data:image/jpeg;base64,", string.Empty);
                        areaObj.ImageUpload = areaObj.ImageUpload.Replace("data:image/png;base64,", string.Empty);
                        byte[] imageByteArray = Convert.FromBase64String(areaObj.ImageUpload);
                        System.IO.File.WriteAllBytes(pathFile, imageByteArray);

                    }
                    else if (string.IsNullOrEmpty(areaObj.ImageUpload))
                    {
                        try
                        {
                            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "areaobject");
                            pathFile = Path.Combine(pathFile, areaObj.ID.ToString() + ".png");
                            System.IO.File.Delete(pathFile);
                        }
                        catch { }
                    }

                    if (existingObj.ObjectType == AreaObjectType.Table)
                    {
                        existingObj.SeatCount = areaObj.SeatCount;
                    }
                   
                }
                else
                {
                    isNew = true;

                    existingObj = new AreaObject();
                    existingObj.Name = areaObj.Name;
                    existingObj.Width = areaObj.Width;
                    existingObj.Height = areaObj.Height;
                    existingObj.PositionX = areaObj.PositionX;
                    existingObj.PositionY = areaObj.PositionY;
                    existingObj.BackColor = areaObj.BackColor;
                    existingObj.TextColor = areaObj.TextColor;
                    //existingObj.BackImage = areaObj.BackImage;
                    existingObj.Shape = areaObj.Shape;
                    existingObj.ObjectType = areaObj.ObjectType;
                    existingObj.Radius = areaObj.Radius;

                    

                    if (existingObj.ObjectType == AreaObjectType.Table)
                    {
                        existingObj.SeatCount = areaObj.SeatCount;
                    }

                    if (existing.AreaObjects == null) existing.AreaObjects = new List<AreaObject>();

                    existing.AreaObjects.Add(areaObj);
                }

                _dbContext.SaveChanges();

                if (isNew) {
                    //Guardamos la imagen del archivo
                    if (!string.IsNullOrEmpty(areaObj.ImageUpload) && !areaObj.ImageUpload.Contains("http:") && !areaObj.ImageUpload.Contains("https:"))
                    {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "areaobject");

                        if (!Directory.Exists(pathFile))
                        {
                            Directory.CreateDirectory(pathFile);
                        }

                        pathFile = Path.Combine(pathFile, areaObj.ID.ToString() + ".png");

                        areaObj.ImageUpload = areaObj.ImageUpload.Replace('-', '+');
                        areaObj.ImageUpload = areaObj.ImageUpload.Replace('_', '/');
                        areaObj.ImageUpload = areaObj.ImageUpload.Replace("data:image/jpeg;base64,", string.Empty);
                        areaObj.ImageUpload = areaObj.ImageUpload.Replace("data:image/png;base64,", string.Empty);
                        byte[] imageByteArray = Convert.FromBase64String(areaObj.ImageUpload);
                        System.IO.File.WriteAllBytes(pathFile, imageByteArray);

                    }
                    else if (string.IsNullOrEmpty(areaObj.ImageUpload))
                    {
                        try
                        {
                            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "areaobject");
                            pathFile = Path.Combine(pathFile, areaObj.ID.ToString() + ".png");
                            System.IO.File.Delete(pathFile);
                        }
                        catch { }
                    }
                }

                

                return Json(new { status = 0, areaObjectId = areaObj.ID });
            }


            return Json(new { status = 1 });
        }

        public JsonResult GetAreaObjectsInArea(long areaID)
        {
            var area = _dbContext.Areas.Include(s => s.AreaObjects.Where(s=>!s.IsDeleted)).FirstOrDefault(s => s.ID == areaID);

            //Obtenemos las urls de las imagenes
            var request = _context.HttpContext.Request;
            var _baseURL = $"https://{request.Host}";
            if (area.AreaObjects != null && area.AreaObjects.Any())
            {
                foreach (var item in area.AreaObjects)
                {
                    //string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "areaobject", item.ID.ToString() + ".png");
                    string pathFile = Environment.CurrentDirectory + "/wwwroot" + "/localfiles/" + Request.Cookies["db"] + "/areaobject/" + item.ID.ToString() + ".png";
                    if (System.IO.File.Exists(pathFile))
                    {
                        var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);                        

                        //item.BackImage = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second); ;
                        item.BackImage = _baseURL + "/localfiles/" + Request.Cookies["db"] + "/areaobject/" + item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second ;
                    }
                    else
                    {
                        item.BackImage = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", "empty.png");
                    }
                }
            }

            return Json(area.AreaObjects);
        }

        public JsonResult GetAreaObject(long objectID)
        {
            var areaObj = _dbContext.AreaObjects.FirstOrDefault(s => s.ID == objectID);

            //Obtenemos la URL de la imagen del archivo            
            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "areaobject", areaObj.ID.ToString() + ".png");
            var request = _context.HttpContext.Request;
            var _baseURL = $"https://{request.Host}";
            if (System.IO.File.Exists(pathFile))
            {
                areaObj.BackImage = pathFile;

                var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

                areaObj.BackImage = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", areaObj.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
            }
            else
            {
                areaObj.BackImage = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", "empty.png");
            }

            return Json(areaObj);
        }

        [HttpPost]
        public JsonResult UpdateAreaObjectSize([FromBody] AreaObjectSizeModel model)
        {
            var area = _dbContext.Areas.FirstOrDefault(s=>s.ID== model.AreaID);

            if (model.Height<= 0 || model.Width <= 0 || area.Width <= model.Width || area.Height <= model.Height || model.PositionX > area.Width - model.Width || model.PositionY > area.Height - model.Height)
            {
                return Json(new { status = 2 });
            }


            var existing = _dbContext.AreaObjects.FirstOrDefault(s => s.ID == model.AreaObjectID);
            if (existing != null)
            {
                existing.Width = Math.Round(model.Width, 2);
                existing.Height = Math.Round(model.Height);
                existing.PositionX = Math.Round(model.PositionX);
                existing.PositionY = Math.Round(model.PositionY);

                _dbContext.SaveChanges();
            }
           
            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult DeleteArea(long areaID)
        {
            var area = _dbContext.Areas.FirstOrDefault(s => s.ID == areaID);
            if (area != null)
            {
                area.IsDeleted = true;
                _dbContext.SaveChanges();
            }
            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult DeleteAreaObject(long areaID, long areaObjectID)
        {
            var area = _dbContext.Areas.Include(s=>s.AreaObjects).FirstOrDefault(s => s.ID == areaID);
            if (area != null)
            {
                var areaObj = area.AreaObjects.FirstOrDefault(s=>s.ID == areaObjectID);

                areaObj.IsDeleted = true;

                _dbContext.SaveChanges();
            }
            return Json(new { status = 0 });
        }
		#endregion

		#region Station
		public IActionResult StationList()
		{
            ViewBag.PrinterChannels = _dbContext.PrinterChannels.Where(s=>s.IsActive).ToList();
            ViewBag.Branchs = _dbContext.t_sucursal.ToList();
            ViewBag.Groups = _dbContext.Groups.Where(s=>s.IsActive).ToList();
            ViewBag.CanAdd = true;
            var stations = _dbContext.Stations.Where(s => s.IsActive).ToList();
            var store = _dbContext.Preferences.FirstOrDefault();
            if (store != null && store.StationLimit >0 )
            {
                if (stations.Count >= store.StationLimit) {
                    ViewBag.CanAdd = false;
                }
            }
			return View();
		}

		[HttpPost]
		public IActionResult GetStationList()
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
				var customerData = _dbContext.Stations.Include(s => s.MenuSelect).Where(s=>!s.IsDeleted).Select(s => new
				{
					s.ID,
					Name = s.Name,
                    Menu = s.MenuSelect != null? s.MenuSelect.Name: "",                 
                    s.IsActive,
                    s.Bussiness,
                    s.TypeOfSales
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

        public JsonResult GetStation(long stationId = 0)
        {
            var station = _dbContext.Stations.Include(s=>s.Printers).ThenInclude(s=>s.PrinterChannel).Include(s => s.Printers).ThenInclude(s => s.Printer).Include(s=>s.MenuSelect).Include(s=>s.Areas).FirstOrDefault(s => s.ID == stationId);

            var channels = _dbContext.PrinterChannels.Where(s=>s.IsActive).OrderByDescending(s => s.IsDefault).ToList();
            var newPrinterChannel = new List<StationPrinterChannel>();
            foreach(var channel in channels)
            {
                var nchannel = new StationPrinterChannel()
                {
                    PrinterChannel = channel,

                };
                if (station != null)
                {
                    var exist = station.Printers.FirstOrDefault(s => s.PrinterChannel == channel);
                    if (exist != null)
                    {
                        nchannel.Printer = exist.Printer;
                    }
                }

                newPrinterChannel.Add(nchannel);
            }

            var printers = _dbContext.Printers.ToList();

            var stationWarehouses = _dbContext.StationWarehouses.Where(s => s.StationID == stationId).ToList();

            var groups = _dbContext.Groups.ToList();
            var warehouses = _dbContext.Warehouses.ToList();
            var resultwarehouse = new List<StationWarehouseViewModel>();
            foreach(var w in stationWarehouses)
            {
                var group = groups.FirstOrDefault(s => s.ID == w.GroupID);
                var warehouse = warehouses.FirstOrDefault(s => s.ID == w.WarehouseID);

                resultwarehouse.Add(new StationWarehouseViewModel()
                {
                    group = group,
                    warehouse = warehouse
                });
            }

            return Json(new { station, channels = newPrinterChannel, printers, warehouses = resultwarehouse });
        }

		[HttpPost]
		public JsonResult EditStation([FromForm] StationCreateModel station)
		{
			var existing = _dbContext.Stations.Include(s=>s.Areas).Include(s=>s.MenuSelect).Include(s=>s.Printers).ThenInclude(s=>s.PrinterChannel).Include(s => s.Printers).ThenInclude(s => s.Printer).FirstOrDefault(s => s.ID == station.ID);
			if (existing != null)
			{
				var other = _dbContext.Stations.FirstOrDefault(s => s.ID != existing.ID && s.Name == station.Name && !s.IsDeleted);
				if (other != null)
				{
					return Json(new { status = 2 });
				}
				
			    existing.Name = station.Name;
                existing.Bussiness = station.Bussiness;
                existing.TypeOfSales = station.TypeOfSales;
                existing.OrderMode = station.OrderMode;
                existing.IsActive = station.IsActive;
                existing.PriceSelect = station.PriceSelect;
                existing.SalesMode = station.SalesMode;
                existing.PrintCopy = station.PrintCopy;
				existing.IDSucursal = station.IDSucursal;
                existing.ImprimirPrecuentaDelivery = station.ImprimirPrecuentaDelivery;
                var menu = _dbContext.Menus.FirstOrDefault(m => m.ID == station.MenuId);
                existing.MenuSelect = menu;
                existing.PrecioDelivery = station.PrecioDelivery;
                existing.PrepareTypeDefault = station.PrepareTypeDefault;
                              
                existing.Areas = new List<Area>();
                existing.AreaPrices = JsonConvert.SerializeObject(station.Areas);

                if (station.Areas != null)
                {
                    var index = 0;
                    foreach(var aid in station.Areas)
                    {
                        var area = _dbContext.Areas.FirstOrDefault(s=>s.ID == aid.AreaID); 
                        if (area != null)
                        {
                            existing.Areas.Add(area);
                        }
                        index++;
                    }
                }

                if (existing.Printers == null) existing.Printers = new List<StationPrinterChannel>();
                foreach(var p in station.PrinterChannels)
                {
                    var printer = _dbContext.Printers.FirstOrDefault(s => s.ID == p.PrinterID);
                    var existprinter = existing.Printers.FirstOrDefault(s => s.PrinterChannel.ID == p.PrinterChannelID);
                    if (existprinter != null)
                    {
                        existprinter.Printer = printer;
                    }
                    else
                    {
                        var channel = _dbContext.PrinterChannels.FirstOrDefault(s => s.ID == p.PrinterChannelID);
                        existing.Printers.Add(new StationPrinterChannel() { Printer = printer, PrinterChannel = channel });
                    }
                }
                var warehouses = _dbContext.StationWarehouses.Where(s=>s.StationID == existing.ID).ToList();
                _dbContext.StationWarehouses.RemoveRange(warehouses);

				_dbContext.SaveChanges();

                foreach(var w in station.GroupWarehouses)
                {
                    _dbContext.StationWarehouses.Add(new StationWarehouse()
                    {
                        GroupID = w.GroupID,
                        WarehouseID = w.WarehouseID,
                        StationID = existing.ID
                    });
                }

                _dbContext.SaveChanges();
			}
            else
            {
                var other = _dbContext.Stations.FirstOrDefault(s=>s.Name == station.Name && !s.IsDeleted);
                if (other != null)
                {
                    return Json(new { status = 2 });
                }

                existing = new Station();
				existing.Name = station.Name;
				existing.Bussiness = station.Bussiness;
				existing.TypeOfSales = station.TypeOfSales;
				existing.OrderMode = station.OrderMode;
				existing.IsActive = station.IsActive;
				existing.PriceSelect = station.PriceSelect;
				existing.SalesMode = station.SalesMode;
                existing.PrintCopy = station.PrintCopy;
                existing.IDSucursal = station.IDSucursal;
                existing.ImprimirPrecuentaDelivery = station.ImprimirPrecuentaDelivery;
                var menu = _dbContext.Menus.FirstOrDefault(m => m.ID == station.MenuId);
				existing.MenuSelect = menu;
                existing.PrecioDelivery = station.PrecioDelivery;

				existing.Areas = new List<Area>();
				existing.AreaPrices = JsonConvert.SerializeObject(station.Areas);

				if (station.Areas != null)
				{
					var index = 0;
					foreach (var aid in station.Areas)
					{
						var area = _dbContext.Areas.FirstOrDefault(s => s.ID == aid.AreaID);
						if (area != null)
						{
							existing.Areas.Add(area);
						}
						index++;
					}
				}

				if (existing.Printers == null) existing.Printers = new List<StationPrinterChannel>();
				foreach (var p in station.PrinterChannels)
				{
					var printer = _dbContext.Printers.FirstOrDefault(s => s.ID == p.PrinterID);
					var existprinter = existing.Printers.FirstOrDefault(s => s.PrinterChannel.ID == p.PrinterChannelID);
					if (existprinter != null)
					{
						existprinter.Printer = printer;
					}
					else
					{
						var channel = _dbContext.PrinterChannels.FirstOrDefault(s => s.ID == p.PrinterChannelID);
						existing.Printers.Add(new StationPrinterChannel() { Printer = printer, PrinterChannel = channel });
					}
				}

                // check station limit
                var store = _dbContext.Preferences.First();
                var stationCount = _dbContext.Stations.Where(s=>s.IsActive).Count();
                if (store.StationLimit > 0 && stationCount >= store.StationLimit)
                {
                    return Json(new { status = 3 });
                }

				_dbContext.Stations.Add(existing);

				// station user
				//_dbContext.User.Add(new Models.User() { Username = station.Name, Password = station.Pin, FullName = station.Name });
				_dbContext.SaveChanges();

				foreach (var w in station.GroupWarehouses)
				{
					_dbContext.StationWarehouses.Add(new StationWarehouse()
					{
						GroupID = w.GroupID,
						WarehouseID = w.WarehouseID,
						StationID = existing.ID
					});
				}

				_dbContext.SaveChanges();

				//var user = _dbContext.User.FirstOrDefault(s => s.Username == station.Name);
				//var adminrole = _dbContext.Role.FirstOrDefault(s => s.RoleName == "Manager");
				//user.Roles = new List<Role>() { adminrole };

				//_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}

        [HttpPost]
        public JsonResult DeleteStation(long stationID)
        {
            var station = _dbContext.Stations.FirstOrDefault(s => s.ID == stationID);
            station.IsDeleted = true;

            _dbContext.SaveChanges();
            return Json(new { status = 0 });
        }
		#endregion

		#region Discount

        public IActionResult DiscountList()
        {
            return View();
        }

		public JsonResult GetDiscount(long discountId)
		{
			var station = _dbContext.Discounts.FirstOrDefault(s => s.ID == discountId);
			return Json(station);
		}

		[HttpPost]
		public IActionResult GetDiscountList()
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
				var customerData = _dbContext.Discounts.Where(s => !s.IsDeleted).Select(s => new
				{
					s.ID,
					Name = s.Name,
					Amount = s.Amount,
                    AmountType = s.DiscountAmountType == AmountType.Percent? "%": "",
					s.IsActive,
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
		public JsonResult EditDiscount([FromForm] Discount discount)
		{
			var existing = _dbContext.Discounts.FirstOrDefault(s => s.ID == discount.ID);
			if (existing != null)
			{
				var other = _dbContext.Discounts.FirstOrDefault(s => s.ID != existing.ID && s.Name == discount.Name);
				if (other != null)
				{
					return Json(new { status = 2 });
				}
				existing.Name = discount.Name;
				existing.Amount = discount.Amount;
				existing.DiscountAmountType = discount.DiscountAmountType;
				existing.IsActive = discount.IsActive;

				_dbContext.SaveChanges();
			}
			else
			{
				var other = _dbContext.Discounts.FirstOrDefault(s => s.Name == discount.Name);
				if (other != null)
				{
					return Json(new { status = 2 });
				}

				existing = new Discount();
				existing.Name = discount.Name;
				existing.Amount = discount.Amount;
				existing.DiscountAmountType = discount.DiscountAmountType;
				existing.IsActive = discount.IsActive;

				_dbContext.Discounts.Add(existing);

				_dbContext.SaveChanges();
			}

			return Json(new { status = 0, id = existing.ID });
		}

		[HttpPost]
		public JsonResult DeleteDiscount(long discountID)
		{
			var discount = _dbContext.Discounts.FirstOrDefault(s => s.ID == discountID);
			discount.IsDeleted = true;

			_dbContext.SaveChanges();
			return Json(new { status = 0 });
		}
		#endregion

		#region Promotion
		public IActionResult PromotionList()
		{
			return View();
		}

        public IActionResult AddPromotion (long promotionID = 0)
        {
            ViewBag.PromotionID = promotionID;
            var promotion = new Promotion();
            if (promotionID > 0)
            {
                var exist = _dbContext.Promotions.Include(s=>s.Targets).FirstOrDefault(p => p.ID == promotionID);
                if (exist != null)
                    promotion = exist;
            }
            return View(promotion);
        }

		public JsonResult GetPromotion(long promotionId)
		{
			var station = _dbContext.Promotions.Include(s=>s.Targets).FirstOrDefault(s => s.ID == promotionId);
			return Json(station);
		}

		[HttpPost]
		public IActionResult GetPromotionList()
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
				var customerData = _dbContext.Promotions.Where(s => !s.IsDeleted).Select(s => new
				{
					s.ID,
					Name = s.Name,
					Amount = s.Amount,
					AmountType = s.AmountType == AmountType.Percent ? "%" : "",
                    Recurring = s.IsRecurring? "Yes": "No",
					s.IsActive,
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
		public JsonResult EditPromotion([FromBody] PromotionCreateModel discount)
        {
            var starttime = new DateTime(2000, 1, 1);
            try
            {
                starttime = DateTime.ParseExact(discount.StartDateStr, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);
            }
            catch { }
            var endtime = new DateTime(2000, 1, 1);
            try
            {
                endtime = DateTime.ParseExact(discount.EndDateStr, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);
            }
            catch { }
           
            var existing = _dbContext.Promotions.Include(s=>s.Targets).FirstOrDefault(s => s.ID == discount.ID);
			if (existing != null)
			{
				var other = _dbContext.Promotions.FirstOrDefault(s => s.ID != existing.ID && s.Name == discount.Name);
				if (other != null)
				{
					return Json(new { status = 2 });
				}
               
                

                existing.Name = discount.Name;
				existing.Amount = discount.Amount;
				existing.AmountType = discount.AmountType;
				existing.IsRecurring = discount.IsRecurring;
				existing.IsAllDay = discount.IsAllDay;
				existing.StartTime = starttime;
				existing.EndTime = endtime;
				//existing.RecurringStartDate = recurdate;
				//existing.Duration = discount.Duration;
				existing.DiscountType = discount.DiscountType;
				existing.ApplyType = discount.ApplyType;
				existing.FirstCount = discount.FirstCount;
				existing.EveryCount = discount.EveryCount;
				//existing.EndOccurrence = discount.EndOccurrence;
				//existing.IsRecurNoEnd = discount.IsRecurNoEnd;
				//existing.WeekNum = discount.WeekNum;
				existing.WeekDays = discount.WeekDays;
				existing.EndOccurrence = discount.EndOccurrence;
                
                if (existing.Targets != null && existing.Targets.Count > 0)
                {
                    
                    foreach(var t in existing.Targets)
                    {
                        _dbContext.PromotionTargets.Remove(t);
                    }
                    existing.Targets.Clear();
                }
                existing.Targets = new List<PromotionTarget>();
                if (discount.Targets != null)
                {
                    foreach (var t in discount.Targets)
                    {
                        existing.Targets.Add(t);
                    }
                }
               
				existing.IsActive = discount.IsActive;

				_dbContext.SaveChanges();
			}
			else
			{
				var other = _dbContext.Promotions.FirstOrDefault(s => s.Name == discount.Name);
				if (other != null)
				{
					return Json(new { status = 2 });
				}

				existing = new Promotion();
				existing.Name = discount.Name;
				existing.Amount = discount.Amount;
				existing.AmountType = discount.AmountType;
                existing.IsRecurring = discount.IsRecurring;
                existing.IsAllDay = discount.IsAllDay;
                existing.StartTime = starttime;
                existing.EndTime = endtime;
                //existing.RecurringStartDate = recurdate;
                //existing.Duration = discount.Duration;
                existing.DiscountType = discount.DiscountType;
                existing.ApplyType = discount.ApplyType;
                existing.FirstCount = discount.FirstCount;
                existing.EveryCount = discount.EveryCount;
                //existing.EndOccurrence = discount.EndOccurrence;
                //existing.IsRecurNoEnd = discount.IsRecurNoEnd;
                //existing.WeekNum = discount.WeekNum;
                existing.WeekDays = discount.WeekDays;
                existing.EndOccurrence = discount.EndOccurrence;
                existing.Targets = new List<PromotionTarget>();
                if (discount.Targets != null)
                {
                    foreach (var t in discount.Targets)
                    {
                        existing.Targets.Add(t);
                    }
                    existing.IsActive = discount.IsActive;
                }
				_dbContext.Promotions.Add(existing);

				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}

		[HttpPost]
		public JsonResult DeletePromotion(long promotionID)
		{
			var promotion = _dbContext.Promotions.Include(s=>s.Targets).FirstOrDefault(s => s.ID == promotionID);

            promotion.Targets.RemoveAll(s=>true);
            
			_dbContext.Promotions.Remove(promotion);

			_dbContext.SaveChanges();
			return Json(new { status = 0 });
		}
		#endregion



		#region Branch

		// branch

		[HttpPost]
		public JsonResult GetAllBranchs()
		{
			return Json(_dbContext.t_sucursal.ToList());
		}

		[HttpPost]
		public IActionResult GetBranchList()
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
				var customerData = (from s in _dbContext.t_sucursal
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
		public JsonResult EditBranch(t_sucursal request)
		{
			try
			{
				var existing = _dbContext.t_sucursal.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					var otherexisting = _dbContext.t_sucursal.FirstOrDefault(x => x.ID != request.ID && x.Name == request.Name);
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
					var otherexisting = _dbContext.t_sucursal.FirstOrDefault(x => x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					_dbContext.t_sucursal.Add(request);
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}

			}
			catch { }

			return Json(new { status = 1 });

		}

		[HttpPost]
		public JsonResult DeleteBranch(long branchId)
		{
			var existing = _dbContext.t_sucursal.FirstOrDefault(x => x.ID == branchId);
			if (existing != null)
			{
				_dbContext.t_sucursal.Remove(existing);
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}


		#endregion
		public IActionResult ReservationList()
		{
            var stations = _dbContext.Stations.Where(s=>s.IsActive && !s.IsDeleted).ToList();
            ViewBag.Stations = stations;
			return View();
		}


		[HttpPost]
		public IActionResult GetReservationList()
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
                var data = (from s in _dbContext.Reservations
                            join a in _dbContext.Areas on s.AreaID equals a.ID
                            join t in _dbContext.Stations on s.StationID equals t.ID
                            select new
                            {
                                StationID = t.ID,
                                StationName = t.Name,
                                AreaName = a.Name,
                                s.AreaID,
                                s.ID,
                                s.TableName,
                                s.TableID,
                                s.GuestCount,
                                s.GuestName,
                                s.Comments,
                                s.PhoneNumber,
                                s.Status,
                                ReservationDate = s.ReservationTime.ToString("MM/dd"),
                                ReservationTime = s.ReservationTime.ToString("HH:mm"),
                                ReservationDateTime = s.ReservationTime,
                                s.Duration,
                                
                            }).OrderByDescending(s=>s.ReservationDateTime).ToList();
				

				var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
				var station = Request.Form["columns[1][search][value]"].FirstOrDefault();
				var from = Request.Form["columns[2][search][value]"].FirstOrDefault();
				var to = Request.Form["columns[3][search][value]"].FirstOrDefault();

				if (!string.IsNullOrEmpty(all))
				{
					all = all.Trim().ToLower();
					data = data.Where(m => m.GuestName.ToLower().Contains(all) || m.AreaName.ToLower().Contains(all) || m.TableName.ToLower().Contains(all) || m.StationName.ToLower().Contains(all)).ToList();
				}
				else
				{
					if (!string.IsNullOrEmpty(searchValue))
					{
						searchValue = searchValue.ToLower();
						data = data.Where(m => m.GuestName.ToLower().Contains(all) || m.AreaName.ToLower().Contains(all) || m.TableName.ToLower().Contains(all) || m.StationName.ToLower().Contains(all)).ToList();
					}
				}

				if (!string.IsNullOrEmpty(to))
				{
					try
					{
						var toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        data = data.Where(s=> s.ReservationDateTime.Date <= toDate.Date).ToList();
					}
					catch { }
				}
				var fromDate = DateTime.Now;
				if (!string.IsNullOrEmpty(from))
				{
					try
					{
						fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
						data = data.Where(s => s.ReservationDateTime.Date >= fromDate.Date).ToList();
					}
					catch { }
				}


				if (!string.IsNullOrEmpty(station))
				{
					data = data.Where(s => "" + s.StationID == station).ToList();
				}
				
				//total number of rows count   
				recordsTotal = data.Count();
				//Paging   
				var result = data.Skip(skip).ToList();
				if (pageSize != -1)
				{
					result = result.Take(pageSize).ToList();
				}
				//Returning Json Data  
				return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

			}
			catch (Exception ex)
			{
				throw;
			}
		}


		#region Kitchen
		public IActionResult KitchenList()
		{
			ViewBag.Printers = _dbContext.Printers.ToList();
			ViewBag.Stations = _dbContext.Stations.Where(s=>s.IsActive).ToList();
			return View();
		}

		[HttpPost]
		public IActionResult GetKitchenList()
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
			
                var customerData = (from s in _dbContext.Kitchen
                                    join t in _dbContext.Printers on s.PrinterID equals t.ID into p
                                    from t in p.DefaultIfEmpty()                                                      
                                    select new
                                    {
                                        s.ID,
                                        Name = s.Name,
                                        Printer = t==null?"" :t.Name,
                                        s.IsActive 
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

		public JsonResult GetKitchen(long kitchenId = 0)
		{
			var kitchen = _dbContext.Kitchen.FirstOrDefault(s => s.ID == kitchenId);

			return Json(new { kitchen });
		}

		[HttpPost]
		public JsonResult EditKitchen([FromForm] Kitchen kitchen)
		{
			var existing = _dbContext.Kitchen.FirstOrDefault(s => s.ID == kitchen.ID);
			if (existing != null)
			{
				var other = _dbContext.Kitchen.FirstOrDefault(s => s.ID != existing.ID && s.Name == kitchen.Name && !s.IsDeleted);
				if (other != null)
				{
					return Json(new { status = 2 });
				}

				existing.Name = kitchen.Name;
				existing.PrinterID = kitchen.PrinterID;
                existing.IsActive = kitchen.IsActive;
                existing.Stations = kitchen.Stations;
				_dbContext.SaveChanges();
			}
			else
			{
				var other = _dbContext.Stations.FirstOrDefault(s => s.Name == kitchen.Name && !s.IsDeleted);
				if (other != null)
				{
					return Json(new { status = 2 });
				}

				existing = new Kitchen();
				existing.Name = kitchen.Name;
				existing.PrinterID = kitchen.PrinterID;
				existing.IsActive = kitchen.IsActive;
				existing.Stations = kitchen.Stations;
				_dbContext.Kitchen.Add(existing);

				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}
        #endregion


        #region Course
        //group
        public IActionResult CourseList()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetCourseList()
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
                var customerData = (from s in _dbContext.Courses
                                    where !s.IsDeleted
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
                    customerData = customerData.Where(m => m.Name.ToLower().Contains(all));
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
		public JsonResult GetAllActiveCourseList()
		{
			return Json(_dbContext.Courses.Where(s => s.IsActive).ToList());
		}
		[HttpPost]
        public JsonResult EditCourse([FromForm] Course request)
        {
            try
            {
                var existing = _dbContext.Courses.FirstOrDefault(x => x.ID == request.ID);
                if (existing != null)
                {
                    var otherexisting = _dbContext.Courses.FirstOrDefault(x => x.ID != request.ID && x.Name == request.Name);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    existing.Name = request.Name;
                    existing.Order = request.Order;
                    existing.IsActive = request.IsActive;
                    _dbContext.SaveChanges();
                    return Json(new { status = 0 });
                }
                else
                {
                    var otherexisting = _dbContext.Courses.FirstOrDefault(x => x.Name == request.Name);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    _dbContext.Courses.Add(request);
                    _dbContext.SaveChanges();
                    return Json(new { status = 0 });
                }

            }
            catch { }

            return Json(new { status = 1 });

        }

        [HttpPost]
        public JsonResult DeleteCourse(long Id)
        {
            var existing = _dbContext.Courses.FirstOrDefault(x => x.ID == Id);
            if (existing != null)
            {
                _dbContext.Courses.Remove(existing);
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }
		#endregion


		#region Saving Size
		public IActionResult ServingSizeList()
		{
			return View();
		}

		[HttpPost]
		public IActionResult GetServingSizeList(int status=0)
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
				var customerData = _dbContext.ServingSize.Where(s => !s.IsDeleted).Select(s => s);

				//Sorting
				if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				{
					try
					{
						customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
					}
					catch { }
				}
				if (!string.IsNullOrEmpty(searchValue))
				{
					searchValue = searchValue.Trim().ToLower();
					customerData = customerData.Where(m => m.Name.ToLower().Contains(searchValue) || (m.Description != null && m.Description.ToLower().Contains(searchValue)));
				}
				if (status == 1)
                {
                    customerData = customerData.Where(s => s.IsActive);
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
		public JsonResult EditServingSize([FromForm] ServingSize model)
		{
			var existing = _dbContext.ServingSize.FirstOrDefault(s => s.ID == model.ID);
			if (existing != null)
			{
				var other = _dbContext.ServingSize.FirstOrDefault(s => s.ID != existing.ID && s.Name == model.Name && !s.IsDeleted);
				if (other != null)
				{
					return Json(new { status = 2 });
				}

				existing.Name = model.Name;
				existing.Description = model.Description;
				existing.IsActive = model.IsActive;
				_dbContext.SaveChanges();
			}
			else
			{
				var other = _dbContext.ServingSize.FirstOrDefault(s => s.Name == model.Name && !s.IsDeleted);
				if (other != null)
				{
					return Json(new { status = 2 });
				}
                existing = new ServingSize();
				existing.Name = model.Name;
				existing.Description = model.Description;
				existing.IsActive = model.IsActive;
				_dbContext.ServingSize.Add(existing);

				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}

		#endregion

	}

    public class ProductPrintRecipeRequest
    {
        public long ProductID { get; set; }
        public int ServingSizeID { get; set; }
    }

    public class ProductPrintLabelRequest
    {
        public long ProductID { get; set; }
        public int Copies { get; set; }
        public DateTime PackingDate { get; set; }
        public DateTime ExpireDate { get; set; }
        public string Dimension { get; set; }  // 3x3 2x3 3x2
    }


    public class StationWarehouseViewModel
    {
        public Group group { get; set; }
        public Warehouse warehouse { get; set; }
    }
}
