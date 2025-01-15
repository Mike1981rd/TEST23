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
    public class SettingController : Controller
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        public SettingController(IUserService userService, ExtendedAppDbContext dbContext, IHttpContextAccessor context)
        {
            _userService = userService;
            _dbContext = dbContext._context;
            _context = context;
        }

        [HttpGet("GetDiaDeTrabajo")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetDiaDeTrabajo([FromBody] int stationId)
        {
            var response = new DiaDeTrabajoResponse();
            try
            {
                var settingsCore = new SettingsCore(_userService, _dbContext, _context);
                var dia = settingsCore.GetDia(stationId);

                if (dia != null)
                {
                    response.Valor = dia.Value.ToString("dd-MM-yyyy");
                    response.Success = true;
                    return Json(response);
                }
                return Json(null);
            }
            catch (Exception ex) {
                response.Error = ex.Message;
                response.Success = false;
                return Json(response);
            }
        }

        [HttpGet("GetActiveVoucherList")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetActiveVoucherList()
        {
            var response = new VoucherListResponse();
            try
            {
                var settingsCore = new SettingsCore(_userService, _dbContext, _context);
                var voucher = settingsCore.GetActiveVoucherList();

                if (voucher != null)
                {
                    response.Valor = voucher;
                    response.Success = true;
                    return Json(voucher);
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

        [HttpGet("GetActiveDeliveryZoneList")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetActiveDeliveryZoneList()
        {
            var response = new DeliveryZoneResponse();
            try
            {
                var settingsCore = new SettingsCore(_userService, _dbContext, _context);
                var deliveryZoneList = settingsCore.GetActiveDeliveryZoneList();

                if (deliveryZoneList != null)
                {
                    response.Valor = deliveryZoneList;
                    response.Success = true;
                    return Json(deliveryZoneList);
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

        [HttpGet("GetStations")]
        [AllowAnonymous]
        public JsonResult GetStations()
        {
            GetStationsResponse result = new GetStationsResponse();

            try
            {
                var stations = _dbContext.Stations
                    .Select(s => new Station { ID = s.ID, Name = s.Name })
                    .ToList();

                if(stations.Any())
                {
                    result.result = stations;
                    result.Success = true;
                } else
                {
                    result.Error = "No hay elementos en la peticion";
                    result.Success = true;
                }
            } catch(Exception e)
            {
                result.Error = e.Message;
                result.Success = false;
            }

            return Json(result);
        }

        [HttpGet("GetActiveCustomerList")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetActiveCustomerList([FromBody] ActiveCustomerListRequest request)
        {
            var response = new ActiveCustomerListResponse();
            try
            {
                var settingsCore = new SettingsCore(_userService, _dbContext, _context);
                var activeCustomerList = settingsCore.GetActiveCustomerList(request);

                if (activeCustomerList != null)
                {
                    response.Valor = activeCustomerList;
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

        [HttpGet("GetPrepareTypes")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetPrepareTypes([FromBody] PrepareTypesRequest request)
        {
            var response = new PrepareTypesResponse();
            try
            {
                var settingsCore = new SettingsCore(_userService, _dbContext, _context);
                var prepareTypesList = settingsCore.GetPrepareTypes(request);

                if (prepareTypesList != null)
                {
                    response.Valor = prepareTypesList;
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

        [HttpGet("GetActiveDeliveryCarrierList")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetActiveDeliveryCarrierList()
        {
            try
            {
                var settingsCore = new SettingsCore(_userService, _dbContext, _context);
                var deliveryCarriers = settingsCore.GetActiveDeliveryCarrierList();

                if (deliveryCarriers != null)
                {
                    return Json(new { Valor = deliveryCarriers, Success = true, Error = "" });
                }
                return Json(null);
            }
            catch (Exception ex)
            {
                return Json(new { Valor = "", Success = false, Error = ex.Message });
            }
        }
    }
}
