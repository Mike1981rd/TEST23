using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.ModelsCentral;
using AuroraPOS.Security;
using AuroraPOS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Diagnostics;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Text;
//using FastReport.AdvMatrix;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using AuroraPOS.Services;
using AuroraPOS.Core;
using AuroraPOS.ModelsJWT;
using PuppeteerSharp;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using NPOI.SS.Formula.Functions;

namespace AuroraPOS.Controllers
{
    [Authorize]
    public class SettingController : BaseController
	{
        private readonly IWebHostEnvironment _hostingEnvironment;
        private AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        private readonly DbAlfaCentralContext _dbCentralContext;
        private readonly IUserService _userService;

        public SettingController(ExtendedAppDbContext dbContext, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor context, IUserService userService)
        {
            _dbContext = dbContext._context;
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _dbCentralContext = new DbAlfaCentralContext();
            _userService = userService;
        }

        public IActionResult Index()
        {
            return View();
        }

		public IActionResult EditCustomer(long customerId = 0)
		{
            ViewBag.Taxes = _dbContext.Taxs.Where(s => s.IsActive).ToList();
            var customer = _dbContext.Customers.Include(s => s.Voucher).FirstOrDefault(s => s.ID == customerId);
			ViewBag.Customer = customer;
			var deliveryZone = _dbContext.DeliveryZones.FirstOrDefault(s => s.IsActive && s.ID == customer.DeliveryZoneID);
			ViewBag.DeliveryZone = deliveryZone?.Name;
			//var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.IsActive && s.ID == customer.Voucher.).ToList();
            return View();
		}

		public IActionResult Accounts()
		{
			return View();
		}

		#region Payment Method
		public IActionResult PaymentMethods()
        {
            return View();
        }

		[HttpPost]
		public async Task<IActionResult> GetPaymentMethods()
		{
			try
			{

				string? draw=null;
				string? start=null;
				string? length=null;
				string? sortColumn=null;
				string? sortColumnDirection=null;
				string? searchValue=null;

				if (HttpContext.Request.ContentLength>0)
				{
					draw = HttpContext.Request.Form["draw"].FirstOrDefault(); 
					
					// Skiping number of Rows count  
					start = Request.Form["start"].FirstOrDefault();
					// Paging Length 10,20  
					length = Request.Form["length"].FirstOrDefault();
					// Sort Column Name  
					sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
					// Sort Column Direction ( asc ,desc)  
					sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
					// Search Value from (Search box)  
					searchValue = Request.Form["search[value]"].FirstOrDefault();
				}
					
				

				//Paging Size (10,20,50,100)  
				int pageSize = length != null ? Convert.ToInt32(length) : -1;
				int skip = start != null ? Convert.ToInt32(start) : 0;
				int recordsTotal = 0;

				// Getting all Customer data  
				var customerData = (from s in _dbContext.PaymentMethods
									orderby s.DisplayOrder
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
					customerData = customerData.Where(m => m.Name.Contains(searchValue)).OrderBy(s => s.DisplayOrder);
				}

				//total number of rows count   
				recordsTotal = customerData.Count();
				//Paging   
				var data = customerData.Skip(skip).ToList();
				if (pageSize != -1)
				{
					data = data.Take(pageSize).ToList();
				}

                //Obtenemos las urls de las imagenes
                var request = _context.HttpContext.Request;
                var _baseURL = $"https://{request.Host}";
                if (data!=null && data.Any()) {
					foreach(var item in data) {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "paymentmethod", item.ID.ToString() + ".png");
                        if (System.IO.File.Exists(pathFile))
                        {
                            var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

                            item.Image = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "paymentmethod", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second); ;
                        }
                        else
                        {
                            item.Image = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "paymentmethod", "empty.png");
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
		public JsonResult GetPaymentMethod(int id)
		{
			var pmethod = _dbContext.PaymentMethods.FirstOrDefault(s=>s.ID == id);

            //Obtenemos la URL de la imagen del archivo            
            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "paymentmethod", pmethod.ID.ToString() + ".png");
            var request = _context.HttpContext.Request;
            var _baseURL = $"https://{request.Host}";
            if (System.IO.File.Exists(pathFile))
            {
                var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);

                pmethod.Image = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "paymentmethod", pmethod.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
            }
			else {                
                pmethod.Image = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "paymentmethod", "empty.png");
            }

            return Json(pmethod);
		}
		[HttpPost]
		public JsonResult EditPaymentMethod([FromBody] PaymentMethod request)
		{
			try
			{
				var existing = _dbContext.PaymentMethods.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					var otherexisting = _dbContext.PaymentMethods.FirstOrDefault(x => x.ID != request.ID && x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					existing.Name = request.Name;
					existing.Description = request.Description;
					existing.Tasa = request.Tasa; ;
					existing.Tip = request.Tip; ;
					//existing.Image = request.Image;
					existing.PaymentType = request.PaymentType; ;
					existing.IsActive = request.IsActive;
					_dbContext.SaveChanges();

					//Guardamos la imagen del archivo
					if (!string.IsNullOrEmpty(request.ImageUpload) && !request.ImageUpload.Contains("http:") && !request.ImageUpload.Contains("https:")) {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"],"paymentmethod");

                        if (!Directory.Exists(pathFile))
                        {
                            Directory.CreateDirectory(pathFile);
                        }

                        pathFile = Path.Combine(pathFile, existing.ID.ToString() + ".png");

                        request.ImageUpload = request.ImageUpload.Replace('-', '+');
                        request.ImageUpload = request.ImageUpload.Replace('_', '/');
                        request.ImageUpload = request.ImageUpload.Replace("data:image/jpeg;base64,", string.Empty);
                        request.ImageUpload = request.ImageUpload.Replace("data:image/png;base64,", string.Empty);
                        byte[] imageByteArray = Convert.FromBase64String(request.ImageUpload);
                        System.IO.File.WriteAllBytes(pathFile, imageByteArray);
                        
                    }
                    else if (string.IsNullOrEmpty(request.ImageUpload))
                    {
                        try
                        {
                            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "paymentmethod");
                            pathFile = Path.Combine(pathFile, existing.ID.ToString() + ".png");
                            System.IO.File.Delete(pathFile);
                        }
                        catch { }
                    }



                    return Json(new { status = 0, id = existing.ID });
				}
				else
				{
					var otherexisting = _dbContext.PaymentMethods.FirstOrDefault(x => x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}

					var pmethods = _dbContext.PaymentMethods.Where(s => s.IsActive).ToList();
					if (pmethods.Count == 0)
					{
						request.DisplayOrder = 1;
					}
					else
					{
						var last = pmethods.OrderBy(s => s.DisplayOrder).Last();
						request.DisplayOrder = last.DisplayOrder + 1;
					}
                  
                   _dbContext.PaymentMethods.Add(request);
                    _dbContext.SaveChanges();

                    //Guardamos la imagen del archivo
                    if (!string.IsNullOrEmpty(request.ImageUpload) && !request.ImageUpload.Contains("http:") && !request.ImageUpload.Contains("https:"))
                    {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "paymentmethod");

                        if (!Directory.Exists(pathFile))
                        {
                            Directory.CreateDirectory(pathFile);
                        }

                        pathFile = Path.Combine(pathFile, request.ID.ToString() + ".png");

                        request.ImageUpload = request.ImageUpload.Replace('-', '+');
                        request.ImageUpload = request.ImageUpload.Replace('_', '/');
                        request.ImageUpload = request.ImageUpload.Replace("data:image/jpeg;base64,", string.Empty);
                        request.ImageUpload = request.ImageUpload.Replace("data:image/png;base64,", string.Empty);
                        byte[] imageByteArray = Convert.FromBase64String(request.ImageUpload);
                        System.IO.File.WriteAllBytes(pathFile, imageByteArray);
                    }
                    else if (string.IsNullOrEmpty(request.ImageUpload))
                    {
                        try
                        {
                            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "paymentmethod");
                            pathFile = Path.Combine(pathFile, existing.ID.ToString() + ".png");
                            System.IO.File.Delete(pathFile);
                        }
                        catch { }
                    }

                    
					return Json(new { status = 0, id = request.ID });
				}

			}
			catch(Exception ex) {
				var m = ex;
			}

			return Json(new { status = 1 });
		}

		[HttpPost]
		public JsonResult UpdatePaymentMethodOrder([FromBody] List<PaymentMethod> model)
		{
			var dbpaymentmethods = _dbContext.PaymentMethods.ToList();
			foreach (var d in model)
			{
				var pmethod = dbpaymentmethods.FirstOrDefault(s => s.ID == d.ID);
				pmethod.DisplayOrder = d.DisplayOrder;
			}
			_dbContext.SaveChanges();

			return Json(new { status = 0 });
		}

		[HttpPost]
		public JsonResult DeletePaymentMethod(long paymentMethodId)
		{
			var existing = _dbContext.PaymentMethods.FirstOrDefault(x => x.ID == paymentMethodId);
			if (existing != null)
			{
				_dbContext.PaymentMethods.Remove(existing);
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}
        #endregion

        #region Customer
        public IActionResult Customers()
		{
			ViewBag.Taxes = _dbContext.Taxs.Where(s => s.IsActive).ToList();
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> GetCustomers()
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
				var all = Request.Form["columns[0][search][value]"].FirstOrDefault();
                var statusValue = Request.Form["columns[1][search][value]"].FirstOrDefault();
                var rncValue = Request.Form["columns[3][search][value]"].FirstOrDefault();

                // Getting all Customer data  
                var customerData = (from s in _dbContext.Customers
									select s);

                if (!string.IsNullOrEmpty(statusValue))
                {
                    var status = int.Parse(statusValue);
                    customerData = customerData.Where(m => m.IsActive == (status == 1));
                }

                if (!string.IsNullOrEmpty(rncValue))
                {
                    customerData = customerData.Where(m => m.RNC.Contains(rncValue));
                }

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
				if (!string.IsNullOrEmpty(all))
				{
					customerData = customerData.Where(m => m.Name.ToLower().Contains(all.ToLower()) || m.Phone.ToLower().Contains(all.ToLower()) || m.Address1.ToLower().Contains(all.ToLower()));
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
        public async Task<IActionResult> GetActiveCustomerList(long clienteid = 0)
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
                var customerData = (from s in _dbContext.Customers
									where s.IsActive
                                    select s);

				if (clienteid > 0) {
					customerData = (from s in _dbContext.Customers
                                    where s.IsActive && s.ID==clienteid
                                    select s);


                }

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
                    customerData = customerData.Where(m => m.Name.ToLower().Contains(searchValue.ToLower()) || m.Phone.ToLower().Contains(searchValue.ToLower()) || m.RNC.ToLower().Contains(searchValue.ToLower()) || m.Address1.ToLower().Contains(searchValue.ToLower()));
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
		public JsonResult GetActiveCustomers()
		{
            var settingsCore = new SettingsCore(_userService, _dbContext, _context);
            var customers = settingsCore.GetActiveCustomers();

            return Json(customers);
		}
		public JsonResult GetCustomer(long customerId)
		{
			var customer = _dbContext.Customers.Include(s=>s.Voucher).FirstOrDefault(s => s.ID == customerId);
			return Json(customer);
		}

		[HttpPost]
		public JsonResult EditCustomer([FromBody] CustomerCreateViewModel request)
		{
            SettingsCore settingsCore = new SettingsCore(_userService, _dbContext, _context);

            try
            {
                Tuple<int, long> result = settingsCore.EditCustomer(request);

                return Json(new { status = result.Item1, id = result.Item2 });
            }
            catch
			{

			}
            return Json(new { status = 1 });

            //try
            //{
            //	var existing = _dbContext.Customers.FirstOrDefault(x => x.ID == request.ID);
            //	if (existing != null)
            //	{					
            //		existing.Name = request.Name;
            //		existing.Phone = request.Phone;
            //		existing.Email = request.Email;
            //		existing.RNC = request.RNC;
            //		existing.Address1 = request.Address1;
            //		existing.City = request.City;
            //		existing.CreditLimit = request.CreditLimit;
            //		existing.CreditDays = request.CreditDays;
            //		existing.Balance = request.Balance;
            //		existing.Avatar = request.Avatar;
            //		existing.IsActive = request.IsActive;
            //		existing.DeliveryZoneID = request.DeliveryZoneID;
            //		existing.Company = request.Company;

            //                 if (request.VoucherId > 0)
            //		{
            //			var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == request.VoucherId);
            //			existing.Voucher = voucher;
            //		}


            //		_dbContext.SaveChanges();
            //		return Json(new { status = 0, id = existing.ID });
            //	}
            //	else
            //	{	

            //		existing = new Customer();
            //		existing.Name = request.Name;
            //		existing.Phone = request.Phone;
            //		existing.Email = request.Email;
            //		existing.RNC = request.RNC;
            //		existing.Address1 = request.Address1;
            //		existing.City = request.City;
            //		existing.CreditLimit = request.CreditLimit;
            //		existing.CreditDays = request.CreditDays;
            //		existing.Balance = request.Balance;
            //		existing.Avatar = request.Avatar;
            //		existing.IsActive = request.IsActive;
            //		existing.DeliveryZoneID = request.DeliveryZoneID;
            //                 existing.Company = request.Company;
            //                 if (request.VoucherId > 0)
            //		{
            //			var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == request.VoucherId);
            //			existing.Voucher = voucher;
            //		}

            //		_dbContext.Customers.Add(existing);
            //		_dbContext.SaveChanges();
            //		return Json(new { status = 0, id = existing.ID });
            //	}

            //}
            //catch { }

            //return Json(new { status = 1 });
        }
        [HttpPost]
        public JsonResult EditCustomerSimple([FromBody] CustomerSimpleCreateViewModel request)
        {
            SettingsCore settingsCore = new SettingsCore(_userService, _dbContext, _context);

            try
            {
                Tuple<int, long> result = settingsCore.EditCustomerSimple(request);

                return Json(new { status = result.Item1, customerid = result.Item2 });
            }
            catch (Exception ex)
            {
                var m = ex;
            }

            return Json(new { status = 1 });

            //         try
            //         {
            //	Debug.WriteLine(request.VoucherId);
            //             var existing = _dbContext.Customers.FirstOrDefault(x => x.ID == request.ID);
            //	Debug.WriteLine(existing);
            //             if (existing != null)
            //             {
            //		Debug.WriteLine("existe");
            //                 existing.Name = request.Name;
            //                 existing.RNC = request.RNC;
            //                 existing.Phone = request.Phone;
            //                 existing.Email = request.Email;
            //		existing.Address1 = request.Address1;
            //		existing.Address2 = request.Address2;
            //		existing.DeliveryZoneID = request.ZoneId;
            //		existing.Company = request.Company;
            //                 /*if (request.ZoneId > 0)
            //                 {
            //                     var zone = _dbContext.DeliveryZones.FirstOrDefault(s => s.ID == request.ZoneId);
            //                     existing.Zone = zone;
            //                 }*/

            //                 if (request.VoucherId > 0)
            //                 {
            //                     var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == request.VoucherId);
            //			Debug.WriteLine("ola");
            //			Debug.WriteLine(voucher);
            //                     existing.Voucher = voucher;
            //                 }


            //                 _dbContext.SaveChanges();
            //                 return Json(new { status = 0, customerid = existing .ID});
            //             }
            //             else
            //             {
            //                 Debug.WriteLine("no existe");
            //                 existing = new Customer();
            //                 existing.Name = request.Name;
            //		existing.RNC = request.RNC;
            //                 existing.Phone = request.Phone;
            //                 existing.Email = request.Email;
            //                 existing.Address1 = request.Address1;
            //                 existing.Address2 = request.Address2;
            //                 existing.DeliveryZoneID = request.ZoneId;
            //                 existing.Company = request.Company;

            //                 /*if (request.ZoneId > 0)
            //                 {
            //                     var zone = _dbContext.DeliveryZones.FirstOrDefault(s => s.ID == request.ZoneId);
            //                     existing.Zone = zone;
            //                 }*/

            //                 if (request.VoucherId > 0)
            //                 {
            //                     Debug.WriteLine("ola2");

            //                     var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == request.VoucherId);
            //                     Debug.WriteLine(voucher);
            //                     existing.Voucher = voucher;
            //                 }

            //                 _dbContext.Customers.Add(existing);
            //                 _dbContext.SaveChanges();
            //                 return Json(new { status = 0, customerid = existing.ID });
            //             }

            //         }
            //         catch (Exception ex) {
            //	var m = ex;
            //}

            //         return Json(new { status = 1 });
        }

        [HttpPost]
		public JsonResult DeleteCustomer(long customerId)
		{
			var existing = _dbContext.Customers.FirstOrDefault(x => x.ID == customerId);
			if (existing != null)
			{
				_dbContext.Customers.Remove(existing);
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}
        #endregion

        public IActionResult Users()
		{
			var objCentral = new AuroraPOS.Services.CentralService();
            
			ViewBag.Roles = _dbContext.Role.Where(s=>!s.IsDeleted).ToList();
			ViewBag.Companies = objCentral.GetAllCompanies();


			// Obtener totales de usuarios
			var totalUsuarios = _dbContext.User.Count(u => !u.IsDeleted);
			var totalUsuariosActivos = _dbContext.User.Count(u => u.IsActive && !u.IsDeleted);
			var totalUsuariosInactivos = totalUsuarios - totalUsuariosActivos;

			// Asignar totales al ViewBag
			ViewBag.TotalUsuarios = totalUsuarios;
			ViewBag.TotalUsuariosActivos = totalUsuariosActivos;
			ViewBag.TotalUsuariosInactivos = totalUsuariosInactivos;

			if (User.Identity.GetName().ToLower() != "admin")
			{
				var lstCompanies = objCentral.GetAllCompanies();
				ViewBag.CurrentCompany = (from m in lstCompanies where m.Database==GetCookieValue("db") select m.Id).FirstOrDefault() ;
			}
			
			
			
            return View();
		}
        [HttpPost]
        public async Task<IActionResult> GetUsers(Boolean validateAdmin = false)
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
                var customerData = (from s in _dbContext.User.Include(s=>s.Roles)
									where s.IsDeleted == false
                                    select s);


				if (validateAdmin && User.Identity.GetName().ToLower()!="admin")
                {
	                customerData = (from m in customerData where m.Username.ToLower()!="alfaadmin" select m);
                }

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
                    customerData = customerData.Where(m => m.FullName.Contains(searchValue));
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
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data
				});

            }
            catch (Exception ex)
            {
                throw;
            }
        }

		

        public JsonResult GetUser(long userId)
        {
            var objModel = new UserAuxiliar();
            objModel.user = _dbContext.User.Include(s => s.Roles).FirstOrDefault(s => s.ID == userId);

            var objCentral = new AuroraPOS.Services.CentralService();
            var lstCompanies = objCentral.GetAllowedCompanies(objModel.user.Username);
            foreach (var c in lstCompanies)
            {
                objModel.companies = new List<ModelsCentral.Company>();
                foreach (var r in lstCompanies)
                {
                    var company = lstCompanies.FirstOrDefault(s => s.Id == r.Id);
                    objModel.companies.Add(company);
                }
            }            

            return Json(objModel);
        }

        [HttpPost]
        public JsonResult EditUser([FromBody] UserCreateModel request)
        {
	        try
	        {
		        //request.Username = request.Username.Trim().ToLower();
		        request.Username = request.Username;
		        var existing = _dbContext.User.Include(s => s.Roles).FirstOrDefault(x => x.ID == request.ID);
		        if (existing != null)
		        {
			        var oldUsername = existing.Username;
			        var otherexisting = _dbContext.User.FirstOrDefault(x =>
				        x.ID != request.ID && (x.Username.Trim().ToLower() == request.Username.Trim().ToLower()));
			        if (otherexisting != null)
			        {
				        return Json(new { status = 2 });
			        }
			        
			        var otherexistingpin = _dbContext.User.FirstOrDefault(x =>
				        x.ID != request.ID && (x.Pin.Trim().ToLower() == request.Pin.Trim().ToLower()));
			        if (otherexistingpin != null)
			        {
				        return Json(new { status = 3 });
			        }

			        existing.FullName = request.FullName;
			        existing.PhoneNumber = request.PhoneNumber;
			        existing.Username = request.Username;
			        existing.Password = request.Password;
			        existing.Pin = request.Pin;
			        existing.Email = request.Email;
			        existing.Address = request.Address;
			        existing.City = request.City;
			        existing.State = request.State;
			        existing.ZipCode = request.ZipCode;
			        existing.ProfileImage = request.ProfileImage;
			        existing.IsActive = request.IsActive;
			        if (request.RoleIds != null)
			        {
				        existing.Roles = new List<Role>();
				        foreach (var r in request.RoleIds)
				        {
					        var role = _dbContext.Role.FirstOrDefault(s => s.ID == r);

					        if (!existing.Roles.Where(s => s.ID == r).Any())
					        {
						        existing.Roles.Add(role);
					        }
				        }
			        }

			        _dbContext.SaveChanges();

			        ActualizaEmpresaUsuario(existing, request.CompanyIds, oldUsername);

			        return Json(new { status = 0 });
		        }
		        else
		        {
			        var otherexisting = _dbContext.User.FirstOrDefault(x => x.Username == request.Username);
			        if (otherexisting != null)
			        {
				        return Json(new { status = 2 });
			        }
			        
			        var otherexistingpin = _dbContext.User.FirstOrDefault(x =>
				        x.ID != request.ID && (x.Pin.Trim().ToLower() == request.Pin.Trim().ToLower()));
			        if (otherexistingpin != null)
			        {
				        return Json(new { status = 3 });
			        }

			        existing = new Models.User();
			        existing.FullName = request.FullName;
			        existing.PhoneNumber = request.PhoneNumber;
			        existing.Username = request.Username;
			        existing.Email = request.Email;
			        existing.Password = request.Password;
			        existing.Pin = request.Pin;
			        existing.Address = request.Address;
			        existing.City = request.City;
			        existing.State = request.State;
			        existing.ZipCode = request.ZipCode;
			        existing.ProfileImage = request.ProfileImage;
			        existing.IsActive = request.IsActive;
			        existing.Roles = new List<Role>();
			        foreach (var r in request.RoleIds)
			        {
				        var role = _dbContext.Role.FirstOrDefault(s => s.ID == r);

				        if (!existing.Roles.Where(s => s.ID == r).Any())
				        {
					        existing.Roles.Add(role);
				        }


			        }

			        _dbContext.User.Add(existing);
			        _dbContext.SaveChanges();

			        ActualizaEmpresaUsuario(existing, request.CompanyIds);

			        return Json(new { status = 0 });
		        }

	        }
	        catch (Exception ex)
	        {
		        var m = ex;
	        }

            return Json(new { status = 1 });
        }

		private void ActualizaEmpresaUsuario(Models.User objUsuario, List<long> lstEmpresas, string oldUsername="")
		{
			var objCentral = new Services.CentralService();

            var username = objUsuario.Username;
            //if (!string.IsNullOrEmpty(oldUsername))
            //{
            //    username = oldUsername;
            //}
			username = username.Trim().ToLower();
            //var lstAllowedCompanies = objCentral.GetAllowedCompanies(username);
			var lstCompanies = objCentral.GetAllCompanies();

            var cookieRequest = Request.Cookies["db"];
            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddDays(1);

			//Recupero los datos del usuario y sus roles

			
            objUsuario = _dbContext.User.Include(s => s.Roles).ThenInclude(s => s.Permissions).FirstOrDefault(s => s.Username.Trim().ToLower() == username.ToLower());

            ModelsCentral.User existingUser = null;
            if (!string.IsNullOrEmpty(oldUsername))
            {
	            existingUser = _dbCentralContext.Users.FirstOrDefault(s => s.Username.Trim().ToLower() == oldUsername.ToLower());
            }
            else
            {
	            existingUser = _dbCentralContext.Users.FirstOrDefault(s => s.Username.Trim().ToLower() == username.ToLower());	
            }
            
			
            if (existingUser == null)
			{
				existingUser = new ModelsCentral.User();
				existingUser.Id = _dbCentralContext.Users.Max(m => m.Id) + 1;
                existingUser.Username = objUsuario.Username;
                existingUser.Password = Encriptacion.CreateHash(objUsuario.Password);
                existingUser.Pin = objUsuario.Pin;
                existingUser.IsActive = true;
                _dbCentralContext.Users.Add(existingUser);
                _dbCentralContext.SaveChanges();

				foreach (var company in lstEmpresas)
				{
					_dbCentralContext.UserCompanies.Add(new UserCompany()
					{
						Id = _dbCentralContext.UserCompanies.Max(m => m.Id) + 1,
						User = existingUser,
						CompanyId = company,
					});
				}

				_dbCentralContext.SaveChanges();

			}
			else
			{
                existingUser.Password = Encriptacion.CreateHash(objUsuario.Password);
                existingUser.Pin = objUsuario.Pin;
                existingUser.Username = objUsuario.Username;
                existingUser.IsActive = objUsuario.IsActive;
                _dbCentralContext.Users.Update(existingUser);
                _dbCentralContext.SaveChanges();

                var existCompanys = _dbCentralContext.UserCompanies.Where(s => s.UserId == existingUser.Id).ToList();
				var removeCompanies = existCompanys.Where(s => !lstEmpresas.Contains(s.CompanyId)).ToList();
				_dbCentralContext.UserCompanies.RemoveRange(removeCompanies);

				_dbCentralContext.SaveChanges();

				foreach (var company in lstEmpresas)
				{
					var exist = existCompanys.FirstOrDefault(s => s.CompanyId == company);
					if (exist == null)
					{
						_dbCentralContext.UserCompanies.Add(new UserCompany()
						{
							Id = _dbCentralContext.UserCompanies.Max(m => m.Id) + 1,
							User = existingUser,
							CompanyId = company,
						});
					}
				}

				_dbCentralContext.SaveChanges();
            }

   //         //Agregamos las nuevas
   //         foreach (var objEmpresaId in lstEmpresas)
			//{
			//	if (!lstAllowedCompanies.Select(d => d.Id).Where(d => d.Equals(objEmpresaId)).Any())
			//	{
   //                var objUserCompany = new ModelsCentral.UserCompany();
			//		objUserCompany.Id = _dbCentralContext.UserCompanies.Select(c => c.Id).Max() + 1;
   //                 objUserCompany.UserId = existingUser.Id;
			//		objUserCompany.CompanyId = objEmpresaId;

			//		_dbCentralContext.UserCompanies.Add(objUserCompany);
			//		_dbCentralContext.SaveChanges();

   //                 //Agregamos el usuario a la nueva base de datos                    

   //                 try
			//		{
			//			//Forzamos que apunte a la base de datos del company
			//			_dbContext = new AppDbContext();
   //                     _dbContext.DBForce = lstCompanies.Select(c => c).Where(c => c.Id == objUserCompany.CompanyId).FirstOrDefault().Database;
   //                 }
   //                 catch(Exception ex)
			//		{
			//			var m = ex;
			//		}

   //                 var objUsuarioAuxiliar = _dbContext.User.Include(s => s.Roles).FirstOrDefault(s => s.Username.ToLower() == objUsuario.Username.ToLower());
			//		Boolean bUsuarioExistia = true;


			//		if (objUsuarioAuxiliar == null)
			//		{
   //                     objUsuarioAuxiliar = new Models.User();
			//			bUsuarioExistia = false;
   //                 }

			//		objUsuarioAuxiliar.Username = objUsuario.Username;
   //                 objUsuarioAuxiliar.Password = objUsuario.Password;
   //                 objUsuarioAuxiliar.Email = objUsuario.Email;
   //                 objUsuarioAuxiliar.PhoneNumber = objUsuario.PhoneNumber;
   //                 objUsuarioAuxiliar.IsActive = objUsuario.IsActive;
   //                 objUsuarioAuxiliar.Type = objUsuario.Type;
   //                 objUsuarioAuxiliar.ProfileImage = objUsuario.ProfileImage;
   //                 objUsuarioAuxiliar.FullName = objUsuario.FullName;
   //                 objUsuarioAuxiliar.Address = objUsuario.Address;
   //                 objUsuarioAuxiliar.City = objUsuario.City;
   //                 objUsuarioAuxiliar.State = objUsuario.State;
   //                 objUsuarioAuxiliar.ZipCode = objUsuario.ZipCode;
   //                 objUsuarioAuxiliar.CreatedDate = objUsuario.CreatedDate;
   //                 objUsuarioAuxiliar.UpdatedDate = objUsuario.UpdatedDate;
   //                 objUsuarioAuxiliar.IsDeleted = objUsuario.IsDeleted;
   //                 objUsuarioAuxiliar.CreatedBy = objUsuario.CreatedBy;
   //                 objUsuarioAuxiliar.UpdatedBy = objUsuario.UpdatedBy;
   //                 objUsuarioAuxiliar.Pin = objUsuario.Pin;

			//		if (objUsuario.Roles.Any())
			//		{
   //                     objUsuarioAuxiliar.Roles = new List<Role>();
   //                     foreach (var objRole in objUsuario.Roles)
   //                     {
   //                         var role = _dbContext.Role.FirstOrDefault(s => s.ID == objRole.ID);

   //                         if (!objUsuarioAuxiliar.Roles.Where(s => s.ID == objRole.ID).Any())
   //                         {
   //                             objUsuarioAuxiliar.Roles.Add(role);
   //                         }
   //                     }                        
   //                 }

			//		if (!bUsuarioExistia)
			//		{
   //                     _dbContext.User.Add(objUsuarioAuxiliar);
   //                 }
					
   //                 _dbContext.SaveChanges();

   //             }
			//}

   //         //Eliminamos las que se deseleccionaron
   //         lstAllowedCompanies = objCentral.GetAllowedCompanies(objUsuario.Username);
   //         foreach (var objEmpresa in lstAllowedCompanies)
   //         {
   //             if (!lstEmpresas.Select(d => d).Where(d => d.Equals(objEmpresa.Id)).Any())
   //             {                    
   //                 var objUserCompany = _dbCentralContext.UserCompanies.Where(r=>r.UserId == objUsuarioCentral.Id && r.CompanyId == objEmpresa.Id).FirstOrDefault();
   //                 if (objUserCompany != null)
   //                 {
			//			_dbCentralContext.UserCompanies.Remove(objUserCompany);
   //                     _dbCentralContext.SaveChanges();
   //                 }

   //                 _dbContext = new AppDbContext();
   //                 _dbContext.DBForce = lstCompanies.Select(c => c).Where(c => c.Id == objEmpresa.Id).FirstOrDefault().Database;

   //                 var objUsuarioAuxiliar = _dbContext.User.FirstOrDefault(s => s.Username.ToLower() == objUsuario.Username.ToLower());

			//		if (objUsuarioAuxiliar != null)
			//		{
			//			objUsuarioAuxiliar.IsDeleted = true;
   //                     _dbContext.SaveChanges();

   //                 }
   //             }
   //         }

   //         //Volvemos a dejar la base de datos inicial
   //         _dbContext.DBForce = cookieRequest;         

        }

        [HttpPost]
        public JsonResult DeleteUser(long userId)
        {
            var existing = _dbContext.User.FirstOrDefault(x => x.ID == userId);
            if (existing != null)
            {
				existing.IsDeleted = true;
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }

        public async Task<IActionResult> Roles()
        {
            try
            {
                // Obtener todos los roles que no están eliminados junto con el conteo de usuarios asociados a cada rol
                var roles = await _dbContext.Role
                    .Where(s => !s.IsDeleted)
                                .Select(role => new RoleCardViewModel
                                {
                                    ID = role.ID,
                                    RoleName = role.RoleName,
                                    Priority = role.Priority,
                                    UserCount = _dbContext.User.Count(user => user.Roles.Any(r => r.RoleName == role.RoleName)),
                                    UserInfo = _dbContext.User
												.Where(user => user.Roles.Any(r => r.RoleName == role.RoleName))
												.Select(user => new RoleUserInfoViewModel
                                                {
                                                    UserName = user.FullName,
                                                    ImageUrl = user.ProfileImage
                                                })
                    							.ToList()
                                })
					.ToListAsync();
                //       .Select(role => new {
                //           role.ID,
                //           role.RoleName,
                //           role.Priority,
                //           UserCount = _dbContext.User.Count(user => user.Roles.Any(r => r.RoleName == role.RoleName)), // Contar usuarios por rol
                //           userImages = _dbContext.User
                //.Where(user => user.Roles.Any(r => r.RoleName == role.RoleName))
                //.Select(user => user.ProfileImage) // Seleccionar la imagen de perfil
                //.ToList()
                //       })
                //       .ToListAsync();

                ViewBag.Roles = roles;

            }
            catch (Exception ex)
            {
                // Manejo de errores, registrar el error
                Console.WriteLine(ex.Message);
                //return StatusCode(500, "Internal server error");
            }


            return View();
        }

		private List<PermissionGroupModel> GetPermissionGroup()
		{
			var permissions = _dbContext.Permissions.ToList();
			var result = new List<PermissionGroupModel>();
			foreach(var p in permissions)
			{
				var exist = result.FirstOrDefault(s => s.Name == p.Group);
				if (exist != null)
				{
					exist.Permissions.Add(new PermissionViewModel()
					{
						ID = p.ID,
						Name = p.DisplayValue,
						IsSelected = false
					});
				}
				else
				{
					var ngroup = new PermissionGroupModel { Name = p.Group, Order= p.Level };
					ngroup.Permissions.Add(new PermissionViewModel()
					{
						ID = p.ID,
						Order = p.GroupOrder,
						Name = p.DisplayValue,
						IsSelected = false
					});

					result.Add(ngroup);
				}
			}

			foreach(var p in result)
			{
				p.Permissions = p.Permissions.OrderBy(s => s.Order).ToList();
			}
			result = result.OrderBy(s=>s.Order).ToList();
			return result;
		}

		public IActionResult AddRole(long roleId = 0)
		{
			var pgroup = GetPermissionGroup();

			var model = new RoleViewModel();
			if (roleId > 0)
			{
				var role = _dbContext.Role.Include(s=>s.Permissions).FirstOrDefault(s=> s.ID == roleId);
				model.Role = role;

				foreach(var p in pgroup)
				{
					p.IsSelected = true;
					if (p.Permissions == null || p.Permissions.Count == 0) 
						p.IsSelected = false;
					else
					{
                        foreach (var pp in p.Permissions)
                        {
                            if (role.Permissions != null && role.Permissions.Any(s => s.ID == pp.ID))
                            {
                                pp.IsSelected = true;
                            }
                            else
                            {
                                p.IsSelected = false;
                            }
                        }
                    }
					
				}
				model.PermissionGroups = pgroup;
			}
			else
			{
				model.Role = new Role();
				model.PermissionGroups = pgroup;
			}

			return View(model);
		}

        [HttpPost]
        public async Task<IActionResult> GetRoles()
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
                var customerData = _dbContext.Role.Where(s=>!s.IsDeleted).ToList();

                ////Sorting
                //if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                //{
                //    customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                //}
                //////Search  
                //if (!string.IsNullOrEmpty(searchValue))
                //{
                //    customerData = customerData.Where(m => m.RoleName.Contains(searchValue));
                //}

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
        public async Task<IActionResult> GetRolesForCards()
        {
            try
            {
                // Obtener todos los roles que no están eliminados junto con el conteo de usuarios asociados a cada rol
                var roles = await _dbContext.Role
                    .Where(s => !s.IsDeleted)
                    .Select(role => new {
                        role.ID,
                        role.RoleName,
                        role.Priority,
                        UserCount = _dbContext.User.Count(user => user.Roles.Any(r => r.RoleName == role.RoleName)), // Contar usuarios por rol
                        userImages = _dbContext.User
            .				Where(user => user.Roles.Any(r => r.RoleName == role.RoleName))
							.Select(user => user.ProfileImage) // Seleccionar la imagen de perfil
							.ToList()
                    })
                    .ToListAsync();

                // Retornar los datos en formato JSON
                return Json(new { data = roles });
            }
            catch (Exception ex)
            {
                // Manejo de errores, registrar el error
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetUsersWithRoles()
        {
            try
            {
                // Obtener la lista de usuarios, filtrando aquellos que no están eliminados
                var userData = await _dbContext.User
                    .Where(u => !u.IsDeleted)
                    .Select(u => new
                    {
                        u.ID,
                        u.FullName, // Este es el nombre que deseas mostrar
                        u.IsActive, // Estado del usuario
                        RoleName = u.Roles.Select(r => r.RoleName).FirstOrDefault() // Selecciona el primer rol o maneja múltiples roles como prefieras
                    })
                    .ToListAsync();

                // Devolver la lista en formato JSON
                return Json(new { data = userData });
            }
            catch (Exception ex)
            {
                // Manejo de errores (puedes personalizar el manejo según tu necesidad)
                return Json(new { error = ex.Message });
            }
        }



        [HttpPost]
        public JsonResult EditRole([FromBody] RoleCreateModel request)
        {
            try
            {
                var existing = _dbContext.Role.Include(s => s.Permissions).FirstOrDefault(x => x.ID == request.ID);
                if (existing != null)
                {
                    var otherexisting = _dbContext.Role.FirstOrDefault(x => x.ID != request.ID && (x.RoleName == request.RoleName));
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    existing.RoleName = request.RoleName;
                    existing.Priority = request.Priority;
                    if (request.PermissionIds != null)
                    {
                        existing.Permissions = new List<Permission>();
                        foreach (var r in request.PermissionIds)
                        {
                            var permission = _dbContext.Permissions.FirstOrDefault(s => s.ID == r);
                            existing.Permissions.Add(permission);
                        }
                    }

                    _dbContext.SaveChanges();
                    return Json(new { status = 0 });
                }
                else
                {
                    var otherexisting = _dbContext.Role.FirstOrDefault(x => x.RoleName == request.RoleName);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }

                    existing = new Role();
                    existing.RoleName = request.RoleName;
                    existing.Priority = request.Priority;
                    if (request.PermissionIds != null)
                    {
                        existing.Permissions = new List<Permission>();
                        foreach (var r in request.PermissionIds)
                        {
                            var permission = _dbContext.Permissions.FirstOrDefault(s => s.ID == r);
                            existing.Permissions.Add(permission);
                        }
                    }

                    _dbContext.Role.Add(existing);
                    _dbContext.SaveChanges();
                    return Json(new { status = 0 });
                }

            }
            catch { }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult DeleteRole(long roleId)
        {
            var existing = _dbContext.User.FirstOrDefault(x => x.ID == roleId);
            if (existing != null)
            {
                existing.IsDeleted = true;
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }

		[HttpPost]
        public JsonResult GetCxCList(string customerName, long customerId = 0 )
        {
            var settingsCore = new SettingsCore(_userService, _dbContext, _context);

            try
            {
                List<OrderTransaction> cxc = settingsCore.GetCxCList(customerName, customerId);

				return Json(cxc);
                //if (customerId == 0)
                //{
                // string customerNameLower = customerName?.ToLower();

                // cxc = _dbContext.OrderTransactions
                //  .Include(ot => ot.Order)
                //  .Where(ot => ot.Order != null &&
                //               ot.Order.ClientName != null &&
                //               ot.Order.ClientName.ToLower() == customerNameLower &&
                //               ot.Method != null &&                                 
                //               ot.PaymentType != null &&
                //               ot.PaymentType.ToUpper() == "C X C")
                //  .Select(ot => new OrderTransaction
                //  {
                //   ID = ot.ID,
                //   Amount = ot.Amount,
                //   Method = ot.Method,
                //   PaymentDate = ot.PaymentDate,
                //   PaymentType = ot.PaymentType,
                //  })
                //  .ToList();
                //}
                //else
                //{
                // string customerNameLower = customerName?.ToLower();

                // cxc = _dbContext.OrderTransactions
                //  .Include(ot => ot.Order)
                //  .Where(ot => ot.Order != null &&
                //               ot.Order.CustomerId == customerId &&
                //               ot.Method != null &&                                 
                //               ot.PaymentType != null &&
                //               ot.PaymentType.ToUpper() == "C X C")
                //  .Select(ot => new OrderTransaction
                //  {
                //   ID = ot.ID,
                //   Amount = ot.Amount,
                //   Method = ot.Method,
                //   PaymentDate = ot.PaymentDate,
                //   PaymentType = ot.PaymentType,
                //  })
                //  .ToList();
                //}


                //   foreach (var order in cxc)
                //   {
                //       // Obtener órdenes asociadas al ReferenceId
                //       var associatedOrders = _dbContext.OrderTransactions
                //           .Where(ot => ot.ReferenceId == order.ID)
                //           .ToList();

                //       // Calcular la diferencia y almacenarla en la propiedad Difference
                //       order.TemporaryDifference = order.Amount - associatedOrders.Sum(ao => ao.Amount);
                //   }

                //   return Json(cxc.Where(s=>s.TemporaryDifference>0).ToList());
            }
            catch (Exception ex)
            {
                return Json(new { error = "Ocurrió un error al obtener los datos: " + ex.Message });
            }
        }


		[HttpPost]
		public JsonResult GetCxCList2(string from, string to, long cliente = 0, long orden = 0, decimal monto = 0)
		{
            var settingsCore = new SettingsCore(_userService, _dbContext, _context);

            List<OrderTransaction> cxcList = settingsCore.GetCxCList2(from, to, cliente, orden, monto);

            // Verificar si los parámetros recibidos están vacíos o nulos
            //DateTime fromDate = DateTime.MinValue;
            //DateTime toDate = DateTime.MaxValue;

            //if (!string.IsNullOrEmpty(from))
            //{
            //	fromDate = DateTime.ParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            //}

            //if (!string.IsNullOrEmpty(to))
            //{
            //	toDate = DateTime.ParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            //	toDate = toDate.AddDays(1).AddMinutes(-1);
            //}

            //// Filtrar la lista basada en los parámetros recibidos
            //var cxcList = _dbContext.OrderTransactions
            //	.Include(ot => ot.Order)
            //	.Where(ot => (orden == 0 || ot.ReferenceId == orden) &&
            //				 (cliente == 0 || ot.Order.CustomerId == cliente) &&
            //				 (fromDate == DateTime.MinValue || ot.Order.OrderTime >= fromDate) &&
            //				 (toDate == DateTime.MaxValue || ot.Order.OrderTime <= toDate) &&
            //				 (monto == 0 || ot.Amount >= monto))
            //	.ToList();

            //if (cxcList != null && cxcList.Any())
            //{
            //	foreach (var objCxc in cxcList)
            //	{
            //		objCxc.Amount = Math.Round(objCxc.Amount, 2, MidpointRounding.AwayFromZero);
            //	}
            //}

            return Json(cxcList);
		}

		[HttpPost]
		public JsonResult GetCxCList3(string customerName)
		{
			try
			{
				string customerNameLower = customerName?.ToLower();

				var cxc = _dbContext.OrderTransactions
					.Include(ot => ot.Order)
					.Where(ot => ot.Order != null &&
								 ot.Order.ClientName != null &&
								 ot.Order.ClientName.ToLower() == customerNameLower &&
								 ot.Method != null &&								 
								 ot.PaymentType != null &&
								 ot.PaymentType.ToUpper() == "C X C")
					.Select(ot => new OrderTransaction
					{
						ID = ot.ID,
						Amount = ot.Amount,
						Method = ot.Method,
						PaymentDate = ot.PaymentDate,
						PaymentType = ot.PaymentType,
						BeforeBalance = ot.BeforeBalance, // Asumiendo que este campo existe
						AfterBalance = ot.AfterBalance // Asumiendo que este campo existe
					})
					.ToList();

				foreach (var order in cxc)
				{
					// Obtener órdenes asociadas al ReferenceId
					var associatedOrders = _dbContext.OrderTransactions
						.Where(ot => ot.ReferenceId == order.ID)
						.ToList();

					// Calcular la diferencia y almacenarla en la propiedad Difference
					order.TemporaryDifference = associatedOrders.Sum(ao => ao.Amount);
                    order.AfterBalance = order.Amount - order.TemporaryDifference;
				}

				//Quitamos las que tengan en su balance 0

				List<OrderTransaction> lstFinal = new List<OrderTransaction>();
                foreach (var order in cxc)
                {
					if (order.AfterBalance > 0) {
						lstFinal.Add(order);

                    }
                }
                
                if (lstFinal != null && lstFinal.Any())
                {
	                foreach (var objCxc in lstFinal)
	                {
		                objCxc.Amount = Math.Round(objCxc.Amount, 2, MidpointRounding.AwayFromZero);
		                objCxc.AfterBalance = Math.Round(objCxc.AfterBalance, 2, MidpointRounding.AwayFromZero);
	                }
                }

                return Json(lstFinal);
			}
			catch (Exception ex)
			{
				return Json(new { error = "Ocurrió un error al obtener los datos: " + ex.Message });
			}
		}



		#region Denomination
		public IActionResult Denominations()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> GetDenominations()
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
				var customerData = (from s in _dbContext.Denominations
									orderby s.DisplayOrder
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
					customerData = customerData.Where(m => m.Name.Contains(searchValue)).OrderBy(s=>s.DisplayOrder);
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
		public JsonResult EditDenomination(Denomination request)
		{
			try
			{
				var existing = _dbContext.Denominations.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					var otherexisting = _dbContext.Denominations.FirstOrDefault(x => x.ID != request.ID && (x.Name == request.Name || x.Amount == request.Amount));
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					existing.Name = request.Name;
					existing.Amount = request.Amount;
					existing.IsActive = request.IsActive;
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}
				else
				{
					
					var otherexisting = _dbContext.Denominations.FirstOrDefault(x => x.Name == request.Name || x.Amount == request.Amount	);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}

					var denomications = _dbContext.Denominations.Where(s => s.IsActive).OrderBy(s=>s.DisplayOrder).ToList();
					if (denomications.Count == 0)
					{
						request.DisplayOrder = 1;
					}
					else
					{
						var last = denomications.Last();

						request.DisplayOrder = last.DisplayOrder + 1;
					}
					_dbContext.Denominations.Add(request);
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}

			}
			catch { }

			return Json(new { status = 1 });
		}

		[HttpPost]
		public JsonResult UpdateDenomicationOrder([FromBody] List<Denomination> denominations)
		{
			var dbdenomications = _dbContext.Denominations.ToList();
			foreach(var d in denominations)
			{
				var denomination = dbdenomications.FirstOrDefault(s => s.ID == d.ID);
				denomination.DisplayOrder = d.DisplayOrder;
			}
			_dbContext.SaveChanges();

			return Json(new {status = 0});
		}

		[HttpPost]
		public JsonResult DeleteDenomination(long denominationId)
		{
			var existing = _dbContext.Denominations.FirstOrDefault(x => x.ID == denominationId);
			if (existing != null)
			{
				_dbContext.Denominations.Remove(existing);
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}
		#endregion

		#region Vouchers
		public IActionResult Vouchers()
		{
			ViewBag.Taxes = _dbContext.Taxs.Where(s=>s.IsActive).ToList();
			return View();
		}

		public JsonResult GetActiveVoucherList()
		{
            var response = new VoucherListResponse();
            try
            {
                var settingsCore = new SettingsCore(_userService, _dbContext, _context);
                var voucher = settingsCore.GetActiveVoucherList();

                if (voucher != null)
                {
                    response.Valor = voucher;
                    response.Success = true;
                    return Json(voucher);
                }
                return Json(null);
            }
            catch (Exception ex)
            {
                response.Error = ex.Message;
                response.Success = false;
                return Json(response);
            }
        }

		[HttpPost]
		public async Task<IActionResult> GetVouchers()
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
				var customerData = (from s in _dbContext.Vouchers
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
					customerData = customerData.Where(m => m.Name.Contains(searchValue));
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
		public JsonResult EditVoucher(VoucherCreateViewModel request)
		{
			try
			{
				var existing = _dbContext.Vouchers.Include(s=>s.Taxes).FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					existing.Name = request.Name;
					existing.Class = request.Class;
					existing.Secuencia = request.Secuencia;
					existing.IsRequireRNC = request.IsRequireRNC;
					existing.IsActive = request.IsActive;
					if (existing.Taxes != null)
						existing.Taxes.Clear();
					else
						existing.Taxes = new List<Tax>();


					if (request.TaxeIDs != null)
					{
						foreach (var t in request.TaxeIDs)
						{
							var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == t);
							if (tax != null)
							{
								existing.Taxes.Add(tax);
							}
						}
					}

					if (request.IsPrimary)
					{
						var othervouchers = _dbContext.Vouchers.Where(s=>s.ID !=  request.ID).ToList();
						foreach(var t in othervouchers)
						{
							t.IsPrimary = false;
						}
						
						existing.IsPrimary = true;
					}

					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}
				else
				{
					existing = new Voucher();
					existing.Name = request.Name;
					existing.Class = request.Class;
					existing.Secuencia = request.Secuencia;
					existing.IsRequireRNC = request.IsRequireRNC;
					existing.IsActive = request.IsActive;
					existing.Taxes = new List<Tax>();
					
					if (request.TaxeIDs != null)
					{
						foreach (var t in request.TaxeIDs)
						{
							var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == t);
							if (tax != null)
							{
								existing.Taxes.Add(tax);
							}
						}
					}

					if (request.IsPrimary)
					{
						var othervouchers = _dbContext.Vouchers.ToList();
						foreach (var t in othervouchers)
						{
							t.IsPrimary = false;
						}

						existing.IsPrimary = true;
					}

					_dbContext.Vouchers.Add(existing);
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}

			}
			catch { }

			return Json(new { status = 1 });
		}

		[HttpPost]
		public JsonResult GetVoucher(long voucherId)
		{
			var existing = _dbContext.Vouchers.Include(s=>s.Taxes).FirstOrDefault(x => x.ID == voucherId);

			return Json(existing);
		}

		[HttpPost]
		public JsonResult DeleteVoucher(long voucherId)
		{
			var existing = _dbContext.Vouchers.FirstOrDefault(x => x.ID == voucherId);
			if (existing != null)
			{
				_dbContext.Vouchers.Remove(existing);
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}
		#endregion

		#region Reason
		public IActionResult Reasons()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> GetVoidReasons()
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
				var customerData = (from s in _dbContext.CancelReasons
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
					customerData = customerData.Where(m => m.Reason.Contains(searchValue));
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
		public JsonResult EditVoidReason(CancelReason request)
		{
			try
			{
				var existing = _dbContext.CancelReasons.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{					
					existing.Reason = request.Reason;
					existing.IsPrintOverrideChannel = request.IsPrintOverrideChannel;
					existing.IsReduceInventory = request.IsReduceInventory;
					existing.IsPrintAccount = request.IsPrintAccount;
					existing.Level = request.Level;
					existing.IsActive = request.IsActive;
					_dbContext.SaveChanges();
					return Json(new { status = 0, id = existing.ID });
				}
				else
				{
					
					_dbContext.CancelReasons.Add(request);
					_dbContext.SaveChanges();
					return Json(new { status = 0, id = request.ID });
				}

			}
			catch { }

			return Json(new { status = 1 });
		}

		[HttpPost]
		public JsonResult GetVoidReason(long reasonId)
		{
			var existing = _dbContext.CancelReasons.FirstOrDefault(x => x.ID == reasonId);
			
			return Json(existing);
		}

		[HttpPost]
		public JsonResult DeleteVoidReason(long reasonId)
		{
			var existing = _dbContext.CancelReasons.FirstOrDefault(x => x.ID == reasonId);
			if (existing != null)
			{
				_dbContext.CancelReasons.Remove(existing);
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}

        #endregion

        #region Preference
        public IActionResult Preferences()
		{
			var preference = new Preference();

			var stores = _dbContext.Preferences.ToList();
			if (stores.Count > 0)
			{
				preference = stores.First();
			}
			else
			{
				_dbContext.Preferences.Add(preference);
				_dbContext.SaveChanges();
			}

			return View(preference);
		}

        [HttpGet]
        public IActionResult GetPreference()
        {
            var preference = _dbContext.Preferences.FirstOrDefault();

            if (preference == null)
            {
                return NotFound(new { message = "No se encontró la configuración de la empresa." });
            }

            return Ok(new
            {
                Name = preference.Name,
                Company = preference.Company,
                Logo = preference.Logo, 
                RNC = preference.RNC,
                Email = preference.Email,
                Phone = preference.Phone,
                Address1 = preference.Address1,
                Address2 = preference.Address2,
                City = preference.City,
                State = preference.State,
                PostalCode = preference.PostalCode,
                Country = preference.Country,
                Currency = preference.Currency,
                SecondCurrency = preference.SecondCurrency,
                Tasa = preference.Tasa
            });
        }




        [HttpPost]
		public JsonResult SavePreference([FromBody] Preference model)
		{
			if (model != null)
			{
                var preference = _dbContext.Preferences.FirstOrDefault(s => s.ID == model.ID);

				preference.Name = model.Name;
				preference.RNC = model.RNC;
				preference.Company = model.Company;
				preference.Address1 = model.Address1;
				preference.Address2 = model.Address2;
				preference.City = model.City;
				preference.State = model.State;
				preference.PostalCode = model.PostalCode;
				preference.Country = model.Country;
				preference.Phone = model.Phone;
				preference.Email = model.Email;
				preference.Logo = model.Logo;
				preference.Currency = model.Currency;
				preference.SecondCurrency = model.SecondCurrency;
				preference.Tasa = model.Tasa;
				if (!string.IsNullOrEmpty(model.Currency))
				{
					AlfaHelper.Currency = model.Currency;
				}
				if (User.IsInRole("ADMIN"))
				{
                    preference.StationLimit = model.StationLimit;
                }
				
				_dbContext.SaveChanges();
				return Json( new {status = 0});
            }
			return Json(new { status = 1 });
		}
        #endregion

        #region Tax
        public IActionResult Taxrates()
		{
			return View();
		}
		[HttpPost]
		public JsonResult GetActiveTaxList(int type = 0)
		{
			var taxes = _dbContext.Taxs.Where(x => x.IsActive).ToList();
			if (type == 1)
			{
				taxes = taxes.Where(s=>s.IsInPurchaseOrder).ToList();
			}
            else if (type == 2)
            {
                taxes = taxes.Where(s => s.IsInArticle).ToList();
            }
            else if (type == 3)
            {
                taxes = taxes.Where(s => s.IsShipping).ToList();
            }
            return Json(taxes);	
		}
        [HttpPost]
		public async Task<IActionResult> GetTaxList()
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
				var customerData = (from s in _dbContext.Taxs							
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
					customerData = customerData.Where(m => m.TaxName.Contains(searchValue));
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
        public JsonResult GetTax(int id)
        {
            var tax = _dbContext.Taxs.FirstOrDefault(s => s.ID == id);
            return Json(tax);
        }

        [HttpPost]
		public JsonResult EditTax(Tax request)
		{
			try
			{
				var existing = _dbContext.Taxs.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					var otherexisting = _dbContext.Taxs.FirstOrDefault(x => x.ID != request.ID && x.TaxName == request.TaxName);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					existing.TaxName = request.TaxName;
					existing.IsInPurchaseOrder= request.IsInPurchaseOrder;
					existing.IsInArticle = request.IsInArticle;
					existing.IsShipping = request.IsShipping;
					existing.TaxValue = request.TaxValue;
					existing.IsToGoExclude = request.IsToGoExclude;
					existing.IsBarcodeExclude = request.IsBarcodeExclude;
					existing.IsKioskExclude = request.IsKioskExclude;
					existing.IsActive = request.IsActive;
					_dbContext.SaveChanges();
					return Json(new { status = 0, id = existing.ID });
				}
				else
				{
					var otherexisting = _dbContext.Taxs.FirstOrDefault(x => x.TaxName == request.TaxName);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					_dbContext.Taxs.Add(request);
					_dbContext.SaveChanges();
					return Json(new { status = 0, id = request.ID });
				}

			}
			catch { }
			
			return Json(new { status = 1 });
		}
		
		[HttpPost]
		public JsonResult DeleteTax(long taxId)
		{
			var existing = _dbContext.Taxs.FirstOrDefault(x => x.ID == taxId);
			if (existing != null)
			{
				_dbContext.Taxs.Remove(existing);
				_dbContext.SaveChanges();
			}
			
			return Json(new { status = 0 });
		}

        #endregion

        #region Propina
        public IActionResult Propinas()
        {
            return View();
        }
        [HttpPost]
        public JsonResult GetActivePropinaList(int type = 0)
        {
            var taxes = _dbContext.Taxs.Where(x => x.IsActive).ToList();
            if (type == 1)
            {
                taxes = taxes.Where(s => s.IsInPurchaseOrder).ToList();
            }
            else if (type == 2)
            {
                taxes = taxes.Where(s => s.IsInArticle).ToList();
            }
            else if (type == 3)
            {
                taxes = taxes.Where(s => s.IsShipping).ToList();
            }
            return Json(taxes);
        }
        [HttpPost]
        public async Task<IActionResult> GetPropinaList()
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
                var customerData = (from s in _dbContext.Propinas
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
                    customerData = customerData.Where(m => m.PropinaName.Contains(searchValue));
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
		public JsonResult GetPropina(int id)
		{
			var propina = _dbContext.Propinas.FirstOrDefault(s => s.ID == id);
			//var propina = _dbContext.Taxs.FirstOrDefault(s => s.ID == id);
            return Json(propina);
		}

		[HttpPost]
        public JsonResult EditPropina(Propina request)
        {
            try
            {
                var existing = _dbContext.Propinas.FirstOrDefault(x => x.ID == request.ID);
                if (existing != null)
                {
                    var otherexisting = _dbContext.Propinas.FirstOrDefault(x => x.ID != request.ID && x.PropinaName == request.PropinaName);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    existing.PropinaName = request.PropinaName;
                    existing.IsInPurchaseOrder = request.IsInPurchaseOrder;
                    existing.IsInArticle = request.IsInArticle;
                    existing.IsShipping = request.IsShipping;
                    existing.PropinaValue = request.PropinaValue;
                    existing.IsToGoExclude = request.IsToGoExclude;
                    existing.IsBarcodeExclude = request.IsBarcodeExclude;
                    existing.IsKioskExclude = request.IsKioskExclude;
                    existing.IsActive = request.IsActive;
                    _dbContext.SaveChanges();
                    return Json(new { status = 0, id = existing.ID });
                }
                else
                {
                    var otherexisting = _dbContext.Propinas.FirstOrDefault(x => x.PropinaName == request.PropinaName);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    _dbContext.Propinas.Add(request);
                    _dbContext.SaveChanges();
                    return Json(new { status = 0, id = request.ID });
                }

            }
            catch { }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult DeletePropina(long propinaId)
        {
            var existing = _dbContext.Propinas.FirstOrDefault(x => x.ID == propinaId);
            if (existing != null)
            {
                _dbContext.Propinas.Remove(existing);
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }

        #endregion

        #region Printers
        public IActionResult Printers()
		{
            var printers = _dbContext.t_impresoras.Select(s=>s.f_impresora).ToList();           

            ViewBag.Printers = printers;
			//ViewBag.Printers = _dbContext.PrinterChannels.Where(s => s.IsActive).ToList();

            return View();
		}

		// printer
		[HttpPost]
		public IActionResult GetPrinterList()
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
				var customerData = (from s in _dbContext.Printers

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
                    customerData = customerData.Where(m => m.Name.ToLower().Contains(searchValue) || m.Model.ToLower().Contains(searchValue) || m.PhysicalName.ToLower().Contains(searchValue));
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
		public JsonResult TestPrinter(TestPrinterModel model)
		{
			try
			{
				var pdocument = new System.Drawing.Printing.PrintDocument(); //evitar error de nombres con selenium
				pdocument.PrinterSettings.PrinterName = model.Printer;
				pdocument.DefaultPageSettings.PaperSize = new PaperSize();
                pdocument.PrintPage += new PrintPageEventHandler(outPrinter_PrintPage);
                pdocument.Print();

				return Json(new { status = 0 });
            }
			catch { }

			return Json(new { status = 1 }); ;
        }

        private void outPrinter_PrintPage(object sender, PrintPageEventArgs ev)
        {
            Font printFont = new Font("Arial", 10);
            PointF printPointF = new PointF(0, 0);
            string strToPrint = "ALFA POS\n --------------------------------";
            ev.Graphics.DrawString(strToPrint, printFont, Brushes.Black, printPointF);
        }

        [HttpPost]
		public JsonResult EditPrinter(Printer request)
		{
			try
			{
				var existing = _dbContext.Printers.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					var otherexisting = _dbContext.Printers.FirstOrDefault(x => x.ID != request.ID && x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					existing.Name = request.Name;
					existing.Model = request.Model;
					existing.PhysicalName = request.PhysicalName;
					existing.IsActive = request.IsActive;
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}
				else
				{
					var otherexisting = _dbContext.Printers.FirstOrDefault(x => x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					_dbContext.Printers.Add(request);
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}

			}
			catch { }

			return Json(new { status = 1 });
		}

		[HttpPost]
		public JsonResult DeletePrinter(long printerId)
		{
			var existing = _dbContext.Printers.FirstOrDefault(x => x.ID == printerId);
			if (existing != null)
			{
				_dbContext.Printers.Remove(existing);
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}
		//printer channel
		#endregion

		#region PrinterChannel
		public IActionResult PrinterChannels()
        {
			var printers = _dbContext.Printers.ToList();
            
            ViewBag.Printers = printers;
            return View();
        }
        [HttpPost]
        public IActionResult GetPrinterChannelList()
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

				var printers = _dbContext.Printers.Where(s=>s.IsActive).ToList();
                // Getting all Customer data  
                var customerData = _dbContext.PrinterChannels.OrderByDescending(s=>s.IsDefault).ToList();
				
                //Sorting
                //if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                //{
                //    result = result.OrderBy(sortColumn + " " + sortColumnDirection).;
                //}
                ////Search  
                //if (!string.IsNullOrEmpty(searchValue))
                //{
                //    searchValue = searchValue.Trim().ToLower();
                //    customerData = customerData.Where(m => m.Name.ToLower().Contains(searchValue) );
                //}

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
        public JsonResult EditPrinterChannel(PrinterChannel request)
        {
            try
            {
                var existing = _dbContext.PrinterChannels.FirstOrDefault(x => x.ID == request.ID);
                if (existing != null)
                {
                    var otherexisting = _dbContext.PrinterChannels.FirstOrDefault(x => x.ID != request.ID && x.Name == request.Name);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    existing.Name = request.Name;
					if (request.IsDefault)
					{
						var channels = _dbContext.PrinterChannels.ToList();
						foreach(var channel in channels)
						{
							channel.IsDefault = false;
						}
					}

					existing.IsDefault = request.IsDefault;
                    existing.IsActive = request.IsActive;
                    _dbContext.SaveChanges();
                    return Json(new { status = 0 });
                }
                else
                {
                    var otherexisting = _dbContext.PrinterChannels.FirstOrDefault(x => x.Name == request.Name);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    if (request.IsDefault)
                    {
                        var channels = _dbContext.PrinterChannels.ToList();
                        foreach (var channel in channels)
                        {
                            channel.IsDefault = false;
                        }
                    }

                    _dbContext.PrinterChannels.Add(request);
                    _dbContext.SaveChanges();
                    return Json(new { status = 0 });
                }

            }
            catch { }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult DeletePrinterChannel(long printerId)
        {
            var existing = _dbContext.PrinterChannels.FirstOrDefault(x => x.ID == printerId);
            if (existing != null)
            {
                _dbContext.PrinterChannels.Remove(existing);
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }
        #endregion

        #region Delivery Carriers

        public IActionResult DeliveryCarriers()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetDeliveryCarriers()
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
                var carrierData = (from s in _dbContext.DeliveryCarriers
								   where !s.IsDeleted
                                    select s);

                //Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    try
                    {
                        carrierData = carrierData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
                    catch { }
                }
                ////Search  
                if (!string.IsNullOrEmpty(searchValue))
                {
                    carrierData = carrierData.Where(m => m.Name.Contains(searchValue));
                }

                //total number of rows count   
                recordsTotal = carrierData.Count();
                //Paging   
                var data = carrierData.Skip(skip).ToList();
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
		public JsonResult GetDeliveryCarrier(int id)
        {
            var pmethod = _dbContext.DeliveryCarriers.FirstOrDefault(s => s.ID == id);
            return Json(pmethod);
        }

        public JsonResult GetActiveDeliveryCarrierList()
        {
            var vouchers = _dbContext.DeliveryCarriers.Where(s => s.IsActive && !s.IsDeleted).ToList();
            return Json(vouchers);
        }

        [HttpPost]
        public JsonResult EditDeliveryCarrier(DeliveryCarrier request)
        {
            try
            {
                var existing = _dbContext.DeliveryCarriers.FirstOrDefault(x => x.ID == request.ID);
                if (existing != null)
                {
                    var otherexisting = _dbContext.DeliveryCarriers.FirstOrDefault(x => x.ID != request.ID && (x.Name == request.Name));
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    existing.Name = request.Name;                    
                    existing.IsActive = request.IsActive;
                    _dbContext.SaveChanges();
                    return Json(new { status = 0 });
                }
                else
                {
                    var otherexisting = _dbContext.DeliveryCarriers.FirstOrDefault(x => x.Name == request.Name);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    _dbContext.DeliveryCarriers.Add(request);
                    _dbContext.SaveChanges();
                    return Json(new { status = 0 });
                }

            }
            catch { }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult DeleteDeliveryCarrier(long carrierId)
        {
            var existing = _dbContext.DeliveryCarriers.FirstOrDefault(x => x.ID == carrierId);
            if (existing != null)
            {
				existing.IsDeleted = true;
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }
        #endregion

        #region Delivery Zones

        public IActionResult DeliveryZones()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetDeliveryZones()
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
                var zoneData = (from s in _dbContext.DeliveryZones
								where s.IsDeleted == false
                                   select s);

                //Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    try
                    {
                        zoneData = zoneData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
                    catch { }
                }
                ////Search  
                if (!string.IsNullOrEmpty(searchValue))
                {
                    zoneData = zoneData.Where(m => m.Name.Contains(searchValue));
                }

                //total number of rows count   
                recordsTotal = zoneData.Count();
                //Paging   
                var data = zoneData.Skip(skip).ToList();
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
        public JsonResult GetDeliveryZone(int id)
        {
            var pmethod = _dbContext.DeliveryZones.FirstOrDefault(s => s.ID == id);
            return Json(pmethod);
        }

        public JsonResult GetActiveDeliveryZoneList()
        {
            var response = new DeliveryZoneResponse();
            try
            {
                var settingsCore = new SettingsCore(_userService, _dbContext, _context);
                var deliveryZoneList = settingsCore.GetActiveDeliveryZoneList();

                if (deliveryZoneList != null)
                {
                    response.Valor = deliveryZoneList;
                    response.Success = true;
                    return Json(deliveryZoneList);
                }
                return Json(null);
            }
            catch (Exception ex)
            {
                response.Error = ex.Message;
                response.Success = false;
                return Json(response);
            }
        }

        [HttpPost]
        public JsonResult EditDeliveryZone(DeliveryZone request)
        {
            try
            {
                var existing = _dbContext.DeliveryZones.FirstOrDefault(x => x.ID == request.ID);
                if (existing != null)
                {
                    var otherexisting = _dbContext.DeliveryZones.FirstOrDefault(x => x.ID != request.ID && (x.Name == request.Name));
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    existing.Name = request.Name;
                    existing.IsPrimary = request.IsPrimary;
                    existing.Cost = request.Cost;
					existing.Time = request.Time;
                    existing.IsActive = request.IsActive;
                    _dbContext.SaveChanges();
                    return Json(new { status = 0, id = request.ID });
                }
                else
                {
                    var otherexisting = _dbContext.DeliveryZones.FirstOrDefault(x => x.Name == request.Name);
                    if (otherexisting != null)
                    {
                        return Json(new { status = 2 });
                    }
                    _dbContext.DeliveryZones.Add(request);
                    _dbContext.SaveChanges();
                    return Json(new { status = 0, id = request.ID });
                }

            }
            catch { }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult DeleteDeliveryZone(long zoneId)
        {
            var existing = _dbContext.DeliveryZones.FirstOrDefault(x => x.ID == zoneId);
            if (existing != null)
            {
				existing.IsDeleted = true;
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }
        #endregion

        #region Work Day

        [HttpPost]
        public JsonResult SetDiaTrabajo(string fecha)
        {
            DateTime selectedDate = DateTime.Now;
            if (!string.IsNullOrEmpty(fecha))
            {
                try
                {
					var auxDate = DateTime.ParseExact(fecha, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                    selectedDate = new DateTime(auxDate.Year, auxDate.Month, auxDate.Day);
                }
                catch { }
            }

            if(selectedDate< (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(-1)) || selectedDate > (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(1))){
                return Json("selected date not valid, you can only select a day before or after the current one");
            }

            var stationID = int.Parse(GetCookieValue("StationID"));
            var objStation = _dbContext.Stations.Where(d => d.ID == stationID).FirstOrDefault();

            var bDayExists = _dbContext.WorkDay.Where(d => d.Day >= selectedDate && d.Day <= selectedDate.AddDays(1).AddMinutes(-1) && d.IDSucursal == objStation.IDSucursal).Any();

			if (bDayExists) {
                return Json("The selected date already exists, please select another date");
            }

            var objDay = new WorkDay();


            var settingsCore = new SettingsCore(_userService, _dbContext, _context);
            settingsCore.DeactivateWorkDay(stationID);

            objDay.Day = selectedDate;
            objDay.IsActive = true;
            objDay.IDSucursal = objStation.IDSucursal;
            _dbContext.WorkDay.Add(objDay);
            _dbContext.SaveChanges();

            return Json("OK");
        }

        [HttpPost]
        public JsonResult CloseDiaTrabajo()
        {
            var settingsCore = new SettingsCore(_userService, _dbContext, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            settingsCore.DeactivateWorkDay(stationID);
            
            return Json("OK");
        }

        public JsonResult GetDiaTrabajo()
        {
            //var stationID = int.Parse(GetCookieValue("StationID"));
            //var objStation = _dbContext.Stations.Where(d => d.ID == stationID).FirstOrDefault();

            //var objDay = _dbContext.WorkDay.Where(d=>d.IsActive==true && d.IDSucursal== objStation.IDSucursal).FirstOrDefault();

            //if (objDay == null)
            //{
            //    return Json(null);
            //}

            //DateTime dtNow = new DateTime(objDay.Day.Year, objDay.Day.Month, objDay.Day.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

            //return Json(dtNow.ToString("dd-MM-yyyy"));


            var settingsCore = new SettingsCore(_userService, _dbContext, _context);
            var dia = settingsCore.GetDia(int.Parse(GetCookieValue("StationID")));

            if (dia != null)
            {
                return Json(dia.Value.ToString("dd-MM-yyyy"));
            }
            return Json(null);
        }

   //     private void DeactivateWorkDay()
   //     {
   //         var stationID = int.Parse(GetCookieValue("StationID"));
   //         var objStation = _dbContext.Stations.Where(d => d.ID == stationID).FirstOrDefault();

   //         var lstDays = _dbContext.WorkDay.Where(d=>d.IsActive==true && d.IDSucursal==objStation.IDSucursal).ToList();

			//foreach (var objDay in lstDays) {
			//	objDay.IsActive = false;

   //         }

   //         _dbContext.SaveChanges();            
   //     }

        public string GetCookieValue(string key)
        {
            var cookieRequest = HttpContext.Request.Cookies[key];
            string cookieResponse = GetCookieValueFromResponse(HttpContext.Response, key);

            if (cookieResponse != null)
            {
                return (cookieResponse);
            }
            else
            {
                return (cookieRequest);
            }
        }

        string GetCookieValueFromResponse(HttpResponse response, string cookieName)
        {
            foreach (var headers in response.Headers.Values)
                foreach (var header in headers)
                    if (header.StartsWith($"{cookieName}="))
                    {
                        var p1 = header.IndexOf('=');
                        var p2 = header.IndexOf(';');
                        return header.Substring(p1 + 1, p2 - p1 - 1);
                    }
            return null;
        }

		#endregion


		#region Accounting

		[HttpPost]
		public JsonResult GetAllAccountDescription()
		{
			return Json(_dbContext.AccountDescriptions.ToList());
		}

		[HttpPost]
		public IActionResult GetAccountDescriptionList()
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
				var customerData = (from s in _dbContext.AccountDescriptions
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
		public JsonResult EditAccountDescription(AccountDescription request)
		{
			try
			{
				var existing = _dbContext.AccountDescriptions.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					var otherexisting = _dbContext.AccountDescriptions.FirstOrDefault(x => x.ID != request.ID && x.Name == request.Name);
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
					var otherexisting = _dbContext.AccountDescriptions.FirstOrDefault(x => x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					_dbContext.AccountDescriptions.Add(request);
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}
			}
			catch { }

			return Json(new { status = 1 });
		}
		[HttpPost]
		public JsonResult GetAllAccountType()
		{
			return Json(_dbContext.AccountTypes.ToList());
		}

		[HttpPost]
		public IActionResult GetAccountTypeList()
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
				var customerData = (from s in _dbContext.AccountTypes
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
		public JsonResult EditAccountType(AccountType request)
		{
			try
			{
				var existing = _dbContext.AccountTypes.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					var otherexisting = _dbContext.AccountTypes.FirstOrDefault(x => x.ID != request.ID && x.Name == request.Name);
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
					var otherexisting = _dbContext.AccountTypes.FirstOrDefault(x => x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					_dbContext.AccountTypes.Add(request);
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}
			}
			catch { }

			return Json(new { status = 1 });
		}

		[HttpPost]
		public IActionResult GetAccountList()
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
				var customerData = _dbContext.Accounts.Include(s=>s.AccountDescription).Include(s=>s.AccountType).Select(s=> new
				{
					ID = s.ID,
					Description = s.AccountDescription.Name,
					DescriptionID = s.AccountDescription.ID,
					TypeID = s.AccountType.ID,
					Type = s.AccountType.Name,
					s.Number,
					s.CreditOrDebit,
					s.IsActive,
					s.Defaults,
					CreditDebitStr = s.CreditOrDebit == 0? "Credit": "Debit",
				}).ToList();

				//Sorting
				//if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				//{
				//	try
				//	{
				//		customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
				//	}
				//	catch { }

				//}
				////Search  
				if (!string.IsNullOrEmpty(searchValue))
				{
					searchValue = searchValue.Trim().ToLower();
					customerData = customerData.Where(m => m.Number.ToLower().Contains(searchValue) || m.Description.ToLower().Contains(searchValue) || m.Type.ToLower().Contains(searchValue)).ToList();
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
		public JsonResult EditAccount(AccountCreateModel request)
		{
			try
			{
				var desc = _dbContext.AccountDescriptions.FirstOrDefault(x => x.ID == request.DescriptionID);
				if (desc == null)
				{
					return Json(new { status = 1 });
				}
				var type = _dbContext.AccountTypes.FirstOrDefault(x => x.ID == request.TypeID);
				if (desc == null)
				{
					return Json(new { status = 1 });
				}

				var existing = _dbContext.Accounts.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					var otherexisting = _dbContext.Accounts.FirstOrDefault(x => x.ID != request.ID && x.Number == request.Number);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					
					existing.AccountType = type;
					existing.CreditOrDebit = request.CreditOrDebit;
					existing.AccountDescription = desc;
					existing.Number = request.Number;
					existing.IsActive = request.IsActive;
					existing.Defaults = request.Defaults;
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}
				else
				{
					var otherexisting = _dbContext.Accounts.FirstOrDefault(x => x.Number == request.Number);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					existing = new Account();
					existing.AccountType = type;
					existing.CreditOrDebit = request.CreditOrDebit;
					existing.AccountDescription = desc;
					existing.Number = request.Number;
					existing.IsActive = request.IsActive;
					existing.Defaults = request.Defaults;
					_dbContext.Accounts.Add(existing);
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}
			}
			catch { }

			return Json(new { status = 1 });
		}

		[HttpPost]
		public JsonResult GetAccountItem(AccountItemGetModel model)
		{
			var items = _dbContext.AccountItems.Include(s=>s.Account).ThenInclude(s=>s.AccountDescription).Include(s=>s.Account).ThenInclude(s=>s.AccountType).Where(s=>s.ItemID == model.ItemID && s.Target == model.Target).OrderBy(s=>s.Order).ToList();

			if (model.ItemID == 0 && items.Count == 0)
			{
				var defaultAccounts = _dbContext.Accounts.Include(s=>s.AccountDescription).Include(s=>s.AccountType).Where(s => s.Defaults.Contains((int)model.Target)).ToList();
				var results = new List<AccountItem>();
				var order = 0;
                foreach (var a in defaultAccounts)
                {
					order++;
					results.Add(new AccountItem()
					{
						Order = order,
						Account = a,
						ItemID = 0,
						Target = model.Target,
					});
                }

				items = results;
            }

			return Json(items);
		}
        

        [HttpPost]
		public JsonResult SaveAccountItems(AccountItemModel model)
		{
			var items = _dbContext.AccountItems.Where(s => s.Target == model.Target && s.ItemID == model.ItemID).ToList() ;
			for(var i = 0; i < items.Count; i++)
			{
				var item = items[i];
				_dbContext.AccountItems.Remove(item);
			}
			if (model.Accounts != null)
			{
				foreach (var item in model.Accounts)
				{
					var account = _dbContext.Accounts.FirstOrDefault(s => s.ID == item.AccountID);
					var acctitem = new AccountItem()
					{
						Target = model.Target,
						ItemID = model.ItemID,
						Order = item.Order,
						Account = account
					};
					_dbContext.AccountItems.Add(acctitem);
				}

			}

			_dbContext.SaveChanges();

			return Json(new { status = 0 });	
		}

		#endregion


		#region Prepare Types

		public IActionResult PrepareTypes()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> GetPrepareTypes(bool justData = false) 
		{
			try
			{

				if (!justData) {
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
                    var pepareTypesData = (from s in _dbContext.PrepareTypes
                                       where !s.IsDeleted
                                       select s);

                    //Sorting
                    if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                    {
                        try
                        {
                            pepareTypesData = pepareTypesData.OrderBy(sortColumn + " " + sortColumnDirection);
                        }
                        catch { }
                    }
                    ////Search  
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        pepareTypesData = pepareTypesData.Where(m => m.Name.Contains(searchValue));
                    }

                    //total number of rows count   
                    recordsTotal = pepareTypesData.Count();
                    //Paging   
                    var data = pepareTypesData.Skip(skip).ToList();
                    if (pageSize != -1)
                    {
                        data = data.Take(pageSize).ToList();
                    }
                    //Returning Json Data  
                    return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
				}
				else {
                    // Getting all Customer data  
                    var pepareTypesData = (from s in _dbContext.PrepareTypes
                                       where !s.IsDeleted
                                       select s);

                    //Returning Json Data  
                    return Json(new { data = pepareTypesData.ToList() });
                }

                

			}
			catch (Exception ex)
			{
				throw;
			}
		}

		[HttpPost]
		public JsonResult GetPrepareType(int id)
		{
			var pmethod = _dbContext.PrepareTypes.FirstOrDefault(s => s.ID == id);
			return Json(pmethod);
		}

		public JsonResult GetPrepareTypesList()
		{
			var vouchers = _dbContext.PrepareTypes.Where(s => s.IsActive && !s.IsDeleted).ToList();
			return Json(vouchers);
		}

		[HttpPost]
		public JsonResult EditPrepareTypes(PrepareTypes request)
		{
			try
			{
				var existing = _dbContext.PrepareTypes.FirstOrDefault(x => x.ID == request.ID);
				if (existing != null)
				{
					var otherexisting = _dbContext.PrepareTypes.FirstOrDefault(x => x.ID != request.ID && (x.Name == request.Name));
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					existing.Name = request.Name;
					existing.IsActive = request.IsActive;
					existing.SinChofer = request.SinChofer;
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}
				else
				{
					var otherexisting = _dbContext.PrepareTypes.FirstOrDefault(x => x.Name == request.Name);
					if (otherexisting != null)
					{
						return Json(new { status = 2 });
					}
					_dbContext.PrepareTypes.Add(request);
					_dbContext.SaveChanges();
					return Json(new { status = 0 });
				}

			}
			catch { }

			return Json(new { status = 1 });
		}

		[HttpPost]
		public JsonResult DeletePrepareTypes(long carrierId)
		{
			var existing = _dbContext.PrepareTypes.FirstOrDefault(x => x.ID == carrierId);
			if (existing != null)
			{
				existing.IsDeleted = true;
				_dbContext.SaveChanges();
			}

			return Json(new { status = 0 });
		}

		[HttpPost]
		public JsonResult ObtenerDatosDGII(string RNC)
		{
            SettingsCore settingsCore = new SettingsCore(_userService, _dbContext, _context);

            DatosDGIIResponse responseDatos = settingsCore.ObtenerDatosDGII(RNC);

            return Json(new { isValid = responseDatos.isValid, compania = responseDatos.compania, nombre = responseDatos.nombre });
		}
        #endregion

    }

	public class DatosDGIIResponse
	{
		public bool isValid { get; set; }
		public string? nombre { get; set; }
		public string? compania { get; set; }
    }


    public class PrinterChannelModel : PrinterChannel
	{
		public string PrinterName { get; set; }
	}
	public class TestPrinterModel
	{
		public string Printer { get; set; }
	}

	public class AccountCreateModel
	{
		public long ID { get; set; }
		public long DescriptionID { get; set; }
		public long TypeID { get; set; }
		public string Number { get; set; }
		public int CreditOrDebit { get; set; }
		public bool IsActive { get; set; }
		public List<int> Defaults { get; set; }
	}

	public class AccountItemGetModel
	{
		public AccountingTarget Target { get; set; }
		public long ItemID { get; set; }
	}

	public class AccountItemModel
	{
		public AccountingTarget Target { get; set; }
		public long ItemID { get; set; }
		public List<AccountItemSubModel> Accounts { get; set; }
	}

	public class AccountItemSubModel
	{
		public long AccountID { get; set; }
		public int Order { get; set; }
	}
}
