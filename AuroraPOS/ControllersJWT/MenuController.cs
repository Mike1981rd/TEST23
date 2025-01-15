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
    public class MenuController : Controller
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        public MenuController(IUserService userService, ExtendedAppDbContext dbContext, IHttpContextAccessor contex)
        {
            _context = contex;
            _userService = userService;
            _dbContext = dbContext._context;
        }

        [HttpGet("GetAllActiveCategoryList")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetAllActiveCategoryList()
        {
            var response = new MenuResponse();
            var menu = new MenuCore(_userService,_dbContext,_context);

            try
            {
                response.Valor = menu.GetAllActiveCategoryList();
                response.Success = true;
                return Json(response);
            }
            catch (Exception ex) {
                response.Success = false;
                response.Error = ex.Message;
                return Json(response);
            }
        }

        [HttpGet("GetProductList")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public JsonResult GetProductList([FromBody]GetProductListRequest rq)
        {
            var response = new MenuProductListResponse();
            var menu = new MenuCore(_userService, _dbContext, _context);

            rq.length = -1;

            try
            {
                response.Valor = menu.GetProductList(rq.draw,rq.start,rq.length,rq.sortColumn,rq.sortColumnDirection,rq.searchValue,rq.all,rq.category,rq.barcode,rq.status,rq.db);
                response.Success = true;
                return Json(response);
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
