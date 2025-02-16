using Microsoft.AspNetCore.Mvc;
using AuroraPOS.Data;
using AuroraPOS.Services;
using AuroraPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuroraPOS.ModelsJWT;
using Org.BouncyCastle.Utilities;
using NPOI.SS.Formula.Functions;
using AuroraPOS.Controllers;
using NPOI.SS.Formula.PTG;
using AuroraPOS.ModelsJWT;
using Newtonsoft.Json;
using System.Diagnostics;
using AuroraPOS.ViewModels;
using AuroraPOS.ModelsCentral;
using System.Globalization;

namespace AuroraPOS.Core;

public class POSCore
{
    private readonly IUserService _userService;
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _context;
    private readonly IPrintService _printService;

    public POSCore(IUserService userService, AppDbContext dbContext, IPrintService printService, IHttpContextAccessor context)
    {
        _userService = userService;
        _dbContext = dbContext;
        _context = context;
        _printService = printService;
    }

    public Area? GetArea(long areaID, string db)
    {
        var request = _context.HttpContext.Request;
        var _baseURL = $"https://{request.Host}";

        var area = _dbContext.Areas.Include(s => s.AreaObjects.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == areaID);

        //Obtenemos la URL de la imagen del archivo            
        string pathFile = Environment.CurrentDirectory + "/wwwroot" + "/localfiles/" + db + "/area/" + area.ID.ToString() + ".png";

        if (System.IO.File.Exists(pathFile))
        {
            var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
            area.BackImage = _baseURL + "/localfiles/" + db + "/area/" + area.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second;
        }
        else
        {
            area.BackImage = _baseURL + "/localfiles/" + db + "/area/" + "empty.png";
        }

        //Obtenemos las urls de las imagenes            
        if (area.AreaObjects != null && area.AreaObjects.Any())
        {
            foreach (var item in area.AreaObjects)
            {
                pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", db, "areaobject", item.ID.ToString() + ".png");
                if (System.IO.File.Exists(pathFile))
                {
                    var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                    item.BackImage = Path.Combine(_baseURL, "localfiles", db, "areaobject", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                }
                else
                {
                    item.BackImage = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", "empty.png");
                }
            }
        }

        /*var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            //MaxDepth = 10, // Fixed
            ReferenceHandler = ReferenceHandler.Preserve
        };*/

        return area;
    }

    public List<MenuGroup>? GetMenuGroupList(int stationId)
    {
        var station = _dbContext.Stations.Include(s => s.MenuSelect).ThenInclude(s => s.Groups).FirstOrDefault(s => s.ID == stationId);

        var menu = station.MenuSelect;

        var groups = menu.Groups.OrderBy(s => s.Order).ToList();

        if (groups != null)
        {
            return groups;
        }
        return null;
    }

    public GetOrderItem? GetOrderItems(long orderId, int dividerId = 0)
    {
        var orderItem = new GetOrderItem();
        orderItem.status = 1;
        var order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions.OrderBy(s => s.Divisor)).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == orderId);
        var lstSmart = _dbContext.SmartButtons.Select(m => m.Name).ToList().Distinct();

        if (order.OrderMode == OrderMode.Seat)
        {
            order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions.OrderBy(s => s.Divisor)).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).FirstOrDefault(o => o.ID == orderId);

            //Quitamos las imagenes del objeto para hacer mas rapido el pos
            if (order.Items != null && order.Items.Any())
            {
                foreach (var objItem in order.Items)
                {
                    objItem.Product.Photo = null;

                    foreach (var objQuestion in objItem.Questions)
                    {
                        if (lstSmart.Any(objQuestion.Description.Contains))
                        {
                            objQuestion.showDesc = true;
                        }
                    }
                }
            }

            orderItem.Order = order;
            orderItem.status = 0;
            orderItem.Seat = order.Seats;

            return orderItem;
        }
        else if (order.OrderMode == OrderMode.Divide && dividerId > 0)
        {
            order = _dbContext.Orders.Include(s => s.Divides).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions.OrderBy(s => s.Divisor)).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == orderId);

            var items = order.Items.Where(s => s.DividerNum == dividerId).ToList();
            var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
            order.GetTotalPrice(voucher, dividerId);

            var clientName = "";
            try
            {
                var divide = order.Divides.FirstOrDefault(s => s.DividerNum == dividerId);
                if (divide != null)
                {
                    clientName = divide.ClientName;
                }
            }
            catch { }
            //Quitamos las imagenes del objeto para hacer mas rapido el pos
            if (order.Items != null && order.Items.Any())
            {
                foreach (var objItem in order.Items)
                {
                    objItem.Product.Photo = null;
                }
            }
            orderItem.status = 0;
            orderItem.Items = items;
            orderItem.Order = order;
            orderItem.clientName = clientName;

            return orderItem;
        }
        else
        {
            //Quitamos las imagenes del objeto para hacer mas rapido el pos
            if (order.Items != null && order.Items.Any())
            {
                foreach (var objItem in order.Items)
                {
                    objItem.Product.Photo = null;

                    foreach (var objQuestion in objItem.Questions)
                    {
                        if (lstSmart.Any(objQuestion.Description.Contains))
                        {
                            objQuestion.showDesc = true;
                        }
                    }
                }
            }
            orderItem.status = 0;
            orderItem.Items = order.Items.OrderBy(s => s.CourseID).ToList();
            orderItem.Order = order;

            return orderItem;
        }

        return orderItem;
    }

    public List<Area> GetAreasInStation(Station station, string db)
    {
        var request = _context.HttpContext.Request;
        var _baseURL = $"https://{request.Host}";
        if (station.Areas != null && station.Areas.Any())
        {
            foreach (var item in station.Areas)
            {
                var pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", db, "area", item.ID.ToString() + ".png");
                if (System.IO.File.Exists(pathFile))
                {
                    var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                    item.BackImage = Path.Combine(_baseURL, "localfiles", db, "area", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                }
                else
                {
                    item.BackImage = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", "empty.png");
                }
            }
        }

        return station.Areas;
    }

    public AreaObjects GetAreaObjectsInArea(AreaObjectsInAreaRequest Arearequest)
    {
        var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == Arearequest.StationId);
        var area = _dbContext.Areas.Include(s => s.AreaObjects.Where(s => !s.IsDeleted)).AsNoTracking().FirstOrDefault(s => s.ID == Arearequest.AreaId);

        //Obtenemos las urls de las imagenes
        var request = _context.HttpContext.Request;
        var _baseURL = $"https://{request.Host}";
        if (area.AreaObjects != null && area.AreaObjects.Any())
        {
            foreach (var item in area.AreaObjects)
            {
                //string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "areaobject", item.ID.ToString() + ".png");
                string pathFile = Environment.CurrentDirectory + "/wwwroot" + "/localfiles/" + Arearequest.Db + "/areaobject/" + item.ID.ToString() + ".png";
                if (System.IO.File.Exists(pathFile))
                {
                    var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                    //item.BackImage = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                    item.BackImage = _baseURL + "/localfiles/" + Arearequest.Db + "/areaobject/" + item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second;
                }
                else
                {
                    item.BackImage = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", "empty.png");
                }
            }
        }

        var orders = _dbContext.Orders.Include(s => s.Table).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).ThenInclude(s => s.ServingSizes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).ThenInclude(s => s.Answer).Where(s => s.Area == area && s.OrderType == OrderType.DiningRoom && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved).ToList();
        var result = new List<StationOrderModel>();
        var holditems = new List<OrderHoldModel>();


        foreach (var order in orders)
        {
            var kichenItems = new List<OrderItem>();
            foreach (var item in order.Items)
            {
                if (item.Status == OrderItemStatus.HoldAutomatic)
                {
                    var time = item.HoldTime;
                    if (DateTime.Now > time)
                    {
                        item.Status = OrderItemStatus.Kitchen;
                        SendKitchenItem(order.ID, item.ID);
                        kichenItems.Add(item);
                        if (item.Product.InventoryCountDownActive)
                        {
                            item.Product.InventoryCount -= item.Qty;

                        }
                        SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, Arearequest.StationId);
                        foreach (var q in item.Questions)
                        {
                            if (!q.IsActive) continue;
                            var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                            SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, Arearequest.StationId);
                        }
                    }
                    else
                    {
                        var diff = (time - DateTime.Now).TotalMinutes;
                        var val = Math.Ceiling(diff) + 1;
                        var exist = holditems.FirstOrDefault(s => s.AreaId == order.Table.ID);
                        if (exist != null)
                        {
                            if (exist.HoldMinutes < (int)val)
                            {
                                exist.HoldMinutes = (int)val;
                            }
                        }
                        else
                        {
                            exist = new OrderHoldModel()
                            {
                                OrderId = order.ID,
                                AreaId = order.Table.ID,
                                HoldMinutes = (int)val
                            };
                            holditems.Add(exist);
                        }
                    }
                }

            }
            var time1 = (int)(DateTime.Now - order.OrderTime).TotalMinutes;
            if (kichenItems.Count > 0)
            {
                _printService.PrintKitchenItems(Arearequest.StationId, order.ID, kichenItems, Arearequest.Db);
            }
            result.Add(new StationOrderModel()
            {
                ID = order.ID,
                AreaId = order.Table.ID,
                OrderTime = time1,
                SubTotal = order.TotalPrice,
                WaiterName = order.WaiterName,
                IsDivide = order.OrderMode == OrderMode.Divide,
                ItemCount = order.Items.Count

            }); ;
            var hold = order.Items.FirstOrDefault(s => s.Status == OrderItemStatus.HoldManually || s.Status == OrderItemStatus.HoldAutomatic);
            if (hold == null && order.Status == OrderStatus.Hold)
            {
                order.Status = OrderStatus.Pending;
                _dbContext.SaveChanges();
            }
        }

        //borramos la imagen de la area
        foreach (var objAreaObject in area.AreaObjects)
        {
            objAreaObject.Area.BackImage = "";

            var reservation = _dbContext.Reservations.Where(s => s.TableID == objAreaObject.ID && s.Status == ReservationStatus.Open).ToList();

            foreach (var r in reservation)
            {
                var st = r.ReservationTime.AddMinutes(-30);
                var en = r.ReservationTime.AddHours((double)r.Duration);

                if (DateTime.Now >= st && DateTime.Now <= en)
                {
                    objAreaObject.BackColor = "#AF69EF";
                }
            }
        }

        var test = new { objects = area.AreaObjects, orders = result, holditems };
        POSAreaObjectsInAreaResponse areas = new POSAreaObjectsInAreaResponse();

        AreaObjects res = new AreaObjects();
        res.objects = area.AreaObjects;
        res.orders = result;
        res.holditems = holditems;

        return res;
    }

    public void SendKitchenItem(long orderID, long itemID)
    {
        var order = _dbContext.Orders.Include(s => s.Station).FirstOrDefault(s => s.ID == orderID);
        if (order != null)
        {
            var kitchens = _dbContext.Kitchen.Where(s => s.IsActive).ToList();
            foreach (var kitchen in kitchens)
            {
                if (!kitchen.Stations.Contains(order.Station.ID)) continue;
                var existingKitchenOrder = _dbContext.KitchenOrder.FirstOrDefault(s => s.KitchenID == kitchen.ID && s.OrderID == order.ID);
                if (existingKitchenOrder == null)
                {
                    existingKitchenOrder = new KitchenOrder()
                    {
                        OrderID = order.ID,
                        KitchenID = kitchen.ID,
                        StartTime = DateTime.Now,
                        Status = KitchenOrderStatus.Open
                    };
                    _dbContext.KitchenOrder.Add(existingKitchenOrder);
                    _dbContext.SaveChanges();
                }
                else
                {
                    existingKitchenOrder.Status = KitchenOrderStatus.Open;
                    existingKitchenOrder.StartTime = DateTime.Now;
                    existingKitchenOrder.IsCompleted = false;
                }

                var kitchenItem = new KitchenOrderItem()
                {
                    OrderItemID = itemID,
                    KitchenOrderID = existingKitchenOrder.ID,
                    Status = KitchOrderItemStatus.Open
                };
                var existitem = _dbContext.KitchenOrderItem.FirstOrDefault(s => s.KitchenOrderID == existingKitchenOrder.ID && s.OrderItemID == itemID);
                if (existitem == null)
                    _dbContext.KitchenOrderItem.Add(kitchenItem);
                _dbContext.SaveChanges();
            }
        }
    }

    public void SubstractProduct(long itemId, long prodId, decimal qty, int servingSizeID, OrderType type, int stationId)
    {
        var product = _dbContext.Products.Include(s => s.RecipeItems.Where(s => s.ServingSizeID == servingSizeID)).FirstOrDefault(s => s.ID == prodId);
        if (product == null || qty == 0) return;

        var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
        var station = _dbContext.Stations.FirstOrDefault(s => s.ID == stationID);
        var groups = _dbContext.Groups.ToList();
        var warehouses = _dbContext.Warehouses.ToList();
        var stationWarehouse = _dbContext.StationWarehouses.Where(s => s.StationID == station.ID).ToList();

        try
        {
            foreach (var item in product.RecipeItems)
            {
                if (item.Type == ItemType.Article)
                {
                    var article = _dbContext.Articles.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Items).FirstOrDefault(s => s.ID == item.ItemID);
                    if (article.DepleteCondition == DepleteCondition.Delivery && type != OrderType.Delivery)
                    {
                        continue;
                    }
                    var sw = stationWarehouse.FirstOrDefault(s => s.GroupID == article.Category.Group.ID);
                    if (sw != null)
                    {
                        var warehouse = warehouses.FirstOrDefault(s => s.ID == sw.WarehouseID);
                        if (warehouse != null)
                        {
                            UpdateStockOfArticle(article, -item.Qty * qty, item.UnitNum, warehouse, StockChangeReason.Kitchen, itemId, stationID);
                        }
                    }
                }
                else if (item.Type == ItemType.Product)
                {
                    SubstractProduct(itemId, item.ItemID, item.Qty, item.UnitNum, type, stationID);
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
                            UpdateStockOfSubRecipe(subrecipe, -item.Qty * qty, item.UnitNum, warehouse, StockChangeReason.Kitchen, itemId, stationID);
                        }
                    }
                }
            }
        }
        catch { }
    }
    public void UpdateStockOfArticle(InventoryItem item, decimal qty, int unitNum, Warehouse warehouse, StockChangeReason reason, long reasonId, int stationId)
    {
        decimal baseQty = ConvertQtyToBase(qty, unitNum, item.Items.ToList());

        var existingWarehouseStock = _dbContext.WarehouseStocks.FirstOrDefault(s => s.Warehouse == warehouse && s.ItemType == ItemType.Article && s.ItemId == item.ID);
        var warehouseHistoryItem = new WarehouseStockChangeHistory();

        //reutilizando la funci�n de SettingsCore para obtener el d�a
        SettingsCore settings = new SettingsCore(_userService, _dbContext, _context);
        warehouseHistoryItem.ForceDate = settings.GetDia(stationId);

        warehouseHistoryItem.Warehouse = warehouse;
        warehouseHistoryItem.Price = 0;
        warehouseHistoryItem.ItemId = item.ID;
        warehouseHistoryItem.ItemType = ItemType.Article;
        warehouseHistoryItem.Qty = baseQty;
        warehouseHistoryItem.BeforeBalance = existingWarehouseStock == null ? 0 : existingWarehouseStock.Qty;
        warehouseHistoryItem.AfterBalance = existingWarehouseStock == null ? baseQty : existingWarehouseStock.Qty + baseQty;
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
    public void UpdateStockOfSubRecipe(SubRecipe item, decimal qty, int unitNum, Warehouse warehouse, StockChangeReason reason, long reasonId, int stationId)
    {
        decimal baseQty = ConvertQtyToBase(qty, unitNum, item.ItemUnits.ToList());

        var existingWarehouseStock = _dbContext.WarehouseStocks.FirstOrDefault(s => s.Warehouse == warehouse && s.ItemType == ItemType.SubRecipe && s.ItemId == item.ID);

        var warehouseHistoryItem = new WarehouseStockChangeHistory();

        //reutilizando la funci�n de SettingsCore para obtener el d�a
        SettingsCore settings = new SettingsCore(_userService, _dbContext, _context);
        warehouseHistoryItem.ForceDate = settings.GetDia(stationId);

        warehouseHistoryItem.Warehouse = warehouse;
        warehouseHistoryItem.Price = 0;
        warehouseHistoryItem.ItemId = item.ID;
        warehouseHistoryItem.ItemType = ItemType.SubRecipe;
        warehouseHistoryItem.Qty = baseQty;
        warehouseHistoryItem.BeforeBalance = existingWarehouseStock == null ? 0 : existingWarehouseStock.Qty;
        warehouseHistoryItem.AfterBalance = existingWarehouseStock == null ? baseQty : existingWarehouseStock.Qty + baseQty;
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
    public decimal ConvertQtyToBase(decimal originQty, int UnitNum, List<ItemUnit> Units)
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

    public List<MenuCategory> GetMenuCategoryList(long groupId)
    {
        var group = _dbContext.MenuGroups.Include(s => s.Categories).FirstOrDefault(s => s.ID == groupId);
        if (group != null)
        {
            var categories = group.Categories?.OrderBy(s => s.Order).ToList();

            if (categories != null)
            {
                return categories;
            }

        }
        return new List<MenuCategory>();
    }

    public List<MenuSubCategory> GetMenuSubCategoryList(long categoryId)
    {
        var group = _dbContext.MenuCategories.Include(s => s.SubCategories).FirstOrDefault(s => s.ID == categoryId);

        var subcategories = group?.SubCategories?.OrderBy(s => s.Order).ToList();

        if (subcategories != null)
        {
            return subcategories;
        }
        return new List<MenuSubCategory>();
    }

    public List<MenuProduct>? GetMenuProductList(long subCategoryId, string db)
    {
        var group = _dbContext.MenuSubCategoris?.Include(s => s.Products).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == subCategoryId);

        if (group != null)
        {
            var products = group.Products?.OrderBy(s => s.Order).ToList();

            //Obtenemos las urls de las imagenes
            var request = _context.HttpContext?.Request;
            if (request != null)
            {
                var _baseURL = $"https://{request.Host}";
                if (products != null && products.Any())
                {
                    foreach (var objProduct in products)
                    {

                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", db, "product", objProduct.Product.ID.ToString() + ".png");
                        if (System.IO.File.Exists(pathFile))
                        {
                            var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                            objProduct.Product.Photo = Path.Combine(_baseURL, "localfiles", db, "product", objProduct.Product.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                        }
                        else
                        {
                            objProduct.Product.Photo = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", "empty.png");
                        }

                    }
                }
            }
            return products;
        }
        return new List<MenuProduct>();
    }

    public Order? GetOrder(long OrderId)
    {
        var order = _dbContext.Orders?.Include(s => s.Taxes).Include(s => s.PrepareType).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).ThenInclude(s => s.Answer).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).Include(s => s.Divides).Include(s => s.Area).Include(s => s.Table).FirstOrDefault(o => o.ID == OrderId);

        if (order != null)
        {
            return order;
        }

        return null;
    }

    //    if (station == null)
    //        return RedirectToAction("Login");

    //    if (station.SalesMode == SalesMode.Kiosk)
    //    {
    //        return Redirect("/POS/Kiosk?orderId=" + orderId);
    //    }
    //    ViewBag.AnotherTables = new List<AreaObject>();
    //    ViewBag.AnotherAreas = new List<Area>();
    //    ViewBag.DiscountType = "";
    //    var current = GetOrder(orderId);
    //    if (current != null)
    //    {
    //        order = current;
    //        // checkout
    //        if (order.PaymentStatus == PaymentStatus.Partly && order.OrderMode != OrderMode.Divide && order.OrderMode != OrderMode.Seat)
    //        {
    //            return Redirect("/POS/Checkout?orderId=" + orderId);
    //        }
    //        if (order.OrderMode == OrderMode.Conduce)
    //        {
    //            return RedirectToAction("Station");
    //        }
    //        var name = HttpContext.User.Identity.GetUserName();
    //        if (order.WaiterName != name)
    //        {
    //            var claims = User.Claims.Where(x => x.Type == "Permission" && x.Value == "Permission.POS.OtherOrder" &&
    //                                                    x.Issuer == "LOCAL AUTHORITY");
    //            if (!claims.Any())
    //            {
    //                return RedirectToAction("Station", new { error = "Permission" });
    //            }
    //        }

    //        if (orderType == OrderType.DiningRoom && areaObject > 0)
    //        {
    //            var table = _dbContext.AreaObjects.Include(s => s.Area).ThenInclude(s => s.AreaObjects.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == areaObject);
    //            if (table != null)
    //            {
    //                ViewBag.AnotherTables = table.Area.AreaObjects.Where(s => s.ObjectType == AreaObjectType.Table && s.IsActive && s.ID != areaObject).ToList();
    //                ViewBag.AnotherAreas = station.Areas.Where(s => s.IsActive && s.ID != table.Area.ID).ToList();
    //            }
    //        }

    //        if (order.Discounts != null && order.Discounts.Count > 0)
    //        {
    //            ViewBag.DiscountType = "order";
    //        }
    //        foreach (var item in order.Items)
    //        {
    //            if (item.Discounts != null && item.Discounts.Count > 0)
    //            {
    //                ViewBag.DiscountType = "item";
    //            }
    //        }

    //    }
    //    else
    //    {
    //        var user = HttpContext.User.Identity.GetUserName();
    //        order.Station = station;
    //        order.WaiterName = user;
    //        order.OrderMode = OrderMode.Standard;
    //        order.OrderType = orderType;
    //        order.Status = OrderStatus.Temp;
    //        var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.IsPrimary);
    //        order.ComprobantesID = voucher.ID;
    //        if (orderType == OrderType.DiningRoom && areaObject > 0)
    //        {
    //            var table = _dbContext.AreaObjects.Include(s => s.Area).ThenInclude(s => s.AreaObjects.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == areaObject);
    //            if (table != null)
    //            {
    //                order.Table = table;
    //                order.Area = table.Area;

    //                ViewBag.AnotherTables = table.Area.AreaObjects.Where(s => s.ObjectType == AreaObjectType.Table && s.IsActive && s.ID != areaObject).ToList();
    //                ViewBag.AnotherAreas = station.Areas.Where(s => s.IsActive && s.ID != table.Area.ID).ToList();
    //            }

    //            order.Person = person;
    //        }


    //        if (orderType == OrderType.Delivery || orderType == OrderType.FastExpress || orderType == OrderType.TakeAway)
    //        {
    //            order.Person = person;
    //        }


    //        if (orderType != OrderType.DiningRoom)
    //        {
    //            order.OrderMode = OrderMode.Standard;
    //        }

    //        _dbContext.Orders.Add(order);
    //        _dbContext.SaveChanges();

    //        order.ForceDate = getCurrentWorkDate();
    //        _dbContext.SaveChanges();

    //        HttpContext.Session.SetInt32("CurrentOrderID", (int)order.ID);
    //    }

    //    if (orderType == OrderType.Delivery)
    //    {
    //        bool deliveryExists = _dbContext.Deliverys.Include(s => s.Order).Where(s => s.IsActive).Where(s => s.Order.ID == order.ID).Any();

    //        if (!deliveryExists)
    //        {
    //            var delivery = new Delivery();
    //            delivery.Order = order;
    //            delivery.Status = StatusEnum.Nuevo;
    //            delivery.StatusUpdated = DateTime.Now;
    //            delivery.DeliveryTime = DateTime.Now;

    //            _dbContext.Deliverys.Add(delivery);

    //            if (station.PrepareTypeDefault.HasValue && station.PrepareTypeDefault > 0)
    //            {
    //                order.PrepareTypeID = station.PrepareTypeDefault.Value; //Para llevar
    //            }
    //            else
    //            {
    //                order.PrepareTypeID = 2; //Para llevar
    //            }


    //            var prepareType = _dbContext.PrepareTypes.FirstOrDefault(s => s.ID == order.PrepareTypeID);
    //            order.PrepareType = prepareType; //Para llevar

    //            _dbContext.SaveChanges();
    //        }
    //    }

    //    var reasons = _dbContext.CancelReasons.ToList();
    //    ViewBag.CancelReasons = reasons;

    //    ViewBag.Discounts = _dbContext.Discounts.Where(s => s.IsActive && !s.IsDeleted).ToList();

    //    return View(order);
    //}

    public OrderItemsInCheckout GetOrderItemsInCheckout(OrderItemsInCheckoutRequest request)
    {
        var orderItemsInCheckout = new OrderItemsInCheckout();
        orderItemsInCheckout.status = 1;

        var store = _dbContext.Preferences.FirstOrDefault();

        var order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == request.OrderId);
        var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order).ToList();
        var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);

        if (order.OrderMode == OrderMode.Seat)
        {
            order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Seats.Where(s => !s.IsPaid)).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).FirstOrDefault(o => o.ID == request.OrderId);

            var seats = order.Seats.ToList();
            if (request.SeatNum > 0)
                seats = order.Seats.Where(s => s.SeatNum == request.SeatNum).ToList();
            order.GetTotalPrice(voucher, 0, request.SeatNum);

            transactions = transactions.Where(s => s.SeatNum == request.SeatNum).ToList();
            if (request.SeatNum > 0 && transactions.Count() > 0)
            {
                var paid = transactions.Sum(s => s.Amount);
                order.Balance = order.Balance - paid;
            }

            orderItemsInCheckout.status = 0;
            orderItemsInCheckout.seats = seats;
            orderItemsInCheckout.order = order;
            orderItemsInCheckout.transactions = transactions;
            orderItemsInCheckout.store = store;
        }
        else if (order.OrderMode == OrderMode.Divide && request.DividerId > 0)
        {
            order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == request.OrderId);

            var items = order.Items.Where(s => !s.IsDeleted && s.DividerNum == request.DividerId).ToList();
            order.GetTotalPrice(voucher, request.DividerId);

            transactions = transactions.Where(s => s.DividerNum == request.DividerId).ToList();
            if (transactions.Count() > 0)
            {
                var paid = transactions.Sum(s => s.Amount);
                order.Balance = order.Balance - paid;
            }

            orderItemsInCheckout.status = 0;
            orderItemsInCheckout.orderItems = items;
            orderItemsInCheckout.order = order;
            orderItemsInCheckout.transactions = transactions;
            orderItemsInCheckout.store = store;
        }
        else
        {
            orderItemsInCheckout.status = 0;
            orderItemsInCheckout.orderItems = order.Items;
            orderItemsInCheckout.order = order;
            orderItemsInCheckout.transactions = transactions;
            orderItemsInCheckout.store = store;
        }
        return orderItemsInCheckout;
    }

    public POSCorePayModel Pay(ApplyPayModel model, int stationId, string db)
    {
        var paymodel = new POSCorePayModel();
        var order = GetOrder(model.OrderId);
        var objSettingCore = new SettingsCore(_userService, _dbContext, _context);

        if (model.Amount <= 0)
        {
            paymodel.Status = 3;
            return paymodel;
        }

        if (model.DividerId == 0)
        {
            if ((Math.Round(order.TotalPrice, 2, MidpointRounding.AwayFromZero) - Math.Round(order.PayAmount, 2, MidpointRounding.AwayFromZero) == 0))
            {
                paymodel.Status = 4;
                return paymodel;
            }
        }
        else
        {
            var transactionsTemp = _dbContext.OrderTransactions.Where(s => s.Order == order);
            var voucher1Temp = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
            order.GetTotalPrice(voucher1Temp, model.DividerId, 0);
            var dividerTransactions = transactionsTemp.Where(s => s.DividerNum == model.DividerId).ToList();
            var balance = Math.Round(order.Balance, 2, MidpointRounding.AwayFromZero) - Math.Round(dividerTransactions.Sum(s => s.Amount), 2, MidpointRounding.AwayFromZero);

            if (balance == 0)
            {
                paymodel.Status = 4;
                return paymodel;
            }
        }


        var method = _dbContext.PaymentMethods.FirstOrDefault(s => s.ID == model.Method);
        if (method == null)
        {
            paymodel.Status = 1;
            return paymodel;
        }
        if (method.PaymentType == "C X C" || method.PaymentType == "Cuenta de la Casa" || method.PaymentType == "Conduce")
        {
            if (order.OrderMode == OrderMode.Divide && model.DividerId > 0)
            {
                var divide = order.Divides.FirstOrDefault(s => s.DividerNum == model.DividerId);
                if (divide == null || string.IsNullOrEmpty(divide.ClientName))
                {
                    paymodel.Status = 2;
                    return paymodel;
                }
            }
            else if (string.IsNullOrEmpty(order.ClientName))
            {
                paymodel.Status = 2;
                return paymodel;
            }
        }

        if (order.ComprobantesID != null && order.ComprobantesID > 0)
        {
            var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == order.ComprobantesID);

            if (voucher.IsRequireRNC)
            {

                if (order.CustomerId == null || order.CustomerId <= 0)
                {
                    paymodel.Status = 3;
                    return paymodel;
                }

                var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);

                if (string.IsNullOrEmpty(customer.RNC))
                {
                    paymodel.Status = 3;
                    return paymodel;
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
                    paymodel.Status = 10;
                    return paymodel;
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
                        item.ForceDate = objSettingCore.GetDia(stationId);
                        if (item.Status == OrderItemStatus.Pending || item.Status == OrderItemStatus.Printed)
                        {
                            item.Status = OrderItemStatus.Saved;
                            if (item.Product.InventoryCountDownActive)
                            {
                                item.Product.InventoryCount -= item.Qty;

                            }
                            SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, stationId);
                            foreach (var q in item.Questions)
                            {
                                if (!q.IsActive) continue;
                                var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                                SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, stationId);
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
                        item.ForceDate = objSettingCore.GetDia(stationId);
                        if (item.Status == OrderItemStatus.Pending || item.Status == OrderItemStatus.Printed)
                        {
                            item.Status = OrderItemStatus.Kitchen;
                            SendKitchenItem(order.ID, item.ID);
                            kichenItems.Add(item);
                        }
                        if (item.Product.InventoryCountDownActive)
                        {
                            item.Product.InventoryCount -= item.Qty;

                        }
                        SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, stationId);
                        foreach (var q in item.Questions)
                        {
                            if (!q.IsActive) continue;
                            var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                            SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, stationId);
                        }
                    }
                }
            }

            model.Amount = model.Amount * method.Tasa;

            if (kichenItems.Count > 0)
                _printService.PrintKitchenItems(stationId, order.ID, kichenItems, db);


            var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order);
            var voucher1 = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);

            if (method.PaymentType == "Conduce")
            {
                order.IsConduce = true;
                order.Status = OrderStatus.Paid;

                _dbContext.SaveChanges();
                _printService.PrintPaymentSummary(stationId, order.ID, db, 0, 0, false);
                paymodel.Status = 5;

                return paymodel;
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
                    transaction.ForceDate = objSettingCore.GetDia(stationId);
                    transaction.PaymentDate = (DateTime)objSettingCore.GetDia(stationId);
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
                    transaction.ForceDate = objSettingCore.GetDia(stationId);
                    transaction.PaymentDate = (DateTime)objSettingCore.GetDia(stationId);
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
                        var items = order.Items.Where(s => !s.IsDeleted && s.DividerNum == model.DividerId && s.Status != OrderItemStatus.Paid).ToList();
                        order.PaymentStatus = PaymentStatus.DividerPaid;
                        order.PayAmount += model.Amount;
                        order.Balance = 0;
                        foreach (var item in items)
                        {
                            item.ForceDate = objSettingCore.GetDia(stationId);
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
                    transaction.ForceDate = objSettingCore.GetDia(stationId);
                    transaction.PaymentDate = (DateTime)objSettingCore.GetDia(stationId);
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
                            item.ForceDate = objSettingCore.GetDia(stationId);
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
                        foreach (var o in selectedConduceOrders)
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

        paymodel.Status = 0;
        paymodel.Difference = Difference;
        paymodel.Balance = order.Balance;
        paymodel.Parcial = (order.PaymentStatus == PaymentStatus.Partly ? true : false);
        return paymodel;
    }

    public bool PayDone(ApplyPayModel model, int stationId, string db)
    {
        var objSettingCore = new SettingsCore(_userService, _dbContext, _context);
        var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == stationId);

        var order = _dbContext.Orders.Include(s => s.Taxes).Include(s => s.PrepareType).Include(s => s.Divides).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).FirstOrDefault(o => o.ID == model.OrderId);

        var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == order.ComprobantesID);
        var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order);

        if (order != null)
        {
            if (model.SeatNum > 0 && (order.PaymentStatus == PaymentStatus.SeatPaid || order.Status == OrderStatus.Paid))
            {
                // Debug.WriteLine("caso1");
                var seatTransactions = transactions.Include(s => s.Order).Where(s => s.SeatNum == model.SeatNum).ToList();
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

                var items = order.Items.Where(s => s.SeatNum == model.SeatNum && !s.IsDeleted);
                foreach (var item in items)
                {
                    item.ForceDate = objSettingCore.GetDia(stationId);
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
                    if (iFactura == null || iFactura == 0)
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

                _printService.PrintPaymentSummary(stationId, nOrder.ID, db, model.SeatNum, 0, false);
                return false;
            }
            else if (model.DividerId > 0 && (order.PaymentStatus == PaymentStatus.DividerPaid || order.Status == OrderStatus.Paid))
            {
                //  Debug.WriteLine("caso2");
                var divideTransactions = transactions.Include(s => s.Order).Where(s => s.DividerNum == model.DividerId).ToList();
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
                    item.ForceDate = objSettingCore.GetDia(stationId);
                    nOrder.Items.Add(item);
                    Debug.WriteLine(item);

                }

                var discounts = order.Discounts.Where(s => s.DividerId == model.DividerId);
                if (discounts.Count() > 0)
                {
                    nOrder.Discounts = new List<DiscountItem>();
                    foreach (var discount in discounts)
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
                    if (iFactura == null || iFactura == 0)
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
                    var xorder = _dbContext.Orders.Include(s => s.Items.Where(s => !s.IsDeleted)).Include(s => s.Divides).FirstOrDefault(s => s.ID == model.OrderId);
                    if (xorder.Items.Count == 0)
                    {
                        xorder.Status = OrderStatus.Void;
                    }
                    else
                    {
                        var index = 1;
                        foreach (var d in xorder.Divides)
                        {
                            var xitems = xorder.Items.Where(s => s.DividerNum == d.DividerNum).ToList();
                            foreach (var ii in xitems)
                            {
                                ii.DividerNum = index;
                            }
                            d.DividerNum = index;
                            index++;
                        }
                    }
                    _dbContext.SaveChanges();

                    _printService.PrintPaymentSummary(stationId, nOrder.ID, db, 0, model.DividerId, false);
                    return false;
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
                        if (iFactura == null || iFactura == 0)
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

                if (order.OrderType == OrderType.Delivery && order.OrderMode != OrderMode.Conduce)
                {


                    var objDelivery = _dbContext.Deliverys.Include(s => s.Order).ThenInclude(s => s.PrepareType).Where(s => s.OrderID == order.ID).First();

                    if (objDelivery.Order.PrepareType != null && objDelivery.Order.PrepareType.SinChofer)
                    {
                        if (objDelivery.Order.Balance < 1)
                        {
                            objDelivery.Status = StatusEnum.Cerrado;
                            objDelivery.UpdatedDate = DateTime.Now;
                        }
                    }
                }

                _dbContext.SaveChanges();

                _printService.PrintPaymentSummary(stationId, model.OrderId, db, 0, 0, false);

                return false;
            }
            else if (order.Status == OrderStatus.Pending && order.PaymentStatus == PaymentStatus.Paid && order.Balance == 0)
            {
                order.Status = OrderStatus.Paid;
                _dbContext.SaveChanges();
                return false;

            }


        }

        return true;
    }

    public POSProductToOrderItem AddProductToOrderItem(AddItemModel model, int stationId, string db)
    {
        var objSettingCore = new SettingsCore(_userService, _dbContext, _context);
        var productoToOrderItem = new POSProductToOrderItem();
        var station = _dbContext.Stations.Include(s => s.MenuSelect).ThenInclude(s => s.Groups).FirstOrDefault(s => s.ID == stationId);

        var product = _dbContext.Products.Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Questions).ThenInclude(s => s.SmartButtons.OrderBy(s => s.Order)).ThenInclude(s => s.Button).Include(s => s.Questions).ThenInclude(s => s.Answers.OrderBy(s => s.Order)).ThenInclude(s => s.Product).ThenInclude(s => s.ServingSizes).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == model.ProductId);

        if (product.InventoryCountDownActive)
        {
            if (product.InventoryCount < model.Qty)
            {
                productoToOrderItem.Status = 3;
                productoToOrderItem.Product = product;

                return productoToOrderItem;
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
                if (station.PrecioDelivery != null)
                {
                    priceSelect = station.PrecioDelivery.Value;
                }

            }
            catch { }
        {
        }

        var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
        if (order == null || product == null)
        {
            productoToOrderItem.Status = 1;

            return productoToOrderItem;
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

            productoToOrderItem.Status = 0;
            productoToOrderItem.ItemId = existitem.ID;
            productoToOrderItem.HasQuestion = false;

            return productoToOrderItem;
        }
        else
        {
            var nItem = new OrderItem();
            nItem.ForceDate = objSettingCore.GetDia(stationId);
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
                servingSizes = product.ServingSizes.OrderBy(s => s.Order).ToList();
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
                foreach (var t in product.Taxes)
                {
                    var tax = new TaxItem();
                    tax.TaxId = t.ID;
                    tax.Description = t.TaxName;
                    tax.Percent = (decimal)t.TaxValue;
                    tax.ToGoExclude = t.IsToGoExclude;
                    tax.BarcodeExclude = t.IsBarcodeExclude;
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
                questions = product.Questions.Where(s => s.IsForced).OrderBy(s => s.DisplayOrder).ToList();

                var request = _context.HttpContext.Request;
                var _baseURL = $"https://{request.Host}";

                foreach (var objQuestion in questions)
                {
                    foreach (var objAnswer in objQuestion.Answers)
                    {

                        //Obtenemos las urls de las imagenes
                        string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", db, "product", objAnswer.Product.ID.ToString() + ".png");
                        if (System.IO.File.Exists(pathFile))
                        {
                            var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                            objAnswer.Product.Photo = Path.Combine(_baseURL, "localfiles", db, "product", objAnswer.Product.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                        }
                        else
                        {
                            objAnswer.Product.Photo = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "product", "empty.png");
                        }


                    }
                }

                //Para las preguntas opcionales, pero preseleccionado
                var questionsAux = product.Questions.Where(s => !s.IsForced).OrderBy(s => s.DisplayOrder).ToList();

                foreach (var objQuestion in questionsAux)
                {

                    int index = 0;
                    int freechoice = 0;

                    foreach (var objAnswer in objQuestion.Answers)
                    {
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
                                    if (objAnswer.MatchSize && nItem.ServingSizeID > 0)
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

            productoToOrderItem.Status = 0;
            productoToOrderItem.ItemId = nItem.ID;
            productoToOrderItem.HasQuestion = questions != null && questions.Count > 0;
            productoToOrderItem.Questions = questions;
            productoToOrderItem.HasServingSize = product.HasServingSize && servingSizes.Count > 0;
            productoToOrderItem.ProductName = product.Name;

            return productoToOrderItem;
        }
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

        var items = item.Order.Items.Where(s => s.SeatNum == item.SeatNum && s.ServingSizeID == item.ServingSizeID && s.Product == item.Product && !s.IsDeleted).OrderBy(s => s.CreatedDate).ToList();
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
                        foreach (var citem in items)
                        {
                            int pqty = 0;
                            for (int i = 0; i < citem.Qty; i++)
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
                            foreach (var w in promotion.WeekDays)
                            {
                                try
                                {
                                    var val = int.Parse("" + w);
                                    if (val - 1 == day)
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

    private decimal GetCostOrderItem(long productID, decimal cantidad, int size = 0)
    {
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

                    if (ret.ServingSizeID == size)
                    {
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

                            if (product1.HasServingSize)
                            {
                                decimal itemCost = (from m in product1.ServingSizes where m.ServingSizeID == item.UnitNum select m.Cost).FirstOrDefault();
                                costoTotal = costoTotal + (itemCost * item.Qty);
                            }
                            else
                            {
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

                    if (productRecipe.HasServingSize)
                    {
                        decimal itemCost = (from m in productRecipe.ServingSizes where m.ServingSizeID == item.UnitNum select m.Cost).FirstOrDefault();
                        costoTotal = costoTotal + (itemCost * item.Qty);
                    }
                    else
                    {
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

    public POSAnswerDetail GetAnswerDetail(int answerID, int servingSizeID)
    {
        var answerDetail = new POSAnswerDetail();
        var answer = _dbContext.Answers.Include(s => s.Product).ThenInclude(s => s.ServingSizes).FirstOrDefault(s => s.ID == answerID);
        var question = _dbContext.Questions.Include(s => s.Answers).ThenInclude(s => s.Product).ThenInclude(s => s.ServingSizes).FirstOrDefault(s => s.ID == answer.ForcedQuestionID);

        if (answer.MatchSize && answer.Product.HasServingSize && servingSizeID > 0)
        {
            var servingSize = answer.Product.ServingSizes.FirstOrDefault(s => s.ServingSizeID == servingSizeID);
            if (servingSize == null)
            {
                answerDetail.Status = 2;

                return answerDetail;
            }
        }

        if (question != null)
        {
            answerDetail.Status = 0;
            answerDetail.Answer = answer;
            answerDetail.SubQuestion = question;

            return answerDetail;
        }

        answerDetail.Status = 1;
        answerDetail.Answer = answer;
        answerDetail.SubQuestion = question;

        return answerDetail;
    }

    public bool AddQuestionToItem(long ItemId, long servingSizeID, List<AddQuestionModel> questions, int stationId)
    {
        var objSettingCore = new SettingsCore(_userService, _dbContext, _context);
        var station = _dbContext.Stations.Include(s => s.MenuSelect).ThenInclude(s => s.Groups).FirstOrDefault(s => s.ID == stationId);
        var orderItem = _dbContext.OrderItems.Include(s => s.Product).ThenInclude(s => s.ServingSizes).Include(s => s.Product).ThenInclude(s => s.Questions).Include(s => s.Order).ThenInclude(s => s.Items).Include(s => s.Questions).FirstOrDefault(s => s.ID == ItemId);
        var order = GetOrder(orderItem.Order.ID);
        var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);

        if (servingSizeID > 0 && orderItem.Product.HasServingSize)
        {
            bool IsAddExisting = false;
            if (orderItem.Product.Questions == null || orderItem.Product.Questions.Count == 0)
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
                    return true;
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

                    orderItem.ForceDate = objSettingCore.GetDia(stationId);

                    _dbContext.SaveChanges();
                }
            }

        }

        orderItem.AnswerVenta = answerTotalVenta;
        orderItem.AnswerCosto = answerTotalCosto;
        orderItem.Costo = orderItem.Costo + answerTotalCosto;

        order.GetTotalPrice(voucher);
        _dbContext.SaveChanges();
        return true;
    }

    public bool SendOrder(long orderId, int stationId, string db, DateTime? saveDate = null)
    {
        var objSettingCore = new SettingsCore(_userService, _dbContext, _context);

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
                    SendKitchenItem(order.ID, item.ID);
                    item.ForceDate = objSettingCore.GetDia(stationId);

                    kichenItems.Add(item);
                    if (item.Product.InventoryCountDownActive)
                    {
                        item.Product.InventoryCount -= item.Qty;

                    }
                    SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, stationId);
                    foreach (var q in item.Questions)
                    {
                        if (!q.IsActive) continue;
                        var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                        SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, stationId);
                    }
                }

                if (item.Status == OrderItemStatus.HoldManually || item.Status == OrderItemStatus.HoldAutomatic)
                {
                    order.Status = OrderStatus.Hold;
                }
            }
            //var stationID = int.Parse(GetCookieValue("StationID"));
            _printService.PrintKitchenItems(stationId, order.ID, kichenItems, db);
        }

        _dbContext.SaveChanges();

        if (order.OrderType == OrderType.Delivery)
        {
            //var stationID = int.Parse(GetCookieValue("StationID"));

            if (stationId > 0)
            {
                var objStation = _dbContext.Stations.Where(s => s.ID == stationId).First();

                if (objStation.ImprimirPrecuentaDelivery)
                {
                    PrintOrderFunc(order.ID, stationId, db);
                }
            }
        }

        //HttpContext.Session.Remove("CurrentOrderID");

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

        return true;
    }

    public void PrintOrderFunc(long OrderID, int stationId, string db, int DivideNum = 0)
    {
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

        _printService.PrintOrder(stationId, OrderID, DivideNum, 0, db);

        foreach (var item in items)
        {
            if (item.Status == OrderItemStatus.Pending)
                item.Status = OrderItemStatus.Printed;
        }
        _dbContext.SaveChanges();
    }

    public bool PrintOrder(long OrderId, int stationId, string db, int DivideNum = 0)
    {
        try
        {
            PrintOrderFunc(OrderId, stationId, db, DivideNum);

        }
        catch
        {
            return true;
        }

        return false;
    }

    public List<Order> GetMyOpenOrders(int stationID, string user)
    {
        var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);
        List<Order> orders = _dbContext.Orders.Include(s => s.Area).Include(s => s.Table).Where(s => /* s.Station == station  &&*/ (s.OrderType == OrderType.DiningRoom || s.OrderType == OrderType.Delivery) && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved && s.WaiterName == user && !s.IsDeleted).ToList();

        return orders;
    }

    public DateTime getCurrentWorkDate(int stationId)
    {
        var stationID = stationId;
        var objStation = _dbContext.Stations.Where(d => d.ID == stationID).FirstOrDefault();

        var objDay = _dbContext.WorkDay.Where(d => d.IsActive == true && d.IDSucursal == objStation.IDSucursal).FirstOrDefault();

        DateTime dtNow = new DateTime(objDay.Day.Year, objDay.Day.Month, objDay.Day.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

        return dtNow;
    }

    public int MoveTable(MoveTableModel model)
    {
        int status = 0;

        if (model.FromTableId == model.ToTableId)
        {
            status = 1;
            return status;
        }

        var srcTable = _dbContext.AreaObjects.Include(s => s.Area).FirstOrDefault(s => s.ID == model.FromTableId);
        var targetTable = _dbContext.AreaObjects.Include(s => s.Area).FirstOrDefault(s => s.ID == model.ToTableId);

        var order = _dbContext.Orders.Include(s => s.Table).Include(s => s.Items).ThenInclude(s => s.Product).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Items).ThenInclude(s => s.Questions).ThenInclude(s => s.Answer).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items).FirstOrDefault(s => s.Table == srcTable && s.OrderType == OrderType.DiningRoom && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved);
        var targetOrder = _dbContext.Orders.Include(s => s.Table).Include(s => s.Items).ThenInclude(s => s.Product).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Items).ThenInclude(s => s.Questions).ThenInclude(s => s.Answer).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Seats).ThenInclude(s => s.Items).FirstOrDefault(s => s.Table == targetTable && s.OrderType == OrderType.DiningRoom && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved);

        if (targetOrder == null)
        {
            order.Table = targetTable;
            order.Area = targetTable.Area;
            _dbContext.SaveChanges();
        }
        else
        {
            if (targetOrder.OrderMode == OrderMode.Seat)
            {
                if (order.OrderMode == OrderMode.Seat)
                {
                    var emptySeat = 0;
                    var seatNums = new List<int>();
                    foreach (var item in targetOrder.Seats)
                    {
                        if (!seatNums.Contains(item.SeatNum))
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

                    foreach (var seat in order.Seats)
                    {
                        if (seat.Items == null || seat.Items.Count == 0) continue;
                        foreach (var item in seat.Items)
                        {
                            item.SeatNum = emptySeat;
                        }
                        seat.SeatNum = emptySeat;

                        targetOrder.Seats.Add(seat);
                        emptySeat++;
                    }
                    var index = 1;
                    foreach (var seat in targetOrder.Seats)
                    {
                        seat.SeatNum = index;
                        foreach (var item in seat.Items)
                        {
                            item.SeatNum = index;
                        }

                        index++;
                    }

                    foreach (var item in order.Items)
                    {
                        targetOrder.Items.Add(item);
                    }
                }
                else
                {
                    var emptySeat = 0;
                    var seatNums = new List<int>();
                    foreach (var item in targetOrder.Items)
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
                    var nseat = new SeatItem() { SeatNum = emptySeat, Items = new List<OrderItem>() };
                    foreach (var item in order.Items)
                    {
                        item.SeatNum = emptySeat;
                        targetOrder.Items.Add(item);
                        nseat.Items.Add(item);
                    }

                    targetOrder.Seats.Add(nseat);
                }
            }
            else if (targetOrder.OrderMode == OrderMode.Divide)
            {
                foreach (var item in order.Items)
                {
                    if (item.Status == OrderItemStatus.Paid) continue;
                    item.DividerNum = 1;
                    targetOrder.Items.Add(item.CopyThis());
                }
            }
            else
            {
                foreach (var item in order.Items)
                {
                    if (item.Status == OrderItemStatus.Paid) continue;
                    targetOrder.Items.Add(item.CopyThis());
                }
            }

            order.Status = OrderStatus.Moved;

            _dbContext.SaveChanges();
            var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == targetOrder.ComprobantesID);
            targetOrder.GetTotalPrice(voucher);

            _dbContext.SaveChanges();
        }

        return status;
    }

    public int GiveOrder(long orderId, long userId, int stationId, string userN)
    {
        int status = 0;
        var user = userN;
        var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
        var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);

        var otherUser = _dbContext.User.FirstOrDefault(s => s.ID == userId);
        if (orderId == 0)
        {
            var orders = _dbContext.Orders.Include(s => s.Area).Include(s => s.Table).Where(s => /*s.Station == station &&*/ (s.OrderType == OrderType.DiningRoom || s.OrderType == OrderType.Delivery) && s.Status != OrderStatus.Paid && s.Status != OrderStatus.Temp && s.Status != OrderStatus.Void && s.Status != OrderStatus.Moved && s.WaiterName == user).ToList();
            foreach (var o in orders)
            {
                o.WaiterName = otherUser.Username;
            }
        }
        else
        {
            var order = _dbContext.Orders.FirstOrDefault(s => s.ID == orderId);
            if (order != null)
                order.WaiterName = otherUser.Username;
        }
        _dbContext.SaveChanges();

        return status;
    }

    public int ReprintOrder(long orderID, int stationID, string db)
    {
        _printService.PrintPaymentSummary(stationID, orderID, db, 0, 0, true);

        return 0;
    }

    public Tuple<int, string> AddEditReservation(ReservationCreateModel reservation, int stationId)
    {
        var reservationTime = new DateTime(2000, 1, 1);
        try
        {
            reservationTime = DateTime.ParseExact(reservation.ReservationTimeStr, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);
        }
        catch { }
        if (reservationTime < DateTime.Now)
        {
            return new Tuple<int, string>(1, "Invalid Reservation Time!");
        }

        var table = _dbContext.AreaObjects.FirstOrDefault(s => s.ID == reservation.TableID && s.ObjectType == AreaObjectType.Table);

        var rst = reservationTime;
        var ren = reservationTime.AddHours((double)reservation.Duration);

        var reservations = _dbContext.Reservations.Where(s => s.ID != reservation.ID && s.Status != ReservationStatus.Canceled && s.TableID == reservation.TableID && s.ReservationTime.Date == reservationTime.Date).ToList();
        foreach (var r in reservations)
        {
            var st = r.ReservationTime;
            var en = r.ReservationTime.AddHours((double)r.Duration);

            if (rst > en) continue;
            if (ren < st) continue;

            return new Tuple<int, string>(1, "There is another reservation in the time.");
        }
        if (reservation.ID > 0)
        {
            var exist = _dbContext.Reservations.FirstOrDefault(s => s.ID == reservation.ID);
            if (exist != null)
            {
                exist.ReservationTime = reservationTime;
                exist.Duration = reservation.Duration;
                exist.ClientName = reservation.ClientName ?? "";
                exist.GuestName = reservation.GuestName ?? "";
                exist.GuestCount = reservation.GuestCount;
                exist.TableID = reservation.TableID;
                exist.Status = reservation.Status;
                exist.Comments = reservation.Comments ?? "";
                exist.Cost = reservation.Cost;
                exist.PhoneNumber = reservation.PhoneNumber;
                exist.TableName = table != null ? table.Name : "";

                _dbContext.SaveChanges();
            }
        }
        else
        {
            var stationID = stationId;
            var newReservation = new Reservation();

            newReservation.StationID = stationID;
            newReservation.AreaID = reservation.AreaID;
            newReservation.ReservationTime = reservationTime;
            newReservation.Duration = reservation.Duration;
            newReservation.ClientName = reservation.ClientName ?? "";
            newReservation.GuestName = reservation.GuestName ?? "";
            newReservation.GuestCount = reservation.GuestCount;
            newReservation.TableID = reservation.TableID;
            newReservation.Status = ReservationStatus.Open;
            newReservation.Comments = reservation.Comments ?? "";
            newReservation.Cost = reservation.Cost;
            newReservation.PhoneNumber = reservation.PhoneNumber;
            newReservation.TableName = table != null ? table.Name : "";


            _dbContext.Reservations.Add(newReservation);
            _dbContext.SaveChanges();
        }

        return new Tuple<int, string>(0, "Operaci�n realizada correctamente");
    }
    public int CancelReservation(long ID)
    {
        var reservations = _dbContext.Reservations.FirstOrDefault(s => s.ID == ID);
        reservations.Status = ReservationStatus.Canceled;

        _dbContext.SaveChanges();
        return 0;
    }

    public Order Kiosk(Station station, string userName, int orderId = 0)
    {
        Order order = null;
        if (orderId > 0)
        {
            order = _dbContext.Orders.FirstOrDefault(s => s.ID == orderId);

            var prepareType = _dbContext.PrepareTypes.FirstOrDefault(s => s.ID == order.PrepareTypeID);
            order.PrepareType = prepareType; //Kiosk
                                             //HttpContext.Session.SetInt32("CurrentOrderID", (int)orderId);
        }
        else
        {
            order = new Order();

            {
                var user = userName;
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

                //HttpContext.Session.SetInt32("CurrentOrderID", (int)order.ID);
            }

        }

        return order;
    }

    public int UpdateCustomerName(long orderID, string clientName)
    {
        var order = _dbContext.Orders.FirstOrDefault(o => o.ID == orderID);
        if (order == null)
        {
            return 1;
        }

        // Actualizar el nombre del cliente en la orden
        order.ClientName = clientName;
        _dbContext.SaveChanges();

        return 0;
    }

    public long SubmitConduceOrders(SubmitConduceOrdersRequest request, int stationId)
    {
        var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == request.CustomerId);
        var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
        var station = _dbContext.Stations.Include(s => s.Areas).FirstOrDefault(s => s.ID == stationID);
        var newOrder = new Order()
        {
            Station = station,
            OrderType = request.Type,
            OrderMode = OrderMode.Conduce,
            OrderTime = DateTime.Now,
            Status = OrderStatus.Temp,
            ClientName = customer.Name,
            CustomerId = customer.ID,
            Delivery = 0,
            Items = new List<OrderItem>(),
        };
        _dbContext.Orders.Add(newOrder);
        _dbContext.SaveChanges();

        var items = new List<OrderItem>();
        var orders = new List<Order>();
        int index = 0;
        foreach (var o in request.Orders)
        {
            var order = GetOrder(o);

            newOrder.Delivery += order.Delivery;

            if (index == 0)
            {
                newOrder.ComprobantesID = order.ComprobantesID;
                newOrder.ComprobanteName = order.ComprobanteName;
            }
            foreach (var item in order.Items)
            {
                var nitem = item.CopyThis();

                var existItem = new OrderItem();
                var isExist = false;
                foreach (var eitem in items)
                {
                    if (nitem.MenuProductID == eitem.MenuProductID && nitem.ServingSizeID == eitem.ServingSizeID && nitem.SubTotal == eitem.SubTotal)
                    {
                        isExist = true;
                        existItem = eitem;
                    }
                }

                if (isExist)
                {
                    existItem.Qty += item.Qty;
                }
                else
                {
                    items.Add(nitem);
                }
            }
            order.ConduceOrderId = newOrder.ID;
        }
        newOrder.Items = items;
        _dbContext.SaveChanges();
        var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == newOrder.ComprobantesID);

        newOrder.GetTotalPrice(voucher);
        _dbContext.SaveChanges();
        return newOrder.ID;
    }

    public UpdateOrderInfoModel UpdateOrderInfo(OrderInfoModel model)
    {
        var customer = _dbContext.Customers.Include(s => s.Voucher).FirstOrDefault(s => s.ID == model.CustomerId);
        var order = _dbContext.Orders.Include(s => s.Divides).Include(s => s.PrepareType).FirstOrDefault(s => s.ID == model.OrderId);
        var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.IsPrimary);
        Console.WriteLine(customer);
        if (model.DividerId > 0)
        {
            if (order.Divides == null) order.Divides = new List<DividerItem>();
            var divide = order.Divides.FirstOrDefault(s => s.DividerNum == model.DividerId);
            if (divide != null)
            {
                divide.ClientName = model.ClientName;
                divide.ClientId = model.CustomerId;
                if (customer != null)
                {
                    if (!model.DontChangeVoucher)
                    {
                        divide.ComprebanteId = customer.Voucher.ID;
                    }
                }
                else
                {
                    divide.ComprebanteId = voucher.ID;

                }
            }
            else
            {
                divide = new DividerItem()
                {
                    DividerNum = model.DividerId,
                    ClientId = model.CustomerId,
                    ClientName = model.ClientName,

                };
                if (customer != null)
                {
                    if (!model.DontChangeVoucher)
                    {
                        divide.ComprebanteId = customer.Voucher.ID;
                    }
                }
                else
                {
                    divide.ComprebanteId = voucher.ID;
                }
                order.Divides.Add(divide);
            }

            _dbContext.SaveChanges();
        }
        else
        {
            order.ClientName = model.ClientName;
            order.CustomerId = model.CustomerId;
            if (order.CustomerId > 0)
            {
                if (customer != null && customer.Voucher != null)
                {
                    if (!model.DontChangeVoucher)
                    {
                        order.ComprobantesID = customer.Voucher.ID;
                        order.ComprobanteName = customer.Voucher.Name;
                    }

                }
                else
                {
                    order.ComprobantesID = voucher.ID;
                    order.ComprobanteName = "";
                }
            }
            else
            {
                order.ComprobantesID = 1;
                order.ComprobanteName = "";
            }
            _dbContext.SaveChanges();
        }

        decimal deliverycost = 0;
        decimal deliverytime = 0;

        if (customer != null && order.OrderType == OrderType.Delivery)
        {
            var deliveryzone = _dbContext.DeliveryZones.FirstOrDefault(s => s.ID == customer.DeliveryZoneID);
            if (deliveryzone != null && !order.PrepareType.SinChofer)
            {
                order.Delivery = deliveryzone.Cost;
            }
            else
            {
                order.Delivery = 0;
            }

            bool deliveryExists = _dbContext.Deliverys.Include(s => s.Order).Where(s => s.IsActive).Where(s => s.Order.ID == order.ID).Any();

            if (deliveryExists)
            {
                var delivery = _dbContext.Deliverys.Include(s => s.Order).Where(s => s.IsActive).Where(s => s.Order.ID == order.ID).First();

                delivery.CustomerID = customer.ID;
                delivery.Address1 = customer.Address1 ?? "";
                delivery.Adress2 = customer.Address2 ?? "";
                delivery.DeliveryZoneID = customer.DeliveryZoneID;

                if (delivery.DeliveryZoneID != null && delivery.DeliveryZoneID > 0)
                {
                    var zone = _dbContext.DeliveryZones.Where(s => s.IsActive).Where(s => s.ID == delivery.DeliveryZoneID).First();
                    deliverycost = zone.Cost;
                    deliverytime = zone.Time;
                }

                _dbContext.SaveChanges();
            }
            else
            {
                var delivery = new Delivery();
                delivery.CustomerID = customer.ID;
                delivery.Address1 = customer.Address1 ?? "";
                delivery.Adress2 = customer.Address2 ?? "";
                delivery.DeliveryZoneID = customer.DeliveryZoneID;
                delivery.OrderID = order.ID;
                delivery.Order = order;

                _dbContext.Deliverys.Add(delivery);
                _dbContext.SaveChanges();
            }

        }

        var order1 = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Items).ThenInclude(s => s.Questions).Include(s => s.Items).ThenInclude(s => s.Discounts).Include(s => s.Items).ThenInclude(s => s.Taxes).Include(s => s.Items).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == model.OrderId);

        if (customer != null && customer.Voucher != null)
        {
            voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == customer.Voucher.ID);
        }

        order1.GetTotalPrice(voucher);
        _dbContext.SaveChanges();

        UpdateOrderInfoModel response = new UpdateOrderInfoModel();

        response.ComprobanteName = voucher.Name;
        response.deliverycost = deliverycost;
        response.deliverytime = deliverytime;
        response.customerName = customer?.Name;
        response.status = 0;

        return response;
    }

    public int ChangeQtyItem(QtyChangeModel model, int stationID)
    {

        var orderItem = _dbContext.OrderItems.Include(s => s.Questions).Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Order).ThenInclude(s => s.Items).Include(s => s.Order).ThenInclude(s => s.Discounts).FirstOrDefault(o => o.ID == model.ItemId);
        var orderId = orderItem.Order.ID;
        orderItem.ForceDate = getCurrentWorkDate(stationID);
        if (orderItem.Status == OrderItemStatus.Pending || orderItem.Status == OrderItemStatus.Printed)
        {
            orderItem.Qty = model.Qty;
            orderItem.SubTotal = orderItem.Price * model.Qty;
        }

        var ret = ApplyPromotion(orderItem, model.Qty, true);
        if (ret)
        {
            var order = orderItem.Order;
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

        var xorder = GetOrder(orderId);

        var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == orderItem.Order.ComprobantesID);
        xorder.GetTotalPrice(voucher);
        _dbContext.SaveChanges();
        return 0;
    }

    public int VoidOrderItem(CancelItemModel model, int StationID)
    {
        var orderItem = _dbContext.OrderItems.Include(s => s.Product).Include(s => s.Questions).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Order).Include(s => s.Order).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).Include(s => s.Order).ThenInclude(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).Include(s => s.Order).ThenInclude(s => s.Discounts).FirstOrDefault(o => o.ID == model.ItemId);
        orderItem.ForceDate = getCurrentWorkDate(StationID);

        if (orderItem == null)
            return 1;

        var orderId = orderItem.Order.ID;
        var order = GetOrder(orderItem.Order.ID);

        var sameItems = new List<OrderItem>();
        if (orderItem.Order.OrderMode == OrderMode.Seat)
        {
            sameItems = order.Items.Where(s => s.Product == orderItem.Product && s.SeatNum == orderItem.SeatNum && !s.IsDeleted).ToList();
        }
        else
        {
            sameItems = order.Items.Where(s => s.Product == orderItem.Product && !s.IsDeleted).ToList();
        }
        bool HasPromotion = false;
        foreach (var item in sameItems)
        {
            var promotion = item.Discounts.Where(s => s.ItemType == DiscountItemType.Promotion).ToList();
            if (promotion.Any())
            {
                HasPromotion = true;
                break;
            }
        }

        //if (HasPromotion)
        //{
        //    foreach(var item in sameItems)
        //    {
        //        VoidItem(new CancelItemModel()
        //        {
        //            ItemId = item.ID,
        //            ReasonId = model.ReasonId
        //        }); 
        //    }                
        //}
        //else
        {
            VoidItem(model, StationID);
        }

        order = GetOrder(orderId);

        var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);

        order.GetTotalPrice(voucher);
        if (order.TotalPrice == 0 && order.OrderMode == OrderMode.Divide)
        {
            order.OrderMode = OrderMode.Standard;
        }
        _dbContext.SaveChanges();


        return 0;
    }

    private void VoidItem(CancelItemModel model, int stationId)
    {
        var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);
        var stationID = stationId;

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
                VoidAddProduct(orderItem.ID, orderItem.Product.ID, -orderItem.Qty, orderItem.ServingSizeID, stationID);
                foreach (var q in orderItem.Questions)
                {
                    if (!q.IsActive) continue;
                    var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                    VoidAddProduct(orderItem.ID, qitem.Answer.Product.ID, -orderItem.Qty * qitem.Qty, qitem.ServingSizeID, stationID);
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

    private void VoidAddProduct(long itemId, long prodId, decimal qty, int servingSizeID, int stationId)
    {
        //var objPOSCore = new POSCore(_userService, _dbContext, _printService, _context);

        var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
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
                            UpdateStockOfArticle(article, -item.Qty * qty, item.UnitNum, warehouse, StockChangeReason.Void, itemId, stationID);
                        }
                    }
                }
                else if (item.Type == ItemType.Product)
                {
                    VoidAddProduct(itemId, item.ItemID, item.Qty, item.UnitNum, stationID);
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
                            UpdateStockOfSubRecipe(subrecipe, -item.Qty * qty, item.UnitNum, warehouse, StockChangeReason.Void, itemId, stationID);
                        }
                    }

                }
            }
        }
        catch { }
    }

    public Tuple<int, string> UpdateOrderInfoPayment(OrderInfoPaymentModel model)
    {
        var order = _dbContext.Orders.Include(s => s.Divides).FirstOrDefault(s => s.ID == model.OrderId);
        var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == model.VoucherId);

        int status = 0;

        if (order.ComprobantesID != null && order.ComprobantesID > 0)
        {
            //var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == order.ComprobantesID);

            if (voucher.IsRequireRNC)
            {

                if (order.CustomerId == null || order.CustomerId <= 0)
                {
                    status = 3;
                }
                else
                {
                    var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);

                    if (string.IsNullOrEmpty(customer.RNC))
                    {
                        status = 3;
                    }
                }



            }
        }


        if (model.DivideId > 0)
        {
            var divide = order.Divides.FirstOrDefault(s => s.DividerNum == model.DivideId);
            if (divide != null)
            {
                divide.ComprebanteId = model.VoucherId;
                _dbContext.SaveChanges();

                return new Tuple<int, string>(status, voucher.Name);
            }
        }
        else
        {
            if (order.ComprobantesID != voucher.ID)
            {
                order.ComprobantesID = voucher.ID;
                order.ComprobanteName = voucher.Name;
                if (order.Status == OrderStatus.Paid)
                {
                    //    Debug.WriteLine("caso3");
                    var length = 11 - voucher.Class.Length;

                    var voucherNumber = voucher.Class + (voucher.Secuencia + 1).ToString().PadLeft(length, '0');
                    voucher.Secuencia = voucher.Secuencia + 1;
                    //if (string.IsNullOrEmpty(order.ComprobanteNumber))
                    order.ComprobanteNumber = voucherNumber;
                    var comprobantes = _dbContext.OrderComprobantes.Where(s => s.OrderId == order.ID);
                    foreach (var c in comprobantes)
                    {
                        c.IsActive = false;
                    }

                    _dbContext.OrderComprobantes.Add(new OrderComprobante()
                    {
                        OrderId = order.ID,
                        VoucherId = voucher.ID,
                        ComprobanteName = voucher.Name,
                        ComprobanteNumber = voucherNumber
                    });
                    _dbContext.SaveChanges();

                }

                _dbContext.SaveChanges();
            }
        }

        return new Tuple<int, string>(status, voucher.Name);
    }

    public class QtyChangeModel
    {
        public long ItemId { get; set; }
        public int Qty { get; set; }
    }

    public class CancelItemModel
    {
        public long ItemId { get; set; }
        public long ReasonId { get; set; }
        public string Pin { get; set; }
        public bool Consolidate { get; set; }
    }

    public class OrderInfoPaymentModel
    {
        public long OrderId { get; set; }
        public int VoucherId { get; set; }
        public int DivideId { get; set; }
    }
}