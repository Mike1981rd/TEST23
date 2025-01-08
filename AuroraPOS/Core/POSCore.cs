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

    public AreaObjects GetAreaObjectsInArea(int stationId, string db, long areaID)
    {
        var stationID = stationId; // HttpContext.Session.GetInt32("StationID");
        var station = _dbContext.Stations.Include(s => s.Areas.Where(s => !s.IsDeleted)).FirstOrDefault(s => s.ID == stationID);
        var area = _dbContext.Areas.Include(s => s.AreaObjects.Where(s => !s.IsDeleted)).AsNoTracking().FirstOrDefault(s => s.ID == areaID);

        //Obtenemos las urls de las imagenes
        var request = _context.HttpContext.Request;
        var _baseURL = $"https://{request.Host}";
        if (area.AreaObjects != null && area.AreaObjects.Any())
        {
            foreach (var item in area.AreaObjects)
            {
                //string pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", Request.Cookies["db"], "areaobject", item.ID.ToString() + ".png");
                string pathFile = Environment.CurrentDirectory + "/wwwroot" + "/localfiles/" + db + "/areaobject/" + item.ID.ToString() + ".png";
                if (System.IO.File.Exists(pathFile))
                {
                    var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                    //item.BackImage = Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                    item.BackImage = _baseURL + "/localfiles/" + db + "/areaobject/" + item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second;
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
                        SubstractProduct(item.ID, item.Product.ID, item.Qty, item.ServingSizeID, order.OrderType, stationID);
                        foreach (var q in item.Questions)
                        {
                            if (!q.IsActive) continue;
                            var qitem = _dbContext.QuestionItems.Include(s => s.Answer).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == q.ID);
                            SubstractProduct(item.ID, qitem.Answer.Product.ID, item.Qty * qitem.Qty, qitem.ServingSizeID, order.OrderType, stationID);
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
                _printService.PrintKitchenItems(stationID, order.ID, kichenItems, db);
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

        //reutilizando la función de SettingsCore para obtener el día
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

        //reutilizando la función de SettingsCore para obtener el día
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

    public OrderItemsInCheckout GetOrderItemsInCheckout(long orderId, int SeatNum, int DividerId)
    {
        var orderItemsInCheckout = new OrderItemsInCheckout();
        orderItemsInCheckout.status = 1;

        var store = _dbContext.Preferences.FirstOrDefault();

        var order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == orderId);
        var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order).ToList();
        var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);

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

            orderItemsInCheckout.status = 0;
            orderItemsInCheckout.seats = seats;
            orderItemsInCheckout.order = order;
            orderItemsInCheckout.transactions = transactions;
            orderItemsInCheckout.store = store;
        }
        else if (order.OrderMode == OrderMode.Divide && DividerId > 0)
        {
            order = _dbContext.Orders.Include(s => s.Discounts).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).FirstOrDefault(s => s.ID == orderId);

            var items = order.Items.Where(s => !s.IsDeleted && s.DividerNum == DividerId).ToList();
            order.GetTotalPrice(voucher, DividerId);

            transactions = transactions.Where(s => s.DividerNum == DividerId).ToList();
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

}