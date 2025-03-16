using AuroraPOS.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AuroraPOS.Data;
using AuroraPOS.Services;
using AuroraPOS.Models;
using Microsoft.EntityFrameworkCore;
using static System.Collections.Specialized.BitVector32;
using System.Net.WebSockets;
using System.Linq.Dynamic.Core;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Emit;
using System;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Security;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using iText.StyledXmlParser.Node;
using Org.BouncyCastle.Ocsp;
using iText.Layout.Borders;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.X509;
using System.Globalization;
using iText.StyledXmlParser.Css.Resolve;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using AuroraPOS.ModelsCentral;
using SixLabors.ImageSharp;
using System.Text.Json;
//using FastReport;
using iText.StyledXmlParser.Jsoup.Nodes;
using System.Resources;
using Org.BouncyCastle.Asn1.Mozilla;
//using FastReport.Export.Dxf.Sections;
//using FastReport.Utils;
using iText.Layout.Element;
using System.Linq;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Asn1.Ocsp;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using Org.BouncyCastle.Asn1.X500;
using Microsoft.CodeAnalysis;
using AuroraPOS;
using AuroraPOS.Core;
using NPOI.HPSF;
using NPOI.SS.Formula.PTG;
using AuroraPOS.ModelsJWT;
using PuppeteerSharp;
using static AuroraPOS.Core.POSCore;

namespace AuroraPOS.Controllers
{
    [Authorize]
    public class POSController : BaseController
    {
		private readonly IUserService _userService;
		private readonly IPrintService _printService;
		private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        public POSController(IUserService userService, ExtendedAppDbContext dbContext, IPrintService printService, IHttpContextAccessor context)
		{
			_userService = userService;
			_dbContext = dbContext._context;
            _context = context;
            _printService = printService;
		}

		public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
		[Route("/POS/Login")]
        public IActionResult Login(long Station= 0)
        {

            int getStation=0; //= int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            if (GetCookieValue("StationID") != null) {
                getStation = int.Parse(GetCookieValue("StationID"));
            }

            if (Station > 0)
            {
                var store = _dbContext.Preferences.FirstOrDefault();
                ViewBag.Store = store;
                ViewBag.Station = Station;
            }
            else
            {
                if (getStation > 0)
                {
                    return RedirectToAction("Station");
                }
            }
            return View();
        }

		[AllowAnonymous]
		[Route("/POS/KitchenLogin")]
		public IActionResult KitchenLogin(long Kitchen = 0)
		{
			int getStation = 0; //= int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");

			if (GetCookieValue("KitchenID") != null)
			{
				getStation = int.Parse(GetCookieValue("KitchenID"));
			}

			if (Kitchen > 0)
			{
				ViewBag.Kitchen = Kitchen;
			}
			else
			{
				if (getStation > 0)
				{
					return RedirectToAction("Kitchen");
				}
			}
			return View();
		}

		[Route("/POS/Logout")]
		public async Task<IActionResult> Logout(string station)
		{            
            if (string.IsNullOrEmpty(station) && GetCookieValue("StationID") != null)
            {
                station = GetCookieValue("StationID");
            }

            await HttpContext.SignOutAsync();

            return RedirectToAction("Login", "POS", new { station });
        }

		[Route("/POS/KitchenLogout")]
		public async Task<IActionResult> KichenLogout(string kitchen)
		{
			if (string.IsNullOrEmpty(kitchen) && GetCookieValue("KitchenID") != null)
			{
				kitchen = GetCookieValue("KitchenID");
			}

			await HttpContext.SignOutAsync();

			return RedirectToAction("KitchenLogin", "POS", new { kitchen });
		}

		public IActionResult WorkDay()
        {
            return View();
        }

        public IActionResult Station(OrderType orderType, OrderMode mode = OrderMode.Standard, long orderId = 0, long areaObject = 0, int person = 0)
        {
			var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
			var station = _dbContext.Stations.Include(s=>s.Areas.Where(t=>!t.IsDeleted)).ThenInclude(s=>s.AreaObjects.Where(s=>s.ObjectType == AreaObjectType.Table && !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);
            ViewBag.Sucursal = station.IDSucursal;
			var order = new Order();
			if (station == null)
			{
				return RedirectToAction("/POS/Login");
			}

            if (station.SalesMode == SalesMode.Barcode)
            {
                return RedirectToAction("/POS/Barcode");
            }
            else if (station.SalesMode == SalesMode.Kiosk)
            {
                return Redirect("/POS/Kiosk");
            }

            ViewBag.StationName = HttpContext.Session.GetString("StationName");

			var denominations = _dbContext.Denominations.OrderByDescending(s => s.Amount).ToList();
			ViewBag.Denominations = denominations;

			var paymentMethods = _dbContext.PaymentMethods.Where(s => s.IsActive).ToList();
			ViewBag.PaymentMethods = paymentMethods;

            ViewBag.Station = station.Name;
            ViewBag.Username = User.Identity.GetUserName();

            ViewBag.OtherUsers = _dbContext.User.Where(s=>s.Username != User.Identity.GetUserName()).ToList();
            ViewBag.Areas = station.Areas;

            ViewBag.ShowExpectedPayment = PermissionChecker("Permission.POS.ShowExpectedPayment");
            ViewBag.Branchs = _dbContext.t_sucursal.ToList();

            var current = _dbContext.Orders.Include(s => s.Table).FirstOrDefault(s => s.ID == orderId);
			if (current != null)
			{
				order = current;
				var name = HttpContext.User.Identity.GetUserName();
				if (order.WaiterName != name)
				{
					var claims = User.Claims.Where(x => x.Type == "Permission" && x.Value == "Permission.POS.OtherOrder" &&
															x.Issuer == "LOCAL AUTHORITY");
					if (!claims.Any())
					{
						return RedirectToAction("Station", new { error = "Permission" });
					}
				}

		
			}
			else
			{
				var user = HttpContext.User.Identity.GetUserName();
				order.Station = station;
				order.WaiterName = user;
				order.OrderMode = OrderMode.Standard;
				order.Status = OrderStatus.Temp;
				var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.IsPrimary);
				order.ComprobantesID = voucher.ID;
				if (orderType == OrderType.DiningRoom && areaObject > 0)
				{
					var table = _dbContext.AreaObjects.Include(s => s.Area).ThenInclude(s => s.AreaObjects.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == areaObject);
					if (table != null)
					{
						order.Table = table;
						order.Area = table.Area;

						ViewBag.AnotherTables = table.Area.AreaObjects.Where(s => s.ObjectType == AreaObjectType.Table && s.IsActive && s.ID != areaObject).ToList();
					}

					order.Person = person;
				}


				if (orderType == OrderType.Delivery || orderType == OrderType.FastExpress || orderType == OrderType.TakeAway)
				{
					order.Person = person;
				}


				if (orderType != OrderType.DiningRoom)
				{
					order.OrderMode = OrderMode.Standard;
				}

				_dbContext.Orders.Add(order);
				_dbContext.SaveChanges();

				HttpContext.Session.SetInt32("CurrentOrderID", (int)order.ID);
			}

			return View(order);
        }

		public IActionResult Kitchen()
		{
			var kitchenID = int.Parse(GetCookieValue("KitchenID")); // HttpContext.Session.GetInt32("StationID");
			var kitchen = _dbContext.Kitchen.FirstOrDefault(s => s.ID == kitchenID);
			if (kitchen == null)
			{
				return RedirectToAction("KitchenLogin");
			}
			
			ViewBag.StationName = HttpContext.Session.GetString("StationName");

			ViewBag.Kitchen = kitchen.Name;
			ViewBag.Username = User.Identity.GetName();
            var printerChannels = _dbContext.PrinterChannels.Where(s => s.IsActive).ToList();
            ViewBag.PrinterChannels = printerChannels;
            return View();
		}

		[HttpPost]
		public JsonResult GetArea(long areaID)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var area = objPOSCore.GetArea(areaID,Request.Cookies["db"]);
            return Json( new { area });
            
            /*  ------- Codigo Original --------
            
			var area = _dbContext.Areas.Include(s => s.AreaObjects.Where(s=>!s.IsDeleted)).FirstOrDefault(s => s.ID == areaID);

            //Obtenemos la URL de la imagen del archivo            
            //string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "area", area.ID.ToString() + ".png");
            string pathFile = Environment.CurrentDirectory + "/wwwroot" + "/localfiles/" + Request.Cookies["db"] + "/area/" + area.ID.ToString() + ".png";
            var request = _context.HttpContext.Request;
            var _baseURL = $"https://{request.Host}";
            if (System.IO.File.Exists(pathFile))
            {
                var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                //area.BackImage = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "area", area.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                area.BackImage = _baseURL + "/localfiles/" + Request.Cookies["db"] + "/area/" + area.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second;
            }
            else
            {
                //area.BackImage = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "area", "empty.png");
                area.BackImage = _baseURL + "/localfiles/" + Request.Cookies["db"] + "/area/" + "empty.png";
            }

            //Obtenemos las urls de las imagenes            
            if (area.AreaObjects != null && area.AreaObjects.Any())
            {
                foreach (var item in area.AreaObjects)
                {
                    pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "areaobject", item.ID.ToString() + ".png");
                    if (System.IO.File.Exists(pathFile))
                    {
                        var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                        item.BackImage = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                    }
                    else
                    {
                        item.BackImage = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", "empty.png");
                    }
                }
            }

            return Json( new { area });
            */
		}

		[HttpPost]
		public JsonResult GetAreasInStation()
		{
			var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");

			var station = _dbContext.Stations.Include(s=>s.Areas.Where(s=>!s.IsDeleted)).FirstOrDefault(s=>s.ID == stationID);

            //Obtenemos las urls de las imagenes
            //         var request = _context.HttpContext.Request;
            //         var _baseURL = $"https://{request.Host}";
            //         if (station.Areas != null && station.Areas.Any())
            //         {
            //             foreach (var item in station.Areas)
            //             {
            //                 var pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "area", item.ID.ToString() + ".png");
            //                 if (System.IO.File.Exists(pathFile))
            //                 {
            //                     var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
            //                     item.BackImage = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "area", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
            //                 }
            //                 else
            //                 {
            //                     item.BackImage = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", "empty.png");
            //                 }
            //             }
            //         }

            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var areas = objPOSCore.GetAreasInStation(station, Request.Cookies["db"]);

            if (station == null)
			{
				return Json("");
			}

			return Json(areas);
		}
				
		[HttpPost]
		public JsonResult GetAreaObjectsInArea(long areaID)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            try
            {
				var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
                var db = Request.Cookies["db"];

                var req = new AreaObjectsInAreaRequest();
                req.StationId = stationID;
                req.AreaId = areaID;
                req.Db = db;

                AreaObjects resultado = objPOSCore.GetAreaObjectsInArea(req);

                return Json(new { objects = resultado.objects, orders = resultado.orders, resultado.holditems });


                //            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
                //            var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);
                //            var area = _dbContext.Areas.Include(s => s.AreaObjects.Where(s => !s.IsDeleted)).AsNoTracking().FirstOrDefault(s => s.ID == areaID);

                //            //Obtenemos las urls de las imagenes
                //            var request = _context.HttpContext.Request;
                //            var _baseURL = $"https://{request.Host}";
                //            if (area.AreaObjects != null && area.AreaObjects.Any())
                //            {
                //                foreach (var item in area.AreaObjects)
                //                {
                //                    //string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "areaobject", item.ID.ToString() + ".png");
                //                    string pathFile = Environment.CurrentDirectory + "/wwwroot" + "/localfiles/" + Request.Cookies["db"] + "/areaobject/" + item.ID.ToString() + ".png";
                //                    if (System.IO.File.Exists(pathFile))
                //                    {
                //                        var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                //                        //item.BackImage = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                //                        item.BackImage = _baseURL + "/localfiles/"  + Request.Cookies["db"] + "/areaobject/" + item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second;
                //                    }
                //                    else
                //                    {
                //                        item.BackImage = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", "empty.png");
                //                    }
                //                }
                //            }

                //            var orders = _dbContext.Orders.Include(s => s.Table).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).ThenInclude(s => s.ServingSizes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).ThenInclude(s => s.Answer).Where(s => s.Area == area && s.OrderType == OrderType.DiningRoom && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved).ToList();
                //var result = new List<StationOrderModel>();
                //var holditems = new List<OrderHoldModel>();


                //foreach (var order in orders)
                //{
                //	var kichenItems = new List<OrderItem>();
                //	foreach (var item in order.Items)
                //	{
                //		if (item.Status == OrderItemStatus.HoldAutomatic)
                //		{
                //			var time = item.HoldTime;
                //			if (DateTime.Now > time)
                //			{
                //				item.Status = OrderItemStatus.Kitchen;
                //				SendKitchenItem(order.ID, item.ID);
                //				kichenItems.Add(item);
                //				if (item.Product.InventoryCountDownActive)
                //				{
                //					item.Product.InventoryCount -= item.Qty;

                //				}
                //				SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType);
                //				foreach (var q in item.Questions)
                //				{
                //					if (!q.IsActive) continue;
                //					var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                //					SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType);
                //				}
                //			}
                //			else
                //			{
                //				var diff = (time - DateTime.Now).TotalMinutes;
                //				var val = Math.Ceiling(diff) + 1;
                //				var exist = holditems.FirstOrDefault(s => s.AreaId == order.Table.ID);
                //				if (exist != null)
                //				{
                //					if (exist.HoldMinutes < (int)val)
                //					{
                //						exist.HoldMinutes = (int)val;
                //					}
                //				}
                //				else
                //				{
                //					exist = new OrderHoldModel()
                //					{
                //						OrderId = order.ID,
                //						AreaId = order.Table.ID,
                //						HoldMinutes = (int)val
                //					};
                //					holditems.Add(exist);
                //				}
                //			}
                //		}

                //	}
                //	var time1 = (int)(DateTime.Now - order.OrderTime).TotalMinutes;
                //	if (kichenItems.Count > 0)
                //	{
                //		_printService.PrintKitchenItems(stationID, order.ID, kichenItems, Request.Cookies["db"]);
                //	}
                //	result.Add(new StationOrderModel()
                //	{
                //		ID = order.ID,
                //		AreaId = order.Table.ID,
                //		OrderTime = time1,
                //		SubTotal = order.TotalPrice,
                //		WaiterName = order.WaiterName,
                //		IsDivide = order.OrderMode == OrderMode.Divide,
                //		ItemCount = order.Items.Count

                //	}); ;
                //	var hold = order.Items.FirstOrDefault(s => s.Status == OrderItemStatus.HoldManually || s.Status == OrderItemStatus.HoldAutomatic);
                //	if (hold == null && order.Status == OrderStatus.Hold)
                //	{
                //		order.Status = OrderStatus.Pending;
                //		_dbContext.SaveChanges();
                //	}
                //}

                ////borramos la imagen de la area
                //foreach (var objAreaObject in area.AreaObjects)
                //{
                //	objAreaObject.Area.BackImage = "";

                //	var reservation = _dbContext.Reservations.Where(s => s.TableID == objAreaObject.ID && s.Status == ReservationStatus.Open).ToList();

                //	foreach (var r in reservation)
                //	{
                //		var st = r.ReservationTime.AddMinutes(-30);
                //		var en = r.ReservationTime.AddHours((double)r.Duration);

                //		if (DateTime.Now >= st && DateTime.Now <= en)
                //		{
                //			objAreaObject.BackColor = "#AF69EF";
                //		}
                //	}
                //}

                //return Json(new { objects = area.AreaObjects, orders = result, holditems });
            }
            catch (Exception ex)
            {

            }
            return Json("");
		}

        [HttpPost]
        public JsonResult GetAreaObjectsInStation()
        {
            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).ThenInclude(s=>s.AreaObjects).FirstOrDefault(s => s.ID == stationID);
            
            return Json(new { areas = station.Areas });
        }

        [HttpPost]
        public JsonResult UpdateHoldStatus(long orderId)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            var stationID = int.Parse(GetCookieValue("StationID"));
            var order = _dbContext.Orders.Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).FirstOrDefault(s => s.ID == orderId);

            var kichenItems = new List<OrderItem>();
            bool updated = false;
            foreach (var item in order.Items)
            {
                if (item.Status == OrderItemStatus.HoldAutomatic)
                {
                    var time = item.HoldTime;
                    if (DateTime.Now > time)
                    {
                        item.Status = OrderItemStatus.Kitchen;
                        objPOSCore.SendKitchenItem(order.ID, item.ID);

                        kichenItems.Add(item);
                        if (item.Product.InventoryCountDownActive)
                        {
                            item.Product.InventoryCount -= item.Qty;

                        }
                        objPOSCore.SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, stationID);
                        foreach (var q in item.Questions)
                        {
                            if (!q.IsActive) continue;
                            var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                            objPOSCore.SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, stationID);
                        }
                        updated = true;
                    }
                }
            }
            if (kichenItems.Count > 0)
                _printService.PrintKitchenItems(stationID, order.ID, kichenItems, Request.Cookies["db"]);
            var hold = order.Items.FirstOrDefault(s => s.Status == OrderItemStatus.HoldManually || s.Status == OrderItemStatus.HoldAutomatic);
            if (hold == null && order.Status == OrderStatus.Hold)
            {
                order.Status = OrderStatus.Pending;
                _dbContext.SaveChanges();
            }
                
            return Json(new { status = 0, updated });
        }

        public IActionResult Barcode(long orderId = 0)
        {
            var denominations = _dbContext.Denominations.OrderByDescending(s => s.Amount).ToList();
            ViewBag.Denominations = denominations;

            var paymentMethods = _dbContext.PaymentMethods.Where(s => s.IsActive).ToList();
            ViewBag.PaymentMethods = paymentMethods;

            var reasons = _dbContext.CancelReasons.ToList();
            ViewBag.CancelReasons = reasons;

            var products = _dbContext.Products.Where(s => s.IsActive).ToList();
            ViewBag.Products = products;

            ViewBag.Discounts = _dbContext.Discounts.Where(s => s.IsActive && !s.IsDeleted).ToList();

            var order = new Order();
            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);
            ViewBag.StationID = stationID;
            if (station == null)
                return RedirectToAction("Login");

            if (station.SalesMode == SalesMode.Restaurant)
            {
                return RedirectToAction("Station");
            }

            var user = HttpContext.User.Identity.GetUserName();

            var current = _dbContext.Orders.Include(s => s.Table).FirstOrDefault(s =>s.OrderType == OrderType.Barcode && s.ID == orderId);
            if (current == null)
            {
                
                order.Station = station;
                order.WaiterName = user;
                order.OrderMode = OrderMode.Invoice;
                order.OrderType = OrderType.Barcode;
                order.Status = OrderStatus.Temp;
                var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.IsPrimary);
                order.ComprobantesID = voucher.ID;
                order.ComprobanteName = voucher.Name;

                _dbContext.Orders.Add(order);
                _dbContext.SaveChanges();

                HttpContext.Session.SetInt32("CurrentOrderID", (int)order.ID);
            }
            else
            {
                HttpContext.Session.SetInt32("CurrentOrderID", (int)orderId);
                order = current;
            }
            return View(order);
        }

		public IActionResult Sales(OrderType orderType, OrderMode mode = OrderMode.Standard, long orderId = 0, long areaObject = 0, int person = 0, string selectedItems = "")
		{

            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            var order = new Order();
            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID); 
            var products = _dbContext.Products.Where(s => s.IsActive).ToList();
            ViewBag.Products = products;
			// Obtener los IDs de los elementos seleccionados
			var selectedIds = JsonConvert.DeserializeObject<List<long>>(selectedItems);

			// Obtener las transacciones de la base de datos que coincidan con los IDs seleccionados
			var selectedTransactions = _dbContext.OrderTransactions.Where(t => selectedIds.Contains(t.ID)).ToList();

			ViewBag.SelectedItems = selectedTransactions;


			if (station == null)
				return RedirectToAction("Login");

            if (station.SalesMode == SalesMode.Kiosk)
            {
                return Redirect("/POS/Kiosk?orderId=" + orderId);
            }
            ViewBag.AnotherTables = new List<AreaObject>();
            ViewBag.AnotherAreas = new List<Area>();
            ViewBag.DiscountType = "";
            var current = GetOrder(orderId);
			if (current != null) 
			{
				order = current;
                // checkout
                if (order.PaymentStatus == PaymentStatus.Partly && order.OrderMode != OrderMode.Divide && order.OrderMode != OrderMode.Seat)
                {
                    return Redirect("/POS/Checkout?orderId=" + orderId);
                }
                if (order.OrderMode == OrderMode.Conduce)
                {
                    return RedirectToAction("Station");
                }
                var name = HttpContext.User.Identity.GetUserName();
                if (order.WaiterName != name)
                {
                    var claims = User.Claims.Where(x => x.Type == "Permission" && x.Value == "Permission.POS.OtherOrder" &&
                                                            x.Issuer == "LOCAL AUTHORITY");
                    if (!claims.Any())
                    {
                        return RedirectToAction("Station", new { error = "Permission" });
                    }
				}

                if (orderType == OrderType.DiningRoom && areaObject > 0)
                {
                    var table = _dbContext.AreaObjects.Include(s => s.Area).ThenInclude(s => s.AreaObjects.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == areaObject);                    
                    if (table != null)
                    {                        
                        ViewBag.AnotherTables = table.Area.AreaObjects.Where(s => s.ObjectType == AreaObjectType.Table && s.IsActive && s.ID != areaObject).ToList();
                        ViewBag.AnotherAreas = station.Areas.Where(s => s.IsActive && s.ID != table.Area.ID).ToList();
                    }
                }

                if (order.Discounts != null && order.Discounts.Count > 0)
                {
                    ViewBag.DiscountType = "order";
                }
                foreach(var item in order.Items)
                {
                    if (item.Discounts != null && item.Discounts.Count > 0)
                    {
                        ViewBag.DiscountType = "item";
                    }
                }

            }
			else
			{
				var user = HttpContext.User.Identity.GetUserName();
				order.Station = station;
                order.WaiterName = user;
				order.OrderMode = OrderMode.Standard;
				order.OrderType = orderType;
				order.Status = OrderStatus.Temp;
                var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.IsPrimary);
                order.ComprobantesID = voucher.ID;                
                if (orderType == OrderType.DiningRoom && areaObject > 0)
                {
					var table = _dbContext.AreaObjects.Include(s=>s.Area).ThenInclude(s=>s.AreaObjects.Where(s=>!s.IsDeleted)).FirstOrDefault(s => s.ID == areaObject);
					if (table != null)
					{
                        order.Table = table;
						order.Area = table.Area;

						ViewBag.AnotherTables = table.Area.AreaObjects.Where(s=>s.ObjectType == AreaObjectType.Table && s.IsActive && s.ID != areaObject).ToList();
                        ViewBag.AnotherAreas = station.Areas.Where(s => s.IsActive && s.ID != table.Area.ID).ToList(); 
                    }

                    order.Person = person;
                }


                if (orderType == OrderType.Delivery || orderType == OrderType.FastExpress || orderType == OrderType.TakeAway)
                {
                    order.Person = person;
                }


                if (orderType != OrderType.DiningRoom)
                {
                    order.OrderMode = OrderMode.Standard;
                }

				_dbContext.Orders.Add(order);
				_dbContext.SaveChanges();

                order.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                _dbContext.SaveChanges();

                HttpContext.Session.SetInt32("CurrentOrderID", (int)order.ID);
            }

            if(orderType == OrderType.Delivery)
            {
                bool deliveryExists = _dbContext.Deliverys.Include(s => s.Order).Where(s => s.IsActive).Where(s=>s.Order.ID == order.ID).Any();                

                if (!deliveryExists)
                {
                    var delivery = new Delivery();
                    delivery.Order = order;
                    delivery.Status = StatusEnum.Nuevo;
                    delivery.StatusUpdated = DateTime.Now;
                    delivery.DeliveryTime = DateTime.Now;                    

                    _dbContext.Deliverys.Add(delivery);

                    if(station.PrepareTypeDefault.HasValue && station.PrepareTypeDefault > 0) {
                        order.PrepareTypeID = station.PrepareTypeDefault.Value; //Para llevar
                    }
                    else {
                        order.PrepareTypeID = 2; //Para llevar
                    }

                    
                    var prepareType = _dbContext.PrepareTypes.FirstOrDefault(s => s.ID == order.PrepareTypeID);
                    order.PrepareType = prepareType; //Para llevar

                    _dbContext.SaveChanges();
                }
            }

			var reasons = _dbContext.CancelReasons.ToList();
			ViewBag.CancelReasons = reasons;

            ViewBag.Discounts = _dbContext.Discounts.Where(s => s.IsActive && !s.IsDeleted).ToList();

            return View(order);
		}

        public IActionResult Kiosk(int orderId = 0)
        {
            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);
            var products = _dbContext.Products.Where(s => s.IsActive).ToList();
            ViewBag.Sucursal = station.IDSucursal;
            ViewBag.Products = products;

            var denominations = _dbContext.Denominations.OrderByDescending(s => s.Amount).ToList();
            ViewBag.Denominations = denominations;

            var paymentMethods = _dbContext.PaymentMethods.Where(s => s.IsActive).ToList();
            ViewBag.PaymentMethods = paymentMethods;

            ViewBag.ShowExpectedPayment = PermissionChecker("Permission.POS.ShowExpectedPayment");
            ViewBag.Branchs = _dbContext.t_sucursal.ToList();

            if (station == null)
                return RedirectToAction("Login");

            ViewBag.OtherUsers = _dbContext.User.Where(s => s.Username != User.Identity.GetUserName()).ToList();

            Order order = null;
            var userName = HttpContext.User.Identity.GetUserName();
            
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            order = objPOSCore.Kiosk(station, userName,orderId);
            HttpContext.Session.SetInt32("CurrentOrderID", (int)order.ID);

            if (orderId > 0)
            {
                if (order.PaymentStatus == PaymentStatus.Partly)
                {
                    return Redirect("/POS/Checkout?orderId=" + orderId);
                }
            }

            /*
            if (orderId > 0)
            {
                order = _dbContext.Orders.FirstOrDefault(s => s.ID == orderId);

                if (order.PaymentStatus == PaymentStatus.Partly )
                {
                    return Redirect("/POS/Checkout?orderId=" + orderId);
                }
                var prepareType = _dbContext.PrepareTypes.FirstOrDefault(s => s.ID == order.PrepareTypeID);
                order.PrepareType = prepareType; //Kiosk
                HttpContext.Session.SetInt32("CurrentOrderID", (int)orderId);
            }
            else
            {
                order = new Order();

                {
                    var user = HttpContext.User.Identity.GetUserName();
                    order.Station = station;
                    order.WaiterName = user;
                    order.OrderMode = OrderMode.Standard;
                    order.OrderType = OrderType.Delivery;
                    order.Status = OrderStatus.Temp;

                    if (station.PrepareTypeDefault.HasValue && station.PrepareTypeDefault > 0)
                    {
                        order.PrepareTypeID = station.PrepareTypeDefault.Value;
                    }
                    else
                    {
                        order.PrepareTypeID = 4; //Kiosk
                    }

                    var prepareType = _dbContext.PrepareTypes.FirstOrDefault(s => s.ID == order.PrepareTypeID);
                    order.PrepareType = prepareType;

                    var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.IsPrimary);
                    order.ComprobantesID = voucher.ID;

                    _dbContext.Orders.Add(order);

                    var delivery = new Delivery();
                    delivery.Order = order;
                    delivery.Status = StatusEnum.Nuevo;
                    delivery.StatusUpdated = DateTime.Now;
                    delivery.DeliveryTime = DateTime.Now;

                    _dbContext.Deliverys.Add(delivery);

                    _dbContext.SaveChanges();

                    HttpContext.Session.SetInt32("CurrentOrderID", (int)order.ID);
                }

            }
            */

            var reasons = _dbContext.CancelReasons.ToList();
            ViewBag.CancelReasons = reasons;
            ViewBag.Discounts = _dbContext.Discounts.Where(s => s.IsActive && !s.IsDeleted).ToList();

            return View(order);
        }

        [HttpPost]
        public JsonResult GetDelivery(long orderId)
        {
            try
            {
                var objDelivery = _dbContext.Deliverys.Where(s => s.OrderID == orderId).First();                
                
                return Json(new { status = 0, data = objDelivery });

            }
            catch (Exception ex)
            {
                var m = ex;
            }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult EditDelivery([FromBody] OrderDeliveryModel request)
        {
            try
            {                
                var objDelivery = _dbContext.Deliverys.Where(s => s.OrderID == request.OrderId).First();

                var objDeliveryZone = _dbContext.DeliveryZones.Where(s => s.ID == request.ZoneId).First();

                objDelivery.Address1 = request.Address1;
                objDelivery.Adress2 = request.Adress2;
                objDelivery.Comments = request.Comments;
                objDelivery.DeliveryZoneID = request.ZoneId;

                var current = _dbContext.Orders.Include(s => s.Table).FirstOrDefault(s => s.ID == request.OrderId);
                
                current.Delivery = objDeliveryZone.Cost;
                objDelivery.DeliveryTime = current.CreatedDate.AddMinutes(decimal.ToDouble(objDeliveryZone.Time));

                var order = GetOrder(request.OrderId);
                var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);

                order.GetTotalPrice(voucher);


                _dbContext.SaveChanges();
                return Json(new { status = 0, cost = objDeliveryZone.Cost, time = objDeliveryZone.Time.ToString() });

            }
            catch(Exception ex) {
                var m = ex;
            }

            return Json(new { status = 1 });
        }

        public JsonResult GetMenuGroupList()
        {
            try
            {
                var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
                var menuGroupList = objPOSCore.GetMenuGroupList(int.Parse(GetCookieValue("StationID")));

                if (menuGroupList != null)
                {
                    return Json(menuGroupList);
                }
                return Json(null);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            //var station = _dbContext.Stations.Include(s => s.MenuSelect).ThenInclude(s => s.Groups).FirstOrDefault(s => s.ID == stationID);

            //var menu = station.MenuSelect;

            //var groups = menu.Groups.OrderBy(s => s.Order).ToList();

            //return Json(groups);
        }

		private void VoidAddProduct(long itemId, long prodId, decimal qty, int servingSizeID)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
			var station = _dbContext.Stations.FirstOrDefault(s => s.ID == stationID);
            var warehouses = _dbContext.Warehouses.ToList();
            var stationWarehouse = _dbContext.StationWarehouses.Where(s => s.StationID == station.ID).ToList();

            var product = _dbContext.Products.Include(s => s.RecipeItems.Where(s => s.ServingSizeID == servingSizeID)).FirstOrDefault(s => s.ID == prodId);
			if (product == null) return;
			try
			{
				foreach (var item in product.RecipeItems)
				{
					if (item.Type == ItemType.Article)
					{
						var article = _dbContext.Articles.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Items).FirstOrDefault(s => s.ID == item.ItemID);
                        var sw = stationWarehouse.FirstOrDefault(s => s.GroupID == article.Category.Group.ID);
                        if (sw != null)
                        {
                            var warehouse = warehouses.FirstOrDefault(s => s.ID == sw.WarehouseID);
                            if (warehouse != null)
                            {
                                objPOSCore.UpdateStockOfArticle(article, -item.Qty * qty, item.UnitNum, warehouse, StockChangeReason.Void, itemId, stationID);
                            }
                        }
					}
					else if (item.Type == ItemType.Product)
					{
						VoidAddProduct(itemId, item.ItemID, item.Qty, item.UnitNum);
					}
					else
					{
						var subrecipe = _dbContext.SubRecipes.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Items).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == item.ItemID);
                        var sw = stationWarehouse.FirstOrDefault(s => s.GroupID == subrecipe.Category.Group.ID);
                        if (sw != null)
                        {
                            var warehouse = warehouses.FirstOrDefault(s => s.ID == sw.WarehouseID);
                            if (warehouse != null)
                            {
                                objPOSCore.UpdateStockOfSubRecipe(subrecipe, -item.Qty * qty, item.UnitNum, warehouse, StockChangeReason.Void, itemId, stationID);
                            }
                        }
                      
					}
				}
			}
			catch { }
		}

		//private decimal ConvertQtyToBase(decimal originQty, int UnitNum, List<ItemUnit> Units)
  //      {
  //          if (UnitNum <= 1) return originQty;

  //          try
  //          {
  //              var realrates = new List<decimal>();
  //              var units = Units.OrderBy(s => s.Number).ToList();
  //              int i = 0;
  //              decimal rate = 0;
  //              foreach (var unit in units)
  //              {
  //                  if (i == 0)
  //                  {
  //                      realrates.Add(unit.Rate);
  //                      rate = unit.Rate;
  //                  }
  //                  else
  //                  {
  //                      var realrate = rate * unit.Rate;
  //                      realrates.Add(realrate);
  //                      rate = realrate;
  //                  }

  //                  i++;
  //              }

  //              return originQty / realrates[UnitNum - 1] * realrates[0];
  //          }
  //          catch { }
  //          return originQty;
  //      }

        private decimal ConvertQtyFromBase(decimal originQty, int UnitNum, List<ItemUnit> Units)
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

                return originQty * realrates[UnitNum - 1] * realrates[0];
            }
            catch { }
            return originQty;
        }

  //      private void UpdateStockOfArticle(InventoryItem item, decimal qty, int unitNum, Warehouse warehouse, StockChangeReason reason, long reasonId)
		//{
		//	decimal baseQty = ConvertQtyToBase(qty, unitNum, item.Items.ToList());
			       
  //          var existingWarehouseStock = _dbContext.WarehouseStocks.FirstOrDefault(s => s.Warehouse == warehouse && s.ItemType == ItemType.Article && s.ItemId == item.ID);
  //          var warehouseHistoryItem = new WarehouseStockChangeHistory();
  //          warehouseHistoryItem.ForceDate = getCurrentWorkDate();
  //          warehouseHistoryItem.Warehouse = warehouse;
  //          warehouseHistoryItem.Price = 0;
  //          warehouseHistoryItem.ItemId = item.ID;
  //          warehouseHistoryItem.ItemType = ItemType.Article;
  //          warehouseHistoryItem.Qty = baseQty;
  //          warehouseHistoryItem.BeforeBalance = existingWarehouseStock == null ? 0 : existingWarehouseStock.Qty;
  //          warehouseHistoryItem.AfterBalance = existingWarehouseStock == null ? baseQty : existingWarehouseStock.Qty + baseQty;
  //          warehouseHistoryItem.UnitNum = 1;
  //          warehouseHistoryItem.ReasonType = reason;
  //          warehouseHistoryItem.ReasonId = reasonId;

  //          _dbContext.WarehouseStockChangeHistory.Add(warehouseHistoryItem);

  //          if (existingWarehouseStock == null)
  //          {
  //              existingWarehouseStock = new WarehouseStock();
  //              existingWarehouseStock.Warehouse = warehouse;
  //              existingWarehouseStock.ItemId = item.ID;
  //              existingWarehouseStock.ItemType = ItemType.Article;
  //              existingWarehouseStock.Qty = baseQty;

  //              _dbContext.WarehouseStocks.Add(existingWarehouseStock);
  //          }
  //          else
  //          {
  //              existingWarehouseStock.Qty += baseQty;
  //          }
  //      }

        //private void UpdateStockOfSubRecipe(SubRecipe item, decimal qty, int unitNum, Warehouse warehouse, StockChangeReason reason, long reasonId)
        //{
        //    decimal baseQty = ConvertQtyToBase(qty, unitNum, item.ItemUnits.ToList());
           
        //    var existingWarehouseStock = _dbContext.WarehouseStocks.FirstOrDefault(s => s.Warehouse == warehouse && s.ItemType == ItemType.SubRecipe && s.ItemId == item.ID);

        //    var warehouseHistoryItem = new WarehouseStockChangeHistory();
        //    warehouseHistoryItem.ForceDate = getCurrentWorkDate();
        //    warehouseHistoryItem.Warehouse = warehouse;
        //    warehouseHistoryItem.Price = 0;
        //    warehouseHistoryItem.ItemId = item.ID;
        //    warehouseHistoryItem.ItemType = ItemType.SubRecipe;
        //    warehouseHistoryItem.Qty = baseQty;
        //    warehouseHistoryItem.BeforeBalance = existingWarehouseStock == null ? 0 : existingWarehouseStock.Qty;
        //    warehouseHistoryItem.AfterBalance = existingWarehouseStock == null ? baseQty : existingWarehouseStock.Qty + baseQty;
        //    warehouseHistoryItem.UnitNum = 1;
        //    warehouseHistoryItem.ReasonType = reason;
        //    warehouseHistoryItem.ReasonId = reasonId;

        //    _dbContext.WarehouseStockChangeHistory.Add(warehouseHistoryItem);
        //    if (existingWarehouseStock == null)
        //    {
        //        existingWarehouseStock = new WarehouseStock();
        //        existingWarehouseStock.Warehouse = warehouse;
        //        existingWarehouseStock.ItemId = item.ID;
        //        existingWarehouseStock.ItemType = ItemType.SubRecipe;
        //        existingWarehouseStock.Qty = baseQty;

        //        _dbContext.WarehouseStocks.Add(existingWarehouseStock);
        //    }
        //    else
        //    {
        //        existingWarehouseStock.Qty += baseQty;
        //    }
        //}

        [HttpPost]
        public JsonResult updatePerson(long orderId, int person)
        {
            try {
                var order = _dbContext.Orders.FirstOrDefault(s => s.ID == orderId);
                order.Person = person;
                _dbContext.SaveChanges();

                return Json(new { status = 1 });
            }
            catch (Exception ex) {
                return Json(new { status = 0 });
            }
            
        }

        public JsonResult GetMenuCategoryList(long groupId)
        {
            try
            {
                var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
                var menuCategoryList = objPOSCore.GetMenuCategoryList(groupId);

                if (menuCategoryList != null)
                {
                    return Json(menuCategoryList);
                }
                return Json(null);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //var group = _dbContext.MenuGroups.Include(s => s.Categories).FirstOrDefault(s => s.ID == groupId);
            //if (group != null)
            //{
            //    var categories = group.Categories.OrderBy(s => s.Order).ToList();

            //    return Json(categories);
            //}
            //return Json(new List<MenuCategory>());
        }

        public JsonResult GetMenuSubCategoryList(long categoryId)
        {
            try
            {
                var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
                var menuSubCategoryList = objPOSCore.GetMenuSubCategoryList(categoryId);

                if (menuSubCategoryList != null)
                {
                    return Json(menuSubCategoryList);
                }
                return Json(null);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            //var group = _dbContext.MenuCategories.Include(s => s.SubCategories).FirstOrDefault(s => s.ID == categoryId);

            //var subcategories = group.SubCategories.OrderBy(s => s.Order).ToList();

            //return Json(subcategories);
        }

        public JsonResult GetMenuProductList(long subCategoryId)
        {
            try
            {
                var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
                var menuProductList = objPOSCore.GetMenuProductList(subCategoryId, Request.Cookies["db"]);

                if (menuProductList != null)
                {
                    return Json(menuProductList);
                }
                return Json(null);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            //var group = _dbContext.MenuSubCategoris.Include(s => s.Products).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == subCategoryId);

            //var products = group.Products.OrderBy(s => s.Order).ToList();

            ////Obtenemos las urls de las imagenes
            //var request = _context.HttpContext.Request;
            //var _baseURL = $"https://{request.Host}";
            //if (products != null && products.Any())
            //{
            //    foreach (var objProduct in products) {               

            //            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "product", objProduct.Product.ID.ToString() + ".png");
            //            if (System.IO.File.Exists(pathFile))
            //            {
            //            var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
            //            objProduct.Product.Photo = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", objProduct.Product.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
            //            }
            //            else
            //            {
            //            objProduct.Product.Photo = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", "empty.png");
            //            }

            //    }
            //}

            //return Json(products);
        }

        public JsonResult GetMenuStaticProductList()
        {
            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.MenuSelect).ThenInclude(s => s.Groups).FirstOrDefault(s => s.ID == stationID);

            var menu = station.MenuSelect;

            var menuproducts = _dbContext.MenuProducts.Include(s => s.Product).Where(s => s.MenuID == menu.ID);

            var products = menuproducts.OrderBy(s => s.Order).ToList();

            //Obtenemos las urls de las imagenes
            var request = _context.HttpContext.Request;
            var _baseURL = $"https://{request.Host}";
            if (products != null && products.Any())
            {
                foreach (var objProduct in products)
                {

                    string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "product", objProduct.Product.ID.ToString() + ".png");
                    if (System.IO.File.Exists(pathFile))
                    {
                        var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                        objProduct.Product.Photo = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", objProduct.Product.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                    }
                    else
                    {
                        objProduct.Product.Photo = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", "empty.png");
                    }

                }
            }

            return Json(products);
        }

        [HttpPost]
        public JsonResult GetMyOpenOrders()
        {
            var user = User.Identity.GetUserName();
			var stationID = int.Parse(GetCookieValue("StationID"));  //HttpContext.Session.GetInt32("StationID");

			//var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);
			//var orders = _dbContext.Orders.Include(s=>s.Area).Include(s => s.Table).Where(s => /* s.Station == station  &&*/ (s.OrderType == OrderType.DiningRoom || s.OrderType == OrderType.Delivery) && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved && s.WaiterName == user && !s.IsDeleted).ToList();

            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            List<Order> orders = objPOSCore.GetMyOpenOrders(stationID, user);

            return Json(orders);
        }

        [HttpPost]
        public JsonResult GiveOrder(long orderId, long userId)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var user = User.Identity.GetUserName();
            var stationID = int.Parse(GetCookieValue("StationID"));

            int status = objPOSCore.GiveOrder(orderId, userId, stationID, user);
           
            return Json(new { status = status });

            //var user = User.Identity.GetUserName();
            //var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            //var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);

            //var otherUser = _dbContext.User.FirstOrDefault(s => s.ID == userId);
            //         if (orderId == 0)
            //         {
            //	var orders = _dbContext.Orders.Include(s => s.Area).Include(s => s.Table).Where(s => /*s.Station == station &&*/ (s.OrderType == OrderType.DiningRoom || s.OrderType == OrderType.Delivery) && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved && s.WaiterName == user).ToList();
            //             foreach(var o in orders)
            //             {
            //                 o.WaiterName = otherUser.Username;
            //             }
            //}
            //         else
            //         {
            //             var order = _dbContext.Orders.FirstOrDefault(s => s.ID == orderId);
            //             if (order != null)
            //                 order.WaiterName = otherUser.Username;
            //         }
            //         _dbContext.SaveChanges();

            //         return Json(new { status = 0 });
        }

        [HttpPost]
		public JsonResult GetOrderItems(long orderId, int dividerId = 0)
		{
            var response = new GetOrderItemResponse();
            try
            {
                var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
                var orderItems = objPOSCore.GetOrderItems(orderId, dividerId);

                
                
                if (orderItems != null)
                {
                    response.Valor = orderItems;
                    response.Success = true;
                    
                    //return Json(response);
                    
                    return Json(new { status = 0, items = orderItems.Items.OrderBy(s=>s.CourseID).ToList(), orderItems.Order });
                }
                //return Json(null);
                return Json(new { status = 1 });
            }
            catch (Exception ex)
            {
                response.Error = ex.Message;
                response.Success = false;
                //return Json(response);
                return Json(new { status = 1 });
            }


            //try
            //{
            //             var order = _dbContext.Orders.Include(s=>s.Discounts).Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s=>s.Questions.OrderBy(s => s.Divisor)).Include(s=>s.Items.Where(s => !s.IsDeleted)).ThenInclude(s=>s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == orderId);
            //             var lstSmart = _dbContext.SmartButtons.Select(m=>m.Name).ToList().Distinct();
            //             if (order.OrderMode == OrderMode.Seat)
            //             {
            //                 order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Seats.Where(s=>!s.IsPaid)).ThenInclude(s => s.Items.Where(s=>!s.IsDeleted)).ThenInclude(s=>s.Questions.OrderBy(s=>s.Divisor)).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).FirstOrDefault(o => o.ID == orderId);

            //                 //Quitamos las imagenes del objeto para hacer mas rapido el pos
            //                 if (order.Items != null && order.Items.Any())
            //                 {
            //                     foreach (var objItem in order.Items)
            //                     {
            //                         objItem.Product.Photo = null;

            //                         foreach (var objQuestion in objItem.Questions)
            //                         {
            //                             if (lstSmart.Any(objQuestion.Description.Contains))
            //                             {
            //                                 objQuestion.showDesc = true;
            //                             }
            //                         }
            //                     }
            //                 }

            //                 return Json(new { status = 0, order.Seats, order } );
            //             }
            //             else if (order.OrderMode == OrderMode.Divide && dividerId > 0)
            //             {
            //                 order = _dbContext.Orders.Include(s=>s.Divides).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions.OrderBy(s => s.Divisor)).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == orderId);

            //                 var items = order.Items.Where(s => s.DividerNum == dividerId).ToList();
            //                 var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
            //                 order.GetTotalPrice(voucher, dividerId);

            //                 var clientName = "";
            //                 try
            //                 {
            //                     var divide = order.Divides.FirstOrDefault(s => s.DividerNum == dividerId);
            //                     if (divide != null)
            //                     {
            //                         clientName = divide.ClientName;
            //                     }


            //                 }
            //                 catch { }

            //                 //Quitamos las imagenes del objeto para hacer mas rapido el pos
            //                 if (order.Items != null && order.Items.Any())
            //                 {
            //                     foreach (var objItem in order.Items)
            //                     {
            //                         objItem.Product.Photo = null;
            //                     }
            //                 }

            //                 return Json(new { status = 0, items, order, client = clientName });
            //             }
            //             else
            //             {
            //                 //Quitamos las imagenes del objeto para hacer mas rapido el pos
            //                 if(order.Items!= null && order.Items.Any()) {
            //                     foreach (var objItem in order.Items)
            //                     {
            //                         objItem.Product.Photo = null;

            //                         foreach (var objQuestion in objItem.Questions)
            //                         {
            //                             if (lstSmart.Any(objQuestion.Description.Contains))
            //                             {
            //                                 objQuestion.showDesc = true;
            //                             }
            //                         }
            //                     }
            //                 }


            //                 return Json(new { status = 0, items = order.Items.OrderBy(s=>s.CourseID).ToList(), order });
            //             }
            //         }
            //catch { }
            //return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult GetOrderItemsInCheckout(long orderId, int SeatNum, int DividerId)
        {
            try
            {
                var store = _dbContext.Preferences.FirstOrDefault();

                var order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == orderId);
                var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order).ToList();
                var voucher = _dbContext.Vouchers.Include(s=>s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
                if (order.OrderMode == OrderMode.Seat)
                {
                    order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).FirstOrDefault(o => o.ID == orderId);

                    var seats = order.Seats.ToList();
                    if (SeatNum > 0)
                        seats = order.Seats.Where(s => s.SeatNum == SeatNum).ToList();
                    order.GetTotalPrice(voucher, 0, SeatNum);

                    transactions = transactions.Where(s => s.SeatNum == SeatNum).ToList();
                    if (SeatNum > 0 && transactions.Count() > 0)
                    {
                        var paid = transactions.Sum(s => s.Amount);
                        order.Balance = order.Balance - paid;
                    }

                    return Json(new { status = 0, seats, order, transactions, store });
                }
                else if (order.OrderMode == OrderMode.Divide && DividerId > 0)
                {
                    order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == orderId);

                    var items = order.Items.Where(s =>!s.IsDeleted && s.DividerNum == DividerId).ToList();
                    order.GetTotalPrice(voucher, DividerId);

                    transactions = transactions.Where(s => s.DividerNum == DividerId).ToList();
                    if (transactions.Count() > 0)
                    {
                        var paid = transactions.Sum(s => s.Amount);
                        order.Balance = order.Balance - paid;
                    }

                    return Json(new { status = 0, items, order, transactions, store });
                }
                else
                {
                    return Json(new { status = 0, order.Items, order, transactions, store = store });
                }
            }
            catch { }
            return Json(new { status = 1 });
        }        

        [HttpPost]
        public JsonResult GetOrderItemsInBarcode(long orderId)
        {
            try
            {
                var order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).ThenInclude(s => s.RecipeItems).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == orderId);
                var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order).ToList();
                var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);

                var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
                var station = _dbContext.Stations.FirstOrDefault(s => s.ID == stationID);
                var warehouses = _dbContext.Warehouses.ToList();
                var stationWarehouse = _dbContext.StationWarehouses.Where(s => s.StationID == station.ID).ToList();


                var items = new List<BarcodeShowOrderItem>();
                foreach (var item in order.Items)
                {
                    var bitem = new BarcodeShowOrderItem(item);
                    bitem.Units = new List<ItemUnit>();
                    items.Add(bitem);
                    var pitem = item.Product.RecipeItems.FirstOrDefault(s => s.Type == ItemType.Article);
                    try
                    {
                        if (pitem != null)
                        {

                            var article = _dbContext.Articles.Include(s => s.Items).FirstOrDefault(s => s.ID == pitem.ItemID);
                            if (article != null)
                            {
                                bitem.Units = article.Items.OrderBy(s => s.Number).ToList();
                                var unit = article.Items.FirstOrDefault(s => s.Number == pitem.UnitNum);
                                if (unit != null)
                                {
                                    bitem.Unit = unit.Name;

                                }
                                var sw = stationWarehouse.FirstOrDefault(s => s.GroupID == article.Category.Group.ID);
                                if (sw != null)
                                {
                                    var warehouse = warehouses.FirstOrDefault(s => s.ID == sw.WarehouseID);
                                    if (warehouse != null)
                                    {
                                        var stocks = _dbContext.WarehouseStocks.FirstOrDefault(s => s.Warehouse == warehouse && s.ItemId == article.ID);
                                        if (stocks != null)
                                        {
                                            decimal baseQty = Math.Round(ConvertQtyFromBase(stocks.Qty, unit.Number, bitem.Units), 2);
                                            bitem.StockQty = baseQty;

                                        }
                                    }
                                }                               
                            }
                        }
                    }
                    catch { }
                }

                //Quitamos las imagenes del objeto para hacer mas rapido el pos
                if (items != null && items.Any())
                {
                    foreach (var objItem in items)
                    {
                        objItem.Product.Photo = null;
                    }
                }

                return Json(new { status = 0, items, order, transactions });
            }
            catch { }
            return Json(new { status = 1 });
        }



        [HttpPost]
        public JsonResult GetCxCItems([FromBody] int[] selectedItems)
        {
            try
            {
                // Obtener las transacciones de la base de datos que coincidan con los IDs seleccionados
                var selectedTransactions = _dbContext.OrderTransactions
                    .Include(t => t.Order)  // Incluir la orden asociada
                    .Where(t => selectedItems.Contains((int)t.ID))
                    .ToList();

                // Calcular el TemporaryDifference para cada transacción seleccionada
                foreach (var transaction in selectedTransactions)
                {
                    // Obtener las transacciones asociadas al ID de referencia de esta transacción
                    var associatedTransactions = _dbContext.OrderTransactions
                        .Where(t => t.ReferenceId == transaction.ID)
                        .ToList();

                    // Calcular la diferencia temporal y almacenarla en la propiedad TemporaryDifference
                    transaction.TemporaryDifference = transaction.Amount - associatedTransactions.Sum(t => t.Amount);
                }
                var store = _dbContext.Preferences.FirstOrDefault();
                return Json(new { items = selectedTransactions, store }); // Devolver un objeto JSON con la propiedad 'items'
            }
            catch (Exception ex)
            {
                return Json(new { error = "Ocurrió un error al obtener los datos: " + ex.Message });
            }
        }

        //ya existe en poscore
        private Order GetOrder(long OrderId)
        {
            var order = _dbContext.Orders.Include(s => s.Taxes).Include(s => s.PrepareType).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).ThenInclude(s=>s.Answer).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).Include(s=>s.Divides).Include(s=>s.Area).Include(s=>s.Table).FirstOrDefault(o => o.ID == OrderId);

            return order;
        }

        [HttpPost]
        public JsonResult AddProductToOrderItem(AddItemModel model)
        {

            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.MenuSelect).ThenInclude(s => s.Groups).FirstOrDefault(s => s.ID == stationID);
			
            var product = _dbContext.Products.Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Questions).ThenInclude(s=>s.SmartButtons.OrderBy(s=>s.Order)).ThenInclude(s=>s.Button).Include(s => s.Questions).ThenInclude(s => s.Answers.OrderBy(s=>s.Order)).ThenInclude(s=>s.Product).ThenInclude(s=>s.ServingSizes).Include(s=>s.ServingSizes).FirstOrDefault(s => s.ID == model.ProductId);

            if (product.InventoryCountDownActive)
            {
                if (product.InventoryCount < model.Qty)
                {
                    return Json(new {status = 3, product});
                }
            }

            var order = GetOrder(model.OrderId);
            int priceSelect = station.PriceSelect;
			if (station.SalesMode == SalesMode.Restaurant && order.Area != null)
			{
                try
                {
                    var prices = JsonConvert.DeserializeObject<List<AreaModel>>(station.AreaPrices);
                    var areaPrice = prices.FirstOrDefault(s => s.AreaID == order.Area.ID);
                    if (areaPrice != null && areaPrice.PriceSelect > 0)
                    {
                        priceSelect = areaPrice.PriceSelect;
                    }
                }
                catch { }

                
            }


            if (station.SalesMode == SalesMode.Restaurant && order.OrderType == OrderType.Delivery)

            try
            {
                if(station.PrecioDelivery!= null) {
                        priceSelect = station.PrecioDelivery.Value;
                }
                
            }
            catch { }
            {
            }

            var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
            if (order == null || product == null)
			{
				return Json(new { status = 1 });
			}

            var existitem = order.Items.FirstOrDefault(s => s.Product == product && !s.Product.HasServingSize && (s.Status == OrderItemStatus.Pending || s.Status == OrderItemStatus.Printed) && s.SeatNum == model.SeatNum && s.DividerNum == model.DividerNum);

            var hasQuestion = existitem != null && existitem.Questions.Count > 0;

            if (existitem != null && !hasQuestion)
            {
                existitem.Qty += 1;
                existitem.SubTotal = existitem.Price * existitem.Qty;

                existitem.Costo = GetCostOrderItem(product.ID, existitem.Qty);

                _dbContext.SaveChanges();
                var ret = ApplyPromotion(existitem, 1);
                _dbContext.SaveChanges();
                if (ret)
                {
                    var orderDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
                    foreach (var d in orderDiscounts)
                    {
                        order.Discounts.Remove(d);
                    }

                    foreach (var item in order.Items)
                    {
                        var itemDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
                        foreach (var i in itemDiscounts)
                        {
                            item.Discounts.Remove(i);
                        }
                    }
                }
                _dbContext.SaveChanges();
                order.GetTotalPrice(voucher);
                _dbContext.SaveChanges();
                return Json(new { status = 0, itemId = existitem.ID, hasQuestion = false });
            }
            else
			{
                var nItem = new OrderItem();
                nItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                nItem.Order = order;
                nItem.MenuProductID = model.MenuProductId;
                nItem.Price = product.Price[priceSelect - 1];
                nItem.OriginalPrice = nItem.Price;
                nItem.Product = product;
                nItem.Name = product.Name;
                nItem.SeatNum = model.SeatNum;
                nItem.DividerNum = model.DividerNum;
                nItem.Qty = model.Qty;
                nItem.SubTotal = nItem.Price * nItem.Qty;
                nItem.Costo = GetCostOrderItem(product.ID, nItem.Qty);

                nItem.Status = OrderItemStatus.Pending;

                var course = _dbContext.Courses.FirstOrDefault(s => s.ID == product.CourseID && s.IsActive);
                if (course != null)
                {
                    nItem.CourseID = product.CourseID;
                    nItem.Course = course.Name;
                }
                else
                {
                    nItem.Course = "";
                    nItem.CourseID = 0;
                }

                var servingSizes = new List<ProductServingSize>();
                if (product.HasServingSize)
                {
                    servingSizes = product.ServingSizes.OrderBy(s=>s.Order).ToList();
                    var defaultServingSize = servingSizes.FirstOrDefault(s => s.IsDefault);
                    if (defaultServingSize != null)
                    {
                        nItem.ServingSizeID = (int)defaultServingSize.ServingSizeID;
                        nItem.ServingSizeName = defaultServingSize.ServingSizeName;
                        nItem.Price = defaultServingSize.Price[priceSelect - 1];
                        nItem.OriginalPrice = nItem.OriginalPrice;
                        nItem.SubTotal = nItem.Price * nItem.Qty;

                        nItem.Costo = GetCostOrderItem(product.ID, nItem.Qty);
                    }
                }

                if (order.Items == null)
                    order.Items = new List<OrderItem>();

                nItem.Taxes = new List<TaxItem>();
                if (product.Taxes != null)
                {
                    foreach(var t in product.Taxes)
                    {
                        var tax = new TaxItem();
                        tax.TaxId = t.ID;
                        tax.Description = t.TaxName;
                        tax.Percent = (decimal)t.TaxValue;
                        tax.ToGoExclude = t.IsToGoExclude;
                        tax.BarcodeExclude = t.IsBarcodeExclude ;
                        tax.IsExempt = order.PrepareTypeID == 4 && t.IsKioskExclude; //Kiosk

                        nItem.Taxes.Add(tax);
                    }
                }
                nItem.Propinas = new List<PropinaItem>();
                if (product.Propinas != null)
                {
                    foreach (var t in product.Propinas)
                    {
                        var tax = new PropinaItem();
                        tax.PropinaId = t.ID;
                        tax.Description = t.PropinaName;
                        tax.Percent = (decimal)t.PropinaValue;
                        tax.ToGoExclude = t.IsToGoExclude;
                        tax.BarcodeExclude = t.IsBarcodeExclude;
                        tax.IsExempt = order.PrepareTypeID == 4 && t.IsKioskExclude;//Kiosk

                        nItem.Propinas.Add(tax);
                    }
                }
                order.Items.Add(nItem);
                _dbContext.SaveChanges();
                if (!product.HasServingSize)
                {
                    var ret = ApplyPromotion(nItem, 1);
                    if (ret)
                    {
                        var orderDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
                        foreach (var d in orderDiscounts)
                        {
                            order.Discounts.Remove(d);
                        }

                        foreach (var item in order.Items)
                        {
                            var itemDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
                            foreach (var i in itemDiscounts)
                            {
                                item.Discounts.Remove(i);
                            }
                        }
                    }
                }
               
                _dbContext.SaveChanges();
                order.GetTotalPrice(voucher);                

                if (order.OrderMode == OrderMode.Seat && model.SeatNum > 0)
                {
                    if (order.Seats == null) order.Seats = new List<SeatItem>();

                    var existSeat = order.Seats.FirstOrDefault(s => s.SeatNum == model.SeatNum);
                    if (existSeat != null)
                    {
                        existSeat.Items.Add(nItem);
                    }
                    else
                    {
                        existSeat = new SeatItem() { OrderId = order.ID, SeatNum = model.SeatNum, Items = new List<OrderItem>() { nItem } };
                        order.Seats.Add(existSeat);
                    }
                }
                _dbContext.SaveChanges();
                var questions = new List<Question>();
                if (product.Questions != null)
                {
                    questions = product.Questions.Where(s => s.IsForced).OrderBy(s=>s.DisplayOrder).ToList();

                    var request = _context.HttpContext.Request;
                    var _baseURL = $"https://{request.Host}";

                    foreach (var objQuestion in questions) {
                        foreach (var objAnswer in objQuestion.Answers) {                            

                            //Obtenemos las urls de las imagenes
                            string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "product", objAnswer.Product.ID.ToString() + ".png");
                            if (System.IO.File.Exists(pathFile))
                            {
                                var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                                objAnswer.Product.Photo = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", objAnswer.Product.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                            }
                            else
                            {
                                objAnswer.Product.Photo = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", "empty.png");
                            }


                        }
                    }
                    
                    //Para las preguntas opcionales, pero preseleccionado
                    var questionsAux = product.Questions.Where(s => !s.IsForced).OrderBy(s=>s.DisplayOrder).ToList();
                    
                    foreach (var objQuestion in questionsAux) {
                        
                        int index = 0;
                        int freechoice = 0;
                        
                        foreach (var objAnswer in objQuestion.Answers) {
                            if (objAnswer.IsPreSelected)
                            {
                                /*var questionItem = new QuestionItem()
                                {
                                    Answer = objAnswer,
                                    Description = "No " + objAnswer.Product.Name,
                                    IsPreSelect = objAnswer.IsPreSelected,
                                    IsActive = true
                                };*/
                                
                                var servingsizeName = "";
                                int servingsizeId = 0;
                                var price = 0.0m;
                                var canRoll = false;
                                if (objAnswer.RollPrice > 0)
                                {
                                    price = objAnswer.RollPrice;
                                    canRoll = true;
                                }
                                else if (objAnswer.FixedPrice > 0)
                                {
                                    price = objAnswer.FixedPrice;
                                }
                                else
                                {
                                    try
                                    {
                                        price = objAnswer.Product.Price[(int)objAnswer.PriceType - 1];
                                        if (objAnswer.MatchSize && nItem.ServingSizeID >0)
                                        {
                                            if (objAnswer.Product.HasServingSize)
                                            {
                                                var servingsize = objAnswer.Product.ServingSizes.FirstOrDefault(s => s.ServingSizeID == nItem.ServingSizeID);
                                                if (servingsize != null)
                                                {
                                                    price = servingsize.Price[(int)objAnswer.PriceType - 1];
                                                    servingsizeName = servingsize.ServingSizeName;
                                                    servingsizeId = nItem.ServingSizeID;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (objAnswer.Product.HasServingSize && objAnswer.ServingSizeID > 0)
                                            {
                                                var servingsize = objAnswer.Product.ServingSizes.FirstOrDefault(s => s.ServingSizeID == objAnswer.ServingSizeID);
                                                if (servingsize != null)
                                                {
                                                    price = servingsize.Price[(int)objAnswer.PriceType - 1];
                                                    servingsizeName = servingsize.ServingSizeName;
                                                    servingsizeId = objAnswer.ServingSizeID;
                                                }
                                            }
                                        }
                               
                                    }
                                    catch { }
                                }
                                
                                var questionItem = new QuestionItem()
                                {
                                    Answer = objAnswer,
                                    Description = objAnswer.Product.Name,
                                    Price = price,
                                    CanRoll = canRoll,
                                    Qty = 1,
                                    //Divisor = (DivisorType)a.Divisor,
                                    ServingSizeID = servingsizeId,
                                    ServingSizeName = servingsizeName,
                                    IsPreSelect = objAnswer.IsPreSelected,
                                    IsActive = true
                                };

                                if (nItem.Questions == null)
                                {
                                    nItem.Questions = new List<QuestionItem>();
                                }
                                
                                if (objAnswer.FixedPrice == 0 && index < objQuestion.FreeChoice)
                                {
                                    for (int j = 0; j < questionItem.Qty; j++)
                                    {
                                        if (index < objQuestion.FreeChoice)
                                        {
                                            questionItem.FreeChoice++;
                                            index++;
                                        }
                                    }
                                }
                                
                                nItem.Questions.Add(questionItem);
                                _dbContext.SaveChanges();
                            }
                        }
                    }
                    order.GetTotalPrice(voucher); 
                    _dbContext.SaveChanges();   
                }

                return Json(new { status = 0, itemId = nItem.ID, hasQuestion = questions != null && questions.Count > 0, questions = questions, hasServingSize = product.HasServingSize && servingSizes.Count > 0, servingSizes, productName = product.Name });
            }
		}

        private decimal GetCostOrderItem(long productID, decimal cantidad, int size=0) {
            decimal costoTotal = 0;

            var product = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions.OrderBy(s => s.DisplayOrder)).FirstOrDefault(s => s.ID == productID);

            if (product.HasServingSize)
            {

                product = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions).FirstOrDefault(s => s.ID == productID);

                foreach (var ss in product.ServingSizes)
                {
                    var ret = new ProductServingSizeViewModel(ss);

                    var items = product.RecipeItems.Where(s => s.ServingSizeID == ss.ServingSizeID);

                    foreach (var item in items)
                    {

                        if (ret.ServingSizeID == size) {
                            var subRecipeItem = new ProductRecipeItemViewModel(item);
                            if (item.Type == ItemType.Article)
                            {
                                var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == item.ItemID);
                                subRecipeItem.Article = article;

                                decimal itemCost = (from m in article.Items where m.Number == item.UnitNum select m.Cost).FirstOrDefault();
                                costoTotal = costoTotal + (itemCost * item.Qty);
                            }
                            else if (item.Type == ItemType.Product)
                            {
                                var product1 = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == item.ItemID);
                                subRecipeItem.Product = product1;

                                if (product1.HasServingSize) {
                                    decimal itemCost = (from m in product1.ServingSizes where m.ServingSizeID == item.UnitNum select m.Cost).FirstOrDefault();
                                    costoTotal = costoTotal + (itemCost * item.Qty);
                                }
                                else {
                                    costoTotal = costoTotal + (product1.ProductCost * item.Qty);
                                }

                                
                            }
                            else
                            {
                                var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == item.ItemID);
                                subRecipeItem.SubRecipe = subRecipe;

                                decimal itemCost = (from m in subRecipe.ItemUnits where m.Number == item.UnitNum select m.Cost).FirstOrDefault();
                                costoTotal = costoTotal + (itemCost * item.Qty);
                            }
                           
                        }
                                           
                    }
                }
            }
            else
            {
                foreach (var recipe in product.RecipeItems)
                {
                    var item = _dbContext.ProductItems.FirstOrDefault(s => s.ID == recipe.ID);

                    var subRecipeItem = new ProductRecipeItemViewModel(item);

                    if (item.Type == ItemType.Article)
                    {
                        var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == item.ItemID);
                        subRecipeItem.Article = article;

                        decimal itemCost = (from m in article.Items where m.Number == item.UnitNum select m.Cost).FirstOrDefault();
                        costoTotal = costoTotal + (itemCost * item.Qty);
                    }
                    else if (item.Type == ItemType.Product)
                    {
                        var productRecipe = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == item.ItemID);
                        subRecipeItem.Product = productRecipe;

                        if (productRecipe.HasServingSize) {
                            decimal itemCost = (from m in productRecipe.ServingSizes where m.ServingSizeID == item.UnitNum select m.Cost).FirstOrDefault();
                            costoTotal = costoTotal + (itemCost * item.Qty);
                        }
                        else {
                            costoTotal = costoTotal + (product.ProductCost * item.Qty);
                        }
                        
                    }
                    else 
                    {
                        var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == item.ItemID);
                        subRecipeItem.SubRecipe = subRecipe;
                       
                        decimal itemCost = (from m in subRecipe.ItemUnits where m.Number == item.UnitNum select m.Cost).FirstOrDefault();
                        costoTotal = costoTotal + (itemCost * item.Qty);
                    }                    
                }
            }

            return costoTotal * cantidad;
        }

        [HttpPost]
        public JsonResult CheckAnswerSizesByServingSize(long productID, long servingSizeID)
        {
            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.MenuSelect).ThenInclude(s => s.Groups).FirstOrDefault(s => s.ID == stationID);

            var product = _dbContext.Products.Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Questions).ThenInclude(s => s.Answers.OrderBy(s => s.Order)).ThenInclude(s => s.Product).ThenInclude(s => s.ServingSizes).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == productID);

            var result = new List<AnswerSizeCheckModel>();
            var questions = product.Questions.Where(s => s.IsForced).ToList();

            foreach(var question in questions)
            {
                foreach(var answer in question.Answers)
                {
                    var m = new AnswerSizeCheckModel()
                    {
                        AnswerID = answer.ID,
                        MatchSize = answer.MatchSize,
                        HasSize = false
                    };
                    if (answer.MatchSize && answer.Product.HasServingSize)
                    {
                        var servingSize = answer.Product.ServingSizes.FirstOrDefault(s => s.ServingSizeID == servingSizeID);
                        if (servingSize != null)
                        {
                            m.HasSize = true;
                        }
                    }
                    result.Add(m);
                }
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult AddProductToOrderItemWithBarcode(AddBarcodeModel model)
        {
            var LogError = new System.Text.StringBuilder();

            if (model != null)
            {

                LogError.Append(System.Text.Json.JsonSerializer.Serialize(model));
            }
            else
            {
                LogError.Append("El modelo es null");
            }            

            try {
            
            var stationID = int.Parse(GetCookieValue("StationID"));  //HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.MenuSelect).ThenInclude(s => s.Groups).FirstOrDefault(s => s.ID == stationID);
            
            var product = _dbContext.Products.Include(s => s.Taxes).Include(s => s.Propinas).FirstOrDefault(s => s.ID == model.ProductId);

                LogError.AppendLine().Append("--------").AppendLine().Append("L1 ");

                
             if (product == null && !string.IsNullOrEmpty(model.Barcode))
            {
                product = _dbContext.Products.Include(s => s.Taxes).Include(s => s.Propinas).FirstOrDefault(s => s.Barcode == model.Barcode);
            }

            if (product == null)
            {
                return Json(new { status = 1 });
            }
            if (product.InventoryCountDownActive)
            {
                if (product.InventoryCount < 1)
                {
                    return Json(new { status = 3, product });
                }
            }                

                LogError.AppendLine().Append("--------").AppendLine().Append("L2 ");

                var order = _dbContext.Orders.Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Product).Include(s => s.Items).ThenInclude(s => s.Discounts).FirstOrDefault(o => o.ID == model.OrderId);
            
            var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
            if (order == null || product == null)
            {
                return Json(new { status = 1 });
            }

            var existitem = order.Items.FirstOrDefault(s => s.Product == product && s.Status == OrderItemStatus.Pending);

                LogError.AppendLine().Append("--------").AppendLine().Append("L3 ");

                if (existitem != null)
            {
                existitem.Qty += 1;
                existitem.SubTotal = existitem.Price * existitem.Qty;
                _dbContext.SaveChanges();
                var ret = ApplyPromotion(existitem, 1);
                _dbContext.SaveChanges();
                if (ret)
                {
                    var orderDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
                        LogError.AppendLine().Append("--------").AppendLine().Append("L4 ");
                        foreach (var d in orderDiscounts)
                    {
                        order.Discounts.Remove(d);
                    }

                    foreach (var item in order.Items)
                    {
                        var itemDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
                        foreach (var i in itemDiscounts)
                        {
                            item.Discounts.Remove(i);
                        }
                    }
                }
                    LogError.AppendLine().Append("--------").AppendLine().Append("L5 ");
                    _dbContext.SaveChanges();
                order.GetTotalPrice(voucher);
                _dbContext.SaveChanges();
                return Json(new { status = 0, itemId = existitem.ID, hasQuestion = false });
            }
            else
            {
                var nItem = new OrderItem();
                nItem.Order = order;
                nItem.MenuProductID = 0;
                    LogError.AppendLine().Append("--------").AppendLine().Append("L6 ");
                    nItem.Price = product.Price[station.PriceSelect-1];
                    nItem.OriginalPrice = nItem.Price;
                    LogError.AppendLine().Append("--------").AppendLine().Append("L7 ");
                    nItem.Product = product;
                nItem.Name = product.Name;
                nItem.Qty = 1;
                nItem.SubTotal = nItem.Price * nItem.Qty;
                nItem.Status = OrderItemStatus.Pending;
                if (order.Items == null)
                    order.Items = new List<OrderItem>();

                nItem.Taxes = new List<TaxItem>();
                    LogError.AppendLine().Append("--------").AppendLine().Append("L8 ");
                    if (product.Taxes != null)
                {
                    foreach (var t in product.Taxes)
                    {
                        var tax = new TaxItem();
                        tax.TaxId = t.ID;
                        tax.Description = t.TaxName;
                        tax.Percent = (decimal)t.TaxValue;
                        tax.ToGoExclude = t.IsToGoExclude;
                        tax.BarcodeExclude = t.IsBarcodeExclude;

                        nItem.Taxes.Add(tax);
                    }
                }
                    nItem.Propinas = new List<PropinaItem>();
                    LogError.AppendLine().Append("--------").AppendLine().Append("L8 ");
                    if (product.Propinas != null)
                    {
                        foreach (var t in product.Propinas)
                        {
                            var tax = new PropinaItem();
                            tax.PropinaId = t.ID;
                            tax.Description = t.PropinaName;
                            tax.Percent = (decimal)t.PropinaValue;
                            tax.ToGoExclude = t.IsToGoExclude;
                            tax.BarcodeExclude = t.IsBarcodeExclude;

                            nItem.Propinas.Add(tax);
                        }
                    }

                    LogError.AppendLine().Append("--------").AppendLine().Append("L9 ");
                    order.Items.Add(nItem);
                _dbContext.SaveChanges();
                var ret = ApplyPromotion(nItem, 1);
                if (ret)
                {
                    var orderDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
                    foreach (var d in orderDiscounts)
                    {
                        order.Discounts.Remove(d);
                    }

                    foreach (var item in order.Items)
                    {
                        var itemDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
                        foreach (var i in itemDiscounts)
                        {
                            item.Discounts.Remove(i);
                        }
                    }
                }
                _dbContext.SaveChanges();
                order.GetTotalPrice(voucher);

                _dbContext.SaveChanges();

                return Json(new { status = 0, itemId = nItem.ID });
            }
            }catch(Exception ex) {

                var objLog = new logs();

                LogError.AppendLine().Append("--------").AppendLine().Append(ex.ToString());

                objLog.ubicacion = "AddProductToOrderItemWithBarcode";
                objLog.descripcion = LogError.ToString();
                objLog.fecha = DateTime.Now;
                

                _dbContext.logs.Add(objLog);
                _dbContext.SaveChanges();

                return Json(new { status = 1 });
            }
        }

        [HttpPost]
        public JsonResult AddNewSeat(long orderId)
        {
            var order = _dbContext.Orders.Include(s=>s.Seats).FirstOrDefault(s => s.ID == orderId);
            if (order != null && order.OrderMode == OrderMode.Seat)
            {
                var seatNum = 0;
                foreach(var s in order.Seats)
                {
                    if (seatNum < s.SeatNum)
                        seatNum = s.SeatNum;
                }

                var seat = new SeatItem()
                {
                    OrderId = order.ID,
                    SeatNum = seatNum + 1,
                    Items = new List<OrderItem>()
                };

                order.Seats.Add(seat);
                _dbContext.SaveChanges();

                return Json(new { status = 0 });
            }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult ChangePaymentType(long transaction, string paymenttype)
        {

            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            var stationID = int.Parse(GetCookieValue("StationID"));
            var objTransaction = _dbContext.OrderTransactions.FirstOrDefault(s => s.ID == transaction);
            var method = _dbContext.PaymentMethods.FirstOrDefault(s => s.Name == paymenttype);

            objTransaction.Method = method.Name;
            objTransaction.PaymentType = method.PaymentType;
            objTransaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
            objTransaction.ForceUpdatedBy = objTransaction.CreatedBy;

            _dbContext.SaveChanges();

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult UpdatePrepareType(long orderId, int type)
        {
            var order = _dbContext.Orders.Include(s=>s.PrepareType).FirstOrDefault(s => s.ID == orderId);

            if (order != null)
            {
                if (order.OrderType != OrderType.Delivery)
                {
                    return Json(new { status = 1 });
                }

                if (order.OrderType == OrderType.Delivery && order.PayAmount>0)
                {
                    return Json(new { status = 2 });
                }

                var prepareType = _dbContext.PrepareTypes.FirstOrDefault(s => s.ID == type);
                order.PrepareTypeID = type;


                if(order.CustomerId != 0) {
                    var objCustomer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);
                    var deliveryzone = _dbContext.DeliveryZones.FirstOrDefault(s => s.ID == objCustomer.DeliveryZoneID);
                    if (deliveryzone != null && !prepareType.SinChofer)
                    {
                        order.Delivery = deliveryzone.Cost;
                    }
                    else
                    {
                        order.Delivery = 0;
                    }

                    var order1 = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Items).ThenInclude(s => s.Questions).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == order.ID);
                    Voucher? objVoucher = null;

                    if (objCustomer != null && objCustomer.Voucher != null)
                    {
                        objVoucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == objCustomer.Voucher.ID);
                        
                    }

                    order1.GetTotalPrice(objVoucher);
                }

                

                /*if (type == 1)
                {
                    order.PrepareTypeID = type; // "To go";
                    
                }
                else if (type == 2)
                {
                    order.PrepareTypeID = type; // "Pickup";
                }
                else if(type == 3)
                {
                    order.PrepareTypeID = type; //"Delivery";
                }*/

                _dbContext.SaveChanges();

                return Json(new { status = 0, type = prepareType.Name });
            }

            return Json(new { status = 1});
        }

        [HttpPost]
        public JsonResult UpdateOrderType(long orderId, int type)
        {
            var order = _dbContext.Orders.FirstOrDefault(s => s.ID == orderId);

            if (order != null)
            {
                var prepareType = _dbContext.PrepareTypes.FirstOrDefault(s => s.ID == type);
                order.PrepareTypeID = type;

                /*if (type == 2)
                {
                    order.PrepareTypeID = type; //"Pick up";
                }
                else if (type == 3)
                {
                    order.PrepareTypeID = type; //"Delivery";
                }
                else if (type == 4)
                {
                    order.PrepareTypeID = type; //"Kiosk";
                }*/
                _dbContext.SaveChanges();

                return Json(new { status = 0, type = prepareType.Name });
            }

            return Json(new { status = 1 });
        }

        [HttpPost]
		public JsonResult SendOrder(long orderId, DateTime? saveDate = null)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            try
            {
				var order = GetOrder(orderId);

				if (order.Items == null || order.Items.Count == 0)
				{
					//_dbContext.Orders.Remove(order);
					order.OrderTime = DateTime.Now;
					//order.OrderTime = getCurrentWorkDate();                
					order.Status = OrderStatus.Saved;
				}
				else
				{
					var kichenItems = new List<OrderItem>();
					if (order.Status == OrderStatus.Temp)
					{
						order.OrderTime = DateTime.Now;
						//order.OrderTime = getCurrentWorkDate();                    
						if (saveDate.HasValue)
						{
							order.CreatedDate = saveDate.Value;
						}
						else
						{
							order.CreatedDate = DateTime.Now;
						}
					}


					order.Status = OrderStatus.Pending;

					// comprobante

					_dbContext.SaveChanges();

					foreach (var item in order.Items)
					{
						if (item.Status == OrderItemStatus.Pending || item.Status == OrderItemStatus.Printed)
						{
							item.Status = OrderItemStatus.Kitchen;
                            objPOSCore.SendKitchenItem(order.ID, item.ID);
							item.ForceDate = objPOSCore.getCurrentWorkDate(stationID);

							kichenItems.Add(item);
							if (item.Product.InventoryCountDownActive)
							{
								item.Product.InventoryCount -= item.Qty;

							}
                            objPOSCore.SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, stationID);
							foreach (var q in item.Questions)
							{
								if (!q.IsActive) continue;
								var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                                objPOSCore.SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, stationID);
							}
						}

						if (item.Status == OrderItemStatus.HoldManually || item.Status == OrderItemStatus.HoldAutomatic)
						{
							order.Status = OrderStatus.Hold;
						}
					}
					//var stationID = int.Parse(GetCookieValue("StationID"));
					_printService.PrintKitchenItems(stationID, order.ID, kichenItems, Request.Cookies["db"]);
				}

				_dbContext.SaveChanges();

				if (order.OrderType == OrderType.Delivery)
				{
					//var stationID = int.Parse(GetCookieValue("StationID"));

					if (stationID > 0)
					{
						var objStation = _dbContext.Stations.Where(s => s.ID == stationID).First();

						if (objStation.ImprimirPrecuentaDelivery)
						{
							PrintOrderFunc(order.ID);
						}
					}
				}

				HttpContext.Session.Remove("CurrentOrderID");

				if (order.OrderType == OrderType.Delivery)
				{
					bool deliveryExists = _dbContext.Deliverys.Include(s => s.Order).Where(s => s.IsActive).Where(s => s.OrderID == order.ID).Any();
					if (deliveryExists)
					{
						var delivery = _dbContext.Deliverys.Include(s => s.Order).Where(s => s.IsActive).Where(s => s.OrderID == order.ID).First();
						var zone = _dbContext.DeliveryZones.Where(s => s.IsActive).Where(s => s.ID == delivery.DeliveryZoneID).FirstOrDefault();

						delivery.StatusUpdated = DateTime.Now;

						if (zone != null)
						{
							delivery.DeliveryTime = DateTime.Now.AddMinutes(decimal.ToInt32(zone.Time));
						}
						else
						{
							delivery.DeliveryTime = DateTime.Now;
						}

						_dbContext.SaveChanges();
					}
				}
			}
            catch(Exception ex)
            {
				return Json(new { status = 1, message = ex.Message + ex.InnerException.Message + ex.InnerException.Source });
			}

            return Json(new { status = 0 });
		}

        [HttpPost]
        public JsonResult SaveBarcodeOrder(long orderId)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            Debug.WriteLine("save controller");
            var order = _dbContext.Orders.Include(s => s.Items).ThenInclude(s => s.Product).Include(s => s.Items).ThenInclude(s => s.Questions).FirstOrDefault(s => s.ID == orderId);
            Debug.WriteLine(order);
            if (order.Items == null || order.Items.Count == 0)
            {
                _dbContext.Orders.Remove(order);
            }
            else
            {
                if (order.Status == OrderStatus.Temp)
                {
                    order.OrderTime = DateTime.Now;
                }
                order.Status = OrderStatus.Saved;

                // comprobante

                _dbContext.SaveChanges();

                foreach (var item in order.Items)
                {
                    if (item.Status == OrderItemStatus.Pending)
                    {
                        item.Status = OrderItemStatus.Saved;
                        if (item.Product.InventoryCountDownActive)
                        {
                            item.Product.InventoryCount -= item.Qty;

                        }
                        objPOSCore.SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, stationID);
                        foreach (var q in item.Questions)
                        {
                            if (!q.IsActive) continue;
                            var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                            objPOSCore.SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, stationID);
                        }
                    }                
                }              
            }

            _dbContext.SaveChanges();

            HttpContext.Session.Remove("CurrentOrderID");
            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult ChangeModel(long orderId)
        {
            var order = _dbContext.Orders.Include(s=>s.Items).Include(s=>s.Seats).FirstOrDefault(s=>s.ID == orderId);
            if (order.OrderMode == OrderMode.Seat) order.OrderMode = OrderMode.Standard;
            else if (order.OrderMode == OrderMode.Standard)
            {
                order.OrderMode = OrderMode.Seat;
                order.Seats = new List<SeatItem>();
                for(var i = 0; i < order.Person; i++)
                {
                    var seat = new SeatItem()
                    {
                        OrderId = order.ID,
                        SeatNum = i + 1,
                        Items = new List<OrderItem>()
                    };

                    order.Seats.Add(seat);
                }

            }
            _dbContext.SaveChanges();
            return Json(new {status = 0});
        }

        [HttpPost]
        public JsonResult ChangeBarcodeMode(long orderId)
        {
            var order = _dbContext.Orders.FirstOrDefault(s => s.ID == orderId);
            var mode = "";
            if (order.OrderMode == OrderMode.Invoice)
            {
                mode = "Quote";
                order.OrderMode = OrderMode.Quote;
            }
            else if (order.OrderMode == OrderMode.Quote)
            {
                mode = "Invoice";
                order.OrderMode = OrderMode.Invoice; 
            }

            _dbContext.SaveChanges();
            return Json(new { status = 0, mode });
        }

        [HttpPost]
		public JsonResult AddQuestionToItem([FromQuery]long ItemId, [FromQuery] long servingSizeID, [FromBody]List<AddQuestionModel> questions)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.MenuSelect).ThenInclude(s => s.Groups).FirstOrDefault(s => s.ID == stationID);

            var orderItem = _dbContext.OrderItems.Include(s=>s.Product).ThenInclude(s=>s.ServingSizes).Include(s => s.Product).ThenInclude(s => s.Questions).Include(s=>s.Order).ThenInclude(s=>s.Items).Include(s=>s.Questions).FirstOrDefault(s=>s.ID == ItemId);
            var order = GetOrder(orderItem.Order.ID);
			var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
			if (servingSizeID > 0 && orderItem.Product.HasServingSize)
            {
                bool IsAddExisting = false;
                if(orderItem.Product.Questions == null || orderItem.Product.Questions.Count == 0)
                {
                    var existitem = order.Items.FirstOrDefault(s => s.Product == orderItem.Product && s.ServingSizeID == servingSizeID && s.ID != orderItem.ID && !s.IsDeleted && (s.Status == OrderItemStatus.Pending || s.Status == OrderItemStatus.Printed) && s.SeatNum == orderItem.SeatNum && s.DividerNum == orderItem.DividerNum);
                    if (existitem != null)
                    {
						existitem.Qty += 1;
                        orderItem.IsDeleted = true;
						existitem.Costo = GetCostOrderItem(orderItem.Product.ID, existitem.Qty);
						_dbContext.SaveChanges();
						var ret2 = ApplyPromotion(existitem, 1);
						_dbContext.SaveChanges();
						if (ret2)
						{
							var orderDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
							foreach (var d in orderDiscounts)
							{
								order.Discounts.Remove(d);
							}

							foreach (var item in order.Items)
							{
								var itemDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
								foreach (var i in itemDiscounts)
								{
									item.Discounts.Remove(i);
								}
							}
						}
                        
						_dbContext.SaveChanges();
						order.GetTotalPrice(voucher);
						_dbContext.SaveChanges();
                        IsAddExisting = true;
						return Json("");
					}
				}

                var defaultServingSize = orderItem.Product.ServingSizes.FirstOrDefault(s => s.ServingSizeID == servingSizeID);
                if (defaultServingSize != null)
                {
                    var order1 = GetOrder(orderItem.Order.ID);
					int priceSelect = station.PriceSelect;
					if (station.SalesMode == SalesMode.Restaurant && order1.Area != null)
					{
						try
						{
							var prices = JsonConvert.DeserializeObject<List<AreaModel>>(station.AreaPrices);
							var areaPrice = prices.FirstOrDefault(s => s.AreaID == order1.Area.ID);
							if (areaPrice != null && areaPrice.PriceSelect > 0)
							{
								priceSelect = areaPrice.PriceSelect;
							}
						}
						catch { }
					}

					orderItem.ServingSizeID = (int)defaultServingSize.ServingSizeID;
                    orderItem.ServingSizeName = defaultServingSize.ServingSizeName;
                    orderItem.Price = defaultServingSize.Price[priceSelect - 1];
                    orderItem.OriginalPrice = orderItem.Price;
                    orderItem.SubTotal = orderItem.Price * orderItem.Qty;
                    orderItem.Costo = GetCostOrderItem(orderItem.Product.ID, orderItem.Qty, (int)defaultServingSize.ServingSizeID);
                    _dbContext.SaveChanges();
                }
                var ret = ApplyPromotion(orderItem, orderItem.Qty);
                if (ret)
                {
                    var orderDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
                    foreach (var d in orderDiscounts)
                    {
                        order.Discounts.Remove(d);
                    }

                    foreach (var item in order.Items)
                    {
                        var itemDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
                        foreach (var i in itemDiscounts)
                        {
                            item.Discounts.Remove(i);
                        }
                    }
                }
            }
           
            decimal answerTotalVenta = 0;
            decimal answerTotalCosto = 0;

            foreach (var q in questions)
            {
                var question = _dbContext.Questions.Include(s => s.Answers).ThenInclude(s => s.Product)
                    .ThenInclude(s => s.ServingSizes).FirstOrDefault(s => s.ID == q.QuestionId);

               

                    if (question != null)
                    {
                        var index = 0;
                        int iindex = 0;
                        var answers = q.Answers.OrderBy(s => s.Order);
                        foreach (var answer in question.Answers)
                        {
                            var a = q.Answers.FirstOrDefault(s => s.AnswerId == answer.ID);

                            var servingsizeName = "";
                            int servingsizeId = 0;
                            var price = 0.0m;
                            var canRoll = false;
                            if (answer.RollPrice > 0)
                            {
                                price = answer.RollPrice;
                                canRoll = true;
                            }
                            else if (answer.FixedPrice > 0)
                            {
                                price = answer.FixedPrice;
                            }
                            else
                            {
                                try
                                {
                                    price = answer.Product.Price[(int)answer.PriceType - 1];
                                    if (answer.MatchSize && orderItem.ServingSizeID > 0)
                                    {
                                        if (answer.Product.HasServingSize)
                                        {
                                            var servingsize = answer.Product.ServingSizes.FirstOrDefault(s =>
                                                s.ServingSizeID == orderItem.ServingSizeID);
                                            if (servingsize != null)
                                            {
                                                price = servingsize.Price[(int)answer.PriceType - 1];
                                                servingsizeName = servingsize.ServingSizeName;
                                                servingsizeId = orderItem.ServingSizeID;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (answer.Product.HasServingSize && answer.ServingSizeID > 0)
                                        {
                                            var servingsize = answer.Product.ServingSizes.FirstOrDefault(s =>
                                                s.ServingSizeID == answer.ServingSizeID);
                                            if (servingsize != null)
                                            {
                                                price = servingsize.Price[(int)answer.PriceType - 1];
                                                servingsizeName = servingsize.ServingSizeName;
                                                servingsizeId = answer.ServingSizeID;
                                            }
                                        }
                                    }

                                }
                                catch
                                {
                                }
                            }

                            if (orderItem.Questions == null)
                                orderItem.Questions = new List<QuestionItem>();

                            if (a != null)
                            {
                                if (a.Qty == 0) a.Qty = 1;

                                var questionItem = orderItem.Questions.Where(r => r.Answer.Product == answer.Product)
                                    .FirstOrDefault();

                                if (questionItem == null)
                                {
                                    questionItem = new QuestionItem()
                                    {
                                        Answer = answer,
                                        Description = answer.Product.Name,
                                        Price = price,
                                        CanRoll = canRoll,
                                        Qty = a.Qty,
                                        Divisor = (DivisorType)a.Divisor,
                                        ServingSizeID = servingsizeId,
                                        ServingSizeName = servingsizeName,
                                        IsPreSelect = answer.IsPreSelected,
                                        FreeChoice = 0
                                    };
                                    orderItem.Questions.Add(questionItem);
                                }
                                else
                                {
                                    questionItem.Answer = answer;
                                    questionItem.Description = answer.Product.Name;
                                    questionItem.Price = price;
                                    questionItem.CanRoll = canRoll;
                                    questionItem.Qty = a.Qty;
                                    questionItem.Divisor = (DivisorType)a.Divisor;
                                    questionItem.ServingSizeID = servingsizeId;
                                    questionItem.ServingSizeName = servingsizeName;
                                    questionItem.IsPreSelect = answer.IsPreSelected;
                                    questionItem.IsActive = answer.IsActive;
                                    questionItem.FreeChoice = 0;
                                }


                                if (answer.FixedPrice == 0 && index < question.FreeChoice)
                                {
                                    for (int j = 0; j < a.Qty; j++)
                                    {
                                        if (index < question.FreeChoice)
                                        {
                                            questionItem.FreeChoice++;
                                            index++;
                                        }
                                    }
                                }

                                if (q.SmartButtons != null)
                                {
                                    var smartid = q.SmartButtons[iindex];
                                    var smartbutton = _dbContext.SmartButtons.FirstOrDefault(s => s.ID == smartid);
                                    if (smartbutton != null)
                                    {
                                        if (smartbutton.IsAfterText)
                                        {
                                            questionItem.Description =
                                                questionItem.Description + " " + smartbutton.Name;
                                        }
                                        else
                                        {
                                            questionItem.Description =
                                                smartbutton.Name + " " + questionItem.Description;
                                        }

                                        if (!smartbutton.IsApplyPrice)
                                        {
                                            price = 0;
                                        }
                                    }

                                }

                                if (answer.HasQty)
                                {
                                    questionItem.Description = questionItem.Description;
                                    if (a.Qty > 1)
                                        questionItem.Description = questionItem.Description + " x " + questionItem.Qty;
                                }


                                if (!string.IsNullOrEmpty(a.SubAnswers))
                                {
                                    var subanswers = a.SubAnswers.Split(",").ToList();
                                    var subdescription = "";
                                    var subprice = 0;
                                    var subquestion = _dbContext.Questions.Include(s => s.Answers)
                                        .ThenInclude(s => s.Product).Include(s => s.Products)
                                        .FirstOrDefault(s => s.ID == answer.ForcedQuestionID);
                                    foreach (var sa in subquestion.Answers)
                                    {
                                        if (subanswers.Contains("" + sa.ID))
                                        {
                                            subdescription += sa.Product.Name + "<br />";

                                        }
                                    }

                                    questionItem.SubDescription = subdescription;
                                }


                                if (questionItem.FreeChoice <= 0)
                                {
                                    answerTotalVenta = answerTotalVenta + price;
                                }


                                if (answer.Product.HasServingSize)
                                {
                                    answerTotalCosto = answerTotalCosto +
                                                       GetCostOrderItem(answer.Product.ID, a.Qty,
                                                           orderItem.ServingSizeID);
                                }
                                else
                                {
                                    answerTotalCosto = answerTotalCosto + GetCostOrderItem(answer.Product.ID, a.Qty);
                                }

                                iindex++;
                            }
                            else if (a == null && answer.IsPreSelected)
                            {
                                var questionItem = orderItem.Questions.Where(r => r.Answer.Product == answer.Product)
                                    .FirstOrDefault();
                                if (questionItem == null)
                                {
                                    if (answer.Comentario)
                                    {
                                        questionItem = new QuestionItem()
                                        {
                                            Answer = answer,
                                            Description = "No " + answer.Product.Name,
                                            IsPreSelect = answer.IsPreSelected,
                                            IsActive = false
                                        };
                                        orderItem.Questions.Add(questionItem);
                                    }
                                }
                                else
                                {
                                    if (answer.Comentario)
                                    {
                                        questionItem.Answer = answer;
                                        questionItem.Description = "No " + answer.Product.Name;
                                        questionItem.IsPreSelect = answer.IsPreSelected;
                                        questionItem.IsActive = false;
                                    }
                                    else
                                    {
                                        orderItem.Questions.Remove(questionItem);
                                    }
                                }

                                answerTotalVenta = answerTotalVenta + price;

                                if (answer.Product.HasServingSize)
                                {
                                    answerTotalCosto = answerTotalCosto +
                                                       GetCostOrderItem(answer.Product.ID, 1, orderItem.ServingSizeID);
                                }
                                else
                                {
                                    answerTotalCosto = answerTotalCosto + GetCostOrderItem(answer.Product.ID, 1);
                                }
                            }
                            else if (a == null)
                            {
                                var questionItem = orderItem.Questions.Where(r => r.Answer.Product == answer.Product)
                                    .FirstOrDefault();

                                if (questionItem != null)
                                {
                                    orderItem.Questions.Remove(questionItem);

                                }

                            }

                            orderItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);

                        _dbContext.SaveChanges();
                        }
                    }
            
        }
           
            orderItem.AnswerVenta = answerTotalVenta;
            orderItem.AnswerCosto= answerTotalCosto;
            orderItem.Costo = orderItem.Costo + answerTotalCosto;

            order.GetTotalPrice(voucher);
            _dbContext.SaveChanges();
            return Json("");
		}
        
        [HttpPost]
        public JsonResult GetInventory(long itemId)
        {
            var orderItem = _dbContext.OrderItems.Include(s=>s.Product).FirstOrDefault(s => s.ID == itemId);
            return Json(new { status = 0, product = orderItem.Product });
        }

        [HttpPost]
        public JsonResult UpdateInventoryCountDown([FromBody]InventoryCountDownModel model)
        {
            var product = _dbContext.Products.FirstOrDefault(s => s.ID == model.ProdId);
            if (product == null)
            {
                var item = _dbContext.OrderItems.Include(s=>s.Product).FirstOrDefault(s=>s.ID == model.ItemId);
                product = item.Product;
            }
            product.InventoryCount = model.Qty;
            product.InventoryCountDownActive = model.Active;
            
            _dbContext.SaveChanges();

            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult AddModifierToItem([FromQuery] long ItemId, [FromBody] List<AddModifierModel> questions)
        {
            var orderItem = _dbContext.OrderItems.Include(s => s.Order).ThenInclude(s => s.Items).Include(s => s.Questions).FirstOrDefault(s => s.ID == ItemId);
            decimal answerTotalVenta = 0;
            decimal answerTotalCosto = 0;

            
            
            foreach (var q in questions)
            {
                var question = _dbContext.Questions.Include(s => s.Answers).ThenInclude(s => s.Product).ThenInclude(s=>s.ServingSizes).FirstOrDefault(s => s.ID == q.QuestionId);
                if (question != null)
                {
					int index = 0;
                    int freechoice = 0;
                    int iindex = 0;
                    var lstRemover = new List<Answer>();
                    
                    foreach (var answer in question.Answers)
                    {
                        var a = q.Answers.FirstOrDefault(s => s.AnswerId == answer.ID);
                        
                        
                        var servingsizeName = "";
                        var price = 0.0m;
                        var canRoll = false;
                        if (answer.RollPrice > 0)
                        {
                            price = answer.RollPrice;
                            canRoll = true;
                        }
                        else if (answer.FixedPrice > 0)
                        {
                            price = answer.FixedPrice;
                        }
                        else
                        {
                            try
                            {
                                price = answer.Product.Price[(int)answer.PriceType - 1];
                                if (answer.Product.HasServingSize)
                                {
                                    var servingsize = answer.Product.ServingSizes.FirstOrDefault(s => s.ServingSizeID == orderItem.ServingSizeID);
                                    if (servingsize != null)
                                    {
                                        price = servingsize.Price[(int)answer.PriceType - 1];
                                        servingsizeName = servingsize.ServingSizeName;
                                    }
                                }
                            }
                            catch { }
                        }

                        if (orderItem.Questions == null)
                            orderItem.Questions = new List<QuestionItem>();

                        if (a != null)
                        {
                            if (a.Qty == 0) a.Qty = 1;
                            
                            var questionItem =  orderItem.Questions.Where(r =>r.Answer!=null &&  r.Answer.Product == answer.Product).FirstOrDefault();
                            //var questionExiste = false;
                            if (questionItem==null)
                            {
                                questionItem = new QuestionItem()
                                {
                                    Answer = answer,
                                    Description = answer.Product.Name,
                                    Price = price,
                                    CanRoll = canRoll,
                                    Qty = a.Qty,
                                    Divisor = (DivisorType)a.Divisor,
                                    ServingSizeID = orderItem.ServingSizeID,
                                    ServingSizeName = servingsizeName,
                                    IsPreSelect = answer.IsPreSelected,
                                    IsActive = answer.IsActive,
                                    FreeChoice = 0
                                };
                                orderItem.Questions.Add(questionItem);
                            }
                            else
                            {
                                questionItem.Answer = answer;
                                questionItem.Description = answer.Product.Name;
                                questionItem.Price = price;
                                questionItem.CanRoll = canRoll;
                                questionItem.Qty = a.Qty;
                                questionItem.Divisor = (DivisorType)a.Divisor;
                                questionItem.ServingSizeID = orderItem.ServingSizeID;
                                questionItem.ServingSizeName = servingsizeName;
                                questionItem.IsPreSelect = answer.IsPreSelected;
                                questionItem.IsActive = answer.IsActive;
                                questionItem.FreeChoice = 0;
                                //questionExiste = true;
                            }
                            
                            
                            if (answer.FixedPrice == 0 && index < question.FreeChoice)
                            {
                                for (int j = 0; j < a.Qty; j++)
                                {
                                    if (index < question.FreeChoice)
                                    {
                                        questionItem.FreeChoice++;
                                        index++;
                                    }
                                }
                            }
                            if (q.SmartButtons != null)
                            {
                                var smartid = q.SmartButtons[iindex];
                                var smartbutton = _dbContext.SmartButtons.FirstOrDefault(s => s.ID == smartid);
                                if (smartbutton != null)
                                {
                                    if (smartbutton.IsAfterText)
                                    {
                                        questionItem.Description = questionItem.Description + " " + smartbutton.Name;
                                    }
                                    else
                                    {
                                        questionItem.Description = smartbutton.Name + " " + questionItem.Description;
                                    }

                                    if (!smartbutton.IsApplyPrice)
                                    {
                                        price = 0;
                                    }
                                }

                            }

                            if (answer.HasQty)
                            {                              
                                questionItem.Description = questionItem.Description;
                                if (a.Qty > 1)
                                    questionItem.Description = questionItem.Description + " x " + questionItem.Qty;
                            }


                            if (!string.IsNullOrEmpty(a.SubAnswers))
                            {
                                var subanswers = a.SubAnswers.Split(",").ToList();
                                var subdescription = "";
                                var subprice = 0;
                                var subquestion = _dbContext.Questions.Include(s => s.Answers).ThenInclude(s => s.Product).Include(s => s.Products).FirstOrDefault(s => s.ID == answer.ForcedQuestionID);
                                foreach (var sa in subquestion.Answers)
                                {
                                    if (subanswers.Contains("" + sa.ID))
                                    {
                                        subdescription += sa.Product.Name + "<br />";

                                    }
                                }
                                questionItem.SubDescription = subdescription;
                            }
                            
                            if (questionItem.FreeChoice <= 0) {
                               answerTotalVenta = answerTotalVenta + price;
                            }
                            
                            if (answer.Product.HasServingSize)
                            {
                                answerTotalCosto = answerTotalCosto + GetCostOrderItem(answer.Product.ID, a.Qty, orderItem.ServingSizeID);
                            }
                            else
                            {
                                answerTotalCosto = answerTotalCosto + GetCostOrderItem(answer.Product.ID, a.Qty);
                            }
                                                        

                            /*if (!questionExiste)
                            {
                                orderItem.Questions.Add(questionItem);    
                            }*/
                            
                            iindex++;
                        }
                        else if (a == null && answer.IsPreSelected)
                        {
                            var questionItem =  orderItem.Questions.Where(r =>r.Answer!=null && r.Answer.Product == answer.Product).FirstOrDefault();
                            if (questionItem==null)
                            {
                                if (answer.Comentario)
                                {
                                    questionItem = new QuestionItem()
                                    {
                                        Answer = answer,
                                        Description = "No " + answer.Product.Name,
                                        IsPreSelect = answer.IsPreSelected,
                                        IsActive = false
                                    };
                                    orderItem.Questions.Add(questionItem);
                                }
                            }
                            else
                            {
                                if (answer.Comentario)
                                {
                                    questionItem.Answer = answer;
                                    questionItem.Description = "No " + answer.Product.Name;
                                    questionItem.IsPreSelect = answer.IsPreSelected;
                                    questionItem.IsActive = false;
                                }
                                else
                                {
                                    orderItem.Questions.Remove(questionItem);
                                }
                            }
                            
                            answerTotalVenta = answerTotalVenta + price;
                            
                            if (answer.Product.HasServingSize)
                            {
                                answerTotalCosto = answerTotalCosto + GetCostOrderItem(answer.Product.ID, 1, orderItem.ServingSizeID);
                            }
                            else
                            {
                                answerTotalCosto = answerTotalCosto + GetCostOrderItem(answer.Product.ID, 1);
                            }
                        }
                        else if (a == null)
                        {
                            
                                var questionItem =  orderItem.Questions.Where(r => r.Answer!=null && r.Answer.Product == answer.Product).FirstOrDefault();

                                if (questionItem != null)
                                {
                                    orderItem.Questions.Remove(questionItem);
                                
                                }
                            
                        }

                        _dbContext.SaveChanges();

                        //var order = GetOrder(orderItem.Order.ID);
                        //var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
                        //order.GetTotalPrice(voucher);
                        //_dbContext.SaveChanges();                     
                    }
                    
                    //Removemos lo que ya no se eligio
                    //foreach (var itemRemove in lstRemover)
                    //{
                    //    question.Answers.Remove(itemRemove);
                    //}
                }
            }

            
            
            var order = GetOrder(orderItem.Order.ID);
            var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
            
            orderItem.AnswerVenta = answerTotalVenta;
            orderItem.AnswerCosto= answerTotalCosto;
            orderItem.Costo = orderItem.Costo + answerTotalCosto;

            order.GetTotalPrice(voucher);
            _dbContext.SaveChanges();
            
            return Json("");
        }

        [HttpPost]
		public JsonResult HoldOrderItem([FromBody]HoldItemModel model)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var orderItem = _dbContext.OrderItems.Include(s => s.Product).FirstOrDefault(o => o.ID == model.ItemId);
            orderItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
            if (model.Type == 1)
			{
				orderItem.Status = OrderItemStatus.HoldManually;
			}
			else if (model.Type == 2)
			{
				orderItem.Status = OrderItemStatus.HoldAutomatic;
				var holdtime = DateTime.Now.AddHours(model.Hour).AddMinutes(model.Minute);
				orderItem.HoldTime = holdtime;
			}
			else
			{
				return Json(new { status = 1 });
			}
            _dbContext.SaveChanges();
            return Json(new {status = 0});
		}

		[HttpPost]
		public JsonResult ReorderItem([FromBody] ReorderModel model)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));
            var orderItem = _dbContext.OrderItems.Include(s=>s.Order).ThenInclude(s=>s.Items).Include(s => s.Order).Include(s => s.Product).Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Questions).ThenInclude(s=>s.Answer).Include(s=>s.Discounts).FirstOrDefault(o => o.ID == model.ItemId);

            var nItem = new OrderItem();
            nItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
            nItem.Order = orderItem.Order;
            nItem.Price = orderItem.Price;
            nItem.OriginalPrice = orderItem.OriginalPrice;
            nItem.Product = orderItem.Product;
            nItem.MenuProductID = orderItem.MenuProductID;
            nItem.Name = orderItem.Product.Name;
            nItem.SeatNum = model.SeatNum;
            nItem.Qty = orderItem.Qty;
            nItem.SubTotal = nItem.Price * nItem.Qty;
            nItem.Status = OrderItemStatus.Pending;
			nItem.Discount = orderItem.Discount;
            nItem.Taxes = orderItem.Taxes;
            nItem.Course = orderItem.Course;
            nItem.CourseID = orderItem.CourseID;
            nItem.ServingSizeID = orderItem.ServingSizeID;
            nItem.ServingSizeName = orderItem.ServingSizeName;
            nItem.Note = orderItem.Note;
            
            if (orderItem.Taxes != null && orderItem.Taxes.Any())
            {
                nItem.Taxes = new List<TaxItem>();
                foreach (var q in orderItem.Taxes)
                {
                    var nq = new TaxItem()
                    {
                        Description = q.Description,
                        Amount = q.Amount,
                        Percent = q.Percent,
                        TaxId = q.TaxId,
                        ToGoExclude = q.ToGoExclude,
                        BarcodeExclude = q.BarcodeExclude
                    };
                    nItem.Taxes.Add(nq);
                }
            }
            nItem.Propinas = orderItem.Propinas;
            if (orderItem.Propinas != null && orderItem.Propinas.Any())
            {
                nItem.Propinas = new List<PropinaItem>();
                foreach (var q in orderItem.Propinas)
                {
                    var nq = new PropinaItem()
                    {
                        Description = q.Description,
                        Amount = q.Amount,
                        Percent = q.Percent,
                        PropinaId = q.PropinaId,
                        ToGoExclude = q.ToGoExclude,
                        BarcodeExclude = q.BarcodeExclude
                    };
                    nItem.Propinas.Add(nq);
                }
            }
            if (orderItem.Questions != null && orderItem.Questions.Any())
			{
				nItem.Questions = new List<QuestionItem>();
				foreach(var q in  orderItem.Questions)
				{
					var nq = new QuestionItem()
					{
						Description = q.Description,
						Price = q.Price,
						Answer = q.Answer,
						CanRoll = q.CanRoll,
                        IsActive = q.IsActive,
                        IsPreSelect =  q.IsPreSelect,
                        Qty = q.Qty,
                        SubDescription = q.SubDescription,
                        SubPrice = q.SubPrice,
                        ServingSizeID = q.ServingSizeID,
                        ServingSizeName = q.ServingSizeName,
                        FreeChoice = q.FreeChoice,
                        Divisor = q.Divisor
					};
					nItem.Questions.Add(nq);
				}
			}
			if (orderItem.Discounts != null && orderItem.Discounts.Any())
			{
                nItem.Discounts = new List<DiscountItem>();
				foreach(var d in orderItem.Discounts)
				{
                    var nq = new DiscountItem()
                    {
                        Amount = d.Amount,
						Name = d.Name,
						ItemID = d.ItemID,
						ItemType = d.ItemType,
						TargetType = d.TargetType,
                    };
                    nItem.Discounts.Add(nq);
                }
            }
            orderItem.Order.Items.Add(nItem);
            var ret = ApplyPromotion(nItem, orderItem.Qty);
			if (ret)
			{
                var order = GetOrder(orderItem.Order.ID);
				var orderDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
				foreach (var d in orderDiscounts)
				{
					order.Discounts.Remove(d);
				}

				foreach (var item in order.Items)
				{
					var itemDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
					foreach (var i in itemDiscounts)
					{
						item.Discounts.Remove(i);
					}
				}
			}
			_dbContext.SaveChanges();
           

            if (orderItem.Order.OrderMode == OrderMode.Seat && model.SeatNum > 0)
            {
                var order = GetOrder(orderItem.Order.ID);
                var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
                order.GetTotalPrice(voucher);

                if (order.Seats == null) order.Seats = new List<SeatItem>();

                var existSeat = order.Seats.FirstOrDefault(s => s.SeatNum == model.SeatNum);
                if (existSeat != null)
                {
                    existSeat.Items.Add(nItem);
                }
                else
                {
                    existSeat = new SeatItem() { OrderId = orderItem.Order.ID, SeatNum = model.SeatNum, Items = new List<OrderItem>() { nItem } };
                    order.Seats.Add(existSeat);
                }
            }
            else
            {
                var order = GetOrder(orderItem.Order.ID);
                var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
                order.GetTotalPrice(voucher);
            }

			_dbContext.SaveChanges();

            return Json("");
		}

        //Pendiente - Se movió a POSCore, quitar y cambiar todas las funciones que depende de ella
        private void VoidItem(CancelItemModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var orderItem = _dbContext.OrderItems.Include(s => s.Product).Include(s => s.Questions).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Order).Include(s => s.Order).ThenInclude(s => s.Items).Include(s => s.Order).ThenInclude(s => s.Seats).ThenInclude(s => s.Items).Include(s => s.Order).ThenInclude(s => s.Discounts).FirstOrDefault(o => o.ID == model.ItemId);
            orderItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);

            if (orderItem.Status == OrderItemStatus.Kitchen || orderItem.Status == OrderItemStatus.Printed)
            {
                var cancelReason = _dbContext.CancelReasons.FirstOrDefault(s => s.ID == model.ReasonId);
                var cancelItem = new CanceledOrderItem()
                {
                    ForceDate = objPOSCore.getCurrentWorkDate(stationID),
                    Item = orderItem,
                    Reason = cancelReason,
                    Product = orderItem.Product
                };

                if (cancelReason != null && !cancelReason.IsReduceInventory)
                {
                    if (orderItem.Product.InventoryCountDownActive)
                    {
                        orderItem.Product.InventoryCount += orderItem.Qty;

                    }
                    VoidAddProduct(orderItem.ID, orderItem.Product.ID, -orderItem.Qty, orderItem.ServingSizeID);
                    foreach (var q in orderItem.Questions)
                    {
                        if (!q.IsActive) continue;
                        var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
						VoidAddProduct(orderItem.ID, qitem.Answer.Product.ID, -orderItem.Qty * qitem.Qty, qitem.ServingSizeID);
                    }
                }

                if (!string.IsNullOrEmpty(model.Pin))
                {
                    var user = _dbContext.User.First(s => s.Pin == model.Pin);
                    cancelItem.ForceUpdatedBy = user.FullName;
                }

                _dbContext.CanceledItems.Add(cancelItem);
                _dbContext.SaveChanges();
            }
            
            if (orderItem.Order.OrderMode == OrderMode.Seat)
            {
                var seat = orderItem.Order.Seats.FirstOrDefault(s => s.SeatNum == orderItem.SeatNum);
                if (seat != null && seat.Items != null)
                    seat.Items.Remove(orderItem);
            }
            
            orderItem.IsDeleted = true;

            _dbContext.SaveChanges();
           
        }

        [HttpPost]
        public JsonResult VoidOrderItem([FromBody] CancelItemModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            int status = objPOSCore.VoidOrderItem(model, stationID);

            return Json(new { status = status });

            //var orderItem = _dbContext.OrderItems.Include(s => s.Product).Include(s => s.Questions).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Order).Include(s => s.Order).ThenInclude(s => s.Items.Where(s=>!s.IsDeleted)).Include(s => s.Order).ThenInclude(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).Include(s => s.Order).ThenInclude(s => s.Discounts).FirstOrDefault(o => o.ID == model.ItemId);
            //orderItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);

            //if (orderItem == null)
            //    return Json(new { status = 1 });

            //var orderId = orderItem.Order.ID;
            //var order = GetOrder(orderItem.Order.ID);
           
            //var sameItems = new List<OrderItem>();
            //if (orderItem.Order.OrderMode == OrderMode.Seat)
            //{
            //    sameItems = order.Items.Where(s=>s.Product == orderItem.Product && s.SeatNum == orderItem.SeatNum && !s.IsDeleted).ToList();
            //}
            //else
            //{
            //    sameItems = order.Items.Where(s => s.Product == orderItem.Product && !s.IsDeleted).ToList();
            //}
            //bool HasPromotion = false;
            //foreach(var item in sameItems)
            //{
            //    var promotion = item.Discounts.Where(s => s.ItemType == DiscountItemType.Promotion).ToList();
            //    if (promotion.Any())
            //    {
            //        HasPromotion = true;
            //        break;
            //    }
            //}

            ////if (HasPromotion)
            ////{
            ////    foreach(var item in sameItems)
            ////    {
            ////        VoidItem(new CancelItemModel()
            ////        {
            ////            ItemId = item.ID,
            ////            ReasonId = model.ReasonId
            ////        }); 
            ////    }                
            ////}
            ////else
            //{
            //    VoidItem(model);
            //}

            //order = GetOrder(orderId);
            
            //var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);

            //order.GetTotalPrice(voucher);
            //if (order.TotalPrice == 0 && order.OrderMode == OrderMode.Divide)
            //{
            //    order.OrderMode = OrderMode.Standard;
            //}
            //_dbContext.SaveChanges();


            //return Json(new {status = 0});
        }

        [HttpPost]
        public JsonResult VoidOrder([FromBody] VoidOrderModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            int status = objPOSCore.VoidOrder(model, stationID);

            return Json(new { status = status });

            //var order = GetOrder(model.OrderId);
            //var items = order.Items.Select(s => s.ID).ToList();
            //foreach(var item in items)
            //{
            //    VoidItem(new CancelItemModel() { ItemId = item, ReasonId = model.ReasonId });
            //}
            //order.Status = OrderStatus.Void;
            //if (order.OrderType == OrderType.Delivery)
            //{
            //    var delivery = _dbContext.Deliverys.FirstOrDefault(s => s.Order == order);
            //    if (delivery != null)
            //    {
            //        delivery.Status = StatusEnum.Cancelado;
            //    }
            //}

            //if (!string.IsNullOrEmpty(model.Pin)) {
            //    var user = _dbContext.User.First(s => s.Pin == model.Pin);
            //    order.ForceUpdatedBy = user.FullName;
            //}            

            //var kitchenOrder = _dbContext.KitchenOrder.Where(s => s.OrderID == order.ID);
            //foreach(var o in kitchenOrder)
            //{
            //    o.Status = KitchenOrderStatus.Void;
            //}

            //_dbContext.SaveChanges();

            //return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult UpdateOrderPromesaPago([FromBody] PromesaPagoModel model)
        {
            var order = GetOrder(model.OrderId);
            order.PromesaPago = model.PromesaPago;

            _dbContext.SaveChanges();

            return Json(new { status = 0, monto = order.PromesaPago, devuelto = order.PromesaPago - order.TotalPrice  });
        }

        [HttpPost]
        public JsonResult ChangeQtyItem([FromBody] QtyChangeModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            int status = objPOSCore.ChangeQtyItem(model, stationID);

            return Json(new { status = status });

            //         var orderItem = _dbContext.OrderItems.Include(s => s.Questions).Include(s=>s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s=>s.Order).ThenInclude(s=>s.Items).Include(s=>s.Order).ThenInclude(s=>s.Discounts).FirstOrDefault(o => o.ID == model.ItemId);
            //         var orderId = orderItem.Order.ID;
            //         orderItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
            //         if (orderItem.Status == OrderItemStatus.Pending || orderItem.Status == OrderItemStatus.Printed)
            //         {
            //	orderItem.Qty = model.Qty;
            //             orderItem.SubTotal = orderItem.Price * model.Qty;
            //         }

            //         var ret = ApplyPromotion(orderItem, model.Qty, true);
            //if (ret)
            //{
            //             var order = orderItem.Order;
            //	var orderDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
            //	foreach (var d in orderDiscounts)
            //	{
            //		order.Discounts.Remove(d);
            //	}

            //	foreach (var item in order.Items)
            //	{
            //		var itemDiscounts = order.Discounts.Where(s => s.ItemType == DiscountItemType.Discount);
            //		foreach (var i in itemDiscounts)
            //		{
            //			item.Discounts.Remove(i);
            //		}
            //	}
            //}
            //_dbContext.SaveChanges();

            //         var xorder = GetOrder(orderId);

            //         var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == orderItem.Order.ComprobantesID);
            //         xorder.GetTotalPrice(voucher);
            //         _dbContext.SaveChanges();
            //         return Json(new { status = 0 });
        }

        [HttpPost]
		public JsonResult FireOrderItem([FromBody] FireItemModel model)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var orderItem = _dbContext.OrderItems.Include(s=>s.Product).Include(s=>s.Questions).Include(s => s.Order).ThenInclude(s=>s.Items).FirstOrDefault(o => o.ID == model.ItemId);
            orderItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);

            //var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            //var stationID = int.Parse(GetCookieValue("StationID"));

            orderItem.Status = OrderItemStatus.Kitchen;
            objPOSCore.SendKitchenItem(orderItem.Order.ID, orderItem.ID);
            objPOSCore.SubstractProduct(orderItem.ID, orderItem.Product.ID, orderItem.Qty, orderItem.ServingSizeID, orderItem.Order.OrderType, stationID);
            foreach (var q in orderItem.Questions)
            {
                if (!q.IsActive) continue;
                var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                objPOSCore.SubstractProduct(orderItem.ID, qitem.Answer.Product.ID, orderItem.Qty * qitem.Qty, qitem.ServingSizeID, orderItem.Order.OrderType, stationID);
            }
            var holdItem = orderItem.Order.Items.FirstOrDefault(s => s.Status == OrderItemStatus.HoldManually || s.Status == OrderItemStatus.HoldAutomatic);
			if (holdItem == null)
			{
				orderItem.Order.Status = OrderStatus.Pending;
			}

			_dbContext.SaveChanges();
            //var stationID = int.Parse(GetCookieValue("StationID"));
            _printService.PrintKitchenItems(stationID, orderItem.Order.ID, new List<OrderItem>() { orderItem}, Request.Cookies["db"]);

            return Json(new {status = 0});
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
                var customerData = _dbContext.Discounts.Where(s => !s.IsDeleted && s.IsActive).Select(s => new
                {
                    s.ID,
                    Name = s.Name,
                    Amount = s.Amount,
                    AmountType = s.DiscountAmountType == AmountType.Percent ? "%" : "",
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
		public JsonResult GetItemModifer(long itemId)
		{
			var orderItem = _dbContext.OrderItems.Include(s=>s.Questions).Include(s=>s.Product).FirstOrDefault(o => o.ID == itemId);
			var product = _dbContext.Products.Include(s=>s.Questions).Include(s=>s.ServingSizes).FirstOrDefault(s => s.ID == orderItem.Product.ID);

			var modifiers = product.Questions.Where(s => !s.IsForced).OrderBy(s=>s.DisplayOrder).ToList();
			if (modifiers != null && modifiers.Count > 0)
			{
				var result = new List<Question>();

				foreach (var m in modifiers)
				{
					var modifier = _dbContext.Questions.Include(s=>s.Answers).ThenInclude(s=>s.Product).Include(s=>s.SmartButtons.OrderBy(s=>s.Order)).ThenInclude(s=>s.Button).FirstOrDefault(s => s.ID == m.ID);

                    foreach (var j in modifier.Answers)
                    {
                        var existe = orderItem.Questions.Where(s => s.Answer != null && s.Answer.Product == j.Product).FirstOrDefault();

                        if (existe != null)
                        {
                            j.IsActive = existe.IsActive;
                            j.Qty = existe.Qty;
                        }else{
                            j.IsActive = false;
                            j.Qty = 1;
                        }
                    }
                    
					result.Add(modifier);
				}
				if (result.Count > 0)
					return Json(new { status = 0, modifiers = result });
            }
            
            var servingSizes = new List<ProductServingSize>();
            var forceds = product.Questions.Where(s => s.IsForced).OrderBy(s=>s.DisplayOrder).ToList();
            if (forceds != null && forceds.Count > 0)
            {
                
                if (product.HasServingSize)
                {
                    servingSizes = product.ServingSizes.OrderBy(s=>s.Order).ToList();
                }
                
                var result = new List<Question>();

                foreach (var m in forceds)
                {
                    var forced = _dbContext.Questions.Include(s=>s.Answers).ThenInclude(s=>s.Product).Include(s=>s.SmartButtons.OrderBy(s=>s.Order)).ThenInclude(s=>s.Button).FirstOrDefault(s => s.ID == m.ID);

                    foreach (var j in forced.Answers)
                    {
                        var existe = orderItem.Questions.Where(s => s.Answer != null && s.Answer.Product == j.Product).FirstOrDefault();

                        if (existe != null)
                        {
                            j.IsActive = existe.IsActive;
                            j.Qty = existe.Qty;
                        }else{
                            j.IsActive = false;
                            j.Qty = 1;
                        }
                    }
                    
                    result.Add(forced);
                }
                if (result.Count > 0)
                    return Json(new { status = 0, questions = result, hasQuestion=true, hasServingSize = product.HasServingSize && servingSizes.Count > 0, servingSizes, productName = product.Name, itemId = itemId  });
            }
            
            if (product.HasServingSize)
            {
                var result = new List<Question>();
                servingSizes = product.ServingSizes.OrderBy(s=>s.Order).ToList();

                foreach (var objServingSize in servingSizes)
                {
                    if (orderItem.ServingSizeID == objServingSize.ServingSizeID)
                    {
                        objServingSize.IsActive = true;
                    }
                    else
                    {
                        objServingSize.IsActive = false;    
                    }
                }
                
                return Json(new { status = 0, questions = result, hasQuestion=true, hasServingSize = product.HasServingSize && servingSizes.Count > 0, servingSizes, productName = product.Name, product = product.ID, itemId = itemId  });
            }

			return Json(new { status = 1 });
		}

		[HttpPost]
		public JsonResult MoveItemToAnotherTable([FromBody] MoveToTableModel model)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var orderItem = _dbContext.OrderItems.Include(s=>s.Product).Include(s=>s.Questions).ThenInclude(s=>s.Answer).Include(s=>s.Taxes).Include(s=>s.Propinas).Include(s=>s.Order).ThenInclude(s=>s.Items).ThenInclude(s=>s.Discounts).FirstOrDefault(s=>s.ID == model.ItemId);
            orderItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
            if (orderItem != null)
            {
                var nItem = orderItem.CopyThis();
                var sourceOrder = GetOrder(orderItem.Order.ID);
                var table = _dbContext.AreaObjects.Include(s=>s.Area).FirstOrDefault(s => s.ID == model.TableId);
                
                var destOrder = _dbContext.Orders.Include(s=>s.Items).Include(s=>s.Seats).ThenInclude(s=>s.Items).FirstOrDefault(s => s.Table == table && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved);
                if (destOrder != null)
                {
                    var emptySeat = 0;
                    var seatNums = new List<int>();
                    try
                    {
                        foreach (var item in destOrder.Seats)
                        {
                            seatNums.Add(item.SeatNum);
                        }
                        for (int i = 1; i < seatNums.Max(); i++)
                        {
                            if (!seatNums.Contains(i))
                            {
                                emptySeat = i;
                                break;
                            }
                        }
                        if (emptySeat == 0) emptySeat = seatNums.Max() + 1;

                    }
                    catch { }
                                       
                    //orderItem.Order = destOrder;
                    var seat = sourceOrder.Seats.FirstOrDefault(s => s.SeatNum == orderItem.SeatNum);
                    if (seat != null)
                    {
                        seat.Items.Remove(orderItem);
                        if (seat.Items.Count == 0)
                        {
                            sourceOrder.Seats.Remove(seat);
                        }
                    }
                    else
                    {
                        sourceOrder.Items.Remove(orderItem);
                    }
                 

                    nItem.Order = destOrder;
                    if (destOrder.OrderMode == OrderMode.Seat)
                    {
                        nItem.SeatNum = emptySeat;
                        destOrder.Seats.Add(new SeatItem { SeatNum = emptySeat, Items = new List<OrderItem>() { nItem } });
                    }
                   
                    destOrder.Items.Add(nItem);
                    _dbContext.SaveChanges();

                    {
                        var kitchenItems = _dbContext.KitchenOrderItem.Where(s => s.OrderItemID == orderItem.ID).ToList();
                        foreach(var item in kitchenItems)
                        {
                            _dbContext.KitchenOrderItem.Remove(item);
                        }

                        objPOSCore.SendKitchenItem(destOrder.ID, nItem.ID);
                    }
                    

                    destOrder = GetOrder(destOrder.ID);
                    var destvoucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == destOrder.ComprobantesID);
                    var srcvoucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == sourceOrder.ComprobantesID);
                    sourceOrder.GetTotalPrice(destvoucher);
                    destOrder.GetTotalPrice(srcvoucher);
                    _dbContext.SaveChanges();
                }
                else
                {
                    sourceOrder = GetOrder(sourceOrder.ID);
                    sourceOrder.Items.Remove(orderItem);
                    var seat = sourceOrder.Seats.FirstOrDefault(s => s.SeatNum == orderItem.SeatNum);
                    if (seat != null)
					{
						seat.Items.Remove(orderItem);
						if (seat.Items.Count == 0)
						{
							sourceOrder.Seats.Remove(seat);
						}
					}

					//var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
                    var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

                    var nOrder = new Order();
                    nOrder.Station = station;
                    nOrder.OrderMode = sourceOrder.OrderMode;
                    nOrder.OrderTime = DateTime.Now; 
                    nOrder.OrderType = sourceOrder.OrderType;
                    nOrder.Status = OrderStatus.Pending;
                    var nvoucher = _dbContext.Vouchers.Include(s=>s.Taxes).FirstOrDefault(s => s.IsPrimary);
                    nOrder.ComprobantesID = nvoucher.ID;
                    nOrder.Table = table;
                    nOrder.Area = table.Area;
                    var user = User.Identity.GetUserName();
                    nOrder.WaiterName = user;

                    nOrder.Items = new List<OrderItem> { nItem };
                    
                    _dbContext.SaveChanges();
                    if (sourceOrder.OrderMode == OrderMode.Seat)
                    {
                        nItem.SeatNum = 1;
                        nOrder.Seats = new List<SeatItem>();
                        nOrder.Seats.Add(new SeatItem() { SeatNum = 1, Items = new List<OrderItem> { nItem } });

                    }
                    
                    _dbContext.Orders.Add(nOrder);
                    _dbContext.SaveChanges();
                    var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == sourceOrder.ComprobantesID);
                    sourceOrder.GetTotalPrice(voucher);
                    nOrder.GetTotalPrice(nvoucher);
                    _dbContext.SaveChanges();
                }
            }

            return Json(new { status = 0 }) ;
		}

        [HttpPost]
        public JsonResult MoveSeatToAnotherTable([FromBody] MoveSeatToTableModel model)
        {
            
            var sourceOrder = _dbContext.Orders.Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Seats).ThenInclude(s => s.Items).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Items).ThenInclude(s => s.Questions).ThenInclude(s=>s.Answer).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == model.OrderId);
            var sourceSeat = sourceOrder.Seats.FirstOrDefault(s=>s.SeatNum == model.SeatNum);
            sourceSeat = _dbContext.SeatItems.Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).Include(s=>s.Items).ThenInclude(s=>s.Discounts).Include(s=>s.Items).ThenInclude(s=>s.Questions).ThenInclude(s=>s.Answer).Include(s=>s.Items).ThenInclude(s=>s.Product).FirstOrDefault(s => s.ID == sourceSeat.ID);
            var items = new List<OrderItem>();
            foreach(var item in sourceSeat.Items)
            {
                items.Add(item.CopyThis());
            }

            var table = _dbContext.AreaObjects.Include(s => s.Area).FirstOrDefault(s => s.ID == model.TableId);
            
            var destOrder = _dbContext.Orders.Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Seats).ThenInclude(s => s.Items).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Items).ThenInclude(s => s.Questions).ThenInclude(s=>s.Answer).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.Table == table && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved);
            if (destOrder != null)
            {
                var emptySeat = 0;
                var seatNums = new List<int>();
                foreach (var item in destOrder.Seats)
                {
                    seatNums.Add(item.SeatNum);
                }
                try
                {
                    for (int i = 1; i < seatNums.Max(); i++)
                    {
                        if (!seatNums.Contains(i))
                        {
                            emptySeat = i;
                            break;
                        }
                    }
                    if (emptySeat == 0) emptySeat = seatNums.Max() + 1;
                }
                catch { }
              
                foreach(var item in sourceSeat.Items)
                {
                    sourceOrder.Items.Remove(item);
                }
                sourceOrder.Seats.Remove(sourceSeat);

                var destSeat = new SeatItem() { SeatNum = emptySeat, Items = new List<OrderItem>() };
                destOrder.Seats.Add(destSeat);
                foreach (var item in items)
                {
                    item.Order = destOrder;
                    item.SeatNum = emptySeat;
                    destOrder.Items.Add(item);
                    destSeat.Items.Add(item);
                }
                _dbContext.SaveChanges();
                var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == sourceOrder.ComprobantesID);
                sourceOrder.GetTotalPrice(voucher);
                destOrder.GetTotalPrice(voucher);
                _dbContext.SaveChanges();
            }
            else
            {
                foreach (var item in sourceSeat.Items)
                {
                    sourceOrder.Items.Remove(item);
                }
                sourceOrder.Seats.Remove(sourceSeat);

                var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
                var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

                var nOrder = new Order();
                nOrder.Station = station;
                nOrder.OrderMode = OrderMode.Seat;
                nOrder.OrderType = OrderType.DiningRoom;
                nOrder.Status = OrderStatus.Pending;
                var nvoucher = _dbContext.Vouchers.Include(s=>s.Taxes).FirstOrDefault(s => s.IsPrimary);
                nOrder.ComprobantesID = nvoucher.ID;
                nOrder.OrderTime = DateTime.Now;
                nOrder.Table = table;
                nOrder.Area = table.Area;
                var user = HttpContext.User.Identity.GetUserName();
                nOrder.WaiterName = user;

                _dbContext.SaveChanges();
                var destSeat = new SeatItem() { SeatNum = 1, Items = new List<OrderItem> () };
                nOrder.Seats = new List<SeatItem>();
                nOrder.Seats.Add(destSeat);
                foreach (var item in items)
                {
                    item.Order = nOrder;
                    item.SeatNum = 1;
                    nOrder.Items = new List<OrderItem>();
                    nOrder.Items.Add(item);
                    destSeat.Items.Add(item);
                }
              
                _dbContext.Orders.Add(nOrder);
                _dbContext.SaveChanges();
                var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == sourceOrder.ComprobantesID);
                sourceOrder.GetTotalPrice(voucher);
                nOrder.GetTotalPrice(nvoucher);
                _dbContext.SaveChanges();
            }
            
            return Json(new { status = 0 });
        }

		[HttpPost]
		public JsonResult UpdateSeatName([FromBody]SeatInfoModel model)
		{
			var seatItem = _dbContext.SeatItems.FirstOrDefault(s => s.ID == model.SeatId);
			seatItem.ClientName = model.ClientName;
            seatItem.ClientId = model.ClientId;
            if (model.ClientId > 0)
            {
                var customer = _dbContext.Customers.Include(s => s.Voucher).FirstOrDefault(s => s.ID == model.ClientId);
                if (customer != null && customer.Voucher != null)
                {
                    seatItem.ComprebanteId = customer.Voucher.ID;
                }
            }
			_dbContext.SaveChanges();


			return Json(new { status = 0 })  ;
		}

		[HttpPost]
		public JsonResult MoveSeatItem([FromBody] MoveToSeatModel model)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var orderItem = _dbContext.OrderItems.Include(s=>s.Order).ThenInclude(s=>s.Items).Include(s=>s.Order).ThenInclude(s=>s.Seats).ThenInclude(s=>s.Items).FirstOrDefault(s => s.ID == model.ItemId);
            orderItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);

            var currentSeat = orderItem.Order.Seats.FirstOrDefault(s => s.SeatNum == orderItem.SeatNum);
			var moveSeat = orderItem.Order.Seats.FirstOrDefault(s => s.SeatNum == model.SeatNum);

			orderItem.SeatNum = model.SeatNum;
			moveSeat.Items.Add(orderItem);
            currentSeat.Items.Remove(orderItem);
			if (currentSeat.Items.Count == 0)
			{
				orderItem.Order.Seats.Remove(currentSeat);
			}
            _dbContext.SaveChanges();

			return Json("");
		}

		[HttpPost]
		public JsonResult SeperateItem(long itemId)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var orderItem = _dbContext.OrderItems.Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Order).ThenInclude(s => s.Items).Include(s => s.Order).ThenInclude(s => s.Seats).Include(s => s.Order).ThenInclude(s => s.Discounts).Include(s => s.Product).Include(s => s.Questions).ThenInclude(s=>s.Answer).Include(s=>s.Discounts).FirstOrDefault(o => o.ID == itemId);
            SeatItem seat = null;
			var qty = orderItem.Qty;
            if (qty <= 1) return Json(new { status = 0 });
            orderItem.Qty = 1;
            orderItem.SubTotal = orderItem.Price;
            orderItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);

            qty -= 1;
            while (qty > 0)
			{
                var nItem = orderItem.CopyThis();
               
                if (qty <= 1)
                {
                    nItem.Qty = qty;
                    nItem.SubTotal = qty * nItem.Price;
                }
                else
                {
                    nItem.Qty = 1;
                    nItem.SubTotal = 1 * nItem.Price;
                }
              
                orderItem.Order.Items.Add(nItem);
               
                if (orderItem.Order.OrderMode == OrderMode.Seat)
                {
                    if (orderItem.Order.Seats == null) orderItem.Order.Seats = new List<SeatItem>();

                    var existSeat = orderItem.Order.Seats.FirstOrDefault(s => s.SeatNum == orderItem.SeatNum);
                    if (existSeat != null )
                    {
						seat = existSeat;
                        if (existSeat.Items != null)
                            existSeat.Items.Add(nItem);
                    }
                }

                _dbContext.SaveChanges();
                qty--;
			}
			
            _dbContext.SaveChanges();
            var xorder = _dbContext.Orders.Include(s => s.Discounts).Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Items).ThenInclude(s => s.Product).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Items).ThenInclude(s => s.Questions).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == orderItem.Order.ID);

            var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == orderItem.Order.ComprobantesID);
            xorder.GetTotalPrice(voucher);
            _dbContext.SaveChanges();

            return Json(new { status = 0 }) ;
		}

		[HttpPost]
		public JsonResult CancelOrder(long orderId)
		{
            var order = _dbContext.Orders.Include(s => s.Items).Include(s => s.Seats).ThenInclude(s => s.Items).FirstOrDefault(o => o.ID == orderId && o.Status == OrderStatus.Temp);
            if (order != null)
            {
				order.IsDeleted = true;
				
                if (order.Status == OrderStatus.Temp)
                { 
                    //order.Status = OrderStatus.Saved;
                    order.OrderTime = DateTime.Now;
                    foreach (var item in order.Items)
                    {
                        item.IsDeleted = true;
                    }
                   
                }
                _dbContext.SaveChanges();
            }
            HttpContext.Session.Remove("CurrentOrderID");
			return Json(new { status = 0 });
		}

		[HttpPost]
		public JsonResult UpdateOrderInfo([FromBody]OrderInfoModel model)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            UpdateOrderInfoModel response = objPOSCore.UpdateOrderInfo(model);

            return Json(new { status = response.status, ComprobanteName = response.ComprobanteName, deliverycost = response.deliverycost, deliverytime = response.deliverytime, customerName = response.customerName });

            //var customer = _dbContext.Customers.Include(s => s.Voucher).FirstOrDefault(s => s.ID == model.CustomerId);
            //var order = _dbContext.Orders.Include(s=>s.Divides).Include(s => s.PrepareType).FirstOrDefault(s => s.ID == model.OrderId);
            //var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.IsPrimary);
            //Console.WriteLine(customer);
            //if (model.DividerId > 0)
            //{
            //    if (order.Divides == null) order.Divides = new List<DividerItem>();
            //    var divide = order.Divides.FirstOrDefault(s => s.DividerNum == model.DividerId);
            //    if (divide != null)
            //    {
            //        divide.ClientName = model.ClientName;
            //        divide.ClientId = model.CustomerId;
            //        if (customer != null)
            //        {
            //            if (!model.DontChangeVoucher) {
            //                divide.ComprebanteId = customer.Voucher.ID;
            //            }                                               
            //        }
            //        else
            //        {
            //            divide.ComprebanteId = voucher.ID;

            //        }
            //    }
            //    else
            //    {
            //        divide = new DividerItem()
            //        {
            //            DividerNum = model.DividerId,
            //            ClientId = model.CustomerId,
            //            ClientName = model.ClientName,

            //        };
            //        if (customer != null)
            //        {
            //            if (!model.DontChangeVoucher)
            //            {
            //                divide.ComprebanteId = customer.Voucher.ID;
            //            }                                                   
            //        }
            //        else
            //        {
            //            divide.ComprebanteId = voucher.ID;
            //        }
            //        order.Divides.Add(divide);
            //    }

            //    _dbContext.SaveChanges();
            //}
            //else
            //{
            //    order.ClientName = model.ClientName;
            //    order.CustomerId = model.CustomerId;
            //    if (order.CustomerId > 0)
            //    {
            //        if (customer != null && customer.Voucher != null)
            //        {
            //            if (!model.DontChangeVoucher)
            //            {
            //                order.ComprobantesID = customer.Voucher.ID;
            //                order.ComprobanteName = customer.Voucher.Name;
            //            }

            //        }
            //        else
            //        {
            //            order.ComprobantesID = voucher.ID;
            //            order.ComprobanteName = "";
            //        }
            //    }
            //    else
            //    {
            //        order.ComprobantesID = 1;
            //        order.ComprobanteName = "";
            //    }
            //    _dbContext.SaveChanges();
            //}

            //decimal deliverycost = 0;
            //decimal deliverytime = 0;

            //if (customer != null && order.OrderType == OrderType.Delivery)
            //{
            //    var deliveryzone = _dbContext.DeliveryZones.FirstOrDefault(s => s.ID == customer.DeliveryZoneID);
            //    if (deliveryzone != null && !order.PrepareType.SinChofer)
            //    {
            //        order.Delivery = deliveryzone.Cost;
            //    }
            //    else {
            //        order.Delivery = 0;
            //    }

            //    bool deliveryExists = _dbContext.Deliverys.Include(s => s.Order).Where(s => s.IsActive).Where(s => s.Order.ID == order.ID).Any();

            //    if (deliveryExists)
            //    {
            //        var delivery = _dbContext.Deliverys.Include(s => s.Order).Where(s => s.IsActive).Where(s => s.Order.ID == order.ID).First();

            //        delivery.CustomerID = customer.ID;
            //        delivery.Address1 = customer.Address1 ??"";
            //        delivery.Adress2 = customer.Address2 ??"";
            //        delivery.DeliveryZoneID = customer.DeliveryZoneID;

            //        if(delivery.DeliveryZoneID != null && delivery.DeliveryZoneID>0)
            //        {
            //            var zone = _dbContext.DeliveryZones.Where(s => s.IsActive).Where(s => s.ID == delivery.DeliveryZoneID).First();
            //            deliverycost = zone.Cost;
            //            deliverytime = zone.Time;
            //        }                    

            //        _dbContext.SaveChanges();
            //    }
            //    else
            //    {
            //        var delivery = new Delivery();
            //        delivery.CustomerID = customer.ID;
            //        delivery.Address1 = customer.Address1 ??"";
            //        delivery.Adress2 = customer.Address2??"";
            //        delivery.DeliveryZoneID = customer.DeliveryZoneID;
            //        delivery.OrderID = order.ID;
            //        delivery.Order = order;

            //        _dbContext.Deliverys.Add(delivery);
            //        _dbContext.SaveChanges();
            //    }

            //}

            //var order1 = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Items).ThenInclude(s => s.Questions).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == model.OrderId);

            //if (customer != null && customer.Voucher!=null)
            //{
            //    voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == customer.Voucher.ID);
            //}

            //order1.GetTotalPrice(voucher);
            //_dbContext.SaveChanges();

            //return Json(new { status = 0, ComprobanteName = voucher.Name, deliverycost = deliverycost, deliverytime= deliverytime, customerName = customer?.Name });
        }



        [HttpPost]
        public JsonResult UpdateCustomerName([FromBody] OrderInfoModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            try
            {
                int status = objPOSCore.UpdateCustomerName(model.OrderId, model.ClientName);

                if(status == 1)
                {
                    return Json(new { error = "No se encontró la orden con el ID proporcionado." });
                }

                return Json(new { success = true, message = "Se actualizó el nombre del cliente exitosamente." });

                //// Obtener la orden correspondiente al ID proporcionado
                //var order = _dbContext.Orders.FirstOrDefault(o => o.ID == model.OrderId);
                //if (order == null)
                //{
                //    return Json(new { error = "No se encontró la orden con el ID proporcionado." });
                //}

                //// Actualizar el nombre del cliente en la orden
                //order.ClientName = model.ClientName;
                //_dbContext.SaveChanges();

                //return Json(new { success = true, message = "Se actualizó el nombre del cliente exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Ocurrió un error al actualizar el nombre del cliente: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult UpdateOrderInfoPayment([FromBody] OrderInfoPaymentModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            var response = objPOSCore.UpdateOrderInfoPayment(model);

            return Json(new { status = response.Item1, ComprobanteName = response.Item2 });

            //var order = _dbContext.Orders.Include(s => s.Divides).FirstOrDefault(s => s.ID == model.OrderId);
            //var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == model.VoucherId);

            //int status = 0;

            //if (order.ComprobantesID != null && order.ComprobantesID > 0)
            //{
            //    //var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == order.ComprobantesID);

            //    if (voucher.IsRequireRNC)
            //    {

            //        if (order.CustomerId == null || order.CustomerId <= 0)
            //        {
            //            status = 3;
            //        }
            //        else {
            //            var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);

            //            if (string.IsNullOrEmpty(customer.RNC))
            //            {
            //                status = 3;
            //            }
            //        }



            //    }
            //}


            //if (model.DivideId > 0)
            //{
            //    var divide = order.Divides.FirstOrDefault(s => s.DividerNum == model.DivideId);
            //    if (divide != null)
            //    {
            //        divide.ComprebanteId = model.VoucherId;
            //        _dbContext.SaveChanges();

            //        return Json(new { status = status, ComprobanteName = voucher.Name }); ;
            //    }
            //}
            //else
            //{
            //    if (order.ComprobantesID != voucher.ID)
            //    {
            //        order.ComprobantesID = voucher.ID;
            //        order.ComprobanteName = voucher.Name;
            //        if (order.Status == OrderStatus.Paid)
            //        {
            //            //    Debug.WriteLine("caso3");
            //            var length = 11 - voucher.Class.Length;

            //            var voucherNumber = voucher.Class + (voucher.Secuencia + 1).ToString().PadLeft(length, '0');
            //            voucher.Secuencia = voucher.Secuencia + 1;
            //            //if (string.IsNullOrEmpty(order.ComprobanteNumber))
            //            order.ComprobanteNumber = voucherNumber;
            //            var comprobantes = _dbContext.OrderComprobantes.Where(s => s.OrderId == order.ID);
            //            foreach (var c in comprobantes)
            //            {
            //                c.IsActive = false;
            //            }

            //            _dbContext.OrderComprobantes.Add(new OrderComprobante()
            //            {
            //                OrderId = order.ID,
            //                VoucherId = voucher.ID,
            //                ComprobanteName = voucher.Name,
            //                ComprobanteNumber = voucherNumber
            //            });
            //            _dbContext.SaveChanges();

            //        }

            //        _dbContext.SaveChanges();
            //    }
            //}



            //return Json(new { status = status, ComprobanteName = voucher.Name }); ;
        }


        public IActionResult Checkout(long OrderId, int Seat = 0, int DividerId = 0, string selectedItems = "" , bool refund=false)
		{
            var LogError = new System.Text.StringBuilder();
           
            LogError.Append("OrderId: ").Append(OrderId).Append(" Seat: ").Append(Seat).Append(" DividerId: ").Append(DividerId);

            ViewBag.Refund = refund;

            try
            {
                LogError.AppendLine().Append("--------").AppendLine().Append("L1 ");

                var model = new CheckOutViewModel();
                var order = _dbContext.Orders.Include(s => s.Divides).Include(s => s.Table).FirstOrDefault(s => s.ID == OrderId);
                var stationId = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");

                LogError.AppendLine().Append("--------").AppendLine().Append("L2 ");

                Console.WriteLine("Station ID: " + stationId);

                if (order == null)
                {
                    return RedirectToAction("Station");
                }
                model.OrderId = OrderId;
                model.SeatNum = Seat;
                model.DividerId = DividerId;
                model.Order = order;
                model.ClientName = order.ClientName;
                model.ComprebanteName = order.ComprobanteName;

                LogError.AppendLine().Append("--------").AppendLine().Append("L3 ");

                if (Seat > 0)
                {
                    model.PaymentType = 1;
                }
                else if (DividerId > 0)
                {
                    try
                    {
                        var divide = order.Divides.FirstOrDefault(s => s.DividerNum == DividerId);
                        if (divide != null)
                        {
                            var comprebante = _dbContext.Vouchers.FirstOrDefault(s => s.ID == divide.ComprebanteId);
                            model.ClientName = divide.ClientName;
                            if (comprebante != null)
                            {
                                model.ComprebanteName = comprebante.Name;
                            }
                        }
                    }
                    catch { }

                    model.PaymentType = 2;
                }

                LogError.AppendLine().Append("--------").AppendLine().Append("L4 ");

                var denominations = _dbContext.Denominations.OrderBy(s => s.DisplayOrder).ToList();
                ViewBag.Denominations = denominations;

                var paymentMethods = _dbContext.PaymentMethods.Where(s => s.IsActive).OrderBy(s=>s.DisplayOrder).ToList();
                if (Seat > 0 || DividerId > 0) 
                {
                    var removes = paymentMethods.Where(s => s.PaymentType == "Conduce").ToList();
                    foreach (var r in removes)
                        paymentMethods.Remove(r);
                }
                //Obtenemos las urls de las imagenes
                var request = _context.HttpContext.Request;
                var _baseURL = $"https://{request.Host}";
                if (paymentMethods != null && paymentMethods.Any())
                {
                    foreach (var item in paymentMethods)
                    {
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "paymentmethod", item.ID.ToString() + ".png");
                        if (System.IO.File.Exists(pathFile))
                        {
                            var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                            item.Image = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "paymentmethod", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                        }
                        else
                        {
                            item.Image = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "paymentmethod", "empty.png");
                        }
                    }
                }

                ViewBag.PaymentMethods = paymentMethods;

                LogError.AppendLine().Append("--------").AppendLine().Append("L5 ");

                ViewData["StationID"] = stationId;

                LogError.AppendLine().Append("--------").AppendLine().Append("L6 ");
                // Obtener los IDs de los elementos seleccionados
                var selectedIds = JsonConvert.DeserializeObject<List<long>>(selectedItems);

                // Obtener las transacciones de la base de datos que coincidan con los IDs seleccionados
                var selectedTransactions = _dbContext.OrderTransactions.Where(t => selectedIds.Contains(t.ID)).ToList();

                ViewBag.SelectedItems = selectedTransactions;

                var store = _dbContext.Preferences.FirstOrDefault();

                ViewBag.HasSecondCurrency = false;
                if (!string.IsNullOrEmpty(store.SecondCurrency))
                {
                    ViewBag.HasSecondCurrency = true;
                }               

                return View(model);
            }
            catch (Exception ex) {
                var objLog = new logs();

                LogError.AppendLine().Append("--------").AppendLine().Append(ex.ToString());

                objLog.ubicacion = "Checkout";
                objLog.descripcion = LogError.ToString();
                objLog.fecha = DateTime.Now;


                _dbContext.logs.Add(objLog);
                _dbContext.SaveChanges();

                throw new Exception(ex.Message);
            }
		}

		[HttpPost]
		public JsonResult AddDiscount([FromBody]DiscountModel model)
		{

            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var res = objPOSCore.AddDiscount(model, stationID);

            if(res.Item1 == 0)
            {
                return Json(new { status = res.Item1, type = res.Item2 });
            }

            return Json(new { status = res.Item1 });


            //if (model.Type == "Order")
            //{
            //             if (model.DivideId > 0) return Json(new { status = 1 });
            //             var order = _dbContext.Orders.Include(s=>s.Taxes).Include(s => s.Propinas).Include(s=>s.Items).ThenInclude(s=>s.Discounts).Include(s => s.Items).ThenInclude(s => s.Questions).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).Include(s=>s.Discounts).FirstOrDefault(s => s.ID == model.TargetId);
            //	var discount = _dbContext.Discounts.FirstOrDefault(s => s.ID == model.DiscountId);
            //             var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
            //             var isPromotion = order.CheckPromotion();
            //	if (!isPromotion && order.Discounts.Count < 1)
            //	{

            //                 bool itemDiscount = false;
            //                 foreach(var item in order.Items)
            //                 {
            //                     var discounts = item.Discounts;
            //                     if (discounts != null && discounts.Count > 0)
            //                         itemDiscount = true;
            //                 }
            //                 if (!itemDiscount)
            //                 {
            //			order.AddDiscount(discount, model.DivideId);
            //			order.GetTotalPrice(voucher);
            //			_dbContext.SaveChanges();
            //			return Json(new { status = 0, type = "order" });
            //		}
            //	}
            //         }
            //else if (model.Type == "OrderItem")
            //{
            //             var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            //             var stationID = int.Parse(GetCookieValue("StationID"));

            //             var orderItem = _dbContext.OrderItems.Include(s=>s.Order).ThenInclude(s=>s.Items).ThenInclude(s=>s.Discounts).Include(s => s.Order).ThenInclude(s => s.Discounts).Include(s=>s.Discounts).FirstOrDefault(s=>s.ID == model.TargetId);
            //             orderItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
            //             var isPromotion = orderItem.Order.CheckPromotion();
            //             var isOrderDiscount = orderItem.Order.Discounts != null && orderItem.Order.Discounts.Count > 0;
            //             var discount = _dbContext.Discounts.FirstOrDefault(s => s.ID == model.DiscountId);
            //             var exist = orderItem.Discounts.FirstOrDefault(s => s.ItemID == model.DiscountId);

            //	if (!isOrderDiscount && !isPromotion && exist == null)
            //	{
            //                 orderItem.AddDiscount(discount);

            //                 var order = _dbContext.Orders.Include(s=>s.Discounts).Include(s=>s.Taxes).Include(s => s.Propinas).Include(s=>s.Items).ThenInclude(s=>s.Questions).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == orderItem.Order.ID);
            //                 _dbContext.SaveChanges();
            //                var  voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == orderItem.Order.ComprobantesID);
            //                 order.GetTotalPrice(voucher);
            //                 _dbContext.SaveChanges();
            //		return Json(new { status = 0, type = "item" });
            //	}
            //         }

            //return Json(new { status = 1});
        }

        [HttpPost]
        public JsonResult CombineSeat([FromBody] CombineSeatModel model)
        {
            var order = _dbContext.Orders.Include(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == model.OrderId);

            var targetSeat = order.Seats.FirstOrDefault(s => s.SeatNum == model.TargetNum);
            var sourceSeat = order.Seats.FirstOrDefault(s => s.SeatNum == model.SourceNum);
			foreach(var item in sourceSeat.Items)
			{
				item.SeatNum = model.TargetNum;
			}
			targetSeat.Items.AddRange(sourceSeat.Items);
			sourceSeat.Items.RemoveAll(s=>s != null);
			order.Seats.Remove(sourceSeat);

            _dbContext.SaveChanges();

            return Json("");
        }

        private List<Promotion> GetPromotions(MenuProduct product, int servingSizeID = 0)
        {

            if (product == null) return new List<Promotion>();
            List<Promotion> result = new List<Promotion>();
            var promotions = _dbContext.Promotions.Include(s => s.Targets).Where(s => s.IsActive).ToList();

            foreach (var promotion in promotions)
            {
                try
                {
                    var IsProduct = false;
                    foreach (var prod in promotion.Targets)
                    {
                        if (prod.ProductRange == ProductRangeType.Group)
                        {
                            if (prod.TargetId == product.GroupID)
                            {
                                IsProduct = true;
                                break;
                            }
                        }
                        else if (prod.ProductRange == ProductRangeType.Category)
                        {
                            if (prod.TargetId == product.CategoryID)
                            {
                                IsProduct = true;
                                break;
                            }
                        }
                        else if (prod.ProductRange == ProductRangeType.SubCategory)
                        {
                            if (prod.TargetId == product.SubCategoryID)
                            {
                                IsProduct = true;
                                break;
                            }
                        }
                        else if (prod.ProductRange == ProductRangeType.Product)
                        {
                            if (prod.TargetId == product.ID && servingSizeID == prod.ServingSizeID)
                            {
                                IsProduct = true;
                                break;
                            }
                        }
                    }
                    if (!IsProduct) continue;
                    var st = new DateTime(promotion.StartTime.Year, promotion.StartTime.Month, promotion.StartTime.Day, 0, 0, 0);
                    var en = new DateTime(promotion.EndTime.Year, promotion.EndTime.Month, promotion.EndTime.Day, 0, 0, 0);
                    if (st.Date > DateTime.Today.Date || en.Date < DateTime.Today.Date)
                    {
                        continue;
                    }
                    if (!promotion.IsAllDay)
                    {
                        var stdate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, promotion.StartTime.Hour, promotion.StartTime.Minute, 0);
                        var endate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, promotion.EndTime.Hour, promotion.EndTime.Minute, 0);
                        if (stdate > DateTime.Now || endate < DateTime.Now)
                        {
                            continue;
                        }
                    }
                                      

                    if (promotion.IsRecurring)
                    {
                        var diff = (DateTime.Now - st).TotalDays;
                        if (diff > 0)
                        {
                            var weeks = Math.Ceiling(diff / 7);
                            var day = (int)DateTime.Today.DayOfWeek;

                            {
                                var weekdays = promotion.WeekDays.Split(new char[] { ',' });
                                foreach(var w in promotion.WeekDays)
                                {
                                    try
                                    {
                                        var val = int.Parse("" + w);
                                        if (val-1 == day)
                                        {
                                            result.Add(promotion);
                                        }
                                    }
                                    catch { }
                                }                                        
                            }
                        }
                    }
                    else
                    {                        
                        result.Add(promotion);
                    }
                }
                catch { }
             
            }

            return result;
        }

        private bool ApplyPromotion(OrderItem item, decimal ChangeQty, bool Clear = false)
        {
            var result = false;
            // remove promotions
            if (Clear && item.Discounts != null)
            {
                var removes = item.Discounts.Where(s => s.ItemType == DiscountItemType.Promotion).ToList();
                for (int i = 0; i < removes.Count(); i++)
                {
                    item.Discounts.Remove(removes[i]);
                }
                _dbContext.SaveChanges();
            }

            var menuProduct = _dbContext.MenuProducts.FirstOrDefault(s => s.ID == item.MenuProductID);
            var promotions = GetPromotions(menuProduct, item.ServingSizeID);

            var items = item.Order.Items.Where(s => s.SeatNum == item.SeatNum && s.ServingSizeID == item.ServingSizeID && s.Product == item.Product && !s.IsDeleted).OrderBy(s=>s.CreatedDate).ToList();
            var AllProdCount = items.Sum(s => s.Qty);

            {
                foreach (var promotion in promotions)
                {
                    if (promotion.FirstCount == 0) continue;
                    if (promotion.ApplyType == PromotionApplyType.FirstCount)
                    {
                        if (promotion.FirstCount < AllProdCount)
                        {
                            int count = 0;
                            foreach (var citem in items)
                            {
                                int pqty = 0;
                                for (int i = 0; i < citem.Qty; i++)
                                {
                                    count++;
                                    if (count > promotion.FirstCount)
                                    {
                                        pqty++;
                                    }
                                }
                                if (pqty > 0 && citem.Status == OrderItemStatus.Pending)
                                    citem.AddPromotion(promotion, pqty);
                            }
                            //item.AddPromotion(promotion, AllProdCount);
                            //result = true;
                        }
                        else
                        {
                            if (item.Discounts != null)
                            {
                                var itempromotion = item.Discounts.Where(s => s.ItemType == DiscountItemType.Promotion && s.ItemID == promotion.ID).ToList();
                                if (itempromotion.Count > 0)
                                {
                                    foreach (var d in itempromotion)
                                    {
                                        item.Discounts.Remove(d);
                                    }
                                }
                            }
                            
                        }
                    }
                    else
                    {
                        if (promotion.FirstCount > 0 && AllProdCount / promotion.FirstCount >= 1)
                        {
                            int count = 0;
                            foreach(var citem in items)
                            {
                                int pqty = 0;
                                for(int i = 0; i < citem.Qty; i++)
                                {
                                    count++;
                                    if (count % promotion.FirstCount == 0)
                                    {
                                        pqty++;
                                    }
                                }
                                if (pqty > 0 && citem.Status == OrderItemStatus.Pending)
                                    citem.AddPromotion(promotion, pqty);
                            }
                            result = true;
						}
                        else
                        {
                            if (item.Discounts != null)
                            {
                                var itempromotion = item.Discounts.Where(s => s.ItemType == DiscountItemType.Promotion && s.ItemID == promotion.ID).ToList();
                                if (itempromotion.Count > 0)
                                {
                                    foreach (var d in itempromotion)
                                    {
                                        item.Discounts.Remove(d);
                                    }
                                }
                            }
                           
                        }
                    }
                }
            }

            return result;
        }

        public JsonResult GetOrderItemsAtDivide(long orderId)
        {
            var order = _dbContext.Orders.Include(s=>s.Items.Where(s=>!s.IsDeleted && s.Status != OrderItemStatus.Paid)).ThenInclude(s=>s.Questions).Include(s=>s.Items.Where(s=>!s.IsDeleted && s.Status != OrderItemStatus.Paid)).ThenInclude(s=>s.Discounts).FirstOrDefault(s=>s.ID == orderId);

            var divides = new List<DivideItemModel>();
            
            foreach(var item in order.Items)
            {
                if (item.DividerNum == 0) item.DividerNum = 1;

                var exist = divides.FirstOrDefault(s => s.DivideId == item.DividerNum);
                if (exist != null)
                {
                    exist.Items.Add(item);
                }
                else
                {
                    var divide = new DivideItemModel();
                    divide.DivideId = item.DividerNum;
                    divide.Items.Add(item);

                    divides.Add(divide);
                }
            }

            var index = 1;
            foreach(var d in divides)
            {
                d.DivideId = index;
                index++;
            }


            return Json(divides);
        }

        public JsonResult GetOrderItemsAtDivideSeat(long orderId)
        {
            var order = _dbContext.Orders.Include(s => s.Items.Where(s => !s.IsDeleted && s.Status != OrderItemStatus.Paid)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted && s.Status != OrderItemStatus.Paid)).ThenInclude(s => s.Discounts).FirstOrDefault(s => s.ID == orderId);

            var divides = new List<DivideItemModel>();

            foreach (var item in order.Items)
            {
                item.DividerNum = item.SeatNum;
                var exist = divides.FirstOrDefault(s => s.DivideId == item.DividerNum);
                if (exist != null)
                {
                    exist.Items.Add(item);
                }
                else
                {
                    var divide = new DivideItemModel();
                    divide.DivideId = item.DividerNum;
                    divide.Items.Add(item);

                    divides.Add(divide);
                }
            }

            return Json(divides);
        }

        public JsonResult GetOrderItemsAtDivideSplited(long orderId, int qty)
        {
            var order = _dbContext.Orders.Include(s => s.Items.Where(s => !s.IsDeleted && s.Status != OrderItemStatus.Paid)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted && s.Status != OrderItemStatus.Paid)).ThenInclude(s => s.Discounts).FirstOrDefault(s => s.ID == orderId);
            foreach (var item in order.Items)
            {
                if (!item.HasPromotion)
                    item.Qty = item.Qty / (decimal)qty;
            }
            var divides = new List<DivideItemModel>();
            for(int i = 0; i < qty; i++)
            {
                var dividerNum = i + 1;
               
                var divide = new DivideItemModel();
                divide.DivideId =dividerNum;
                foreach (var item in order.Items)
                {
                    if (dividerNum > 1 && item.HasPromotion) continue;
                    divide.Items.Add(item);
                }

                divides.Add(divide);
            }
           
            return Json(divides);
        }

        [HttpPost]
        public JsonResult SaveDivideResult([FromQuery] long orderId, [FromQuery] int divideId, [FromBody]List<DivideResultItemModel> items)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var order = _dbContext.Orders.Include(s=>s.Discounts).Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Items).ThenInclude(s => s.Product).Include(s=>s.Items).ThenInclude(s=>s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).Include(s => s.Items).ThenInclude(s => s.Questions).Include(s=>s.Items).ThenInclude(s=>s.Discounts).Include(s=>s.Seats).ThenInclude(s=>s.Items).FirstOrDefault(s=>s.ID == orderId);
            var newItems = new List<OrderItem>();

            order.OrderMode = OrderMode.Divide;
			if (order.Status == OrderStatus.Temp)
				order.OrderTime = DateTime.Now;
			order.Status = OrderStatus.Pending;

            var kitchenItems = new List<KitchenOrderItem>();
            foreach(var item in items)
            {
                var orderItem = _dbContext.OrderItems.Include(s=>s.Order).Include(s=>s.Product).Include(s=>s.Questions).ThenInclude(s=>s.Answer).Include(s=>s.Discounts).FirstOrDefault(s => s.ID == item.ItemId);
                
                var newItem = orderItem.CopyThis();
                newItem.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                newItem.Qty = item.Qty;
                newItem.DividerNum = item.DivideId;
                newItem.SeatNum = item.SeatNum;
                newItem.SubTotal = item.Qty * newItem.Price;
                newItems.Add(newItem);
            }
            
            foreach(var item in order.Items)
            {
                if (item.Questions != null)
                {
                    foreach (var q in item.Questions)
                    {

                        _dbContext.QuestionItems.Remove(q);
                    }
                }
                if (item.Discounts != null)
                {
                    foreach (var q in item.Discounts)
                    {

                        _dbContext.DiscountItems.Remove(q);
                    }
                }
            }
            order.Items.Clear();
            _dbContext.SaveChanges();
            foreach (var item in newItems)
            {
                order.Items.Add(item);
            }
            
            _dbContext.SaveChanges();
            int index = 0;
            foreach(var item in order.Items)
            {
                var olditem = items[index];
                if (item.Status == OrderItemStatus.Kitchen)
                {
                    CopyKitchenItem(order.ID, olditem.ItemId, item.ID);
                }
                index++;
            }
            var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
            order.GetTotalPrice(voucher);
            _dbContext.SaveChanges();

            if (divideId > 0)
            {
                var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order);
                order.GetTotalPrice(voucher, divideId, 0);
                var dividerTransactions = transactions.Where(s => s.DividerNum == divideId).ToList();
                var dividePaidAmount = dividerTransactions.Sum(s => s.Amount);
                var balance = order.Balance - dividePaidAmount;
                if (dividePaidAmount > 0 && balance > 0)
                {
                    return Json(new { status = 1 });
                }
            }

            return Json(new { status = 0 });
        }

        public JsonResult PayDone([FromBody] ApplyPayModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);

            var order = _dbContext.Orders.Include(s => s.Taxes).Include(s=>s.PrepareType).Include(s=>s.Divides).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).FirstOrDefault(o => o.ID == model.OrderId);

            var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == order.ComprobantesID);
            var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order);
                     
            if (order != null)
            {
                if (model.SeatNum > 0 && (order.PaymentStatus == PaymentStatus.SeatPaid || order.Status == OrderStatus.Paid))
                {
                   // Debug.WriteLine("caso1");
                    var seatTransactions = transactions.Include(s=>s.Order).Where(s => s.SeatNum == model.SeatNum).ToList();
                    var seatPayAmount = seatTransactions.Sum(s => s.Amount);
                    var nOrder = new Order();
                    nOrder.Station = station;
                    nOrder.OrderMode = OrderMode.Seat;
                    nOrder.OrderTime = DateTime.Now;
                    nOrder.OrderType = OrderType.DiningRoom;
                    nOrder.Status = OrderStatus.Paid;
                    nOrder.PaymentStatus = PaymentStatus.Paid;
                    nOrder.PayAmount = seatPayAmount;
                    order.PayAmount -= seatPayAmount;
                    var nvoucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.IsPrimary);
                    
                    nOrder.ComprobantesID = nvoucher.ID;

                    var seat = order.Seats.FirstOrDefault(s => s.SeatNum == model.SeatNum);
                    if (seat.ComprebanteId > 0)
                    {
                        nvoucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == seat.ComprebanteId);
                        nOrder.ComprobantesID = nvoucher.ID;
                    }
                    nOrder.ClientName = seat.ClientName;
                    nOrder.CustomerId = seat.ClientId;
                    nOrder.Table = order.Table;
                    nOrder.Area = order.Area;
                    nOrder.Items = new List<OrderItem>();

                    var items = order.Items.Where(s=>s.SeatNum == model.SeatNum && !s.IsDeleted);
                    foreach(var item in items)
                    {
                        item.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                        nOrder.Items.Add(item);
                        Debug.WriteLine(item);
                    }
                    var length = 11 - nvoucher.Class.Length;

                    var voucherNumber = nvoucher.Class + (nvoucher.Secuencia + 1).ToString().PadLeft(length, '0');
                    nvoucher.Secuencia = nvoucher.Secuencia + 1;

                    nOrder.ComprobanteNumber = voucherNumber;
                    nOrder.ComprobanteName = nvoucher.Name;

                    long? iFactura = _dbContext.Orders.Max(s => s.Factura);
                    try
                    {
                        if (iFactura== null || iFactura==0)
                        {
                            string iFacturaInicial = AppConfiguration.GetLLaveConfig("FacturaInicial");
                            iFactura = long.Parse(iFacturaInicial);
                        }
                    }
                    catch (Exception ex)
                    {
                        iFactura = 0;
                    }
                    iFactura++;
                    nOrder.Factura = iFactura.Value;

                    _dbContext.Orders.Add(nOrder);
                  
                    _dbContext.SaveChanges();

                    var comprobantes = _dbContext.OrderComprobantes.Where(s => s.OrderId == nOrder.ID);
                    foreach(var c in comprobantes)
                    {
                        c.IsActive = false;
                    }

                    _dbContext.OrderComprobantes.Add(new OrderComprobante()
                    {
                        OrderId = nOrder.ID,
                        VoucherId = nvoucher.ID,
                        ComprobanteName = nvoucher.Name,
                        ComprobanteNumber = voucherNumber
                    });
                    _dbContext.SaveChanges();
                    foreach (var s in seatTransactions)
                    {
                        s.Order = nOrder;
                    }
                    nOrder.GetTotalPrice(nvoucher);
                    order.GetTotalPrice(voucher);

                    var xorder = _dbContext.Orders.Include(s => s.Items).FirstOrDefault(s => s.ID == model.OrderId);
                    if (xorder.Items.Count == 0)
                    {
                        xorder.Status = OrderStatus.Void;
                    }
                    _dbContext.SaveChanges();

                    _printService.PrintPaymentSummary(stationID, nOrder.ID, Request.Cookies["db"], model.SeatNum, 0, false);
                    return Json(new { status = 0 });
                }
                else if (model.DividerId > 0 && (order.PaymentStatus == PaymentStatus.DividerPaid || order.Status == OrderStatus.Paid))
                {
                  //  Debug.WriteLine("caso2");
                    var divideTransactions = transactions.Include(s=>s.Order).Where(s => s.DividerNum == model.DividerId).ToList();
                    var dividePaidAmount = divideTransactions.Sum(s => s.Amount);
                    var nOrder = new Order();
                    nOrder.Station = station;
                    nOrder.OrderMode = OrderMode.Divide;
                    nOrder.OrderTime = DateTime.Now;
                    nOrder.OrderType = OrderType.DiningRoom;
                    nOrder.Status = OrderStatus.Paid;
                    nOrder.PaymentStatus = PaymentStatus.Paid;
                    nOrder.PayAmount = dividePaidAmount;
                    var nvoucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.IsPrimary);

                    nOrder.ComprobantesID = nvoucher.ID;
                  
                    var divide = order.Divides.FirstOrDefault(s => s.DividerNum == model.DividerId);
                    if (divide != null && divide.ComprebanteId > 0)
                    {
                        if (divide.ComprebanteId > 0)
                        {
                            nvoucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == divide.ComprebanteId);
                            nOrder.ComprobantesID = nvoucher.ID;
                        }
                        
                        nOrder.ClientName = divide.ClientName;
                        nOrder.CustomerId = divide.ClientId;
                        order.Divides.Remove(divide);
                    }
                    

                   
                    nOrder.Table = order.Table;
                    nOrder.Area = order.Area;
                    nOrder.Items = new List<OrderItem>();

                    var items = order.Items.Where(s => s.DividerNum == model.DividerId);
                    
                    foreach (var item in items)
                    {
                        item.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                        nOrder.Items.Add(item);
                        Debug.WriteLine(item);
                        
                    }

                    var discounts = order.Discounts.Where(s=>s.DividerId == model.DividerId);
                    if (discounts.Count() > 0)
                    {
                        nOrder.Discounts = new List<DiscountItem>();
                        foreach(var discount in discounts)
                        {
                            nOrder.Discounts.Add(discount);
                        }
                    }

                    var length = 11 - nvoucher.Class.Length;

                    var voucherNumber = nvoucher.Class + (nvoucher.Secuencia + 1).ToString().PadLeft(length, '0');
                    nvoucher.Secuencia = nvoucher.Secuencia + 1;

                    nOrder.ComprobanteNumber = voucherNumber;
                    nOrder.ComprobanteName = nvoucher.Name;
                    
                    long? iFactura = _dbContext.Orders.Max(s => s.Factura);
                    try
                    {
                        if (iFactura== null || iFactura==0)
                        {
                            string iFacturaInicial = AppConfiguration.GetLLaveConfig("FacturaInicial");
                            iFactura = long.Parse(iFacturaInicial);
                        }
                    }
                    catch (Exception ex)
                    {
                        iFactura = 0;
                    }
                    iFactura++;
                    nOrder.Factura = iFactura.Value;
                    
                    nOrder.GetTotalPrice(nvoucher);

                    if (Math.Round(nOrder.TotalPrice, 2, MidpointRounding.AwayFromZero) ==
                        Math.Round(dividePaidAmount, 2, MidpointRounding.AwayFromZero))
                    {
                        _dbContext.Orders.Add(nOrder);
                    
                    _dbContext.SaveChanges();
                    var comprobantes = _dbContext.OrderComprobantes.Where(s => s.OrderId == nOrder.ID);
                    foreach (var c in comprobantes)
                    {
                        c.IsActive = false;
                    }

                    _dbContext.OrderComprobantes.Add(new OrderComprobante()
                    {
                        OrderId = nOrder.ID,
                        VoucherId = nvoucher.ID,
                        ComprobanteName = nvoucher.Name,
                        ComprobanteNumber = voucherNumber
                    });
                    _dbContext.SaveChanges();

                    
                    foreach (var d in divideTransactions)
                    {
                        d.Order = nOrder;
                    }
                    var xorder = _dbContext.Orders.Include(s => s.Items.Where(s=>!s.IsDeleted)).Include(s=>s.Divides).FirstOrDefault(s => s.ID == model.OrderId);
                    if (xorder.Items.Count == 0)
                    {
                        xorder.Status = OrderStatus.Void;
                    }
                    else
                    {
                        var index = 1;
                        foreach(var d in xorder.Divides)
                        {
                            var xitems = xorder.Items.Where(s => s.DividerNum == d.DividerNum).ToList();
                            foreach(var ii in xitems)
                            {
                                ii.DividerNum = index;
                            }                            
                            d.DividerNum = index;
                            index++;
                        }
                    }
                    _dbContext.SaveChanges();

                    _printService.PrintPaymentSummary(stationID, nOrder.ID, Request.Cookies["db"], 0, model.DividerId, false);
                    return Json(new { status = 0 });
                    }
                    
                    
                }

                else if (order.Status == OrderStatus.Paid)
                {
                //    Debug.WriteLine("caso3");
                    var length = 11 - voucher.Class.Length;
                    if (string.IsNullOrEmpty(order.ComprobanteNumber))
                    {
                        var voucherNumber = voucher.Class + (voucher.Secuencia + 1).ToString().PadLeft(length, '0');
                        voucher.Secuencia = voucher.Secuencia + 1;

                        order.ComprobanteName = voucher.Name;
                        order.ComprobanteNumber = voucherNumber;
                        var comprobantes = _dbContext.OrderComprobantes.Where(s => s.OrderId == order.ID);
                        foreach (var c in comprobantes)
                        {
                            c.IsActive = false;
                        }
                        
                        long? iFactura = _dbContext.Orders.Max(s => s.Factura);
                        try
                        {
                            if (iFactura== null || iFactura==0)
                            {
                                string iFacturaInicial = AppConfiguration.GetLLaveConfig("FacturaInicial");
                                iFactura = long.Parse(iFacturaInicial);
                            }
                        }
                        catch (Exception ex)
                        {
                            iFactura = 0;
                        }
                        iFactura++;
                        order.Factura = iFactura.Value;

                        _dbContext.OrderComprobantes.Add(new OrderComprobante()
                        {
                            OrderId = order.ID,
                            VoucherId = voucher.ID,
                            ComprobanteName = voucher.Name,
                            ComprobanteNumber = voucherNumber
                        });                      
                    }

                    if(order.OrderType == OrderType.Delivery && order.OrderMode != OrderMode.Conduce) {

                       
                        var objDelivery = _dbContext.Deliverys.Include(s => s.Order).ThenInclude(s=>s.PrepareType).Where(s => s.OrderID == order.ID).First();

                        if(objDelivery.Order.PrepareType != null && objDelivery.Order.PrepareType.SinChofer) {
                            if (objDelivery.Order.Balance<1)
                            {
                                objDelivery.Status = StatusEnum.Cerrado;
                                objDelivery.UpdatedDate = DateTime.Now;
                            }
                        }                        
                    }

                    _dbContext.SaveChanges();                    

                    _printService.PrintPaymentSummary(stationID, model.OrderId, Request.Cookies["db"], 0, 0, false);

                    return Json(new { status = 0 });
                }
                else if (order.Status == OrderStatus.Pending && order.PaymentStatus== PaymentStatus.Paid && order.Balance==0)
                {
                    order.Status = OrderStatus.Paid;
                    _dbContext.SaveChanges();   
                    return Json(new { status = 0 });
                    
                }


            }

            return Json(new { status = 1 });
        }
        
        
        public JsonResult PayDone2([FromBody] ApplyPayModel2 model)
        {
            
            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);

            var order = _dbContext.Orders.Include(s => s.Taxes).Include(s=>s.PrepareType).Include(s=>s.Divides).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).FirstOrDefault(o => o.ID == model.OrderId);

            var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == order.ComprobantesID);
            var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order);

            try
            {
                _printService.PrintCxCSummary(stationID, model.ReferenceIds, Request.Cookies["db"], 0, 0, false);
            }
            catch (Exception ex)
            {
                var m = ex;
            }
            
             
            /*
            if (order != null)
            {
                if (model.SeatNum > 0 && (order.PaymentStatus == PaymentStatus.SeatPaid || order.Status == OrderStatus.Paid))
                {
                   // Debug.WriteLine("caso1");
                    var seatTransactions = transactions.Include(s=>s.Order).Where(s => s.SeatNum == model.SeatNum).ToList();
                    var seatPayAmount = seatTransactions.Sum(s => s.Amount);
                    var nOrder = new Order();
                    nOrder.Station = station;
                    nOrder.OrderMode = OrderMode.Seat;
                    nOrder.OrderTime = DateTime.Now;
                    nOrder.OrderType = OrderType.DiningRoom;
                    nOrder.Status = OrderStatus.Paid;
                    nOrder.PaymentStatus = PaymentStatus.Paid;
                    nOrder.PayAmount = seatPayAmount;
                    order.PayAmount -= seatPayAmount;
                    var nvoucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.IsPrimary);
                    
                    nOrder.ComprobantesID = nvoucher.ID;

                    var seat = order.Seats.FirstOrDefault(s => s.SeatNum == model.SeatNum);
                    if (seat.ComprebanteId > 0)
                    {
                        nvoucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == seat.ComprebanteId);
                        nOrder.ComprobantesID = nvoucher.ID;
                    }
                    nOrder.ClientName = seat.ClientName;
                    nOrder.CustomerId = seat.ClientId;
                    nOrder.Table = order.Table;
                    nOrder.Area = order.Area;
                    nOrder.Items = new List<OrderItem>();

                    var items = order.Items.Where(s=>s.SeatNum == model.SeatNum && !s.IsDeleted);
                    foreach(var item in items)
                    {
                        item.ForceDate = getCurrentWorkDate();
                        nOrder.Items.Add(item);
                        Debug.WriteLine(item);
                    }
                    var length = 11 - nvoucher.Class.Length;

                    var voucherNumber = nvoucher.Class + (nvoucher.Secuencia + 1).ToString().PadLeft(length, '0');
                    nvoucher.Secuencia = nvoucher.Secuencia + 1;

                    nOrder.ComprobanteNumber = voucherNumber;
                    nOrder.ComprobanteName = nvoucher.Name;

                    long? iFactura = _dbContext.Orders.Max(s => s.Factura);
                    try
                    {
                        if (iFactura== null || iFactura==0)
                        {
                            string iFacturaInicial = AppConfiguration.GetLLaveConfig("FacturaInicial");
                            iFactura = long.Parse(iFacturaInicial);
                        }
                    }
                    catch (Exception ex)
                    {
                        iFactura = 0;
                    }
                    iFactura++;
                    nOrder.Factura = iFactura.Value;

                    _dbContext.Orders.Add(nOrder);
                  
                    _dbContext.SaveChanges();

                    var comprobantes = _dbContext.OrderComprobantes.Where(s => s.OrderId == nOrder.ID);
                    foreach(var c in comprobantes)
                    {
                        c.IsActive = false;
                    }

                    _dbContext.OrderComprobantes.Add(new OrderComprobante()
                    {
                        OrderId = nOrder.ID,
                        VoucherId = nvoucher.ID,
                        ComprobanteName = nvoucher.Name,
                        ComprobanteNumber = voucherNumber
                    });
                    _dbContext.SaveChanges();
                    foreach (var s in seatTransactions)
                    {
                        s.Order = nOrder;
                    }
                    nOrder.GetTotalPrice(nvoucher);
                    order.GetTotalPrice(voucher);

                    var xorder = _dbContext.Orders.Include(s => s.Items).FirstOrDefault(s => s.ID == model.OrderId);
                    if (xorder.Items.Count == 0)
                    {
                        xorder.Status = OrderStatus.Void;
                    }
                    _dbContext.SaveChanges();

                    _printService.PrintPaymentSummary(stationID, nOrder.ID, Request.Cookies["db"], model.SeatNum, 0, false);
                    return Json(new { status = 0 });
                }
                else if (model.DividerId > 0 && (order.PaymentStatus == PaymentStatus.DividerPaid || order.Status == OrderStatus.Paid))
                {
                  //  Debug.WriteLine("caso2");
                    var divideTransactions = transactions.Include(s=>s.Order).Where(s => s.DividerNum == model.DividerId).ToList();
                    var dividePaidAmount = divideTransactions.Sum(s => s.Amount);
                    var nOrder = new Order();
                    nOrder.Station = station;
                    nOrder.OrderMode = OrderMode.Divide;
                    nOrder.OrderTime = DateTime.Now;
                    nOrder.OrderType = OrderType.DiningRoom;
                    nOrder.Status = OrderStatus.Paid;
                    nOrder.PaymentStatus = PaymentStatus.Paid;
                    nOrder.PayAmount = dividePaidAmount;
                    var nvoucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.IsPrimary);

                    nOrder.ComprobantesID = nvoucher.ID;
                  
                    var divide = order.Divides.FirstOrDefault(s => s.DividerNum == model.DividerId);
                    if (divide != null && divide.ComprebanteId > 0)
                    {
                        if (divide.ComprebanteId > 0)
                        {
                            nvoucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == divide.ComprebanteId);
                            nOrder.ComprobantesID = nvoucher.ID;
                        }
                        
                        nOrder.ClientName = divide.ClientName;
                        nOrder.CustomerId = divide.ClientId;
                        order.Divides.Remove(divide);
                    }
                    

                   
                    nOrder.Table = order.Table;
                    nOrder.Area = order.Area;
                    nOrder.Items = new List<OrderItem>();

                    var items = order.Items.Where(s => s.DividerNum == model.DividerId);
                    
                    foreach (var item in items)
                    {
                        item.ForceDate = getCurrentWorkDate();
                        nOrder.Items.Add(item);
                        Debug.WriteLine(item);
                        
                    }

                    var discounts = order.Discounts.Where(s=>s.DividerId == model.DividerId);
                    if (discounts.Count() > 0)
                    {
                        nOrder.Discounts = new List<DiscountItem>();
                        foreach(var discount in discounts)
                        {
                            nOrder.Discounts.Add(discount);
                        }
                    }

                    var length = 11 - nvoucher.Class.Length;

                    var voucherNumber = nvoucher.Class + (nvoucher.Secuencia + 1).ToString().PadLeft(length, '0');
                    nvoucher.Secuencia = nvoucher.Secuencia + 1;

                    nOrder.ComprobanteNumber = voucherNumber;
                    nOrder.ComprobanteName = nvoucher.Name;
                    
                    long? iFactura = _dbContext.Orders.Max(s => s.Factura);
                    try
                    {
                        if (iFactura== null || iFactura==0)
                        {
                            string iFacturaInicial = AppConfiguration.GetLLaveConfig("FacturaInicial");
                            iFactura = long.Parse(iFacturaInicial);
                        }
                    }
                    catch (Exception ex)
                    {
                        iFactura = 0;
                    }
                    iFactura++;
                    nOrder.Factura = iFactura.Value;
                    
                    nOrder.GetTotalPrice(nvoucher);

                    if (Math.Round(nOrder.TotalPrice, 2, MidpointRounding.AwayFromZero) ==
                        Math.Round(dividePaidAmount, 2, MidpointRounding.AwayFromZero))
                    {
                        _dbContext.Orders.Add(nOrder);
                    
                    _dbContext.SaveChanges();
                    var comprobantes = _dbContext.OrderComprobantes.Where(s => s.OrderId == nOrder.ID);
                    foreach (var c in comprobantes)
                    {
                        c.IsActive = false;
                    }

                    _dbContext.OrderComprobantes.Add(new OrderComprobante()
                    {
                        OrderId = nOrder.ID,
                        VoucherId = nvoucher.ID,
                        ComprobanteName = nvoucher.Name,
                        ComprobanteNumber = voucherNumber
                    });
                    _dbContext.SaveChanges();

                    
                    foreach (var d in divideTransactions)
                    {
                        d.Order = nOrder;
                    }
                    var xorder = _dbContext.Orders.Include(s => s.Items.Where(s=>!s.IsDeleted)).Include(s=>s.Divides).FirstOrDefault(s => s.ID == model.OrderId);
                    if (xorder.Items.Count == 0)
                    {
                        xorder.Status = OrderStatus.Void;
                    }
                    else
                    {
                        var index = 1;
                        foreach(var d in xorder.Divides)
                        {
                            var xitems = xorder.Items.Where(s => s.DividerNum == d.DividerNum).ToList();
                            foreach(var ii in xitems)
                            {
                                ii.DividerNum = index;
                            }                            
                            d.DividerNum = index;
                            index++;
                        }
                    }
                    _dbContext.SaveChanges();

                    _printService.PrintPaymentSummary(stationID, nOrder.ID, Request.Cookies["db"], 0, model.DividerId, false);
                    return Json(new { status = 0 });
                    }
                    
                    
                }

                else if (order.Status == OrderStatus.Paid)
                {
                //    Debug.WriteLine("caso3");
                    var length = 11 - voucher.Class.Length;
                    if (string.IsNullOrEmpty(order.ComprobanteNumber))
                    {
                        var voucherNumber = voucher.Class + (voucher.Secuencia + 1).ToString().PadLeft(length, '0');
                        voucher.Secuencia = voucher.Secuencia + 1;

                        order.ComprobanteName = voucher.Name;
                        order.ComprobanteNumber = voucherNumber;
                        var comprobantes = _dbContext.OrderComprobantes.Where(s => s.OrderId == order.ID);
                        foreach (var c in comprobantes)
                        {
                            c.IsActive = false;
                        }
                        
                        long? iFactura = _dbContext.Orders.Max(s => s.Factura);
                        try
                        {
                            if (iFactura== null || iFactura==0)
                            {
                                string iFacturaInicial = AppConfiguration.GetLLaveConfig("FacturaInicial");
                                iFactura = long.Parse(iFacturaInicial);
                            }
                        }
                        catch (Exception ex)
                        {
                            iFactura = 0;
                        }
                        iFactura++;
                        order.Factura = iFactura.Value;

                        _dbContext.OrderComprobantes.Add(new OrderComprobante()
                        {
                            OrderId = order.ID,
                            VoucherId = voucher.ID,
                            ComprobanteName = voucher.Name,
                            ComprobanteNumber = voucherNumber
                        });                      
                    }

                    if(order.OrderType == OrderType.Delivery && order.OrderMode != OrderMode.Conduce) {

                       
                        var objDelivery = _dbContext.Deliverys.Include(s => s.Order).ThenInclude(s=>s.PrepareType).Where(s => s.OrderID == order.ID).First();

                        if(objDelivery.Order.PrepareType != null && objDelivery.Order.PrepareType.SinChofer) {
                            if (objDelivery.Order.Balance<1)
                            {
                                objDelivery.Status = StatusEnum.Cerrado;
                                objDelivery.UpdatedDate = DateTime.Now;
                            }
                        }                        
                    }

                    _dbContext.SaveChanges();                    

                    _printService.PrintPaymentSummary(stationID, model.OrderId, Request.Cookies["db"], 0, 0, false);

                    return Json(new { status = 0 });
                }
                else if (order.Status == OrderStatus.Pending && order.PaymentStatus== PaymentStatus.Paid && order.Balance==0)
                {
                    order.Status = OrderStatus.Paid;
                    _dbContext.SaveChanges();   
                    return Json(new { status = 0 });
                    
                }


            }*/

            return Json(new { status = 0 });
        }
        
        
        
        
        
        public JsonResult Pay([FromBody] ApplyPayModel model)
        {
            var order = GetOrder(model.OrderId);
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            if (model.Amount <= 0)
            {
                return Json(new { status = 3 });
            }

            if (model.DividerId == 0)
            {
                if ((Math.Round(order.TotalPrice, 2, MidpointRounding.AwayFromZero) - Math.Round(order.PayAmount, 2, MidpointRounding.AwayFromZero) == 0)) {
                    return Json(new { status = 4 });
                }
            }
            else
            {
                var transactionsTemp = _dbContext.OrderTransactions.Where(s => s.Order == order);
                var voucher1Temp = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
                order.GetTotalPrice(voucher1Temp, model.DividerId, 0);
                var dividerTransactions = transactionsTemp.Where(s => s.DividerNum == model.DividerId).ToList();
                var balance = Math.Round(order.Balance,2,MidpointRounding.AwayFromZero) - Math.Round(dividerTransactions.Sum(s => s.Amount),2,MidpointRounding.AwayFromZero);

                if (balance==0)
                {
                    return Json(new { status = 4 });
                }    
            }
           

            var method = _dbContext.PaymentMethods.FirstOrDefault(s => s.ID == model.Method);
            if (method == null)
            {
                return Json(new { status = 1 });
            }
            if (method.PaymentType == "C X C" || method.PaymentType == "Cuenta de la Casa" || method.PaymentType == "Conduce")
            {
                if (order.OrderMode == OrderMode.Divide && model.DividerId > 0)
                {
                    var divide = order.Divides.FirstOrDefault(s => s.DividerNum == model.DividerId);
                    if (divide == null || string.IsNullOrEmpty(divide.ClientName))
                    {
                        return Json(new { status = 2 });
                    }
                }
                else if (string.IsNullOrEmpty(order.ClientName))
                {
                    return Json(new {status = 2});
                }               
            }

            if(order.ComprobantesID!= null && order.ComprobantesID > 0) {
                var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == order.ComprobantesID);

                if (voucher.IsRequireRNC) {

                    if(order.CustomerId == null || order.CustomerId <= 0) {
                        return Json(new { status = 3 });
                    }

                    var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);

                    if (string.IsNullOrEmpty(customer.RNC)) {
                        return Json(new { status = 3 });
                    }

                }
            }

            if (method.PaymentType == "C X C")
            {
				var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);
                if (customer.CreditLimit > 0)
                {
                    var consume = customer.CreditLimit - customer.Balance;
                    if (model.Amount > consume)
                    {
                        return Json(new { status = 10 });
                    }
                }
			}

            var kichenItems = new List<OrderItem>();
			decimal Difference = 0;
			if (order != null && model.Amount > 0)
            {
                if (order.Status == OrderStatus.Temp)
                {
                    if (order.OrderType == OrderType.Barcode)
                    {
                        order.Status = OrderStatus.Pending;
                        order.OrderTime = DateTime.Now;
                        foreach (var item in order.Items)
                        {
                            item.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                            if (item.Status == OrderItemStatus.Pending || item.Status == OrderItemStatus.Printed)
                            {
                                item.Status = OrderItemStatus.Saved;
                                if (item.Product.InventoryCountDownActive)
                                {
                                    item.Product.InventoryCount -= item.Qty;

                                }
                                objPOSCore.SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, stationID);
                                foreach (var q in item.Questions)
                                {
                                    if (!q.IsActive) continue;
                                    var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                                    objPOSCore.SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, stationID);
                                }
                            }
                           
                        }
                    }
                    else
                    {
                        order.Status = OrderStatus.Pending;
                        order.OrderTime = DateTime.Now;
                        foreach (var item in order.Items)
                        {
                            item.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                            if (item.Status == OrderItemStatus.Pending || item.Status == OrderItemStatus.Printed)
                            {
                                item.Status = OrderItemStatus.Kitchen;
                                objPOSCore.SendKitchenItem(order.ID, item.ID);
                                kichenItems.Add(item);
                            }
                            if (item.Product.InventoryCountDownActive)
                            {
                                item.Product.InventoryCount -= item.Qty;

                            }
                            objPOSCore.SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, stationID);
                            foreach (var q in item.Questions)
                            {
                                if (!q.IsActive) continue;
                                var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                                objPOSCore.SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, stationID);
                            }
                        }
                    }
                   
                    //var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == order.ComprobantesID);
                    //if (voucher != null)
                    //{
                    //    var length = 11 - voucher.Class.Length;
                    //    var voucherNumber = voucher.Class + (voucher.Secuencia + 1).ToString().PadLeft(length, '0');
                    //    voucher.Secuencia = voucher.Secuencia + 1;

                    //    order.ComprobanteNumber = voucherNumber;
                    //    order.ComprobanteName = voucher.Name;
                    //}
                }

                model.Amount = model.Amount * method.Tasa;

                //var transaction = new OrderTransaction();
                //transaction.PaymentDate = DateTime.Now;
                //transaction.Amount = model.Amount;
                //transaction.BeforeBalance = order.Balance;
                //transaction.AfterBalance = order.Balance - model.Amount;
                //transaction.Order = order;
                //transaction.Method = method.Name;
                //transaction.SeatNum = model.SeatNum;
                //transaction.DividerNum = model.DividerId;
                //transaction.Note = "";
                //transaction.Status = TransactionStatus.Open;
                //transaction.Type = TransactionType.Payment;

                //_dbContext.OrderTransactions.Add(transaction);
                //_dbContext.SaveChanges();
                //var stationID = int.Parse(GetCookieValue("StationID"));
                if (kichenItems.Count > 0)
                    _printService.PrintKitchenItems(stationID, order.ID, kichenItems, Request.Cookies["db"]);


                var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order);
                var voucher1 = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);

                if (method.PaymentType == "Conduce")
                {
                    order.IsConduce = true;
                    order.Status = OrderStatus.Paid;
					
					_dbContext.SaveChanges();
					_printService.PrintPaymentSummary(stationID, order.ID, Request.Cookies["db"], 0, 0, false);
					return Json(new { status = 5 });
                }
                else
                {
                    if (model.SeatNum > 0)
                    {
                        order.GetTotalPrice(voucher1, 0, model.SeatNum);
                        var seatTransactions = transactions.Where(s => s.SeatNum == model.SeatNum).ToList();
                        var seatBalance = order.Balance - seatTransactions.Sum(s => s.Amount);
                        if (seatBalance < model.Amount)
                        {
                            Difference = model.Amount - seatBalance;
                            model.Amount = seatBalance;
                        }

                        var transaction = new OrderTransaction();
                        transaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.Amount = model.Amount;
                        transaction.BeforeBalance = order.Balance;
                        transaction.Difference = Difference;
                        transaction.AfterBalance = Math.Round(order.Balance - model.Amount, 2);
                        transaction.Order = order;
                        transaction.Method = method.Name;
                        transaction.PaymentType = method.PaymentType;
                        
                        transaction.SeatNum = model.SeatNum;
                        transaction.DividerNum = model.DividerId;
                        transaction.Note = "";
                        transaction.Status = TransactionStatus.Open;
                        transaction.Type = TransactionType.Payment;

                        _dbContext.OrderTransactions.Add(transaction);
                        _dbContext.SaveChanges();
                       
                        seatBalance = Math.Round(seatBalance - model.Amount, 2);

						if (seatBalance <= 0)
                        {
                            var seat = order.Seats.FirstOrDefault(s => s.SeatNum == model.SeatNum);
                            seat.IsPaid = true;
                            order.PaymentStatus = PaymentStatus.SeatPaid;
                            order.PayAmount += model.Amount;
                            order.Balance = 0;
                            foreach (var item in seat.Items)
                            {
                                item.Status = OrderItemStatus.Paid;
                            }
                        }
                        else
                        {
                            order.PaymentStatus = PaymentStatus.Partly;
                            order.PayAmount += model.Amount;
                            order.Balance = order.Balance - model.Amount;
                        }
                     
                        // seat payment
                    }
                    else if (model.DividerId > 0)
                    {
                        order.GetTotalPrice(voucher1, model.DividerId, 0);
                        var dividerTransactions = transactions.Where(s => s.DividerNum == model.DividerId).ToList();
                        var balance = order.Balance - dividerTransactions.Sum(s => s.Amount);
                        if (balance < model.Amount)
                        {
                            Difference = model.Amount - balance;
                            model.Amount = balance;
                        }
                        var transaction = new OrderTransaction();
						transaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.Amount = model.Amount;
						transaction.BeforeBalance = order.Balance;
                        transaction.Difference = Difference;
                        transaction.AfterBalance = Math.Round(order.Balance - model.Amount, 2);
						transaction.Order = order;
						transaction.Method = method.Name; 
                        transaction.PaymentType = method.PaymentType;
                        transaction.SeatNum = model.SeatNum;
						transaction.DividerNum = model.DividerId;
						transaction.Note = "";
						transaction.Status = TransactionStatus.Open;
						transaction.Type = TransactionType.Payment;

						_dbContext.OrderTransactions.Add(transaction);
						_dbContext.SaveChanges();

						balance = Math.Round(balance - model.Amount, 2);

						if (balance <= 0)
                        {
                            var items = order.Items.Where(s =>!s.IsDeleted && s.DividerNum == model.DividerId && s.Status != OrderItemStatus.Paid).ToList();
                            order.PaymentStatus = PaymentStatus.DividerPaid;
                            order.PayAmount += model.Amount;
                            order.Balance = 0;
                            foreach (var item in items)
                            {
                                item.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                                item.Status = OrderItemStatus.Paid;
                            }
                        }
                        else
                        {
                            order.PaymentStatus = PaymentStatus.Partly;
                            order.PayAmount += model.Amount;
                            order.Balance = order.Balance - model.Amount;
                        }
                        // divider payment
                    }
                    else
                    {
                        if (order.Balance < model.Amount)
                        {
                            Difference = model.Amount - order.Balance;
                            model.Amount = order.Balance;
                        }
                        order.TotalPrice = model.Amount;
                        var transaction = new OrderTransaction();
						transaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.Amount = model.Amount;
                        transaction.Difference = Difference;
                        transaction.BeforeBalance = order.Balance;
						transaction.AfterBalance = Math.Round(order.Balance - model.Amount, 2);
						transaction.Order = order;
						transaction.Method = method.Name;
                        transaction.PaymentType = method.PaymentType;
                        transaction.SeatNum = model.SeatNum;
						transaction.DividerNum = model.DividerId;
						transaction.Note = "";
						transaction.Status = TransactionStatus.Open;
						transaction.Type = TransactionType.Payment;

						_dbContext.OrderTransactions.Add(transaction);
						_dbContext.SaveChanges();

						order.PayAmount = order.PayAmount + model.Amount;
                        order.Balance = Math.Round(order.Balance - model.Amount, 2);

                        if (order.Balance <= 0)
                        {
                            order.PaymentStatus = PaymentStatus.Paid;
                            order.Status = OrderStatus.Paid;

                            foreach (var item in order.Items)
                            {
                                item.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                                item.Status = OrderItemStatus.Paid;
                            }
                        }
                        else
                        {
                            order.PaymentStatus = PaymentStatus.Partly;
                        }

                        if (order.OrderMode == OrderMode.Conduce)
                        {
                            var selectedConduceOrders = _dbContext.Orders.Where(s => s.IsConduce && s.ConduceOrderId == order.ID && s.CustomerId == order.CustomerId).ToList();
                            foreach(var o in selectedConduceOrders)
                            {
                                o.IsConduce = false;
                            }
                        }
                    }
                }
                _dbContext.SaveChanges();
                var order1 = GetOrder(model.OrderId);
                order1.GetTotalPrice(voucher1);
               
                _dbContext.SaveChanges();
                
            }

            return Json(new {status = 0, difference = Difference, balance = order.Balance, parcial = (order.PaymentStatus == PaymentStatus.Partly ? true : false)});
        }

        public JsonResult Pay2([FromBody] ApplyPayModel2 model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var order = GetOrder(model.OrderId);
            if (model.Amount <= 0)
            {
                return Json(new { status = 3 });
            }
            var method = _dbContext.PaymentMethods.FirstOrDefault(s => s.ID == model.Method);
            if (method == null)
            {
                return Json(new { status = 1 });
            }
            if (method.PaymentType == "C X C" || method.PaymentType == "Cuenta de la Casa")
            {
                if (order.OrderMode == OrderMode.Divide && model.DividerId > 0)
                {
                    var divide = order.Divides.FirstOrDefault(s => s.DividerNum == model.DividerId);
                    if (divide == null || string.IsNullOrEmpty(divide.ClientName))
                    {
                        return Json(new { status = 2 });
                    }
                }
                else if (string.IsNullOrEmpty(order.ClientName))
                {
                    return Json(new { status = 2 });
                }
            }

            var kichenItems = new List<OrderItem>();
            decimal Difference = 0;
            if (order != null && model.Amount > 0)
            {
                if (order.Status == OrderStatus.Temp)
                {
                    if (order.OrderType == OrderType.Barcode)
                    {
                        order.Status = OrderStatus.Pending;
                        order.OrderTime = DateTime.Now;
                        foreach (var item in order.Items)
                        {
                            item.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                            if (item.Status == OrderItemStatus.Pending || item.Status == OrderItemStatus.Printed)
                            {
                                item.Status = OrderItemStatus.Saved;
                                if (item.Product.InventoryCountDownActive)
                                {
                                    item.Product.InventoryCount -= item.Qty;

                                }
                                objPOSCore.SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, stationID);
                                foreach (var q in item.Questions)
                                {
                                    var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                                    objPOSCore.SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, stationID);
                                }
                            }

                        }
                    }
                    else
                    {
                        order.Status = OrderStatus.Pending;
                        order.OrderTime = DateTime.Now;
                        foreach (var item in order.Items)
                        {
                            item.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                            if (item.Status == OrderItemStatus.Pending || item.Status == OrderItemStatus.Printed)
                            {
                                item.Status = OrderItemStatus.Kitchen;
                                objPOSCore.SendKitchenItem(order.ID, item.ID);
                                kichenItems.Add(item);
                            }
                            if (item.Product.InventoryCountDownActive)
                            {
                                item.Product.InventoryCount -= item.Qty;

                            }
                            objPOSCore.SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, stationID);
                            foreach (var q in item.Questions)
                            {
                                var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                                objPOSCore.SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, stationID);
                            }
                        }
                    }
                }

                model.Amount = model.Amount * method.Tasa;

                //var stationID = int.Parse(GetCookieValue("StationID"));
                if (kichenItems.Count > 0)
                    _printService.PrintKitchenItems(stationID, order.ID, kichenItems, Request.Cookies["db"]);


                var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order);
                var voucher1 = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);


                {
                    if (model.SeatNum > 0)
                    {
                        order.GetTotalPrice(voucher1, 0, model.SeatNum);
                        var seatTransactions = transactions.Where(s => s.SeatNum == model.SeatNum).ToList();
                        var seatBalance = order.Balance - seatTransactions.Sum(s => s.Amount);
                        if (seatBalance < model.Amount)
                        {
                            Difference = model.Amount - seatBalance;
                            model.Amount = seatBalance;
                        }

                        var transaction = new OrderTransaction();
                        transaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.Amount = model.Amount;
                        transaction.BeforeBalance = order.Balance;
                        transaction.Difference = Difference;
                        transaction.AfterBalance = Math.Round(order.Balance - model.Amount, 2);
                        transaction.Order = order;
                        transaction.Method = method.Name;
                        transaction.PaymentType = method.PaymentType;
                        transaction.ReferenceId = model.ReferenceId;

                        transaction.SeatNum = model.SeatNum;
                        transaction.DividerNum = model.DividerId;
                        transaction.Note = "";
                        transaction.Status = TransactionStatus.Open;
                        transaction.Type = TransactionType.Payment;

                        _dbContext.OrderTransactions.Add(transaction);
                        _dbContext.SaveChanges();

                        seatBalance = Math.Round(seatBalance - model.Amount, 2);

                        if (seatBalance <= 0)
                        {
                            var seat = order.Seats.FirstOrDefault(s => s.SeatNum == model.SeatNum);
                            seat.IsPaid = true;
                            order.PaymentStatus = PaymentStatus.SeatPaid;
                            order.PayAmount += model.Amount;
                            order.Balance = 0;
                            foreach (var item in seat.Items)
                            {
                                item.Status = OrderItemStatus.Paid;
                            }
                        }
                        else
                        {
                            order.PaymentStatus = PaymentStatus.Partly;
                            order.PayAmount += model.Amount;
                            order.Balance = order.Balance - model.Amount;
                        }

                        // seat payment
                    }
                    else if (model.DividerId > 0)
                    {
                        order.GetTotalPrice(voucher1, model.DividerId, 0);
                        var dividerTransactions = transactions.Where(s => s.DividerNum == model.DividerId).ToList();
                        var balance = order.Balance - dividerTransactions.Sum(s => s.Amount);
                        if (balance < model.Amount)
                        {
                            Difference = model.Amount - balance;
                            model.Amount = balance;
                        }
                        var transaction = new OrderTransaction();
                        transaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.Amount = model.Amount;
                        transaction.BeforeBalance = order.Balance;
                        transaction.Difference = Difference;
                        transaction.AfterBalance = Math.Round(order.Balance - model.Amount, 2);
                        transaction.Order = order;
                        transaction.Method = method.Name;
                        transaction.PaymentType = method.PaymentType;
                        transaction.SeatNum = model.SeatNum;
                        transaction.DividerNum = model.DividerId;
                        transaction.Note = "";
                        transaction.Status = TransactionStatus.Open;
                        transaction.Type = TransactionType.Payment;
                        transaction.ReferenceId = model.ReferenceId;

                        _dbContext.OrderTransactions.Add(transaction);
                        _dbContext.SaveChanges();

                        balance = Math.Round(balance - model.Amount, 2);

                        if (balance <= 0)
                        {
                            var items = order.Items.Where(s => !s.IsDeleted && s.DividerNum == model.DividerId && s.Status != OrderItemStatus.Paid).ToList();
                            order.PaymentStatus = PaymentStatus.DividerPaid;
                            order.PayAmount += model.Amount;
                            order.Balance = 0;
                            foreach (var item in items)
                            {
                                item.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                                item.Status = OrderItemStatus.Paid;
                            }
                        }
                        else
                        {
                            order.PaymentStatus = PaymentStatus.Partly;
                            order.PayAmount += model.Amount;
                            order.Balance = order.Balance - model.Amount;
                        }
                        // divider payment
                    }
                    else
                    {
                        if (order.Balance < model.Amount)
                        {
                            Difference = model.Amount - order.Balance;
                            model.Amount = model.Amount;
                        }
                        var transaction = new OrderTransaction();
                        transaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                        transaction.Amount = model.Amount;
                        transaction.Difference = Difference;
                        transaction.BeforeBalance = order.Balance;
                        transaction.AfterBalance = Math.Round(order.Balance - model.Amount, 2);
                        transaction.Order = order;
                        transaction.Method = method.Name;
                        transaction.PaymentType = method.PaymentType;
                        transaction.SeatNum = model.SeatNum;
                        transaction.DividerNum = model.DividerId;
                        transaction.Note = "";
                        transaction.Status = TransactionStatus.Open;
                        transaction.Type = TransactionType.Payment;
                        transaction.ReferenceId = model.ReferenceId;

                        _dbContext.OrderTransactions.Add(transaction);
                        _dbContext.SaveChanges();

                        order.PayAmount = order.PayAmount + model.Amount;
                        order.Balance = Math.Round(order.Balance - model.Amount, 2);

                        if (order.Balance <= 0)
                        {
                            order.PaymentStatus = PaymentStatus.Paid;
                            order.Status = OrderStatus.Paid;

                            foreach (var item in order.Items)
                            {
                                item.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                                item.Status = OrderItemStatus.Paid;
                            }
                        }
                        else
                        {
                            order.PaymentStatus = PaymentStatus.Partly;
                        }
                    }
                }
                _dbContext.SaveChanges();
                var order1 = GetOrder(model.OrderId);
                order1.GetTotalPrice(voucher1);

                _dbContext.SaveChanges();

            }

            return Json(new { status = 0, difference = Difference });
        }
        public JsonResult PayTip([FromBody] ApplyTipModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var order = _dbContext.Orders.Include(s=>s.Discounts).Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Items).ThenInclude(s => s.Product).Include(s => s.Items).ThenInclude(s => s.Questions).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items).FirstOrDefault(s => s.ID == model.OrderId);
            if (model.Amount <= 0 || order.OrderMode == OrderMode.Conduce)
            {
                return Json(new { status = 3 });
            }
            var method = _dbContext.PaymentMethods.FirstOrDefault(s => s.ID == model.Method);
            if (method == null)
            {
                return Json(new { status = 1 });
            }
            if (method.PaymentType == "C X C" || method.PaymentType == "Cuenta de la Casa")
            {
                if (string.IsNullOrEmpty(order.ClientName))
                {
                    return Json(new { status = 2 });
                }
            }

            if (order != null && model.Amount > 0)
            {
                model.Amount = model.Amount * method.Tasa;

                var transaction = new OrderTransaction();
                transaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                transaction.Amount = model.Amount;
                transaction.BeforeBalance = order.Balance;
                transaction.AfterBalance = order.Balance;
                transaction.Order = order;
                transaction.Method = method.Name;
                transaction.PaymentType = method.PaymentType;
                transaction.Note = "TIP";
                transaction.SeatNum = model.SeatNum;
                transaction.DividerNum = model.DividerId;
                transaction.Status = TransactionStatus.Open;
                transaction.Type = TransactionType.Tip;

				_dbContext.OrderTransactions.Add(transaction);
                order.Tip = model.Amount;
                var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
                order.GetTotalPrice(voucher);
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }

        public JsonResult PayRefund([FromBody] ApplyTipModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var order = _dbContext.Orders.Include(s=>s.Discounts).Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Items).ThenInclude(s => s.Product).Include(s => s.Items).ThenInclude(s => s.Questions).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items).FirstOrDefault(s => s.ID == model.OrderId);

            if (model.Amount <= 0)
            {
                return Json(new { status = 2 });
            }
            if (order.PayAmount < model.Amount)
            {
                return Json(new { status = 3 });
            }
            
            var method = _dbContext.PaymentMethods.FirstOrDefault(s => s.ID == model.Method);
            if (method == null)
            {
                return Json(new { status = 1 });
            }
            if (method.PaymentType == "C X C" || method.PaymentType == "Cuenta de la Casa")
            {
                if (string.IsNullOrEmpty(order.ClientName))
                {
                    return Json(new { status = 2 });
                }
            }

            
            if (order != null && model.Amount > 0)
            {
                model.Amount = model.Amount * method.Tasa;

                var transaction = new OrderTransaction();
                transaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                transaction.Amount = -model.Amount;
                transaction.BeforeBalance = order.Balance;
                transaction.AfterBalance = order.Balance + model.Amount;
                transaction.Order = order;
                transaction.Method = method.Name; 
                transaction.PaymentType = method.PaymentType;
                transaction.Note = "REFUND";
                transaction.Status = TransactionStatus.Open;
                transaction.Type = TransactionType.Refund;

                _dbContext.OrderTransactions.Add(transaction);
                order.Tip = model.Amount;
                order.PayAmount -= model.Amount;

                var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
                order.GetTotalPrice(voucher);
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }

        public JsonResult CheckPermission(string permission)
        {
            var valid = PermissionChecker(permission);

            if (valid)
            {
                return Json(new { status = 0 });
            }
            return Json(new { status = 1 });
        }

        public bool PermissionChecker(string permission) {

            var claims = User.Claims;
            var permissionss = claims.Where(x => x.Type == "Permission" && x.Value == permission &&
                                                            x.Issuer == "LOCAL AUTHORITY");

            var valid = permissionss.Any();

            return valid;
        }

        [HttpPost]
        public JsonResult AllowPermission([FromBody] AllowPermissionModel model)
        {
            var user = _dbContext.User.Include(s => s.Roles).ThenInclude(s => s.Permissions).FirstOrDefault(s => s.Pin == model.Pin);
            if (user == null)
            {
                return Json(new { status = 1 });
            }
            else
            {

            }
            foreach(var r in user.Roles)
            {
                foreach(var p in r.Permissions)
                {
                    if (p.Value == model.Permission)
                    {
                        return Json(new { status = 0 });
                    }
                }
            }

            return Json(new {status = 1});
        }

        [HttpPost]
        public JsonResult GetPrepareCloseDrawer()
        {
			var stationID = int.Parse(GetCookieValue("StationID"));  // HttpContext.Session.GetInt32("StationID");
			var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

            var user = User.Identity.GetName();
            var username = User.Identity.GetUserName();
            var transactions = _dbContext.OrderTransactions.Include(s=>s.Order).Where(s=>s.UpdatedBy == user && (s.Type == TransactionType.Payment || s.Type == TransactionType.Refund) && /*s.Order.Station == station &&*/ s.Status == TransactionStatus.Open).ToList();

            var tipTransactions = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.UpdatedBy == user && s.Type == TransactionType.Tip && /*s.Order.Station == station &&*/ s.Status == TransactionStatus.Open).ToList();
			var orders = _dbContext.Orders.Include(s => s.Area).Include(s => s.Table).Where(s => s.Station == station && (s.OrderType == OrderType.DiningRoom || s.OrderType == OrderType.Delivery) && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved && s.WaiterName == username && s.TotalPrice>0).ToList();
            if (orders.Count > 0)
            {
                var strOrdenes = new System.Text.StringBuilder();

                foreach(var objOrden in orders) {
                    if (!string.IsNullOrEmpty(strOrdenes.ToString())) {
                        strOrdenes.Append(", ");
                    }

                    strOrdenes.Append("<a href='/POS/Sales?orderId=").Append(objOrden.ID).Append("' style='color:white'>").Append(objOrden.ID).Append("</a>");
                }

                return Json(new { status = 2, ordenes = strOrdenes.ToString() });
            }
			decimal total = 0;
            var lstEsperadoTipoPago = new List<PaymentMethodSummary>();
			if (transactions.Count > 0)
            {
				total = transactions.Sum(s => s.Amount);

                var paymentMethods = _dbContext.PaymentMethods.Where(s => s.IsActive).ToList();

                foreach (var objPaymentMethod in paymentMethods) {
                    decimal totData = 0;

                    totData = transactions.Where(s => s.Method == objPaymentMethod.Name).Sum(s=>s.Amount);
                    int qty = transactions.Where(s => s.Method == objPaymentMethod.Name).Count();
                    var objData = new KeyValuePair<long, decimal>(objPaymentMethod.ID, totData);
                    bool isMain = false;
                    if (objPaymentMethod.PaymentType == "Effectivo") isMain = true;
                    lstEsperadoTipoPago.Add(new PaymentMethodSummary()
                    {
                        ID = objPaymentMethod.ID,
                        Qty = qty,
                        Total = totData
                    });                    
                }


            }
            decimal tiptotal = 0;
            if (tipTransactions.Count > 0)
            {
                tiptotal = tipTransactions.Sum(s=>s.Amount);
            }

            long id = 0;
            var latest = _dbContext.CloseDrawers.OrderByDescending(s => s.ID).FirstOrDefault();
            if (latest != null)
            {
                id = latest.ID + 1;
            }
            
            return Json(new { status = 0, name = user, expected = total, sequance=id, expectedtip = tiptotal, expectedpayments = lstEsperadoTipoPago });
		}

        [HttpPost]
        public JsonResult GetPrepareCloseDrawerBarcode()
        {
            var stationID = int.Parse(GetCookieValue("StationID"));  //HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.FirstOrDefault(s => s.ID == stationID);

            var user = User.Identity.GetName();
            var username = User.Identity.GetUserName();
            var transactions = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.UpdatedBy == user && s.Type == TransactionType.Payment && /*s.Order.Station == station &&*/ s.Status == TransactionStatus.Open).ToList();
            var tipTransactions = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.UpdatedBy == user && s.Type == TransactionType.Tip && /*s.Order.Station == station &&*/ s.Status == TransactionStatus.Open).ToList();
            var orders = _dbContext.Orders.Where(s => /*s.Station == station &&*/ s.OrderType == OrderType.Barcode && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved && s.WaiterName == username).ToList();
            if (orders.Count > 0)
            {
                return Json(new { status = 2 });
            }
            decimal total = 0;
            if (transactions.Count > 0)
            {
                total = transactions.Sum(s => s.Amount);
            }
            decimal tiptotal = 0;
            if (tipTransactions.Count > 0)
            {
                tiptotal = tipTransactions.Sum(s => s.Amount);
            }
            return Json(new { status = 0, name = user, expected = total, expectedtip = tiptotal });
        }

        [HttpPost]
        public JsonResult SubmitCloseDrawer([FromBody] CloseDrawerModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
			var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

			var user = User.Identity.GetName();
			var transactions = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.UpdatedBy == user && /*s.Order.Station == station &&*/ s.Status == TransactionStatus.Open).ToList();
			var tipTransactions = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.UpdatedBy == user && s.Type == TransactionType.Tip && /*s.Order.Station == station &&*/ s.Status == TransactionStatus.Open).ToList();
            decimal total = 0;
            if (transactions.Count > 0)
            {
                total = transactions.Sum(s => s.Amount);
            }
            decimal tiptotal = 0;
            if (tipTransactions.Count > 0)
            {
                tiptotal = tipTransactions.Sum(s => s.Amount);
            }
            var printModel = new CloseDrawerPrinterModel()
            {
                TransTotal = total,
                TipTotal = tiptotal,
                TransDifference = model.Difference,
                TipDifference = model.TipDifference,
                UserName = user,
            };
            var denominations = _dbContext.Denominations.OrderByDescending(s => s.Amount).ToList();
            var pmethods = _dbContext.PaymentMethods.Where(s=>s.IsActive).ToList();
            decimal GrandTotal = 0;
            foreach(var t in model.Subs)
            {
                if (t.Amount > 0)
                {
                    if (t.Type == "Denomination")
                    {
                        var domination = denominations.FirstOrDefault(s => s.ID == t.ID);
                        var amt = t.Amount * domination.Amount;

                        printModel.Denominations.Add(new CloseDrawerDominationModel()
                        {
                            Name = domination.Name,
                            Qty = (int)t.Amount,
                            Amount = amt
                        }); ;
                       // GrandTotal += amt;

                    }
                    if (t.Type == "PaymentMethod")
                    {
						var pmethod = pmethods.FirstOrDefault(s => s.ID == t.ID);
                        var amt = t.Amount * pmethod.Tasa;
						printModel.PMethods.Add(new CloseDrawerPaymentModel()
						{
							Name = pmethod.Name,
							Amount = amt
						});
						GrandTotal += amt;
					}
                }
            }
            printModel.GrandTotal = GrandTotal;

			foreach (var t in transactions)
            {
                t.Status = TransactionStatus.Closed;
                t.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
            }
            foreach(var t in tipTransactions)
            {
                t.Status = TransactionStatus.Closed;
                t.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
            }

            if (model.Difference > 0)
            {
				var transaction = new OrderTransaction();
				transaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                transaction.Amount = model.Difference;
				transaction.BeforeBalance = 0;
				transaction.AfterBalance = 0;
				transaction.Order = null;
				transaction.Method = ""; 
                transaction.PaymentType = "";
                transaction.Note = "Close Drawer";
				transaction.Status = TransactionStatus.Closed;
				transaction.Type = TransactionType.CloseDrawer;
                transaction.Memo = JsonConvert.SerializeObject(model.Subs);

				_dbContext.OrderTransactions.Add(transaction);
			}

            if (model.TipDifference > 0)
            {
				var transaction = new OrderTransaction();
                //transaction.PaymentDate = DateTime.Now;
                transaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                transaction.Amount = model.TipDifference;
				transaction.BeforeBalance = 0;
				transaction.AfterBalance = 0;
				transaction.Order = null;
				transaction.Method = ""; 
                transaction.PaymentType = "";
                transaction.Note = "Close Drawer Tip";
				transaction.Status = TransactionStatus.Closed;
				transaction.Type = TransactionType.CloseDrawer;

				_dbContext.OrderTransactions.Add(transaction);
			}

            {
                var closeDrawer = new CloseDrawer()
                {
                    ForceDate = objPOSCore.getCurrentWorkDate(stationID),
                    Username = printModel.UserName,
                    GrandTotal = printModel.GrandTotal,
                    TransDifference = printModel.TransDifference,
                    TipDifference = printModel.TipDifference,
                    TipTotal = printModel.TipTotal,
                    TransTotal = printModel.TransTotal,
                    Denominations = JsonConvert.SerializeObject(printModel.Denominations),
                    PaymentMethods = JsonConvert.SerializeObject(printModel.PMethods)
                };

                _dbContext.CloseDrawers.Add(closeDrawer);
            }

            _dbContext.SaveChanges();
            _printService.PrintCloseDrawerSummary((long)stationID, printModel);

            return Json(new { status = 0 }); ;
		}

        [HttpPost]
        public JsonResult SubmitCloseDrawerBarcode([FromBody] CloseDrawerModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

            var user = User.Identity.GetName();
            var transactions = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.UpdatedBy == user && /*s.Order.Station == station &&*/ s.Status == TransactionStatus.Open).ToList();
            var tipTransactions = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.UpdatedBy == user && s.Type == TransactionType.Tip && /*s.Order.Station == station &&*/ s.Status == TransactionStatus.Open).ToList();
            decimal total = 0;
            if (transactions.Count > 0)
            {
                total = transactions.Sum(s => s.Amount);
            }
            decimal tiptotal = 0;
            if (tipTransactions.Count > 0)
            {
                tiptotal = tipTransactions.Sum(s => s.Amount);
            }
            var printModel = new CloseDrawerPrinterModel()
            {
                TransTotal = total,
                TipTotal = tiptotal,
                TransDifference = model.Difference,
                TipDifference = model.TipDifference,
                UserName = user,
            };
            var denominations = _dbContext.Denominations.OrderByDescending(s => s.Amount).ToList();
            var pmethods = _dbContext.PaymentMethods.Where(s => s.IsActive).ToList();
            decimal GrandTotal = 0;
            foreach (var t in model.Subs)
            {
                if (t.Amount > 0)
                {
                    if (t.Type == "Denomination")
                    {
                        var domination = denominations.FirstOrDefault(s => s.ID == t.ID);
                        var amt = t.Amount * domination.Amount;

                        printModel.Denominations.Add(new CloseDrawerDominationModel()
                        {
                            Name = domination.Name,
                            Qty = (int)t.Amount,
                            Amount = amt
                        }); ;
                        GrandTotal += amt;

                    }
                    if (t.Type == "PaymentMethod")
                    {
                        var pmethod = pmethods.FirstOrDefault(s => s.ID == t.ID);
                        var amt = t.Amount * pmethod.Tasa;
                        printModel.PMethods.Add(new CloseDrawerPaymentModel()
                        {
                            Name = pmethod.Name,
                            Amount = amt
                        });
                        GrandTotal += amt;
                    }
                }
            }
            printModel.GrandTotal = GrandTotal;

            foreach (var t in transactions)
            {
                t.Status = TransactionStatus.Closed;
            }
            foreach (var t in tipTransactions)
            {
                t.Status = TransactionStatus.Closed;
            }

            if (model.Difference > 0)
            {
                var transaction = new OrderTransaction();
                //transaction.PaymentDate = DateTime.Now;
                transaction.ForceDate = objPOSCore.getCurrentWorkDate(stationID);
                transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                transaction.Amount = model.Difference;
                transaction.BeforeBalance = 0;
                transaction.AfterBalance = 0;
                transaction.Order = null;
                transaction.Method = ""; 
                transaction.PaymentType = "";
                transaction.Note = "Close Drawer";
                transaction.Status = TransactionStatus.Closed;
                transaction.Type = TransactionType.CloseDrawer;
                transaction.Memo = JsonConvert.SerializeObject(model.Subs);

                _dbContext.OrderTransactions.Add(transaction);
            }

            if (model.TipDifference > 0)
            {
                var transaction = new OrderTransaction();
                //transaction.PaymentDate = DateTime.Now;
                transaction.PaymentDate = objPOSCore.getCurrentWorkDate(stationID);
                transaction.Amount = model.TipDifference;
                transaction.BeforeBalance = 0;
                transaction.AfterBalance = 0;
                transaction.Order = null;
                transaction.Method = ""; 
                transaction.PaymentType = "";
                transaction.Note = "Close Drawer Tip";
                transaction.Status = TransactionStatus.Closed;
                transaction.Type = TransactionType.CloseDrawer;

                _dbContext.OrderTransactions.Add(transaction);
            }

            {
                var closeDrawer = new CloseDrawer()
                {
                    ForceDate = objPOSCore.getCurrentWorkDate(stationID),
                    Username = printModel.UserName,
                    GrandTotal = printModel.GrandTotal,
                    TransDifference = printModel.TransDifference,
                    TipDifference = printModel.TipDifference,
                    TipTotal = printModel.TipTotal,
                    TransTotal = printModel.TipTotal,
                    Denominations = JsonConvert.SerializeObject(printModel.Denominations),
                    PaymentMethods = JsonConvert.SerializeObject(printModel.PMethods)
                };

                _dbContext.CloseDrawers.Add(closeDrawer);
            }


            _dbContext.SaveChanges();
            _printService.PrintCloseDrawerSummary((long)stationID, printModel);

            return Json(new { status = 0 }); ;
        }

        [HttpPost]
        public JsonResult MoveTable([FromBody] MoveTableModel model)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            int status = objPOSCore.MoveTable(model);

            return Json(new { status });
            //         if (model.FromTableId == model.ToTableId)
            //         {
            //             return Json(new { status = 1 });
            //         }

            //         var srcTable = _dbContext.AreaObjects.Include(s=>s.Area).FirstOrDefault(s => s.ID == model.FromTableId);
            //         var targetTable = _dbContext.AreaObjects.Include(s=>s.Area).FirstOrDefault(s => s.ID == model.ToTableId);

            //         var order = _dbContext.Orders.Include(s=>s.Table).Include(s => s.Items).ThenInclude(s=>s.Product).Include(s=>s.Items).ThenInclude(s=>s.Discounts).Include(s => s.Items).ThenInclude(s => s.Questions).ThenInclude(s=>s.Answer).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).Include(s=>s.Discounts).Include(s=>s.Seats).ThenInclude(s=>s.Items).FirstOrDefault(s => s.Table == srcTable && s.OrderType == OrderType.DiningRoom && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved);
            //         var targetOrder = _dbContext.Orders.Include(s => s.Table).Include(s => s.Items).ThenInclude(s => s.Product).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Items).ThenInclude(s => s.Questions).ThenInclude(s=>s.Answer).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Seats).ThenInclude(s => s.Items).FirstOrDefault(s => s.Table == targetTable && s.OrderType == OrderType.DiningRoom && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved);

            //if (targetOrder == null)
            //         {
            //             order.Table = targetTable;
            //             order.Area = targetTable.Area;
            //             _dbContext.SaveChanges();
            //         }
            //         else
            //         {
            //             if (targetOrder.OrderMode == OrderMode.Seat)
            //             {
            //                 if (order.OrderMode == OrderMode.Seat)
            //                 {
            //                     var emptySeat = 0;
            //                     var seatNums = new List<int>();
            //                     foreach (var item in targetOrder.Seats)
            //                     {
            //                         if (!seatNums.Contains(item.SeatNum))
            //                             seatNums.Add(item.SeatNum);
            //                     }
            //                     for (int i = 1; i < seatNums.Max(); i++)
            //                     {
            //                         if (!seatNums.Contains(i))
            //                         {
            //                             emptySeat = i;
            //                             break;
            //                         }
            //                     }
            //                     if (emptySeat == 0) emptySeat = seatNums.Max() + 1;

            //                     foreach (var seat in order.Seats)
            //			{
            //                         if (seat.Items == null || seat.Items.Count == 0) continue;
            //				foreach (var item in seat.Items)
            //				{
            //					item.SeatNum = emptySeat;
            //				}
            //				seat.SeatNum = emptySeat;

            //				targetOrder.Seats.Add(seat);
            //                         emptySeat++;
            //			}
            //                     var index = 1;
            //                     foreach(var seat in targetOrder.Seats)
            //                     {
            //                         seat.SeatNum = index;
            //                         foreach(var item in seat.Items)
            //                         {
            //                             item.SeatNum = index;
            //                         }

            //                         index++;
            //                     }

            //			foreach (var item in order.Items)
            //			{
            //				targetOrder.Items.Add(item);
            //			}
            //		}
            //                 else
            //                 {
            //			var emptySeat = 0;
            //			var seatNums = new List<int>();
            //			foreach (var item in targetOrder.Items)
            //			{
            //				seatNums.Add(item.SeatNum);
            //			}
            //			for (int i = 1; i < seatNums.Max(); i++)
            //			{
            //				if (!seatNums.Contains(i))
            //				{
            //					emptySeat = i;
            //					break;
            //				}
            //			}
            //			if (emptySeat == 0) emptySeat = seatNums.Max() + 1;
            //			var nseat = new SeatItem() { SeatNum = emptySeat, Items = new List<OrderItem>() };
            //			foreach (var item in order.Items)
            //			{
            //                         item.SeatNum = emptySeat;
            //				targetOrder.Items.Add(item);
            //                         nseat.Items.Add(item);
            //			}

            //                     targetOrder.Seats.Add(nseat);
            //		}                    			
            //	}
            //             else if (targetOrder.OrderMode == OrderMode.Divide)
            //             {
            //                 foreach(var item in order.Items)
            //                 {
            //                     if (item.Status == OrderItemStatus.Paid) continue;
            //                     item.DividerNum = 1;
            //                     targetOrder.Items.Add(item.CopyThis());
            //                 }
            //             }
            //             else
            //             {
            //                 foreach (var item in order.Items)
            //                 {
            //			if (item.Status == OrderItemStatus.Paid) continue;
            //			targetOrder.Items.Add(item.CopyThis());
            //                 }
            //	}

            //             order.Status = OrderStatus.Moved;

            //             _dbContext.SaveChanges();
            //             var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == targetOrder.ComprobantesID);
            //             targetOrder.GetTotalPrice(voucher);

            //             _dbContext.SaveChanges();
            //         }

            //return Json(new { status = 0 });
        }

        [HttpPost]
		public IActionResult GetOrderList(long areaId, string from, string to, long cliente=0, long orden=0, decimal monto=0, int branch = 0)
		{
			try
			{
				var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
				var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

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

                var area = _dbContext.Areas.FirstOrDefault(s => s.ID == areaId);

				// Getting all Customer data  
				var customerData = _dbContext.Orders.Include(s=>s.Station).Include(s=>s.Area).Where(s=>/*s.Station == station && s.Area == area &&*/ s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void).OrderByDescending(s=>s.OrderTime).Select(s => new
				{
					s.ID,
					OrderDate = s.OrderTime.ToString("dd/MM/yyyy"),
                    s.TotalPrice,
                    s.PayAmount,
                    s.Status,
                    s.OrderTime,
                    s.ClientName,
                    s.WaiterName,
                    s.OrderMode,                    
                    s.CustomerId,
                    Branch = s.Station.IDSucursal
                });

                if (cliente > 0) {
                    customerData = (from s in customerData where s.CustomerId == cliente select s);
                }

                if (orden > 0)
                {
                    customerData = (from s in customerData where s.ID == orden select s);
                }

                if (monto > 0)
                {
                    customerData = (from s in customerData where s.TotalPrice.ToString().Contains(monto.ToString()) select s);
                }

				if (branch > 0)
				{
					customerData = (from s in customerData where s.Branch == branch select s);
				}



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

                customerData = customerData.Where(s => s.OrderTime.Date >= fromDate.Date && s.OrderTime.Date <= toDate.Date);

                //total number of rows count   
                recordsTotal = customerData.Count();
				//Paging   
				var data = customerData.Skip(skip).ToList();
				if (pageSize != -1)
				{
					data = data.Take(pageSize).ToList();
				}

				//data = data.Where(s => s.OrderTime.Date >= fromDate.Date && s.OrderTime.Date <= toDate.Date).ToList();

				//Returning Json Data  
				return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

			}
			catch (Exception ex)
			{
				throw;
			}
		}

        [HttpPost]
        public IActionResult GetOrderBarcodeList( string from, string to)
        {
            try
            {
                var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
                var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

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
                var customerData = _dbContext.Orders.Include(s => s.Station).Where(s => s.Station == station && s.OrderType == OrderType.Barcode && s.Status == OrderStatus.Saved && s.Status != OrderStatus.Void).OrderByDescending(s => s.OrderTime).Select(s => new
                {
                    s.ID,
                    OrderDate = s.OrderTime.ToString("dd/MM/yyyy"),
                    s.TotalPrice,
                    s.PayAmount,
                    s.Status,
                    s.OrderTime,
                    s.ClientName,
                    s.WaiterName,
                    s.OrderMode
                });

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

                //total number of rows count   
                recordsTotal = customerData.Count();
                //Paging   
                var data = customerData.Skip(skip).ToList();
                if (pageSize != -1)
                {
                    data = data.Take(pageSize).ToList();
                }

                data = data.Where(s => s.OrderTime.Date >= fromDate.Date && s.OrderTime.Date <= toDate.Date).ToList();

                //Returning Json Data  
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public IActionResult GetPaidOrderBarcodeList(string from, string to)
        {
            try
            {
                var stationID = HttpContext.Session.GetInt32("StationID");
                var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

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
                var customerData = _dbContext.Orders.Include(s => s.Station).Where(s => s.Station == station && s.OrderType == OrderType.Barcode && s.Status == OrderStatus.Paid && s.Status != OrderStatus.Void).OrderByDescending(s => s.OrderTime).Select(s => new
                {
                    s.ID,
                    OrderDate = s.OrderTime.ToString("dd/MM/yyyy"),
                    s.TotalPrice,
                    s.PayAmount,
                    s.Status,
                    s.OrderTime,
                    s.ClientName,
                    s.WaiterName,
                    s.OrderMode
                });

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

                //total number of rows count   
                recordsTotal = customerData.Count();
                //Paging   
                var data = customerData.Skip(skip).ToList();
                if (pageSize != -1)
                {
                    data = data.Take(pageSize).ToList();
                }

                data = data.Where(s => s.OrderTime.Date >= fromDate.Date && s.OrderTime.Date <= toDate.Date).ToList();

                //Returning Json Data  
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                throw;
            }
        }


        [HttpPost]
        public JsonResult ReprintOrder3332([FromBody] ReprintModel model)
        {
            try
            {
				var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
				var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);
                var area = _dbContext.Areas.FirstOrDefault(s => s.ID == model.AreaId);
				var user = User.Identity.GetUserName();

				if (model.TargetId == -1)
                {
					var order = _dbContext.Orders.Include(s=>s.Area).Include(s => s.Discounts).Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).ThenInclude(s => s.Answer).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == model.OrderId);

                    var newOrder = new Order()
                    {
                        OrderType = OrderType.Delivery,
                        OrderMode = OrderMode.Standard,
                        Station = station,
                        Area    = area,
                        OrderTime = DateTime.Now,
                        Status = OrderStatus.Temp,
                        ClientName = order.ClientName,
                        WaiterName = user,
                        ComprobanteName = order.ComprobanteName,
                        ComprobanteNumber = order.ComprobanteNumber,
                        ComprobantesID = order.ComprobantesID,
                        CustomerId = order.CustomerId,
                        Items = new List<OrderItem>(),
                    };
					_dbContext.Orders.Add(newOrder);
					_dbContext.SaveChanges();
					foreach (var item in order.Items)
                    {
                        var nItem = item.CopyThis();
                        nItem.Order = newOrder;
                        nItem.Status = OrderItemStatus.Pending;
                        newOrder.Items.Add(nItem);
                    }
                  
                    _dbContext.SaveChanges();
					var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == newOrder.ComprobantesID);
					newOrder.GetTotalPrice(voucher);
                    _dbContext.SaveChanges();

                    return Json(new { status = 0, orderId = newOrder.ID });
				}
                else
                {
					var table = _dbContext.AreaObjects.Include(s=>s.Area).FirstOrDefault(s => s.ID == model.TargetId);
					var order = _dbContext.Orders.Include(s => s.Discounts).Include(s=>s.Taxes).Include(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s=>s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).ThenInclude(s=>s.Answer).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Seats).FirstOrDefault(s => s.ID == model.OrderId);

					var newOrder = new Order()
					{
						OrderType = OrderType.DiningRoom,
						OrderMode = order.OrderMode,
						Station = station,
                        Area = table.Area,
                        Table = table,
						Status = OrderStatus.Temp,
                        OrderTime = DateTime.Now,
						ClientName = order.ClientName,
						WaiterName = user,
                        IsDivider = order.IsDivider,
						ComprobanteName = order.ComprobanteName,
						ComprobanteNumber = order.ComprobanteNumber,
						ComprobantesID = order.ComprobantesID,
						CustomerId = order.CustomerId,
						Items = new List<OrderItem>(),
						Seats = new List<SeatItem>(),
					}; 
                    _dbContext.Orders.Add(newOrder);
					_dbContext.SaveChanges();
					foreach (var item in order.Items)
					{
						var nItem = item.CopyThis();
                        nItem.SeatNum = item.SeatNum;
						nItem.Order = newOrder;
						nItem.Status = OrderItemStatus.Pending;
						newOrder.Items.Add(nItem);
					}
              
                    _dbContext.SaveChanges();

					if (order.OrderMode == OrderMode.Seat)
                    {
                        foreach (var seat in order.Seats)
                        {
                            var nSeat = new SeatItem()
                            {
                                SeatNum = seat.SeatNum,
                                OrderId = newOrder.ID,
                                Items = new List<OrderItem>(),
                            };

                            var items = newOrder.Items.Where(s=>!s.IsDeleted && s.SeatNum == nSeat.SeatNum).ToList();
                            foreach (var item in items)
                            {
                                nSeat.Items.Add(item);
                            }
                            newOrder.Seats.Add(nSeat);
                        }
                    }

					var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == newOrder.ComprobantesID);
					newOrder.GetTotalPrice(voucher);
					_dbContext.SaveChanges();

					return Json(new { status = 0, orderId = newOrder.ID });
				}
			}
			catch { }

            return Json(new { status = 1 });
        }

		[HttpPost]
		public IActionResult GetPaidOrderList(long areaId,string from, string to, long cliente = 0, long orden = 0, decimal monto = 0, int branch = 0, int factura=0)
		{
			try
			{
				var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
				var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

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
                var area = _dbContext.Areas.FirstOrDefault(s => s.ID == areaId);
                // Getting all Customer data
                
                var customerData = _dbContext.Orders.Include(s => s.Station).Include(s => s.Area).Where(s =>/* s.Station == station && s.Area == area &&*/ s.Status == OrderStatus.Paid).OrderByDescending(s => s.OrderTime).Select(s => new
				{
					s.ID,
                    s.Factura,
					OrderDate = s.OrderTime.ToString("dd/MM/yyyy"),
					s.TotalPrice,
					s.PayAmount,
					s.Status,
                    s.OrderTime,
                    s.UpdatedDate,
                    s.ClientName,
					s.WaiterName,
					s.OrderMode,
                    s.CustomerId,
                    Branch = s.Station.IDSucursal
				});

                if (cliente > 0)
                {
                    customerData = (from s in customerData where s.CustomerId == cliente select s);
                }

                if (orden > 0)
                {
                    customerData = (from s in customerData where s.ID == orden select s);
                }
                
                if (factura > 0)
                {
                    customerData = (from s in customerData where s.Factura == factura select s);
                }

                if (monto > 0)
                {
                    customerData = (from s in customerData where s.TotalPrice == monto select s);
                }

                if (branch > 0)
                {
					customerData = (from s in customerData where s.Branch == branch select s);
				}

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

                customerData = customerData.Where(s => s.OrderTime.Date >= fromDate.Date && s.OrderTime.Date <= toDate.Date);

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
		public JsonResult PrintOrder(long OrderID, int DivideNum = 0)
        {
            try
            {
                PrintOrderFunc(OrderID, DivideNum);

            }
            catch {
                return Json(new { status = 1 });
            }
            
            return Json(new { status = 0 });
        }

        public void PrintOrderFunc(long OrderID, int DivideNum = 0) {
            var stationID = int.Parse(GetCookieValue("StationID"));

            var order = _dbContext.Orders.Include(s => s.Items.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == OrderID);
            if (order.Items.Count == 0)
                throw new Exception("Error");                

            if (order.Status == OrderStatus.Temp)
            {
                order.OrderTime = DateTime.Now;
                order.Status = OrderStatus.Saved;
            }
            var items = order.Items.ToList();
            if (DivideNum > 0)
                items = order.Items.Where(s => s.DividerNum == DivideNum).ToList();

            _printService.PrintOrder(stationID, OrderID, DivideNum, 0, Request.Cookies["db"]);

            foreach (var item in items)
            {
                if (item.Status == OrderItemStatus.Pending)
                    item.Status = OrderItemStatus.Printed;
            }
            _dbContext.SaveChanges();
        }


        [HttpPost]
		public JsonResult PrintOrderSeat(long OrderID, int Seat = 0)
		{
			try
			{
				var stationID = int.Parse(GetCookieValue("StationID"));

				var order = _dbContext.Orders.Include(s => s.Items.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == OrderID);
				if (order.Status == OrderStatus.Temp || order.Items.Count == 0)
					return Json(new { status = 1 });

				var items = order.Items.ToList();
				if (Seat > 0)
					items = order.Items.Where(s => s.SeatNum == Seat).ToList();

				_printService.PrintOrder(stationID, OrderID, 0, Seat, Request.Cookies["db"]);

				foreach (var item in items)
				{
					if (item.Status == OrderItemStatus.Pending)
						item.Status = OrderItemStatus.Printed;
				}
				_dbContext.SaveChanges();

			}
			catch
			{
				return Json(new { status = 1 });
			}

			return Json(new { status = 0 });
		}

		[HttpPost]
		public JsonResult PrintDivideOrder(long OrderID)
		{
			try
			{
				var stationID = int.Parse(GetCookieValue("StationID"));

				var order = _dbContext.Orders.Include(s=>s.Divides).Include(s => s.Items.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == OrderID);
				if (order.Status == OrderStatus.Temp || order.Items.Count == 0)
					return Json(new { status = 1 });

				var items = order.Items.ToList();
				
                if (order.OrderMode == OrderMode.Divide)
                {
                    var divides = new List<int>();
                    foreach(var d in order.Items)
                    {
                        if (!divides.Contains(d.DividerNum))
                        {
                            divides.Add(d.DividerNum);
                        }
						
					}

                    foreach(var d in divides)
                    {
                        _printService.PrintOrder(stationID, OrderID, d, 0, Request.Cookies["db"]);
                    }
                }
			}
			catch
			{
				return Json(new { status = 1 });
			}

			return Json(new { status = 0 });
		}

		[HttpPost]
		public JsonResult RePrintOrder(long OrderID)
		{
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            try
            {
                var stationID = int.Parse(GetCookieValue("StationID"));
                var db = Request.Cookies["db"];

                int status = objPOSCore.ReprintOrder(OrderID, stationID, db);

            }
            catch
            {
                return Json(new { status = 1 });
            }

            return Json(new { status = 0 });
            //try
            //{
            //             var stationID = int.Parse(GetCookieValue("StationID"));
            //             _printService.PrintPaymentSummary(stationID, OrderID, Request.Cookies["db"], 0, 0, true);
            //}
            //catch
            //{
            //	return Json(new { status = 1 });
            //}

            //return Json(new { status = 0 });
        }


        [HttpPost]
		public JsonResult RePrintCxC(long cxcID)
		{
			try
			{
				var stationID = int.Parse(GetCookieValue("StationID"));
				_printService.PrintPaymentSummary(stationID, cxcID, Request.Cookies["db"], 0, 0, true);
			}
			catch
			{
				return Json(new { status = 1 });
			}

			return Json(new { status = 0 });
		}



		[HttpPost]
        public JsonResult UpdateOrderNote([FromBody] OrderNoteModel model)
        {
            var order = _dbContext.Orders.FirstOrDefault(s => s.ID == model.OrderId);
            if (order != null)
            {
                order.Note = model.Note;
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult UpdateTerms([FromBody] OrderTermsModel model)
        {
            var order = _dbContext.Orders.FirstOrDefault(s => s.ID == model.OrderId);
            if (order != null)
            {
                order.Terms = model.Terms;
                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult GetUserWithoutCloseStation()
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            var stationID = int.Parse(GetCookieValue("StationID"));
            var objStation = _dbContext.Stations.Where(d => d.ID == stationID).FirstOrDefault();

            DateTime dtFechaAux = objPOSCore.getCurrentWorkDate(stationID);
            DateTime dtFechaInicio = new DateTime(dtFechaAux.Year, dtFechaAux.Month, dtFechaAux.Day, 0, 0, 0);
            DateTime dtFechaFin = dtFechaInicio.AddDays(1).AddSeconds(-1);

            var orders = _dbContext.OrderItems.Include(s => s.Order).Include(s => s.Order.Station).Where(s => (s.Status == OrderItemStatus.Paid || s.Status == OrderItemStatus.Kitchen) && s.UpdatedDate >= dtFechaInicio && s.UpdatedDate <= dtFechaFin && s.Order.Station.IDSucursal == objStation.IDSucursal).ToList();

            var lstUsuarios =
                    (from o in orders
                     group o by o.CreatedBy into newGroup
                     select newGroup.Key).ToList();

            string strUsuarios = "";
            bool todosCerrados = true;

            foreach (var objUsuario in lstUsuarios)
            {
               
                var transactions = _dbContext.OrderTransactions.Include(s => s.Order).Include(s => s.Order.Station).Where(s => s.UpdatedBy == objUsuario && (s.Type == TransactionType.Payment || s.Type == TransactionType.Refund) && s.Order.Station.IDSucursal == objStation.IDSucursal && s.Status == TransactionStatus.Open).ToList();
                decimal total = 0;
                if (transactions.Count > 0)
                {
                    total = transactions.Sum(s => s.Amount);
                }

                if (total > 0) {
                    if (!string.IsNullOrEmpty(strUsuarios))
                    {
                        strUsuarios = strUsuarios + ", ";
                    }

                    strUsuarios = strUsuarios + objUsuario;
                    todosCerrados = false;
                }
            }
            
            var ordersOpen = _dbContext.Orders.Include(s => s.Table).Include(s => s.Station).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).ThenInclude(s => s.ServingSizes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).ThenInclude(s => s.Answer).Where(s => s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved && s.TotalPrice>0 && s.Station.IDSucursal == objStation.IDSucursal).ToList();

            if (ordersOpen.Any())
            {
                
                    var strOrdenes = new System.Text.StringBuilder();

                    foreach(var objOrden in ordersOpen) {
                        
                        if (!string.IsNullOrEmpty(strOrdenes.ToString())) {
                            strOrdenes.Append(", ");
                        }

                        strOrdenes.Append("<a href='/POS/Sales?orderId=").Append(objOrden.ID).Append("' style='color:white'>").Append(objOrden.ID).Append("</a>");
                    }

                    return Json(new { status = 2, ordenes = strOrdenes.ToString() });
                
            }
            
            

            return Json(new {status=1, todos = todosCerrados, usuarios = strUsuarios });
        }

        //private DateTime getCurrentWorkDate()
        //{
        //    var stationID = int.Parse(GetCookieValue("StationID"));
        //    var objStation = _dbContext.Stations.Where(d => d.ID == stationID).FirstOrDefault();

        //    var objDay = _dbContext.WorkDay.Where(d => d.IsActive == true && d.IDSucursal==objStation.IDSucursal).FirstOrDefault();
            
        //    DateTime dtNow = new DateTime(objDay.Day.Year, objDay.Day.Month, objDay.Day.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                        
        //    return dtNow;
        //}

        public string GetCookieValue(string key)
        {
            var cookieRequest = HttpContext.Request.Cookies[key];
            string cookieResponse = GetCookieValueFromResponse(HttpContext.Response, key);

            if (cookieResponse != null)
            {
                return(cookieResponse);
            }
            else
            {
                return(cookieRequest);
            }
        }

        public void DeleteCookie(string key)
        {
            var cookieRequest = HttpContext.Request.Cookies[key];
            string cookieResponse = GetCookieValueFromResponse(HttpContext.Response, key);

            if (cookieResponse != null)
            {
                HttpContext.Response.Cookies.Delete(key);
            }
            else
            {
               // HttpContext.Request.Cookies.Delete(key);

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

        // reservation
        [HttpPost]
        public JsonResult AddEditReservation(ReservationCreateModel reservation)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var stationID = int.Parse(GetCookieValue("StationID"));

            var result = objPOSCore.AddEditReservation(reservation, stationID);

            return Json(new { status = result.Item1, message = result.Item2 });

            //var reservationTime = new DateTime(2000, 1, 1);
            //try
            //{
            //	reservationTime = DateTime.ParseExact(reservation.ReservationTimeStr, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);
            //}
            //catch { }
            //if (reservationTime < DateTime.Now)
            //         {
            //             return Json(new { status = 1, message = "Invalid Reservation Time!" });
            //         }

            //         var table = _dbContext.AreaObjects.FirstOrDefault(s => s.ID == reservation.TableID && s.ObjectType == AreaObjectType.Table);

            //         var rst = reservationTime;
            //         var ren = reservationTime.AddHours((double)reservation.Duration);

            //         var reservations = _dbContext.Reservations.Where(s => s.ID != reservation.ID && s.Status != ReservationStatus.Canceled && s.TableID == reservation.TableID && s.ReservationTime.Date == reservationTime.Date).ToList();
            //         foreach(var r in reservations)
            //         {
            //             var st = r.ReservationTime;
            //             var en = r.ReservationTime.AddHours((double)r.Duration);

            //             if (rst > en) continue;
            //             if (ren < st) continue;

            //             return Json(new { status = 1, message = "There is another reservation in the time." });
            //         }
            //if (reservation.ID > 0)
            //{
            //	var exist = _dbContext.Reservations.FirstOrDefault(s => s.ID == reservation.ID);
            //	if (exist != null)
            //	{
            //                 exist.ReservationTime = reservationTime;
            //                 exist.Duration = reservation.Duration;
            //                 exist.ClientName = reservation.ClientName ?? "";
            //                 exist.GuestName = reservation.GuestName??"";
            //                 exist.GuestCount = reservation.GuestCount;
            //                 exist.TableID = reservation.TableID;
            //                 exist.Status = reservation.Status;
            //                 exist.Comments = reservation.Comments??"";
            //                 exist.Cost = reservation.Cost;
            //                 exist.PhoneNumber = reservation.PhoneNumber;
            //                 exist.TableName = table != null?table.Name:"";

            //                 _dbContext.SaveChanges();
            //	}
            //}
            //         else
            //{
            //	var stationID = int.Parse(GetCookieValue("StationID"));
            //	var newReservation = new Reservation();

            //             newReservation.StationID = stationID;
            //             newReservation.AreaID = reservation.AreaID;
            //	newReservation.ReservationTime = reservationTime;
            //	newReservation.Duration = reservation.Duration;
            //	newReservation.ClientName = reservation.ClientName??"";
            //	newReservation.GuestName = reservation.GuestName??"";
            //	newReservation.GuestCount = reservation.GuestCount;
            //	newReservation.TableID = reservation.TableID;
            //	newReservation.Status = ReservationStatus.Open;
            //	newReservation.Comments = reservation.Comments??"";
            //	newReservation.Cost = reservation.Cost;
            //	newReservation.PhoneNumber = reservation.PhoneNumber;
            //	newReservation.TableName = table != null ? table.Name : "";


            //	_dbContext.Reservations.Add(newReservation);
            //	_dbContext.SaveChanges();
            //}

            //return Json(new {status = 0});
        }

        [HttpPost]
        public JsonResult CancelReservation(ReservationCreateModel request)
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            int status = objPOSCore.CancelReservation(request.ID);

            return Json(new { status = status });

            //var reservations = _dbContext.Reservations.FirstOrDefault(s => s.ID == request.ID);
            //reservations.Status = ReservationStatus.Canceled;

            //_dbContext.SaveChanges();
            //return Json(new { status = 0 });
        }

        public JsonResult GetReservations(int areaId)
        {
            var reservations = _dbContext.Reservations.Where(s => s.AreaID == areaId && s.ReservationTime > DateTime.Now).OrderBy(s=>s.ReservationTime).ToList();

            return Json(reservations);
        }

		public JsonResult GetReservation(int id)
		{
			var reservation = _dbContext.Reservations.FirstOrDefault(s => s.ID == id);
            if (reservation == null)
            {
                return Json(new { status = 1 });
            }
			return Json(new { status = 0, reservation, dt = reservation.ReservationTime.ToString("MM-dd-yyyy"), time = reservation.ReservationTime.ToString("HH:mm") });
		}

        [HttpPost]
        public JsonResult CheckReservation(int tableID)
        {
            var reservation = _dbContext.Reservations.Where(s => s.TableID == tableID && s.Status == ReservationStatus.Open ).ToList();

            foreach(var r in reservation)
            {
                var st = r.ReservationTime.AddMinutes(-30);
                var en = r.ReservationTime.AddHours((double)r.Duration);

                if (DateTime.Now >= st && DateTime.Now <= en)
                {
                    return Json(new {status = 0, reservation = r, time = r.ReservationTime.ToString("HH:mm")});
                }
            }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult GuestArrived(int reservationID)
        {
			var reservation = _dbContext.Reservations.FirstOrDefault(s =>s.ID == reservationID);
            if (reservation != null)
            {
				reservation.ForceDate = DateTime.Now;
				reservation.Status = ReservationStatus.Arrived;

				_dbContext.SaveChanges();

				return Json(new { status = 0 });
			}
			
			return Json(new { status = 1 });
        }

		[HttpPost]
		public IActionResult GetReservationList(long areaId)
		{
			try
			{
				var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
				var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

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
				var area = _dbContext.Areas.FirstOrDefault(s => s.ID == areaId);
				//var passedReservations = _dbContext.Reservations.AsEnumerable().Where(s => s.ReservationTime < DateTime.Now).ToList();
				//foreach (var p in passedReservations)
				//{
				//	p.Status = ReservationStatus.Done;
				//}
				//_dbContext.SaveChanges();
				// Getting all Customer data  
				var customerData = _dbContext.Reservations.AsEnumerable().Where(s => s.AreaID == areaId && s.Status != ReservationStatus.Canceled && s.Status != ReservationStatus.Arrived && s.ReservationTime > DateTime.Now).OrderBy(s => s.ReservationTime).Select(s=> new
                {
                    ID = s.ID,
                    ReservationDate = s.ReservationTime.ToString("MM/dd/yyyy"),
                    ReservationTime = s.ReservationTime.ToString("HH:mm"),
                    Status = s.Status,
                    Duration = s.Duration,
                    GuestName = s.GuestName,
                    PhoneNumber = s.PhoneNumber,
                    Comments = s.Comments,
                    TableName = s.TableName
                });
               
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

        private void CopyKitchenItem(long orderID, long oldItemID, long newItemID)
        {
            var kitchenItems = _dbContext.KitchenOrderItem.Where(s => s.OrderItemID == oldItemID);
            foreach(var item in kitchenItems)
            {
                item.OrderItemID = newItemID;
            }

            _dbContext.SaveChanges();
        }

        private long GetTotalSecond(DateTime time)
        {
            return time.Hour * 3600 + time.Minute * 60 + time.Second;
        }

        [HttpPost]
        public JsonResult GetKitchenOrders(int completed, int printerChannel, string from, string to)
        {
            try
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


                var kitchenID = int.Parse(GetCookieValue("KitchenID"));
                List<KitchenOrder> orders;
                if (completed == 1)
                {
                    orders = _dbContext.KitchenOrder.Where(s => s.KitchenID == kitchenID && s.IsCompleted && s.StartTime.Date >= fromDate.Date && s.StartTime.Date <= toDate.Date).OrderBy(s => s.StartTime).ToList();
                }
                else
                {
                    orders = _dbContext.KitchenOrder.Where(s => s.KitchenID == kitchenID && !s.IsCompleted).OrderBy(s => s.StartTime).ToList();
                }

                bool hasNew = false;
                int index = 1;
                var result = new List<KitchenOrderModel>();
                foreach (var order in orders)
                {
                    var realorder = _dbContext.Orders.Include(s => s.Table).Include(s => s.PrepareType).Include(s => s.Items).ThenInclude(s => s.Questions).Include(s => s.Items).ThenInclude(s => s.Product).ThenInclude(s => s.PrinterChannels).FirstOrDefault(s => s.ID == order.OrderID);
                    if (realorder == null) continue;

                    


                    var ret = new KitchenOrderModel()
                    {
                        index = index,
                        ID = order.ID,
                        StartTime = order.StartTime,
                        TotalSecond = GetTotalSecond(order.StartTime),
                        KitchenID = order.KitchenID,
                        Status = order.Status,
                        TableName = realorder.Table == null? "" : realorder.Table.Name,
                        order = new Order()
                        {
                            ID = realorder.ID,
                            OrderType = realorder.OrderType,
                            ClientName = realorder.ClientName,
                            WaiterName = realorder.WaiterName,
                            PrepareType = realorder.PrepareType
                        },
                        IsCompleted = order.IsCompleted,
                       Items = new List<KitchenOrderItemModel>()
                    };
                    if (realorder != null && realorder.Table != null)
                    {
                        ret.TableName = realorder.Table.Name;
                    }
                    
                    if (order.Status == KitchenOrderStatus.Open)
                    {
                        //order.Status = KitchenOrderStatus.Started;
                        ret.IsNew = true;
                        hasNew = true;
                    }
                    var kitems = _dbContext.KitchenOrderItem.Where(s => s.KitchenOrderID == order.ID).ToList();
                    foreach (var item in kitems)
                    {
                        var ritem = realorder.Items.FirstOrDefault(s => s.ID == item.OrderItemID);
                        if (ritem == null) continue;

                        if (ritem.Product != null && printerChannel > 0)
                        {
                            var havePrinterChannel = ritem.Product.PrinterChannels.FirstOrDefault(s => s.ID == printerChannel);
                            if (havePrinterChannel == null)
                                continue;
                        }
                        var name = ritem.Product.Printer;
                        if (string.IsNullOrEmpty(name))
                        {
                            name = ritem.Name;
                        }

                        var nritem = new KitchenOrderItemModel()
                        {
                            ID = item.ID,
                            Status = item.Status,
                            KitchenOrderID = item.KitchenOrderID,
                            OrderItemID = item.OrderItemID,
                            Name = name,
                            ServingSizeName = ritem.ServingSizeName,
                            Qty = ritem.Qty,
                            Questions = ritem.Questions.OrderBy(s=>s.Divisor).ToList(),
                            Note = ritem.Note,
                            Course = ritem.Course,
                            CourseID = ritem.CourseID
                        };

                        
                        if (item.Status == KitchOrderItemStatus.Open)
                        {
                            item.Status = KitchOrderItemStatus.Started;
                            
                            hasNew = true;
                        }

                        if (ritem.IsDeleted && (item.Status == KitchOrderItemStatus.Started || item.Status == KitchOrderItemStatus.Rush))
                        {
                            nritem.Status = KitchOrderItemStatus.Void;
                            item.Status = KitchOrderItemStatus.Void;
                            hasNew = true;
                            nritem.IsNew = true;
                            ret.IsNew = true;
                            order.Status = KitchenOrderStatus.Open;
                        }

                        if (nritem.Status == KitchOrderItemStatus.Void)
                        {
                            var voiditem = _dbContext.CanceledItems.Include(s => s.Item).Include(s => s.Reason).FirstOrDefault(s => s.Item == ritem);
                            if (voiditem != null)
                            {
                                nritem.CancelReason = voiditem.Reason.Reason;
                            }
                        }

                        

                        ret.Items.Add(nritem);
                    }
                    ret.Items = ret.Items.OrderBy(s => s.CourseID).ToList();

                    
                     /*var aux = (from s in _dbContext.PrepareTypes
                                   where !s.IsDeleted && s.ID == ret.order.PrepareTypeID
                                   select s).FirstOrDefault();
                    ret.order.PrepareTypeName = aux != null ? aux.Name : "";*/
                    
                    

                    result.Add(ret);
                    index++;
                }
                _dbContext.SaveChanges();
                return Json(new { data = result, hasNew });
            }
            catch(Exception ex)
            {
                return Json(new { status = 1 });
            }            
        }

        [HttpPost]
        public JsonResult GetKitchenOrderItems(long orderId, int printerChannel)
        {
            var korder = _dbContext.KitchenOrder.FirstOrDefault(s => s.ID == orderId);
            var realorder = _dbContext.Orders.Include(s => s.Table).Include(s => s.Items).ThenInclude(s => s.Questions).Include(s => s.Items).ThenInclude(s => s.Product).ThenInclude(s => s.PrinterChannels).FirstOrDefault(s => s.ID == korder.OrderID);
            var kitems = _dbContext.KitchenOrderItem.Where(s => s.KitchenOrderID == orderId).ToList();

            var result = new List<KitchenOrderItemModel>();
            foreach (var item in kitems)
            {
                var ritem = realorder.Items.FirstOrDefault(s => s.ID == item.OrderItemID);
                if (ritem == null) continue;

                if (ritem.Product != null && printerChannel > 0)
                {
                    var havePrinterChannel = ritem.Product.PrinterChannels.FirstOrDefault(s => s.ID == printerChannel);
                    if (havePrinterChannel == null)
                        continue;
                }

                var nritem = new KitchenOrderItemModel()
                {
                    ID = item.ID,
                    Status = item.Status,
                    KitchenOrderID = item.KitchenOrderID,
                    OrderItemID = item.OrderItemID,
                    Name = ritem.Name,
                    Qty = ritem.Qty,
                    Questions = ritem.Questions
                };

                if (item.Status == KitchOrderItemStatus.Void)
                {
                    var voiditem = _dbContext.CanceledItems.Include(s => s.Item).Include(s => s.Reason).FirstOrDefault(s => s.Item == ritem);
                    if (voiditem != null)
                    {
                        nritem.CancelReason = voiditem.Reason.Reason;
                    }
                }
                if (item.Status == KitchOrderItemStatus.Open)
                {
                    item.Status = KitchOrderItemStatus.Started;
                    nritem.IsNew = true;
                }
                result.Add(nritem);
            }

            return Json(new { data = result });
        }

        [HttpPost]
        public JsonResult StartKitchenOrder(KitchenOrderUpdateRequest request)
        {
            var order = _dbContext.KitchenOrder.FirstOrDefault(s => s.ID == request.KitchenOrderID);
            if (order != null)
            {
                if (order.Status == KitchenOrderStatus.Open)
                    order.Status = KitchenOrderStatus.Started;

                _dbContext.SaveChanges();
            }
            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult FinishKitchenOrder(KitchenOrderUpdateRequest request)
        {
            var order = _dbContext.KitchenOrder.FirstOrDefault(s => s.ID == request.KitchenOrderID);
            if (order != null)
            {
                if (order.Status == KitchenOrderStatus.Started)
                    order.Status = KitchenOrderStatus.Done;

                var items = _dbContext.KitchenOrderItem.Where(s => s.KitchenOrderID == request.KitchenOrderID).ToList();
                foreach(var item in items)
                {
                    item.Status = KitchOrderItemStatus.Done;
                }

                order.IsCompleted = true;
                order.CompleteTime = DateTime.Now;

                _dbContext.SaveChanges();
            }
            return Json(new { status = 0 }) ;
        }

        [HttpPost]
        public JsonResult FinishKitchenOrderItem(KitchenOrderItemUpdateRequest request)
        {
            var item = _dbContext.KitchenOrderItem.FirstOrDefault(s => s.ID == request.KitchenOrderItemID);
            if (item != null)
            {
                item.Status = request.Status;
                _dbContext.SaveChanges();
                var orderItems = _dbContext.KitchenOrderItem.Where(s => s.KitchenOrderID == item.KitchenOrderID).ToList();
                int doneCount = 0;
                foreach(var d in orderItems)
                {
                    if (d.Status == KitchOrderItemStatus.Done) doneCount++;
                }

                if (doneCount == orderItems.Count)
                {
                    var order = _dbContext.KitchenOrder.FirstOrDefault(s => s.ID == item.KitchenOrderID);
                    if (order != null)
                    {
                        order.Status = KitchenOrderStatus.Done;
                        order.CompleteTime = DateTime.Now;

                        _dbContext.SaveChanges();
                    }
                }
            }

            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult PrintKitchenOrder(KitchenPrintModel request)
        {
            var kitchenID = int.Parse(GetCookieValue("KitchenID"));
            var order = _dbContext.KitchenOrder.FirstOrDefault(s => s.ID == request.KitchenOrderID);
            if (order != null)
            {
                var items = new List<OrderItem>();
                var realorder = _dbContext.Orders.Include(s => s.Table).Include(s => s.Items).ThenInclude(s => s.Questions).Include(s => s.Items).ThenInclude(s => s.Product).ThenInclude(s => s.PrinterChannels).FirstOrDefault(s => s.ID == order.OrderID);

                if (realorder != null)
                {
                    var kitems = _dbContext.KitchenOrderItem.Where(s => s.KitchenOrderID == order.ID).ToList();
                    foreach (var item in kitems)
                    {
                        var ritem = realorder.Items.FirstOrDefault(s => s.ID == item.OrderItemID);
                        if (ritem == null) continue;
                        if (request.PrinterChannelID == 0)
                        {
                            items.Add(ritem);
                        }
                        else
                        {
                            if (ritem.Product != null)
                            {
                                var havePrinterChannel = ritem.Product.PrinterChannels.FirstOrDefault(s => s.ID == request.PrinterChannelID);
                                if (request.PrinterChannelID == 0 || havePrinterChannel != null)
                                {
                                    items.Add(ritem);
                                }
                            }
                        }
                    }
                }

                if (items.Count > 0)
                {
                    _printService.PrintKitchenOrderItems(kitchenID, realorder.ID, items, "");
                }
            }
            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult GetAnswerDetail(int answerID, int servingSizeID)
        {
            var answer = _dbContext.Answers.Include(s => s.Product).ThenInclude(s => s.ServingSizes).FirstOrDefault(s => s.ID == answerID);
            var question = _dbContext.Questions.Include(s => s.Answers).ThenInclude(s => s.Product).ThenInclude(s => s.ServingSizes).FirstOrDefault(s => s.ID == answer.ForcedQuestionID);

            if (answer.MatchSize && answer.Product.HasServingSize && servingSizeID > 0)
            {
                var servingSize = answer.Product.ServingSizes.FirstOrDefault(s => s.ServingSizeID == servingSizeID);
                if (servingSize == null)
                {
                    return Json(new { status = 2 });
                }
            }

            if ( question != null)
            {
                return Json(new { status = 0, answer, subquestion = question });
            }

            return Json(new {  status = 1, answer, subquestion = question });
        }

        [HttpPost]
        public JsonResult GetCourseNote(long itemId)
        {
            var item = _dbContext.OrderItems.FirstOrDefault(s => s.ID == itemId);
            var courses = _dbContext.Courses.Where(s => s.IsActive).OrderBy(s => s.Order).ToList();

            return Json(new {  item = item, courses });
        }


        [HttpPost]
        public JsonResult ChangeCourseNote([FromBody] CourseNoteChangeModel model)
        {
            var item = _dbContext.OrderItems.FirstOrDefault(s => s.ID == model.ItemID);
            var course = _dbContext.Courses.FirstOrDefault(s => s.ID == model.CourseID);
            if (item != null)
            {
                item.Note = model.Note??"";
                if (course != null)
                {
                    item.CourseID = course.ID;
                    item.Course = course.Name;
                }

                _dbContext.SaveChanges();
            }

            return Json(new { status = 0 });
        }

        [HttpPost]
        public JsonResult GetConduceOrders([FromBody] ConduceOrderRequest request)
        {
            var orders = _dbContext.Orders.Where(s => s.IsConduce && s.CustomerId == request.CustomerId & s.OrderType == request.Type).OrderBy(s=>s.OrderTime).ToList();

            return Json(orders);
        }

        [HttpPost]
        public JsonResult SubmitConduceOrders([FromBody] SubmitConduceOrdersRequest request)
        {
            POSCore objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

            if (request.Orders.Count == 0)
            {
                return Json(new { status = 1 });
            }

            try
            {
                var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");

                var newOrderID = objPOSCore.SubmitConduceOrders(request, stationID);
                
                return Json(new { status = 0, id = newOrderID });

                //            var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == request.CustomerId);
                //var stationID = int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");
                //var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);
                //var newOrder = new Order()
                //            {
                //                Station = station,
                //                OrderType = request.Type,
                //                OrderMode = OrderMode.Conduce,
                //                OrderTime = DateTime.Now,
                //                Status = OrderStatus.Temp,
                //                ClientName = customer.Name,
                //                CustomerId = customer.ID,
                //                Delivery = 0,
                //                Items = new List<OrderItem>(),
                //            };
                //            _dbContext.Orders.Add(newOrder);
                //            _dbContext.SaveChanges();

                //            var items = new List<OrderItem>();
                //            var orders = new List<Order>();
                //            int index = 0;
                //            foreach (var o in request.Orders)
                //            {
                //                var order = GetOrder(o);

                //                newOrder.Delivery += order.Delivery;

                //                if (index == 0)
                //                {
                //                    newOrder.ComprobantesID = order.ComprobantesID;
                //                    newOrder.ComprobanteName = order.ComprobanteName;
                //                }
                //                foreach (var item in order.Items)
                //                {
                //                    var nitem = item.CopyThis();

                //                    var existItem = new OrderItem();
                //                    var isExist = false;
                //                    foreach (var eitem in items)
                //                    {
                //                        if (nitem.MenuProductID == eitem.MenuProductID && nitem.ServingSizeID == eitem.ServingSizeID)
                //                        {
                //                            isExist = true;
                //                            existItem = eitem;
                //                        }
                //                    }

                //                    if (isExist)
                //                    {
                //                        existItem.Qty += item.Qty;
                //                    }
                //                    else
                //                    {
                //                        items.Add(nitem);
                //                    }
                //                }
                //                order.ConduceOrderId = newOrder.ID;
                //            }
                //            newOrder.Items = items;
                //            _dbContext.SaveChanges();
                //            var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == newOrder.ComprobantesID);

                //            newOrder.GetTotalPrice(voucher);
                //            _dbContext.SaveChanges();
                //            return Json(new { status = 0, id = newOrder.ID });
            }
            catch (Exception ex)
            {
                Console.WriteLine();
            }

            return Json(new { status = 1 });
        }
    }
    public class CourseNoteChangeModel
    {
        public long ItemID { get; set; }
        public int CourseID { get; set; }
        public string Note { get; set; }

    }

    public class ReprintModel
    {
        public long OrderId { get; set; }
        public long AreaId { get; set; }
        public long TargetId { get; set; }
    }

	public class MoveTableModel
    {
        public long FromTableId { get; set; }
        public long ToTableId { get; set; }
        public long ToAreaId { get; set; }
    }
    public class CloseDrawerModel
    {
        public decimal Difference { get; set; } = 0;
        public decimal TipDifference { get; set; } = 0;
        public List<CloseDrawerSub> Subs { get; set; }
    }
    public class CloseDrawerSub
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
    }
    public class StationOrderModel
    {
        public long ID { get; set; }
        public long AreaId { get; set; }
        public int OrderTime { get; set; }
        public string WaiterName { get; set; }
        public decimal SubTotal { get; set; }
        public bool IsDivide { get; set; }
        public int ItemCount { get; set; }
    }
    public class OrderHoldModel
    {
        public long AreaId { get; set; }
        public long OrderId { get; set; }
        public int HoldMinutes { get; set; }
    }
    public class InventoryCountDownModel
    {
        public long ProdId { get; set; }
        public long ItemId { get; set; }
        public int Qty { get; set; }
        public bool Active { get; set; }
    }
    public class DivideResultItemModel
    {
        public int DivideId { get; set; }
        public long ItemId { get; set; }
        public int SeatNum { get; set; }
        public decimal Qty { get; set; }
    }
    public class DivideItemModel
    {
        public int DivideId { get; set; }
        public List<OrderItem> Items { get; set; }
        public DivideItemModel() {
            Items = new List<OrderItem>();
        }
    }

	public class SeatInfoModel
	{
		public long SeatId { get; set; }
		public string ClientName { get; set; }	
		public long ClientId { get; set; }	
	}
    public class OrderInfoModel
	{
		public long OrderId { get; set; }
        public int DividerId { get; set; }
		public string ClientName { get; set; }
        public long CustomerId { get; set; }

        public string PhoneNumber { get; set; }
        public bool DontChangeVoucher { get; set; }
    }

    //public class OrderInfoPaymentModel
    //{
    //    public long OrderId { get; set; }
    //    public int VoucherId { get; set; }
    //    public int DivideId { get; set; }
    //}
    public class AddItemModel
	{
		public long OrderId { get; set; }
		public long ProductId { get; set; }
		public long MenuProductId { get; set; }
		public decimal Qty { get; set; }
		public int SeatNum { get; set; }
        public int DividerNum { get; set; }

    }
    public class AddBarcodeModel
    {
        public long OrderId { get; set; }
        public long ProductId { get; set; }
        public string Barcode { get; set; }

    }
    public class AddQuestionModel
	{
		public long QuestionId { get; set; }
		public List<AnswerModel> Answers { get; set; }
		//public long SmartButtonId { get; set; }
        public List<long> SmartButtons { get; set; }
	}

    public class AnswerModel
    {
        public int Order { get; set; }
        public long AnswerId { get; set; }
        public int Qty { get; set; }
        public int ServingSizeID { get; set; }
        public int Divisor { get; set; }
        public string SubAnswers { get; set; }
    }

    public class AddModifierModel
    {
        public long QuestionId { get; set; }
        public List<AnswerModel> Answers { get; set; }
        public List<long> SmartButtons { get; set; }
    }
    public class FireItemModel
	{
		public long ItemId { get; set; }
	}

 //   public class CancelItemModel <-- POSCORE
	//{
	//	public long ItemId { get; set; }
	//	public long ReasonId { get; set; }
 //       public string Pin { get; set; }
 //       public bool Consolidate { get; set; }
 //   }

    public class VoidOrderModel
    {
        public long OrderId { get; set; }
        public long ReasonId { get; set; }
        public string Pin { get; set; }
    }

    public class PromesaPagoModel
    {
        public long OrderId { get; set; }
        public decimal PromesaPago { get; set; }        
    }


    public class OrderNoteModel
    {
        public long OrderId { get; set; }
        public string Note { get; set; }
    }


    public class OrderTermsModel
    {
        public long OrderId { get; set; }
        public string Terms { get; set; }
    }

    //public class QtyChangeModel <- en POSCore
    //{
    //    public long ItemId { get; set; }
    //    public int Qty { get; set; }
    //}

    public class HoldItemModel
	{
		public long ItemId { get; set; }
		public int Type { get; set; }
		public int Hour { get; set; }
		public int Minute { get; set; }
	}

	public class MoveToSeatModel
	{
		public long ItemId { get; set; }
		public int SeatNum { get; set; }
	}
    public class CombineSeatModel
    {
		public long OrderId { get; set; }
        public int SourceNum { get; set; }
        public int TargetNum { get; set; }
    }
    public class MoveToTableModel
	{
		public long ItemId { get; set; }
		public long TableId { get; set; }
	}
    public class MoveToAreaTableModel : MoveToTableModel
    {
        public long AreaId { get; set; }
    }
    public class MoveSeatToTableModel
    {
        public long OrderId { get; set; }
        public int SeatNum { get; set; }
        public long TableId { get; set; }
    }
    public class ReorderModel
	{
		public long ItemId { get; set; }
		public int SeatNum { get; set; }
	}

	public class DiscountModel
	{
		public string Type { get; set; }
		public long TargetId { get; set; }
		public long DiscountId { get; set; }
        public int DivideId { get; set; }

    }

	public class ApplyPayModel2
    {
        public long OrderId { get; set; }
        public decimal Amount { get; set; }
        public long Method { get; set; }
        public int SeatNum { get; set; }
        public int DividerId { get; set; }
        public long ReferenceId { get; set; }
        public List<long> ReferenceIds { get; set; }

    }

    public class ApplyPayModel
    {
        public long OrderId { get; set; }
        public decimal Amount { get; set; }
        public long Method { get; set; }
        public int SeatNum { get; set; }
        public int DividerId { get; set; }
    }


    public class ApplyTipModel
    {
        public long OrderId { get; set; }
        public decimal Amount { get; set; }
        public long Method { get; set; }
        public int SeatNum { get; set; }
        public int DividerId { get; set; }
    }

    public class AllowPermissionModel
    {
        public string Permission { get; set; }
        public string Pin { get; set; }
    }

    public class ReservationCreateModel : Reservation
    {
        public string ReservationTimeStr { get; set; }
    }

    public class KitchenOrderModel: KitchenOrder
    {
        public int index { get; set; }
        public Order order { get; set; }
        public bool IsNew { get; set; }
        public long TotalSecond { get; set; }
        public string TableName { get; set; }
        public List<KitchenOrderItemModel> Items { get; set; }
    }

    public class KitchenOrderItemModel : KitchenOrderItem
    {
        public bool IsNew { get; set; }
        public string CancelReason { get; set; }
        public string Name { get; set; }
        public string ServingSizeName { get; set; }
        public decimal Qty { get; set; }
        public string Course { get; set; }
        public string Note { get; set; }
        public long CourseID { get; set; }
        
		public List<QuestionItem> Questions { get; set; }
    }

    public class KitchenOrderUpdateRequest
    {
        public long KitchenOrderID { get; set; }
        
    }

    public class KitchenOrderItemUpdateRequest
    {
        public long KitchenOrderItemID { get; set; }
        public KitchOrderItemStatus Status { get; set; }
    }

    public class KitchenPrintModel
    {
        public long KitchenOrderID { get; set; }
        public long PrinterChannelID { get; set; }
    }

    public class AnswerSizeCheckModel
    {
        public long AnswerID { get; set; }
        public bool MatchSize { get; set; }
        public bool HasSize { get; set; }
    }


    public class PaymentMethodSummary
    {
        public bool IsMain { get; set; }
        public long ID { get; set; }
        public int Qty { get; set; }
        public decimal Total { get; set; }
    }

    public class ConduceOrderRequest
    {
        public long CustomerId { get; set; }
        public OrderType Type { get; set; }
    }
    public class SubmitConduceOrdersRequest
    {
        public long CustomerId { get; set; }
        public OrderType Type { get; set; }
        public List<long> Orders { get; set; }
    }
}
