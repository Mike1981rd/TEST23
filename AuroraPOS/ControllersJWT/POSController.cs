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
using Newtonsoft.Json;
using System.Security.Claims;
using AuroraPOS.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;


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
    public JsonResult Station([FromBody] OrderType orderType, [FromBody] int stationId, [FromBody] OrderMode mode = OrderMode.Standard, [FromBody] long orderId = 0, [FromBody] long areaObject = 0, [FromBody] int person = 0)
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

    //Checar por que da diferente respuesta
    [HttpPost("CheckPermission")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult CheckPermission([FromBody] string permission)
    {
        var valid = PermissionChecker(permission);

        if (valid)
        {
            return Json(new { status = 0 });
        }
        return Json(new { status = 1 });
    }

    //preg por qué User.Claims no funciona en POSCore
    //Y verificar que la ruta Station si da el valor correcto nuevamente, si es que se cambia el uso
    //de User.Claims
    public bool PermissionChecker([FromBody] string permission)
    {

        var claims = User.Claims;
        var permissionss = claims.Where(x => x.Type == "Permission" && x.Value == permission &&
                                                        x.Issuer == "LOCAL AUTHORITY");

        var valid = permissionss.Any();

        return valid;
    }

    [HttpPost("GetArea")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetArea([FromBody] long areaID)
    {
        var objPOSCore = new POSCore(_userService, _dbContext,_printService, _context);
        var area = objPOSCore.GetArea(areaID,"AlfaPrimera");
        return Json( new { area });
    }

    [HttpPost("GetMenuGroupList")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetMenuGroupList([FromBody] int stationId)
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
    public JsonResult GetOrderItems([FromBody] long orderId, [FromBody] int dividerId = 0)
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
    public JsonResult GetAreasInStation([FromBody] int stationID, [FromBody] string db)
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
    public JsonResult GetAreaObjectsInArea([FromBody] int stationId, [FromBody] string db, [FromBody] long areaID)
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
    public JsonResult GetOrderList([FromBody] long areaId, [FromBody] string from, [FromBody] string to, 
        int stationId, 
        string drawF,
        string startF,
        string lengthF,
        string sortColumnF,
        string sortColumnDirectionF,
        string searchValueF,
        long cliente = 0, long orden = 0, decimal monto = 0, int branch = 0)
    {
        GetOrderListResponse response = new GetOrderListResponse();

        try
        {
            var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

            //var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
            //// Skiping number of Rows count  
            //var start = Request.Form["start"].FirstOrDefault();
            //// Paging Length 10,20  
            //var length = Request.Form["length"].FirstOrDefault();
            //// Sort Column Name  
            //var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            //// Sort Column Direction ( asc ,desc)  
            //var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            //// Search Value from (Search box)  
            //var searchValue = Request.Form["search[value]"].FirstOrDefault();

            var draw = drawF;
            // Skiping number of Rows count  
            var start = startF;
            // Paging Length 10,20  
            var length = lengthF;
            // Sort Column Name  
            var sortColumn = sortColumnF;
            // Sort Column Direction ( asc ,desc)  
            var sortColumnDirection = sortColumnDirectionF;
            // Search Value from (Search box)  
            var searchValue = searchValueF;

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
    public IActionResult GetPaidOrderList([FromBody] long areaId, [FromBody] string from, [FromBody] string to, [FromBody] int stationId,
        [FromBody] string drawF,
        [FromBody] string startF,
        [FromBody] string lengthF,
        [FromBody] string sortColumnF,
        [FromBody] string sortColumnDirectionF,
        [FromBody] string searchValueF,
        [FromBody] long cliente = 0,
        [FromBody] long orden = 0,
        [FromBody] decimal monto = 0,
        [FromBody] int branch = 0,
        [FromBody] int factura = 0)
    {
        try
        {
            var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
            var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);

            var draw = drawF;
            // Skiping number of Rows count  
            var start = startF;
            // Paging Length 10,20  
            var length = lengthF;
            // Sort Column Name  
            var sortColumn = sortColumnF;
            // Sort Column Direction ( asc ,desc)  
            var sortColumnDirection = sortColumnDirectionF;
            // Search Value from (Search box)  
            var searchValue = searchValueF;

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
    public JsonResult CheckReservation([FromBody] int tableID)
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

    [HttpPost("Sales")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult Sales([FromBody] OrderType orderType, [FromBody] int stationId, [FromBody] string userName, 
        [FromBody] OrderMode mode = OrderMode.Standard, [FromBody] long orderId = 0, [FromBody] long areaObject = 0,
        [FromBody] int person = 0, [FromBody] string selectedItems = "")
    {
        var response = new POSSalesResponse();
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        var objSettingCore = new SettingsCore(_userService,_dbContext,_context);
        var order = new Order(); // HttpContext.Session.GetInt32("StationID");
        var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationId);
        var products = _dbContext.Products.Where(s => s.IsActive).ToList();
        ViewBag.Products = products;
        // Obtener los IDs de los elementos seleccionados
        var selectedIds = JsonConvert.DeserializeObject<List<long>>(selectedItems);

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
            response.redirectTo = "/POS/Kiosk?orderId=" + orderId;

            return Json(response);
        }

        ViewBag.AnotherTables = new List<AreaObject>();
        ViewBag.AnotherAreas = new List<Area>();
        ViewBag.DiscountType = "";
        var current = objPOSCore.GetOrder(orderId);
        if (current != null)
        {
            order = current;
            // checkout
            if (order.PaymentStatus == PaymentStatus.Partly && order.OrderMode != OrderMode.Divide && order.OrderMode != OrderMode.Seat)
            {
                response.status = 3;
                response.success = false;
                response.redirectTo = "/POS/Checkout?orderId=" + orderId;

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
            if (order.WaiterName != userName)
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
            order.WaiterName = userName;
            order.OrderMode = OrderMode.Standard;
            order.OrderType = orderType;
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

            order.ForceDate = objSettingCore.GetDia(stationId);
            _dbContext.SaveChanges();

            HttpContext.Session.SetInt32("CurrentOrderID", (int)order.ID);
        }

        if (orderType == OrderType.Delivery)
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

    [HttpPost("GetOrderItemsInCheckout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetOrderItemsInCheckout([FromBody] long orderId, [FromBody] int SeatNum, [FromBody] int DividerId)
    {
        var response = new GetOrderItemsInCheckoutResponse();

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var orderItemsInCheckout = objPOSCore.GetOrderItemsInCheckout(orderId, SeatNum, DividerId);

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
    public JsonResult Checkout([FromBody] long OrderId, [FromBody] int stationId, [FromBody] string db,
        [FromBody] int Seat = 0, [FromBody] int DividerId = 0, [FromBody] string selectedItems = "",
        [FromBody] bool refund = false)
    {
        var checkoutResponse = new CheckoutResponse();
        var LogError = new System.Text.StringBuilder();

        LogError.Append("OrderId: ").Append(OrderId).Append(" Seat: ").Append(Seat).Append(" DividerId: ").Append(DividerId);

        ViewBag.Refund = refund;

        try
        {
            LogError.AppendLine().Append("--------").AppendLine().Append("L1 ");

            var model = new CheckOutViewModel();
            var order = _dbContext.Orders.Include(s => s.Divides).Include(s => s.Table).FirstOrDefault(s => s.ID == OrderId);

            LogError.AppendLine().Append("--------").AppendLine().Append("L2 ");

            Console.WriteLine("Station ID: " + stationId);

            if (order == null)
            {
                checkoutResponse.Success = false;
                checkoutResponse.redirectTo = "jwt/POS/Station";

                return Json(checkoutResponse);
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

            var paymentMethods = _dbContext.PaymentMethods.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToList();
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
                    string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", db, "paymentmethod", item.ID.ToString() + ".png");
                    if (System.IO.File.Exists(pathFile))
                    {
                        var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                        item.Image = Path.Combine(_baseURL, "localfiles", db, "paymentmethod", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                    }
                    else
                    {
                        item.Image = Path.Combine(_baseURL, "localfiles", db, "paymentmethod", "empty.png");
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
    public JsonResult Pay([FromBody] int amount, [FromBody] int dividerId,
        [FromBody] int method, [FromBody] int orderId, [FromBody] int seatNum,
        [FromBody] int stationId, [FromBody] string db)
    {
        var response = new POSPayResponse();
        var model = new ApplyPayModel
        {
            Amount = amount,
            DividerId = dividerId,
            Method = method,
            OrderId = orderId,
            SeatNum = seatNum
        };

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var payModel = objPOSCore.Pay(model,stationId,db);

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
    public JsonResult PayDone([FromBody] int amount, [FromBody] int dividerId, [FromBody] int method,
        [FromBody] int orderId, [FromBody] int seatNum, [FromBody] int stationId, [FromBody] string db)
    {
        var response = new POSPayDoneResponse();
        var model = new ApplyPayModel
        {
            Amount = amount,
            DividerId = dividerId,
            Method = method,
            OrderId = orderId,
            SeatNum = seatNum
        };

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var payDoneModel = objPOSCore.PayDone(model, stationId, db);

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
    public JsonResult AddProductToOrderItem([FromBody] long orderId, [FromBody] long productId, [FromBody] long menuProductId,
        [FromBody] decimal qty, [FromBody] int seatNum, [FromBody] int dividerNum, [FromBody] int stationId, [FromBody] string db)
    {
        var response = new ProductToOrderItemResponse();
        var model = new AddItemModel
        {
            OrderId = orderId,
            ProductId = productId,
            MenuProductId = menuProductId,
            Qty = qty,
            SeatNum = seatNum,
            DividerNum = dividerNum
        };

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var productToOrderItem = objPOSCore.AddProductToOrderItem(model, stationId, db);

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

    [HttpPost("GetAnswerDetail")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public JsonResult GetAnswerDetail([FromBody] int answerId, [FromBody] int servingSizeId)
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
    public JsonResult AddQuestionToItem([FromBody] long itemId, [FromBody] long servingSizeId, [FromBody] List<AddQuestionModel> questions, [FromBody] int stationId)
    {
        var response = new AddQuestionToItemResponse();

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var answerDetail = objPOSCore.AddQuestionToItem(itemId, servingSizeId, questions, stationId);

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
    public JsonResult SendOrder([FromBody] long orderId, [FromBody] int stationId, [FromBody] string db, [FromBody] DateTime? saveDate = null)
    {
        var response = new SendOrderResponse();

        try
        {
            var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
            var sendOrder = objPOSCore.SendOrder(orderId,stationId,db);

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


}