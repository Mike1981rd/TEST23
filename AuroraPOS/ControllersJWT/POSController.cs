﻿using AuroraPOS.Core;
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
using Newtonsoft.Json;
using System.Security.Claims;
using AuroraPOS.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using iText.Layout.Borders;
using static AuroraPOS.Core.POSCore;


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
    public JsonResult Station(StationRequest request)
    {
        var station = _dbContext.Stations.Include(s => s.Areas.Where(t => !t.IsDeleted)).ThenInclude(s => s.AreaObjects.Where(s => s.ObjectType == AreaObjectType.Table && !s.IsDeleted)).FirstOrDefault(s => s.ID == request.StationId);
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

        var current = _dbContext.Orders.Include(s => s.Table).FirstOrDefault(s => s.ID == request.OrderId);
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
            if (request.OrderType == OrderType.DiningRoom && request.AreaObject > 0)
            {
                var table = _dbContext.AreaObjects.Include(s => s.Area).ThenInclude(s => s.AreaObjects.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == request.AreaObject);
                if (table != null)
                {
                    order.Table = table;
                    order.Area = table.Area;

                    ViewBag.AnotherTables = table.Area.AreaObjects.Where(s => s.ObjectType == AreaObjectType.Table && s.IsActive && s.ID != request.AreaObject).ToList();
                }

                order.Person = request.Person;
            }


            if (request.OrderType == OrderType.Delivery || request.OrderType == OrderType.FastExpress || request.OrderType == OrderType.TakeAway)
            {
                order.Person = request.Person;
            }


            if (request.OrderType != OrderType.DiningRoom)
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

    //Checar por que da diferente respuesta
    [HttpGet("CheckPermission")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult CheckPermission(string permission)
    {
        var valid = PermissionChecker(permission);

        if (valid)
        {
            return Json(new { status = 0 });
        }
        return Json(new { status = 1 });
    }

    public bool PermissionChecker(string permission)
    {

        //var claims = User.Claims;
        //var permissionss = claims.Where(x => x.Type == "Permission" && x.Value == permission &&
        //                                                x.Issuer == "LOCAL AUTHORITY");

        //var valid = permissionss.Any();

        //return valid;

        var tienePermiso = User.Claims.Any(claim => claim.Value == permission); // Ajusta el Issuer si es necesario

        return tienePermiso;
    }
    
    [HttpGet("GetArea")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] 
    public JsonResult GetArea(long areaID)
    {
        var objPOSCore = new POSCore(_userService, _dbContext,_printService, _context);
        var area = objPOSCore.GetArea(areaID);
        return Json( new { area });
    }

    [HttpGet("GetMenuGroupList")]
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
    
    [HttpGet("GetMenuCategoryList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetMenuCategoryList(long groupId)
    {
        var response = new GetMenuCategoryListResponse();

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
    
    [HttpGet("GetMenuSubCategoryList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetMenuSubCategoryList(long categoryId)
    {
        var response = new GetMenuSubCategoryListResponse();

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
    
    [HttpGet("GetMenuProductList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetMenuProductList(long subCategoryId)
    {
        var response = new GetMenuProductListResponse();

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var menuProductList = objPOSCore.GetMenuProductList(subCategoryId);

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

    [HttpGet("GetOrderItems")]
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

    [HttpGet("GetAreasInStation")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetAreasInStation(int stationID)
    {
        var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);

        //Obtenemos las urls de las imagenes
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        var areas = objPOSCore.GetAreasInStation(station);

        if (station == null)
        {
            return Json("");
        }

        return Json(areas);
    }

    [HttpGet("GetAreaObjectsInArea")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetAreaObjectsInArea(AreaObjectsInAreaRequest request)
    {
        var objPOSCore =  new POSCore(_userService, _dbContext,_printService, _context);
        POSAreaObjectsInAreaResponse response = new POSAreaObjectsInAreaResponse();

        try
        {
            AreaObjects resultado = objPOSCore.GetAreaObjectsInArea(request);

            response.result = resultado;

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
    [HttpGet("GetOrderList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetOrderList(GetOrderListRequest request)
    {
        GetOrderListResponse response = new GetOrderListResponse();

        try
        {
            //var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == request.StationId);

            //Paging Size (10,20,50,100)  
            //int pageSize = request.Length != null ? Convert.ToInt32(request.Length) : 0;
            int pageSize = -1;
            //int skip = request.Start != null ? Convert.ToInt32(request.Start) : 0;
            int skip = 0;
            int recordsTotal = 0;

            var area = _dbContext.Areas.FirstOrDefault(s => s.ID == request.AreaId);

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

            if (request.Cliente > 0)
            {
                customerData = (from s in customerData where s.CustomerId == request.Cliente select s);
            }

            if (request.Orden > 0)
            {
                customerData = (from s in customerData where s.ID == request.Orden select s);
            }

            if (request.Monto > 0)
            {
                customerData = (from s in customerData where s.TotalPrice.ToString().Contains(request.Monto.ToString()) select s);
            }

            if (request.Branch > 0)
            {
                customerData = (from s in customerData where s.Branch == request.Branch select s);
            }



            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(request.To))
            {
                try
                {
                    toDate = DateTime.ParseExact(request.To, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(request.From))
            {
                try
                {
                    fromDate = DateTime.ParseExact(request.From, "dd-MM-yyyy", CultureInfo.InvariantCulture);
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

            data = data.Where(s => s.OrderTime.Date >= fromDate.Date && s.OrderTime.Date <= toDate.Date).ToList();

            //Returning Json Data 
            response.Success = true;
            response.result = new { recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data };

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
    [HttpGet("GetPaidOrderList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult GetPaidOrderList(GetPaidOrderListRequest request)
    {
        try
        {
            //var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == request.StationId);

            //Paging Size (10,20,50,100)  
            //int pageSize = request.Length != null ? Convert.ToInt32(request.Length) : 0;
            int pageSize = -1;
            int skip = 0;
            //int skip = request.Start != null ? Convert.ToInt32(request.Start) : 0;
            int recordsTotal = 0;

            var area = _dbContext.Areas.FirstOrDefault(s => s.ID == request.AreaId);
            // Getting all Customer data

            var customerData = _dbContext.Orders.Include(s => s.Station).Include(s => s.Area).Where(s => s.Station == station && s.Area == area && s.Status == OrderStatus.Paid).OrderByDescending(s => s.OrderTime).Select(s => new
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

            if (request.Cliente > 0)
            {
                customerData = (from s in customerData where s.CustomerId == request.Cliente select s);
            }

            if (request.Orden > 0)
            {
                customerData = (from s in customerData where s.ID == request.Orden select s);
            }

            if (request.Factura > 0)
            {
                customerData = (from s in customerData where s.Factura == request.Factura select s);
            }

            if (request.Monto > 0)
            {
                customerData = (from s in customerData where s.TotalPrice == request.Monto select s);
            }

            if (request.Branch > 0)
            {
                customerData = (from s in customerData where s.Branch == request.Branch select s);
            }

            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(request.To))
            {
                try
                {
                    toDate = DateTime.ParseExact(request.To, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(request.From))
            {
                try
                {
                    fromDate = DateTime.ParseExact(request.From, "dd-MM-yyyy", CultureInfo.InvariantCulture);
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
            return Json(new { recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    [HttpGet("CheckReservation")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult CheckReservation(int tableId)
    {
        var reservation = _dbContext.Reservations.Where(s => s.TableID == tableId && s.Status == ReservationStatus.Open).ToList();

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

    [HttpPost("Sales")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult Sales([FromBody] SalesRequest request)
    {
        var response = new POSSalesResponse();
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        var objSettingCore = new SettingsCore(_userService,_dbContext,_context);
        var order = new Order(); // HttpContext.Session.GetInt32("StationID");
        var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == request.StationId);
        var products = _dbContext.Products.Where(s => s.IsActive).ToList();
        ViewBag.Products = products;
        // Obtener los IDs de los elementos seleccionados
        var selectedIds = JsonConvert.DeserializeObject<List<long>>(request.SelectedItems);

        // Obtener las transacciones de la base de datos que coincidan con los IDs seleccionados
        var selectedTransactions = _dbContext.OrderTransactions.Where(t => selectedIds.Contains(t.ID)).ToList();

        ViewBag.SelectedItems = selectedTransactions;


        if (station == null)
        {
            response.status = 2;
            response.success = false;
            response.redirectTo = "Login";

            return Json(response);
        }

        if (station.SalesMode == SalesMode.Kiosk)
        {
            response.status = 1;
            response.success = false;
            response.redirectTo = "/POS/Kiosk?orderId=" + request.OrderId;

            return Json(response);
        }

        ViewBag.AnotherTables = new List<AreaObject>();
        ViewBag.AnotherAreas = new List<Area>();
        ViewBag.DiscountType = "";
        var current = objPOSCore.GetOrder(request.OrderId);
        if (current != null)
        {
            order = current;
            // checkout
            if (order.PaymentStatus == PaymentStatus.Partly && order.OrderMode != OrderMode.Divide && order.OrderMode != OrderMode.Seat)
            {
                response.status = 3;
                response.success = false;
                response.redirectTo = "/POS/Checkout?orderId=" + request.OrderId;

                return Json(response);
            }
            if (order.OrderMode == OrderMode.Conduce)
            {
                response.status = 4;
                response.success = false;
                response.redirectTo = "Station";

                return Json(response);
            }
            //var name = HttpContext.User.Identity.GetUserName();
            if (order.WaiterName != request.UserName)
            {
                var claims = User.Claims.Where(x => x.Type == "Permission" && x.Value == "Permission.POS.OtherOrder" &&
                                                        x.Issuer == "LOCAL AUTHORITY");
                if (!claims.Any())
                {
                    response.status = 5;
                    response.success = false;
                    response.redirectTo = "Station";
                    response.error = "Permission";

                    return Json(response);
                }
            }

            if (request.OrderType == OrderType.DiningRoom && request.AreaObject > 0)
            {
                var table = _dbContext.AreaObjects.Include(s => s.Area).ThenInclude(s => s.AreaObjects.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == request.AreaObject);
                if (table != null)
                {
                    ViewBag.AnotherTables = table.Area.AreaObjects.Where(s => s.ObjectType == AreaObjectType.Table && s.IsActive && s.ID != request.AreaObject).ToList();
                    ViewBag.AnotherAreas = station.Areas.Where(s => s.IsActive && s.ID != table.Area.ID).ToList();
                }
            }

            if (order.Discounts != null && order.Discounts.Count > 0)
            {
                ViewBag.DiscountType = "order";
            }
            foreach (var item in order.Items)
            {
                if (item.Discounts != null && item.Discounts.Count > 0)
                {
                    ViewBag.DiscountType = "item";
                }
            }

        }
        else
        {
            //var user = HttpContext.User.Identity.GetUserName();
            order.Station = station;
            order.WaiterName = request.UserName;
            order.OrderMode = OrderMode.Standard;
            order.OrderType = request.OrderType;
            order.Status = OrderStatus.Temp;
            var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.IsPrimary);
            order.ComprobantesID = voucher.ID;
            if (request.OrderType == OrderType.DiningRoom && request.AreaObject > 0)
            {
                var table = _dbContext.AreaObjects.Include(s => s.Area).ThenInclude(s => s.AreaObjects.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == request.AreaObject);
                if (table != null)
                {
                    order.Table = table;
                    order.Area = table.Area;

                    ViewBag.AnotherTables = table.Area.AreaObjects.Where(s => s.ObjectType == AreaObjectType.Table && s.IsActive && s.ID != request.AreaObject).ToList();
                    ViewBag.AnotherAreas = station.Areas.Where(s => s.IsActive && s.ID != table.Area.ID).ToList();
                }

                order.Person = request.Person;
            }


            if (request.OrderType == OrderType.Delivery || request.OrderType == OrderType.FastExpress || request.OrderType == OrderType.TakeAway)
            {
                order.Person = request.Person;
            }


            if (request.OrderType != OrderType.DiningRoom)
            {
                order.OrderMode = OrderMode.Standard;
            }

            _dbContext.Orders.Add(order);
            _dbContext.SaveChanges();

            order.ForceDate = objSettingCore.GetDia(request.StationId);
            _dbContext.SaveChanges();

            HttpContext.Session.SetInt32("CurrentOrderID", (int)order.ID);
        }

        if (request.OrderType == OrderType.Delivery)
        {
            bool deliveryExists = _dbContext.Deliverys.Include(s => s.Order).Where(s => s.IsActive).Where(s => s.Order.ID == order.ID).Any();

            if (!deliveryExists)
            {
                var delivery = new Delivery();
                delivery.Order = order;
                delivery.Status = StatusEnum.Nuevo;
                delivery.StatusUpdated = DateTime.Now;
                delivery.DeliveryTime = DateTime.Now;

                _dbContext.Deliverys.Add(delivery);

                if (station.PrepareTypeDefault.HasValue && station.PrepareTypeDefault > 0)
                {
                    order.PrepareTypeID = station.PrepareTypeDefault.Value; //Para llevar
                }
                else
                {
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

        response.success = true;
        response.status = 0;
        response.order = order;

        return Json(response);
    }

    [HttpGet("GetOrderItemsInCheckout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetOrderItemsInCheckout(OrderItemsInCheckoutRequest request)
    {
        var response = new GetOrderItemsInCheckoutResponse();

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var orderItemsInCheckout = objPOSCore.GetOrderItemsInCheckout(request);

            if (orderItemsInCheckout != null)
            {
                response.Valor = orderItemsInCheckout;
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
    
    

    [HttpPost("Checkout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult Checkout([FromBody] CheckoutRequest chkrequest)
    {
        var checkoutResponse = new CheckoutResponse();
        var LogError = new System.Text.StringBuilder();
        var model = new CheckOutViewModel();

        LogError.Append("OrderId: ").Append(chkrequest.OrderId).Append(" Seat: ").Append(chkrequest.Seat).Append(" DividerId: ").Append(chkrequest.DividerId);

        model.Refund = chkrequest.Refund;

        try
        {
            LogError.AppendLine().Append("--------").AppendLine().Append("L1 ");
            
            var order = _dbContext.Orders.Include(s => s.Divides).Include(s => s.Table).FirstOrDefault(s => s.ID == chkrequest.OrderId);

            LogError.AppendLine().Append("--------").AppendLine().Append("L2 ");

            Console.WriteLine("Station ID: " + chkrequest.StationId);

            if (order == null)
            {
                checkoutResponse.Success = false;
                checkoutResponse.redirectTo = "jwt/POS/Station";

                return Json(checkoutResponse);
            }

            model.OrderId = chkrequest.OrderId;
            model.SeatNum = chkrequest.Seat;
            model.DividerId = chkrequest.DividerId;
            model.Order = order;
            model.ClientName = order.ClientName;
            model.ComprebanteName = order.ComprobanteName;

            LogError.AppendLine().Append("--------").AppendLine().Append("L3 ");

            if (chkrequest.Seat > 0)
            {
                model.PaymentType = 1;
            }
            else if (chkrequest.DividerId > 0)
            {
                try
                {
                    var divide = order.Divides.FirstOrDefault(s => s.DividerNum == chkrequest.DividerId);
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
            model.Denominations = denominations;

            var paymentMethods = _dbContext.PaymentMethods.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToList();
            if (chkrequest.Seat > 0 || chkrequest.DividerId > 0)
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
                    string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", chkrequest.db, "paymentmethod", item.ID.ToString() + ".png");
                    if (System.IO.File.Exists(pathFile))
                    {
                        var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                        item.Image = Path.Combine(_baseURL, "localfiles", chkrequest.db, "paymentmethod", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                    }
                    else
                    {
                        item.Image = Path.Combine(_baseURL, "localfiles", chkrequest.db, "paymentmethod", "empty.png");
                    }
                }
            }

            model.PaymentMethods = paymentMethods;

            LogError.AppendLine().Append("--------").AppendLine().Append("L5 ");

            model.StationId = chkrequest.StationId;

            LogError.AppendLine().Append("--------").AppendLine().Append("L6 ");
            // Obtener los IDs de los elementos seleccionados
            var selectedIds = JsonConvert.DeserializeObject<List<long>>(chkrequest.SelectedItems);

            // Obtener las transacciones de la base de datos que coincidan con los IDs seleccionados
            var selectedTransactions = _dbContext.OrderTransactions.Where(t => selectedIds.Contains(t.ID)).ToList();

            model.SelectedItems = selectedTransactions;

            var store = _dbContext.Preferences.FirstOrDefault();

            model.HasSecondCurrency = false;
            if (!string.IsNullOrEmpty(store.SecondCurrency))
            {
                model.HasSecondCurrency = true;
            }

            checkoutResponse.Success = true;
            checkoutResponse.Valor = model;

            return Json(checkoutResponse);
        }
        catch (Exception ex)
        {
            var objLog = new logs();

            LogError.AppendLine().Append("--------").AppendLine().Append(ex.ToString());

            objLog.ubicacion = "Checkout";
            objLog.descripcion = LogError.ToString();
            objLog.fecha = DateTime.Now;


            _dbContext.logs.Add(objLog);
            _dbContext.SaveChanges();

            checkoutResponse.Success = false;
            checkoutResponse.Error = ex.Message;

            return Json(checkoutResponse);
        }
    }

    [HttpPost("Pay")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult Pay([FromBody] PayRequest request)
    {
        var response = new POSPayResponse();
        var model = new ApplyPayModel
        {
            Amount = request.Amount,
            DividerId = request.DividerId,
            Method = request.Method,
            OrderId = request.OrderId,
            SeatNum = request.SeatNum
        };

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var payModel = objPOSCore.Pay(model, request.StationId);

            if (payModel != null)
            {
                response.Valor = payModel;
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

    [HttpPost("PayDone")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult PayDone([FromBody] PayRequest request)
    {
        var response = new POSPayDoneResponse();
        var model = new ApplyPayModel
        {
            Amount = request.Amount,
            DividerId = request.DividerId,
            Method = request.Method,
            OrderId = request.OrderId,
            SeatNum = request.SeatNum
        };

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var payDoneModel = objPOSCore.PayDone(model, request.StationId);

            response.Valor = payDoneModel;
            response.Success = true;
            return Json(response);
        }
        catch (Exception ex)
        {
            response.Error = ex.Message;
            response.Success = false;
            return Json(response);
        }
    }

    [HttpPost("AddProductToOrderItem")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult AddProductToOrderItem([FromBody] AddProductToOrderItemRequest request)
    {
        var response = new ProductToOrderItemResponse();
        var model = new AddItemModel
        {
            OrderId = request.OrderId,
            ProductId = request.ProductId,
            MenuProductId = request.MenuProductId,
            Qty = request.Qty,
            SeatNum = request.SeatNum,
            DividerNum = request.DividerNum
        };

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var productToOrderItem = objPOSCore.AddProductToOrderItem(model, request.StationId, request.db);

            if (productToOrderItem != null)
            {
                response.Valor = productToOrderItem;
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

    [HttpGet("GetAnswerDetail")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetAnswerDetail(int answerId, int servingSizeId)
    {
        var response = new AnswerDetailResponse();

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var answerDetail = objPOSCore.GetAnswerDetail(answerId, servingSizeId);

            if (answerDetail != null)
            {
                response.Valor = answerDetail;
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

    [HttpPost("AddQuestionToItem")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult AddQuestionToItem([FromBody] AddQuestionToItemRequest request)
    {
        var response = new AddQuestionToItemResponse();

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var answerDetail = objPOSCore.AddQuestionToItem(request.ItemId, request.ServingSizeId, request.Questions, request.StationId);

            response.Valor = answerDetail;
            response.Success = true;
            return Json(response);

        }
        catch (Exception ex)
        {
            response.Error = ex.Message;
            response.Success = false;
            return Json(response);
        }
    }

    [HttpPost("SendOrder")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult SendOrder([FromBody] OrderRequest request)
    {
        var response = new SendOrderResponse();

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var sendOrder = objPOSCore.SendOrder(request.OrderId, request.StationId);

            response.Valor = sendOrder;
            response.Success = true;
            return Json(response);

        }
        catch (Exception ex)
        {
            response.Error = ex.Message;
            response.Success = false;
            return Json(response);
        }
    }

    [HttpPost("PrintOrder")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult PrintOrder([FromBody] OrderRequest request)
    {

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var wasPrinted = objPOSCore.PrintOrder(request.OrderId, request.StationId);

            return Json(new { Status = true, Valor = wasPrinted, Error = "" });

        }
        catch (Exception ex)
        {
            return Json(new { Status = false, Valor = false, Error = ex.Message });
        }
    }

    [HttpPost("GetMyOpenOrders")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetMyOpenOrders([FromBody] int stationId)
    {
        var user = User.Identity.GetUserName();
        var stationID = stationId;  //HttpContext.Session.GetInt32("StationID");

        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        GetMyOpenOrdersResponse response = new GetMyOpenOrdersResponse();

        try
        {
            List<Order> orders = objPOSCore.GetMyOpenOrders(stationID, user);

            response.Success = true;
            response.result = orders;

            if(!orders.Any())
            {
                response.message = "En este momento no hay ordenes que dar";
            }
        }
        catch(Exception e)
        {
            response.Error = e.Message;
            response.Success = false;
        }

        return Json(response);
    }

    [HttpPost("GetPrepareCloseDrawer")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetPrepareCloseDrawer(int stationId)
    {
        var stationID = stationId;  // HttpContext.Session.GetInt32("StationID");
        var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

        var user = User.Identity.GetName();
        var username = User.Identity.GetUserName();
        var transactions = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.UpdatedBy == user && (s.Type == TransactionType.Payment || s.Type == TransactionType.Refund) && /*s.Order.Station == station &&*/ s.Status == TransactionStatus.Open).ToList();

        var tipTransactions = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.UpdatedBy == user && s.Type == TransactionType.Tip && /*s.Order.Station == station &&*/ s.Status == TransactionStatus.Open).ToList();
        var orders = _dbContext.Orders.Include(s => s.Area).Include(s => s.Table).Where(s => s.Station == station && (s.OrderType == OrderType.DiningRoom || s.OrderType == OrderType.Delivery) && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved && s.WaiterName == username && s.TotalPrice > 0).ToList();

        GetPrepareCloseDrawerResponse response = new GetPrepareCloseDrawerResponse();

        if (orders.Count > 0)
        {
            response.status = 2;
            response.resultOrders = orders;
            response.message = "Existen ordenes";

            return Json(response);
        }
        decimal total = 0;
        var lstEsperadoTipoPago = new List<PaymentMethodSummary>();
        if (transactions.Count > 0)
        {
            total = transactions.Sum(s => s.Amount);

            var paymentMethods = _dbContext.PaymentMethods.Where(s => s.IsActive).ToList();

            foreach (var objPaymentMethod in paymentMethods)
            {
                decimal totData = 0;

                totData = transactions.Where(s => s.Method == objPaymentMethod.Name).Sum(s => s.Amount);
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
            tiptotal = tipTransactions.Sum(s => s.Amount);
        }

        long id = 0;
        var latest = _dbContext.CloseDrawers.OrderByDescending(s => s.ID).FirstOrDefault();
        if (latest != null)
        {
            id = latest.ID + 1;
        }

        response.status = 0;
        response.name = user;
        response.expected = total;
        response.sequance = id;
        response.expectedtip = tiptotal;
        response.resultExpectedPayments = lstEsperadoTipoPago;
       
        return Json(response);
    }

    //
    [HttpGet("GetReservationList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult GetReservationList(GetReservationListRequest request)
    {
        GetReservationListResponse response = new GetReservationListResponse();

        try
        {
            var stationID = request.stationId; // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

            //var draw = request.draw;
            // Skiping number of Rows count  
            var start = 0;//var start = request.start;
            // Paging Length 10,20  
            var length = -1;
            // Sort Column Name  
            //var sortColumn = request.sortColumn;
            // Sort Column Direction ( asc ,desc)  
            //var sortColumnDirection = request.sortColumnDirection;
            // Search Value from (Search box)  
            //var searchValue = request.searchValue;

            //Paging Size (10,20,50,100)  
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            int recordsTotal = 0;
            var area = _dbContext.Areas.FirstOrDefault(s => s.ID == request.areaId);
            //var passedReservations = _dbContext.Reservations.AsEnumerable().Where(s => s.ReservationTime < DateTime.Now).ToList();
            //foreach (var p in passedReservations)
            //{
            //	p.Status = ReservationStatus.Done;
            //}
            //_dbContext.SaveChanges();
            // Getting all Customer data  
            var customerData = _dbContext.Reservations.AsEnumerable().Where(s => s.AreaID == request.areaId && s.Status != ReservationStatus.Canceled && s.Status != ReservationStatus.Arrived && s.ReservationTime > DateTime.Now).OrderBy(s => s.ReservationTime).Select(s => new
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

            //Mapear elementos para su envÃ­o utilizando la clase CustomerData, creada a partir de la informaciÃ³n que se extrae en la query de CustomerData
            List<CustomerData> cData = new List<CustomerData>();
            foreach(var cd in data)
            {
                CustomerData d = new CustomerData();
                d.ID = cd.ID;
                d.ReservationDate = cd.ReservationDate;
                d.ReservationTime = cd.ReservationTime;
                d.Status = cd.Status;
                d.Duration = cd.Duration;
                d.GuestName = cd.GuestName;
                d.PhoneNumber = cd.PhoneNumber;
                d.Comments = cd.Comments;
                d.TableName = cd.TableName;

                cData.Add(d);
            }


            response.Success = true;
            //response.draw = draw;
            response.recordsFiltered = recordsTotal;
            response.recordsTotal = recordsTotal;
            response.data = cData;

            //Returning Json Data  
            return Json(response);

        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Error = ex.Message;
            return Json(response);
        }
    }

    [HttpPost("GetUserWithoutCloseStation")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetUserWithoutCloseStation(int stationId)
    {
        var stationID = stationId;
        var objStation = _dbContext.Stations.Where(d => d.ID == stationID).FirstOrDefault();

        GetUserWithoutCloseStationResponse response = new GetUserWithoutCloseStationResponse();

        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

        DateTime dtFechaAux = objPOSCore.getCurrentWorkDate(stationId);
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

            if (total > 0)
            {
                if (!string.IsNullOrEmpty(strUsuarios))
                {
                    strUsuarios = strUsuarios + ", ";
                }

                strUsuarios = strUsuarios + objUsuario;
                todosCerrados = false;
            }
        }

        var ordersOpen = _dbContext.Orders.Include(s => s.Table).Include(s => s.Station).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).ThenInclude(s => s.ServingSizes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).ThenInclude(s => s.Answer).Where(s => s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved && s.TotalPrice > 0 && s.Station.IDSucursal == objStation.IDSucursal).ToList();

        if (ordersOpen.Any())
        {
            response.status = 2;
            response.ordersOpen = ordersOpen;

            return Json(response);

        }


        response.status = 1;
        response.todos = todosCerrados;
        response.usuarios = strUsuarios;

        return Json(response);
    }

    //
    [HttpPost("GetConduceOrders")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetConduceOrders([FromBody] ConduceOrderRequest request)
    {
        GetConduceOrdersResponse response = new GetConduceOrdersResponse();

        try
        {
            var orders = _dbContext.Orders.Where(s => s.IsConduce && s.CustomerId == request.CustomerId & s.OrderType == request.Type).OrderBy(s => s.OrderTime).ToList();

            response.Success = true;
            response.result = orders;

            return Json(response);
        }
        catch (Exception e)
        {
            response.Success = false;
            response.Error = e.Message;

            return Json(response);
        }

    }

    [HttpPost("MoveTable")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult MoveTable([FromBody] MoveTableModel model)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        MoveTableResponse response = new MoveTableResponse();

        try
        {
            int status = objPOSCore.MoveTable(model);

            response.Success = true;
            response.status = status;
            
            if (status == 1) 
                response.Message = "Se intentÃ³ mover a la misma mesa";
            else 
                response.Message = "Mesa movida correctamente";

            return Json(response);
        }
        catch (Exception e)
        {
            response.Success = false;
            response.status = -1;
            response.Message = "OcurriÃ³ un error al realizar la operaciÃ³n: " + e.Message;

            return Json(response);
        }

    }

    [HttpPost("GiveOrder")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GiveOrder([FromBody] GiveOrderRequest request)
    {
        GiveOrderResponse response = new GiveOrderResponse();
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

        try
        {
            var user = User.Identity.GetUserName();

            int status = objPOSCore.GiveOrder(request.orderId, request.userId, request.stationId, user);
            response.Success = true;
            response.status = status;
            response.Message = "La solicitud se realizÃ³ exitosamente.";
            
            return Json(response);
        }
        catch (Exception e)
        {
            response.Success = false;
            response.Message = "OcurriÃ³ un error en la solicitud: " + e.Message;

            return Json(response);
        }
    }

    [HttpPost("RePrintOrder")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult RePrintOrder([FromBody]RePrintOrderRequest request)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        RePrintOrderResponse response = new RePrintOrderResponse();

        try
        {
            int status = objPOSCore.ReprintOrder(request.orderId, request.stationId);

            response.Success = true;
            response.status = status;
            response.Message = "La consulta se realizada correctamente";
        }
        catch (Exception e)
        {
            response.Success = false;
            response.status = 1;
            response.Message = "Hubo un error durante la consulta: " + e.Message;
            return Json(new { status = 1 });
        }

        return Json(response);
    }

    [HttpPost("AddEditReservation")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult AddEditReservation([FromBody]ReservationCreateModel reservation)
    {
        AddEditReservationResponse response = new AddEditReservationResponse();
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

        try
        {
            var result = objPOSCore.AddEditReservation(reservation, reservation.StationID);

            response.Success = true;
            response.status = result.Item1;
            response.Message = result.Item2;

            return Json(response);
        }catch (Exception e)
        {
            response.Success = false;
            response.status = -1;
            response.Message = "Ocurrió un error en la consulta: " + e.Message;
            response.Message = "Ocurri� un error en la consulta: " + e.Message;

            return Json(response);
        }
    }

    [HttpPost("CancelReservation")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult CancelReservation([FromBody]ReservationCreateModel request)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        CancelReservationResponse response = new CancelReservationResponse();

        try
        {
            int status = objPOSCore.CancelReservation(request.ID);

            response.Success = true;
            response.status = status;
            response.Message = "Operaci�n realizada exitosamente";

            return Json(response);
        }
        catch (Exception e)
        {
            response.Success = false;
            response.status = -1;
            response.Message = "Ocurri� un error en la consulta: " + e.Message;

            return Json(response);
        }
    }
    
    /*
    [HttpGet("Kiosk")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult Kiosk(int stationId, int orderId = 0)
    {
        var objPOSCore =  new POSCore(_userService, _dbContext,_printService, _context);
        KioskResponse response = new KioskResponse();

        try
        {
            var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationId);
            var products = _dbContext.Products.Where(s => s.IsActive).ToList();
            response.sucursalId = station.IDSucursal;
            response.products  = products;

            var denominations = _dbContext.Denominations.OrderByDescending(s => s.Amount).ToList();
            response.denominations = denominations;

            var paymentMethods = _dbContext.PaymentMethods.Where(s => s.IsActive).ToList();
            response.paymentMethods = paymentMethods;

            response.showExpectedPayment = PermissionChecker("Permission.POS.ShowExpectedPayment");
            response.branchs = _dbContext.t_sucursal.ToList();

            if (station == null)
            {
                throw new Exception("Estación no existe");
            }
            
            response.otherUsers = _dbContext.User.Where(s => s.Username != User.Identity.GetUserName()).ToList();

            Order order = null;
            var userName = HttpContext.User.Identity.GetUserName();
            
            order = objPOSCore.Kiosk(station, userName,orderId);
            response.currentOrderID = (int)order.ID;
            

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
            

            var reasons = _dbContext.CancelReasons.ToList();
            response.cancelReasons = reasons;
            response.discounts = _dbContext.Discounts.Where(s => s.IsActive && !s.IsDeleted).ToList();
            
            response.order = order;

            response.Success = true;

            return Json(response);
        }
        catch (Exception ex)
        {
            response.order = null;
            response.Success = false;
            response.Error = ex.Message;
            return Json(response);
        }
    }*/

    [HttpPost("UpdateCustomerName")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult UpdateCustomerName([FromBody] OrderInfoModel model)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        UpdateCustomerNameResponse response = new UpdateCustomerNameResponse();

        try
        {
            int status = objPOSCore.UpdateCustomerName(model.OrderId, model.ClientName);
            response.status = status;

            if (status == 1)
            {
                response.Message = "No se encontr� la orden con el ID proporcionado.";
            }
            else
            {
                response.Message = "Se actualiz� el nombre del cliente exitosamente.";
            }

            response.Success = true;

            return Json(response);
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = "Ocurri� un error al actualizar el nombre del cliente: " + ex.Message;
            response.status = -1;

            return Json(response);
        }
    }

    [HttpPost("SubmitConduceOrders")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult SubmitConduceOrders([FromBody] SubmitConduceOrdersRequest request, [FromBody] int stationId)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        SubmitConduceOrdersResponse response = new SubmitConduceOrdersResponse();

        if (request.Orders.Count == 0)
        {
            response.status = 1;
            response.Success = false;
            response.Message = "No se encontraron �rdenes para conduce";

            return Json(response);
        }

        try
        {
            response.newOrderId = objPOSCore.SubmitConduceOrders(request, stationId);
            response.Success = true;
            response.status = 0;

            return Json(response);
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.status = -1;
            response.Message = "Ocurri� un error al realizar la operaci�n: " + ex.Message;

            return Json(response);
        }
    }
    
    [HttpGet("Kiosk")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult Kiosk(int stationId, int orderId = 0)
    {
        var objPOSCore =  new POSCore(_userService, _dbContext,_printService, _context);
        KioskResponse response = new KioskResponse();

        try
        {
            var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationId);
            var products = _dbContext.Products.Where(s => s.IsActive).ToList();
            response.sucursalId = station.IDSucursal;
            response.products  = products;

            var denominations = _dbContext.Denominations.OrderByDescending(s => s.Amount).ToList();
            response.denominations = denominations;

            var paymentMethods = _dbContext.PaymentMethods.Where(s => s.IsActive).ToList();
            response.paymentMethods = paymentMethods;

            response.showExpectedPayment = PermissionChecker("Permission.POS.ShowExpectedPayment");
            response.branchs = _dbContext.t_sucursal.ToList();

            if (station == null)
            {
                throw new Exception("Estaci�n no existe");
            }
            
            response.otherUsers = _dbContext.User.Where(s => s.Username != User.Identity.GetUserName()).ToList();

            Order order = null;
            var userName = HttpContext.User.Identity.GetUserName();
            
            order = objPOSCore.Kiosk(station, userName,orderId);
            response.currentOrderID = (int)order.ID;
            

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
            response.cancelReasons = reasons;
            response.discounts = _dbContext.Discounts.Where(s => s.IsActive && !s.IsDeleted).ToList();
            
            response.order = order;

            response.Success = true;

            return Json(response);
        }
        catch (Exception ex)
        {
            response.order = null;
            response.Success = false;
            response.Error = ex.Message;
            return Json(response);
        }
    }

    [HttpPost("UpdateOrderInfo")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public UpdateOrderInfoResponse UpdateOrderInfo([FromBody] OrderInfoModel model)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        UpdateOrderInfoResponse response = new UpdateOrderInfoResponse();

        try
        {
            response.Data = objPOSCore.UpdateOrderInfo(model);
            response.Success = true;
            response.Message = "La orden fue actualizada correctamente";
            response.status = response.Data.status;

            return response;
        } catch (Exception e)
        {
            response.Success = false;
            response.Message = "Ocurrió un error al actualizar la información de la orden: " + e.Message;
            response.status = -1;

            return response;
        }
    }

    [HttpPost("ChangeQtyItem")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult ChangeQtyItem([FromBody] ChangeQtyItemRequest rq)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        var stationID = rq.stationId;

        ChangeQtyItemResponse response = new ChangeQtyItemResponse();

        QtyChangeModel model = new QtyChangeModel
        {
            ItemId = rq.ItemId,
            Qty = rq.Qty
        };

        try
        {
            response.status = objPOSCore.ChangeQtyItem(model, stationID);
            response.Success = true;
            response.Message = "La cantidad del producto fue actualizada correctamente";

            return Json(response);

        } catch (Exception e)
        {
            response.status = -1;
            response.Success = false;
            response.Message = "Ocurrió un error al actualizar la cantidad del producto: " + e.Message;

            return Json(response);
        }
    }

    [HttpPost("VoidOrderItem")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult VoidOrderItem([FromBody] VoidOrderItemRequest rq)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        var stationID = rq.stationId;

        CancelItemModel model = new CancelItemModel
        {
            ItemId = rq.ItemId,
            ReasonId = rq.ReasonId,
            Pin = rq.Pin,
            Consolidate = rq.Consolidate
        };

        VoidOrderItemResponse response = new VoidOrderItemResponse();

        try
        {
            int status = objPOSCore.VoidOrderItem(model, stationID);
            
            if(status == 0)
                response.Message = "La operación se realizó correctamente";
            
            else
                response.Message = "La orden no fue encontrada";

            response.Success = true;
            response.status = status;

            return Json(response);

        } catch (Exception e)
        {
            response.Success = false;
            response.status = -1;
            response.Message = "Ocurrió un error con la operación: " + e.Message;

            return Json(response);
        }
    }

    [HttpPost("UpdateOrderInfoPayment")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult UpdateOrderInfoPayment([FromBody] OrderInfoPaymentModel model)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        UpdateOrderInfoPaymentResponse response = new UpdateOrderInfoPaymentResponse();
        try
        {
            var res = objPOSCore.UpdateOrderInfoPayment(model);

            response.Success = true;
            response.status = res.Item1;
            response.ComprobanteName = res.Item2;
            response.Message = "La operación se realizó correctamente";

            return Json(response);

        } catch (Exception e)
        {
            response.Success = false;
            response.status = -1;
            response.Message = "Ocurrió un error con la operación: " + e.Message;

            return Json(response);

        }
    }

    [HttpPost("AddDiscount")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult AddDiscount([FromBody] AddDiscountRequest req)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        AddDiscountResponse response = new AddDiscountResponse();

        try
        {
            var res = objPOSCore.AddDiscount(req.discountModel, req.stationId);

            response.Success = true;
            response.status = res.Item1;
            response.type = res.Item2;
            response.Message = "La operación se realizó correctamente";

            return Json(response);
        }
        catch (Exception e)
        {
            response.Success = false;
            response.status = -1;
            response.Message = "Ocurrió un error con la operación: " + e.Message;

            return Json(response);
        }
    }

    [HttpPost("VoidOrder")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult VoidOrder([FromBody] VoidOrderRequest request)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        VoidOrderResponse response = new VoidOrderResponse();

        try
        {
            int status = objPOSCore.VoidOrder(request.orderModel, request.stationId);
            response.Success = true;
            response.status = status;
            response.Message = "La operación se realizó correctamente";

            return Json(response);
        }
        catch (Exception e)
        {
            response.Success = false;
            response.status = -1;
            response.Message = "Ocurrió un error con la operación: " + e.Message;

            return Json(response);
        }
    }

}