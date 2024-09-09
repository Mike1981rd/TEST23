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

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AuroraPOS.Controllers
{
    [Authorize]
    public class DeliveryController : Controller
    {
        private readonly IUserService _userService;
        private readonly IPrintService _printService;
        private readonly AppDbContext _dbContext;
        public DeliveryController(IUserService userService, ExtendedAppDbContext dbContext, IPrintService printService)
        {
            _userService = userService;
            _dbContext = dbContext._context;
            _printService = printService;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            var reasons = _dbContext.CancelReasons.ToList();
            ViewBag.CancelReasons = reasons;
            ViewBag.Branchs = _dbContext.t_sucursal.ToList();
            ViewBag.FilterBranch = HttpContext.Session.GetInt32("FilterBranch");

            int getStation = 0; //= int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");

            if (GetCookieValue("StationID") != null)
            {
                getStation = int.Parse(GetCookieValue("StationID"));
                var objEstacion = _dbContext.Stations.Where(s => s.ID == getStation).First();
                ViewBag.SucursalActual = objEstacion.IDSucursal;
            }

            return View();
        }

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


        [HttpPost]
        public IActionResult GetDeliveryList(string fecha, int? status, int branch = 0)
        {
            var reasons = _dbContext.CancelReasons.ToList();
            ViewBag.CancelReasons = reasons;
            try
            {
                //var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                // Skiping number of Rows count  
                //var start = Request.Form["start"].FirstOrDefault();
                // Paging Length 10,20  
                //var length = Request.Form["length"].FirstOrDefault();
                // Sort Column Name  
                //var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                // Sort Column Direction ( asc ,desc)  
                //var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                // Search Value from (Search box)  
                //var searchValue = Request.Form["search[value]"].FirstOrDefault();

                //Paging Size (10,20,50,100)  
                //int pageSize = length != null ? Convert.ToInt32(length) : 0;
                //int skip = start != null ? Convert.ToInt32(start) : 0;
                //int recordsTotal = 0;
                if (branch > 0)
                {
                    HttpContext.Session.SetInt32("FilterBranch",  branch);
                }
                // Getting all Customer data

                DateTime dtFecha = DateTime.ParseExact(fecha, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                var deliveryData = _dbContext.Deliverys.Where(d=>d.CreatedDate >= new DateTime(dtFecha.Year, dtFecha.Month, dtFecha.Day) && d.CreatedDate < (new DateTime(dtFecha.Year, dtFecha.Month, dtFecha.Day)).AddDays(1)).Include(s=>s.Order).ThenInclude(s=>s.Station).Include(s => s.Order).Include(s => s.Order.PrepareType).Include(s => s.Order.Items.Where(s=> !s.IsDeleted)).Include(s => s.Carrier).Include(s => s.Zone).Include(s => s.Customer).ToList();


                //deliveryData = (from d in deliveryData where d.CreatedDate >= new DateTime(dtFecha.Year, dtFecha.Month, dtFecha.Day) && d.CreatedDate < (new DateTime(dtFecha.Year, dtFecha.Month, dtFecha.Day)).AddDays(1) select d).ToList();

                if (status!=null) {
                    deliveryData = (from d in deliveryData where d.Status == (StatusEnum)status select d).ToList();
                }

                var lstResultado = new List<DeliveryAuxiliar>();

                foreach(var objDelivery in deliveryData) {
                    var objDeliveryAuxiliar = new DeliveryAuxiliar();

                    if (branch > 0 && objDelivery.Order.Station.IDSucursal != branch) continue;

                    objDeliveryAuxiliar.DeliveryId = (long)objDelivery.ID;
                    objDeliveryAuxiliar.Creacion = (objDelivery.UpdatedDate> objDelivery.CreatedDate ? objDelivery.UpdatedDate : objDelivery.CreatedDate) ;
                    objDeliveryAuxiliar.Entrega = objDelivery.DeliveryTime;
                    objDeliveryAuxiliar.Orden = objDelivery.Order;
                    objDeliveryAuxiliar.Direccion = string.IsNullOrEmpty(objDelivery.Address1) && objDelivery.Zone == null ? "Para llevar" : objDelivery.Address1;

                    if (objDelivery.Order.PrepareType!=null && objDelivery.Order.PrepareType.SinChofer) {
                        objDeliveryAuxiliar.Zona = (objDelivery.Zone != null ? objDelivery.Zone.Name  : "");
                    }
                    else {
                        objDeliveryAuxiliar.Zona = (objDelivery.Zone != null ? objDelivery.Zone.Name + "($" + objDelivery.Zone.Cost.ToString() + ")" : "");
                    }

                    
                    objDeliveryAuxiliar.Repartidor = (objDelivery.Carrier != null ? objDelivery.Carrier.Name : "");
                    objDeliveryAuxiliar.Status = objDelivery.Status;
                    if (objDelivery.Order.ConduceOrderId > 0 || objDelivery.Order.IsConduce)
                    {
                        objDeliveryAuxiliar.Status = StatusEnum.Cerrado;
                    }
                    objDeliveryAuxiliar.Actualizacion = objDelivery.UpdatedDate;
                    objDeliveryAuxiliar.DeliveryType = objDelivery.Order.PrepareType != null ? objDelivery.Order.PrepareType.Name : "";


                    objDeliveryAuxiliar.Total = objDelivery.Order.TotalPrice /*+ (objDelivery.Zone!=null ? objDelivery.Zone.Cost : 0)*/;

                    foreach(var objOrderItem in objDeliveryAuxiliar.Orden.Items) {

                        if (!string.IsNullOrEmpty(objDeliveryAuxiliar.Descripcion)) {
                            objDeliveryAuxiliar.Descripcion = objDeliveryAuxiliar.Descripcion + ", ";
                        }

                        objDeliveryAuxiliar.Descripcion = objDeliveryAuxiliar.Descripcion + objOrderItem.Name;
                    }

                    if(objDeliveryAuxiliar.Orden != null &&  objDeliveryAuxiliar.Orden.Items.Any()) {
                        lstResultado.Add(objDeliveryAuxiliar);
                    }

                   
                }

                /*Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    try
                    {
                        customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
                    catch { }
                }*/
                ////Search  
                /*if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.Trim().ToLower();
                    customerData = customerData.Where(m => m.Name.ToLower().Contains(searchValue) || m.Model.ToLower().Contains(searchValue) || m.PhysicalName.ToLower().Contains(searchValue));
                }*/

                //total number of rows count   
                //recordsTotal = customerData.Count();
                //Paging   
                /*var data = customerData.Skip(skip).ToList();
                if (pageSize != -1)
                {
                    data = data.Take(pageSize).ToList();
                }*/
                //Returning Json Data  
                return PartialView("_DeliveryList", lstResultado);

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public JsonResult GetDelivery(long Id)
        {
            try
            {
                var objDelivery = _dbContext.Deliverys.Where(s => s.ID == Id).First();

                return Json(new { status = 0, data = objDelivery });

            }
            catch (Exception ex)
            {
                var m = ex;
            }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult EditCarrier([FromBody] EditDeliveryCarrierModel request)
        {
            try
            {
                var objDelivery = _dbContext.Deliverys.Where(s => s.ID == request.Id).First();
                                
                objDelivery.DeliveryCarrierID = request.CarrierId;
                objDelivery.Status = StatusEnum.EnRuta;
                objDelivery.UpdatedDate = DateTime.Now;

                _dbContext.SaveChanges();
                return Json(new { status = 0 });

            }
            catch (Exception ex)
            {
                var m = ex;
            }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult MarkAsDelivered(long Id)
        {
            try
            {
                var objDelivery = _dbContext.Deliverys.Where(s => s.ID == Id).First();              

                objDelivery.Status = StatusEnum.Entregado;
                objDelivery.UpdatedDate = DateTime.Now;

                _dbContext.SaveChanges();
                return Json(new { status = 0 });

            }
            catch (Exception ex)
            {
                var m = ex;
            }

            return Json(new { status = 1 });
        }

        [HttpPost]
        public JsonResult MarkAsClosed(long Id)
        {
            try
            {
                var objDelivery = _dbContext.Deliverys.Include(s => s.Order).Where(s => s.ID == Id).First();

                if ( decimal.Truncate(objDelivery.Order.TotalPrice) != decimal.Truncate(objDelivery.Order.PayAmount)) {
                    return Json(new { status = 3 });
                }

                objDelivery.Status = StatusEnum.Cerrado;
                objDelivery.UpdatedDate = DateTime.Now;

                _dbContext.SaveChanges();
                return Json(new { status = 0 });

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
                objDelivery.Comments = request.Comments;
                objDelivery.DeliveryZoneID = request.ZoneId;

                var current = _dbContext.Orders.Include(s => s.Table).FirstOrDefault(s => s.ID == request.OrderId);

                objDelivery.DeliveryTime = current.CreatedDate.AddMinutes(decimal.ToDouble(objDeliveryZone.Time));

                _dbContext.SaveChanges();

                return Json(new { status = 0, cost = objDeliveryZone.Cost, time = objDeliveryZone.Time.ToString() });
            }
            catch (Exception ex)
            {
                var m = ex;
            }

            return Json(new { status = 1 });
        }

    }
}

