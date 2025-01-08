using AuroraPOS.Data;
using AuroraPOS.ModelsJWT;
using AuroraPOS.Services;
using AuroraPOS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;

namespace AuroraPOS.Core
{
    public class MenuCore
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        public MenuCore(IUserService userService, AppDbContext dbContext, IHttpContextAccessor context)
        {
            _userService = userService;
            _dbContext = dbContext;
            _context = context;
        }

        public List<CategoryViewModel>? GetAllActiveCategoryList()
        {
            var categoryList = (_dbContext.Categories.Include(s => s.Taxs).Include(s => s.Propinas).Include(s => s.PrinterChannels).Where(s => s.IsActive).Select(s => new CategoryViewModel()
            {
                ID = s.ID,
                Name = s.Name,
                Plato = s.Plato,
                IsActive = s.IsActive,
                GroupID = s.Group.ID,
                GroupName = s.Group.GroupName,
                CourseID = s.CourseID,
                Taxes = s.Taxs == null ? new List<long>() : s.Taxs.Select(x => x.ID).ToList(),
                Propinas = s.Propinas == null ? new List<long>() : s.Propinas.Select(x => x.ID).ToList(),
                PrinterChannels = s.PrinterChannels == null ? new List<long>() : s.PrinterChannels.Select(x => x.ID).ToList()

            }).ToList());

            if (categoryList != null)
            {
                return categoryList;
            }
            return null;
            
        }

        public MenuProductList GetProductList(string draw, int start, int length,
                string sortColumn, string sortColumnDirection, string searchValue, 
                string all, string category, string barcode, string status, string db)
        {
            var productList = new MenuProductList();

            //Paging Size (10,20,50,100)  
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            int recordsTotal = 0;


            // Getting all Customer data  
            var result = _dbContext.Products.Include(x => x.Category).Include(s => s.SubCategory).OrderByDescending(s => s.CreatedDate).Select(s => new ProductFiltered
            {
                ID = s.ID,
                Name = s.Name,
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
                if (status == "1")
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
                    string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", db, "product", item.ID.ToString() + ".png");
                    if (System.IO.File.Exists(pathFile))
                    {
                        var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

                        item.Photo = (string?)Path.Combine(_baseURL, "localfiles", db, "product", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second); ;
                    }
                    else
                    {
                        item.Photo = null; //Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", "empty.png");
                    }
                }
            }

            //Returning Json Data  
            productList.draw = draw;
            productList.recordsFiltered = recordsTotal;
            productList.recordsTotal = recordsTotal;
            productList.productFiltereds = data;

            return productList;
        }
    }
}
