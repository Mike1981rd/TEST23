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

        [HttpPost("GetDiaDeTrabajo")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetDiaDeTrabajo(int stationId)
        {
            var response = new SettingResponse();
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

        [HttpPost("GetActiveVoucherList")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetActiveVoucherList()
        {
            var response = new SettingResponse();
            try
            {
                var settingsCore = new SettingsCore(_userService, _dbContext, _context);
                var voucher = settingsCore.GetActiveVoucherList();

                if (voucher != null)
                {
                    response.Valor = voucher.ToString();
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
    }
}
