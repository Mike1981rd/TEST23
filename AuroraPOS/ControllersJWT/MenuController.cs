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
        public JsonResult GetProductList(string searchValue, string category, string barcode, string status, string db)
        {
            var response = new MenuProductListResponse();
            var menu = new MenuCore(_userService, _dbContext, _context);
            
            var rq = new GetProductListRequest();
            
            rq.length = -1;
            rq.searchValue = searchValue;
            rq.category = category;
            rq.barcode = barcode;
            rq.status = status;
            rq.db = db;

            try
            {
                response.Valor = menu.GetProductList("",0,-1,rq.sortColumn,rq.sortColumnDirection,rq.searchValue,rq.all,rq.category,rq.barcode,rq.status,rq.db);
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
