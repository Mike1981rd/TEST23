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
    }
}
