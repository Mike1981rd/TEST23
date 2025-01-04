using Microsoft.AspNetCore.Mvc;
using AuroraPOS.Data;
using AuroraPOS.Services;
using AuroraPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuroraPOS.ModelsJWT;
using Org.BouncyCastle.Utilities;

namespace AuroraPOS.Core;

public class POSCore
{
    private readonly IUserService _userService;
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _context;
    public POSCore(IUserService userService, AppDbContext dbContext, IHttpContextAccessor context)
    {
        _userService = userService;
        _dbContext = dbContext;
        _context = context;
    }
    
    public Area? GetArea(long areaID, string db)
		{
            var request = _context.HttpContext.Request;
            var _baseURL = $"https://{request.Host}";
            
			var area = _dbContext.Areas.Include(s => s.AreaObjects.Where(s=>!s.IsDeleted)).FirstOrDefault(s => s.ID == areaID);

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

        if(groups != null)
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
}