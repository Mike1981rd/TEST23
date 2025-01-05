using AuroraPOS.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuroraPOS.Data;
using AuroraPOS.Services;
using AuroraPOS.Models;
using AuroraPOS.ModelsJWT;
using Microsoft.EntityFrameworkCore;
using AuroraPOS.Controllers;
using NPOI.SS.Formula.PTG;
using static SkiaSharp.HarfBuzz.SKShaper;
using AuroraPOS.ModelsJWT;
using System.Globalization;
using PuppeteerSharp;


namespace AuroraPOS.ControllersJWT;


[Route("jwt/[controller]")]
public class POSController : Controller
{
    private readonly IUserService _userService;
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _context;
    private readonly IPrintService _printService;

    public POSController(IUserService userService, ExtendedAppDbContext dbContext, IPrintService printService, IHttpContextAccessor context)
    {
        _userService = userService;
        _dbContext = dbContext._context;
        _printService = printService;
        _context = context;
    }

    [HttpGet("Station")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult Station(OrderType orderType, int stationId, OrderMode mode = OrderMode.Standard, long orderId = 0, long areaObject = 0, int person = 0)
    {
        var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
        var station = _dbContext.Stations.Include(s => s.Areas.Where(t => !t.IsDeleted)).ThenInclude(s => s.AreaObjects.Where(s => s.ObjectType == AreaObjectType.Table && !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);
        ViewBag.Sucursal = station.IDSucursal;
        var order = new Order();

        POSStationResponse response = new POSStationResponse();

        if (station == null)
        {
            response.status = "station is null";
            response.redirectTo = "Login";

            return Json(response);
        }

        if (station.SalesMode == SalesMode.Barcode)
        {
            response.status = "station salesmode equals to barcode salesmode";
            response.redirectTo = "Barcode";

            return Json(response);
        }
        else if (station.SalesMode == SalesMode.Kiosk)
        {
            response.status = "station salesmode equals to kiosk salesmode";
            response.redirectTo = "Kiosk";

            return Json(response);
        }

        ViewBag.StationName = HttpContext.Session.GetString("StationName");

        var denominations = _dbContext.Denominations.OrderByDescending(s => s.Amount).ToList();
        ViewBag.Denominations = denominations;

        var paymentMethods = _dbContext.PaymentMethods.Where(s => s.IsActive).ToList();
        ViewBag.PaymentMethods = paymentMethods;

        ViewBag.Station = station.Name;
        ViewBag.Username = User.Identity.GetUserName();

        ViewBag.OtherUsers = _dbContext.User.Where(s => s.Username != User.Identity.GetUserName()).ToList();
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
                    response.redirectTo = "Station";
                    response.error = "Permission";

                    return Json(response);
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

        response.status = "OK";
        response.order = order;

        return Json(response);
    }

    public bool PermissionChecker(string permission)
    {

        var claims = User.Claims;
        var permissionss = claims.Where(x => x.Type == "Permission" && x.Value == permission &&
                                                        x.Issuer == "LOCAL AUTHORITY");

        var valid = permissionss.Any();

        return valid;
    }

    [HttpPost("GetArea")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetArea(long areaID)
    {
        var objPOSCore = new POSCore(_userService, _dbContext,_printService, _context);
        var area = objPOSCore.GetArea(areaID,"AlfaPrimera");
        return Json( new { area });
    }

    [HttpPost("GetMenuGroupList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetMenuGroupList(int stationId)
    {
        var response = new MenuGroupResponse();
        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var menuGroupList = objPOSCore.GetMenuGroupList(stationId);

            if (menuGroupList != null)
            {
                response.Valor = menuGroupList;
                response.Success = true;
                return Json(response);
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

    [HttpPost("GetOrderItems")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
                return Json(response);
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

    [HttpPost("GetAreasInStation")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetAreasInStation(int stationID, string db)
    {
        var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);

        //Obtenemos las urls de las imagenes
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        var areas = objPOSCore.GetAreasInStation(station, db);

        if (station == null)
        {
            return Json("");
        }

        return Json(areas);
    }

    [HttpPost("GetAreaObjectsInArea")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetAreaObjectsInArea(int stationId, string db, long areaID)
    {
        var objPOSCore =  new POSCore(_userService, _dbContext,_printService, _context);
        POSAreaObjectsInAreaResponse response = new POSAreaObjectsInAreaResponse();

        try
        {
            AreaObjects resultado = objPOSCore.GetAreaObjectsInArea(stationId, db, areaID);

            response.result = resultado;

            //response.result.objects = resultado.objects;
            //response.result.orders = resultado.orders;
            //response.result.holditems = resultado.holditems;

            response.Success = true;

            return Json(response);
        }
        catch (Exception ex)
        {
            response.result = null;
            response.Success = false;
            response.Error = ex.Message;
            return Json(response);
        }
    }

    //pendiente
    [HttpPost("GetOrderList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetOrderList(long areaId, string from, string to, int stationId, long cliente = 0, long orden = 0, decimal monto = 0, int branch = 0)
    {
        GetOrderListResponse response = new GetOrderListResponse();

        try
        {
            var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
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
            var customerData = _dbContext.Orders.Include(s => s.Station).Include(s => s.Area).Where(s =>/*s.Station == station && s.Area == area &&*/ s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void).OrderByDescending(s => s.OrderTime).Select(s => new
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

            if (cliente > 0)
            {
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
            response.Success = true;
            response.result = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data };

            return Json(response);

        }
        catch (Exception ex)
        {
            response.Error = ex.Message;
            response.Success = false;
            return Json(response);
        }
    }

    //pendiente
    [HttpPost("GetPaidOrderList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult GetPaidOrderList(long areaId, string from, string to, int stationId, long cliente = 0, long orden = 0, decimal monto = 0, int branch = 0, int factura = 0)
    {
        try
        {
            var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
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

    [HttpPost("CheckReservation")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult CheckReservation(int tableID)
    {
        var reservation = _dbContext.Reservations.Where(s => s.TableID == tableID && s.Status == ReservationStatus.Open).ToList();

        var response = new POSReservationResponse();

        foreach (var r in reservation)
        {
            var st = r.ReservationTime.AddMinutes(-30);
            var en = r.ReservationTime.AddHours((double)r.Duration);

            if (DateTime.Now >= st && DateTime.Now <= en)
            {
                response.status = 0;
                response.reservation = r;
                response.time = r.ReservationTime.ToString("HH:mm");

                return Json(response);
            }
        }

        response.status = 1;
        return Json(response);
    }
}