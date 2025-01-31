using AuroraPOS.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuroraPOS.Data;
using AuroraPOS.Services;
using AuroraPOS.Models;
using AuroraPOS.ModelsJWT;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
        public JsonResult GetDiaDeTrabajo(int stationId)
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
        public JsonResult GetStations(bool soloTipoKiosco = true)
        {
            GetStationsResponse result = new GetStationsResponse();

            try
            {
                var stations = _dbContext.Stations
                    .Select(s => new Station { ID = s.ID, Name = s.Name, SalesMode = s.SalesMode, IDSucursal = s.IDSucursal})
                    .ToList();

                if (stations.Any() && soloTipoKiosco)
                {
                    stations = (from s in stations where s.SalesMode == SalesMode.Kiosk select s).ToList();
                }

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
        public JsonResult GetActiveCustomerList(ActiveCustomerListRequest request)
        {
            var response = new ActiveCustomerListResponse();
            try
            {
                var settingsCore = new SettingsCore(_userService, _dbContext, _context);
                var activeCustomerList = settingsCore.GetActiveCustomerList(request);

                if (activeCustomerList.activeCustomers.Any())
                {
                    activeCustomerList.activeCustomers =
                        activeCustomerList.activeCustomers.OrderBy(m => m.Name).ToList();
                }

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
        public JsonResult GetPrepareTypes(PrepareTypesRequest request)
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

        [HttpPost("GetActiveCustomers")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetActiveCustomers()
        {
            var settingsCore = new SettingsCore(_userService, _dbContext, _context);
            var response = new GetActiveCustomersResponse();

            try
            {
                var customers = settingsCore.GetActiveCustomers();

                if (customers.Any())
                {
                    customers = customers.OrderBy(m => m.Name).ToList();    
                }
                

                response.result = customers;
                response.Success = true;

                if(!customers.Any())
                    response.message = "No existe ningún cliente activo";

            }
            catch (Exception e)
            {
                response.Success = false;
                response.Error = e.Message;
            }

            return Json(response);
        }

        [HttpPost("GetCxCList")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetCxCList([FromBody] GetCxCListRequest request)
        {
            var settingsCore = new SettingsCore(_userService, _dbContext, _context);
            var response = new GetCxCListResponse();

            try
            {
                List<OrderTransaction> cxc = settingsCore.GetCxCList(request.customerName, request.customerId);

                response.Success = true;
                response.result = cxc;

                return Json(response);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Error = "Ocurrió un error al obtener los datos: " + ex.Message;

                return Json(response);
            }
        }

        [HttpPost("GetCxCList2")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetCxCList2(GetCxCList2Request request)
        {
            var settingsCore = new SettingsCore(_userService, _dbContext, _context);
            var response = new GetCxCListResponse();

            try
            {
                List<OrderTransaction> cxc = settingsCore.GetCxCList2(request.from, request.to, request.cliente, request.orden, request.monto);

                response.Success = true;
                response.result = cxc;

                return Json(response);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Error = "Ocurrió un error al obtener los datos: " + ex.Message;

                return Json(response);
            }
        }

        [HttpPost("CloseDiaTrabajo")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult CloseDiaTrabajo([FromBody] int stationID)
        {
            var settingsCore = new SettingsCore(_userService, _dbContext, _context);

           settingsCore.DeactivateWorkDay(stationID);

            return Json("OK");
        }

    }
}
