using AuroraPOS.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuroraPOS.Data;
using AuroraPOS.Services;
using AuroraPOS.Models;
using AuroraPOS.ModelsJWT;

namespace AuroraPOS.ControllersJWT
{
    [Route("jwt/[controller]")]
    public class DeliveryController : Controller
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        public DeliveryController(IUserService userService, ExtendedAppDbContext dbContext, IHttpContextAccessor contex)
        {
            _context = contex;
            _userService = userService;
            _dbContext = dbContext._context;
        }

        [HttpGet("Index")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult Index(int? stationId)
        {
            DeliveryIndexResponse response = new DeliveryIndexResponse();

            response.cancelReasons = _dbContext.CancelReasons.ToList();
            response.Branchs = _dbContext.t_sucursal.ToList();
            response.filterBranch = HttpContext.Session.GetInt32("FilterBranch");

            int? getStation = 0; //= int.Parse(GetCookieValue("StationID")); // HttpContext.Session.GetInt32("StationID");

            if (stationId != null)
            {
                getStation = stationId;
                var objEstacion = _dbContext.Stations.Where(s => s.ID == getStation).First();
                response.SucursalActual = objEstacion.IDSucursal;
            }

            return Json(response);
        }

        [HttpGet("GetDeliveryList")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetDeliveryList([FromBody] GetDeliveryRequest request)
        {
            var response = new DeliveryListResponse();
            var delivery = new DeliveryCore(_userService, _dbContext, _context);

            try
            {
                var deliveryList = delivery.GetDeliveryList(request.Fecha, request.Status, request.Branch);

                if (deliveryList != null)
                {
                    response.Valor = deliveryList;
                    response.Success = true;
                    return Json(response);
                }
                return Json(null);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Error = ex.Message;
                return Json(response);
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
    }
}
