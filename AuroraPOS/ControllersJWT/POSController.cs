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

    [HttpPost("GetMenuCategoryList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetMenuCategoryList(long groupId)
    {
        var response = new MenuCategoryListResponse();
        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var menuCategoryList = objPOSCore.GetMenuCategoryList(groupId);

            if (menuCategoryList != null)
            {
                response.Valor = menuCategoryList;
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

    [HttpPost("GetMenuSubCategoryList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetMenuSubCategoryList(long categoryId)
    {
        var response = new MenuSubCategoryListResponse();
        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var menuSubCategoryList = objPOSCore.GetMenuSubCategoryList(categoryId);

            if (menuSubCategoryList != null)
            {
                response.Valor = menuSubCategoryList;
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

    [HttpPost("GetMenuProductList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetMenuProductList(long subCategoryId, string db)
    {
        var response = new MenuProductListResponse();
        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var menuProductList = objPOSCore.GetMenuProductList(subCategoryId, db);

            if (menuProductList != null)
            {
                response.Valor = menuProductList;
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
}