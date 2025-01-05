using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.Services;
using AuroraPOS.ViewModels;
using Edi.Captcha;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;
using System.Text;
using Newtonsoft.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AuroraPOS.Controllers
{
    //[Authorize]
	public class AccountController : BaseController
	{
        private readonly ISessionBasedCaptcha _captcha;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICentralService _centralService;
        private readonly IUserService _userService;
        private readonly AppDbContext _dbContext;
        public AccountController(IUserService userService, ICentralService centralService, ExtendedAppDbContext dbContext, IHttpContextAccessor httpContextAccessor, ISessionBasedCaptcha captcha)
		{
            _centralService = centralService;
            _userService = userService;
            _dbContext = dbContext._context;
            _httpContextAccessor = httpContextAccessor;
            _captcha = captcha;
        }

        [AllowAnonymous]        
        public IActionResult GetCaptchaImage()
        {
            var s = _captcha.GenerateCaptchaImageFileStream(
                HttpContext.Session,
                100,
                36
            );
            return s;
        }

        [AllowAnonymous]
		public async Task<IActionResult> Login()
        {
            //HttpContext.Session.Remove("StationID");
            DeleteCookie("StationID");
            
            var request = new LoginViewModel();
            return View(request);
        }
        
        [AllowAnonymous]
        public ContentResult CreateManifest(int station=0)
        {

            string host = HttpContext.Request.Host.Value;
            var strManifest = "{\n  \"name\": \"Alfa POS Station [0]\",\n  \"short_name\": \"Alfa POS Station [0]\",\n  \"description\": \"Alfa POS PWA\",\n  \"icons\": [\n    {\n      \"src\": \"/icon192.png\",\n      \"sizes\": \"192x192\"\n    },\n    {\n      \"src\": \"/icon512.png\",\n      \"sizes\": \"512x512\"\n    }\n  ],\n  \"display\": \"standalone\",\n  \"start_url\": \"/pos/login?station=[0]\"\n , \"scope\" : \"[1]\"}";
            strManifest = strManifest.Replace("[0]", station.ToString());
            strManifest = strManifest.Replace("[1]", "https://" + host);
            
            /*{
                "name": "Alfa POS",
                "short_name": "Alfa POS",
                "description": "Alfa POS PWA",
                "icons": [
                {
                    "src": "/icon192.png",
                    "sizes": "192x192"
                },
                {
                    "src": "/icon512.png",
                    "sizes": "512x512"
                }
                ],
                "display": "standalone",
                "start_url": "/"
            }*/
            
            
            return new ContentResult { Content = strManifest, ContentType = "application/json" };
        }

        private void AddFeature()
		{
			//var existing = _dbContext.User.FirstOrDefault(s => s.Username == "alfaadmin");
			//if (existing != null) { return; }
			//_dbContext.User.Add(new Models.User() { Username = "waiter", Password = "1234", FullName = "waiter" });
			//_dbContext.SaveChanges();

			//_dbContext.Role.Add(new Role() { RoleName = "Admin" });
   //         _dbContext.Role.Add(new Role() { RoleName = "Manager" });
   //         _dbContext.Role.Add(new Role() { RoleName = "Waiter" });
   //         _dbContext.SaveChanges();


   //         var user = _dbContext.User.FirstOrDefault(s=>s.Username == "waiter");
			//var adminrole = _dbContext.Role.FirstOrDefault(s=>s.RoleName == "Waiter");
			//user.Roles = new List<Role>() { adminrole };
			
            var permissions = new List<Permission>();

			permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 1, Value = "Permission.BACKOFFICE.Store", DisplayValue = "Configuracion de Negocios" });
			permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 2, Value = "Permission.BACKOFFICE.Role", DisplayValue = "Administracion de Roles" });
			permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 3, Value = "Permission.BACKOFFICE.User", DisplayValue = "Configuracion de Usuarios" });
			permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 4, Value = "Permission.BACKOFFICE.Customer", DisplayValue = "Configuracion de Clientes" });
			permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 5, Value = "Permission.BACKOFFICE.Printer", DisplayValue = "Configuracion de Impresoras" });
			permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 6, Value = "Permission.BACKOFFICE.PrinterChannel", DisplayValue = "Configuracion de Canales de Impresion" });
			permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 7, Value = "Permission.BACKOFFICE.Tax", DisplayValue = "Configuracino de Impuestos" });			
			permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 8, Value = "Permission.BACKOFFICE.Denomination", DisplayValue = "Denominaciones" });
			permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 9, Value = "Permission.BACKOFFICE.PaymentMethod", DisplayValue = "Metodos de Pagos" });
			permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 10, Value = "Permission.BACKOFFICE.VoidReason", DisplayValue = "Razones de Anulacion" });
            permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 11, Value = "Permission.BACKOFFICE.Comprobantes", DisplayValue = "Configuracion de Comprobantes Fiscales" });
            permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 12, Value = "Permission.BACKOFFICE.Repartidores", DisplayValue = "Configuracion de Repartidores" });
            permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 13, Value = "Permission.BACKOFFICE.ZonasReparto", DisplayValue = "Configuracion de Zonas de Reparto" });
			permissions.Add(new Permission() { Group = "Oficina", Level = 1, GroupOrder = 14, Value = "Permission.BACKOFFICE.Propina", DisplayValue = "Configuracion de Propina" });

			permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 1, Value = "Permission.POS", DisplayValue = "POS" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 2, Value = "Permission.POS.CreateOrder", DisplayValue = "Crear Orden" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 3, Value = "Permission.POS.OtherOrder", DisplayValue = "Manejar ordenes de otros usuarios" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 4, Value = "Permission.POS.VoidProduct", DisplayValue = "Cancelar Productos" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 5, Value = "Permission.POS.Discounts", DisplayValue = "Descuentos" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 6, Value = "Permission.POS.Split", DisplayValue = "Dividir Mesas" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 7, Value = "Permission.POS.Refunds", DisplayValue = "Modificar Facturas" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 8, Value = "Permission.POS.CloseCashDrawer", DisplayValue = "Cierre de Caja" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 9, Value = "Permission.POS.Pay", DisplayValue = "Pantalla de Pago" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 10, Value = "Permission.POS.VoidOrder", DisplayValue = "Cancelar Orden" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 11, Value = "Permission.POS.TransferOrder", DisplayValue = "Transferir Ordenes" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 12, Value = "Permission.POS.EndOfDay", DisplayValue = "Cierre del Dia" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 13, Value = "Permission.POS.Reservation", DisplayValue = "Reservaciones" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 14, Value = "Permission.POS.MoveItem", DisplayValue = "Mover Productos a otra Mesa" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 14, Value = "Permission.POS.MoveItemToOtherArea", DisplayValue = "Mover Productos a otra Area" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 15, Value = "Permission.POS.MoveSeat", DisplayValue = "Mover Silla" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 16, Value = "Permission.POS.MoveTable", DisplayValue = "Mover Mesas" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 17, Value = "Permission.POS.CXC", DisplayValue = "CXC" });
            permissions.Add(new Permission() { Group = "Punto de Venta", Level = 2, GroupOrder = 18, Value = "Permission.POS.ShowExpectedPayment", DisplayValue = "Valores Esperados Cuadre de Caja" });

            permissions.Add(new Permission() { Group = "Inventario", Level = 3, GroupOrder = 1, Value = "Permission.INVENTORY.Warehouse", DisplayValue = "Configuracion de Almacen" });
			permissions.Add(new Permission() { Group = "Inventario", Level = 3, GroupOrder = 2, Value = "Permission.INVENTORY.Supplier", DisplayValue = "Configuracion de Suplidores" });
			permissions.Add(new Permission() { Group = "Inventario", Level = 3, GroupOrder = 3, Value = "Permission.INVENTORY.Article", DisplayValue = "Configuracion de Articulos" });
			permissions.Add(new Permission() { Group = "Inventario", Level = 3, GroupOrder = 4, Value = "Permission.INVENTORY.PurchaseOrder", DisplayValue = "Ordenes de Compra" });
			permissions.Add(new Permission() { Group = "Inventario", Level = 3, GroupOrder = 5, Value = "Permission.INVENTORY.SubRecipe", DisplayValue = "Configuracion de Sub Recetas" });
			permissions.Add(new Permission() { Group = "Inventario", Level = 3, GroupOrder = 6, Value = "Permission.INVENTORY.Move", DisplayValue = "Mover Articulo" });
			permissions.Add(new Permission() { Group = "Inventario", Level = 3, GroupOrder = 7, Value = "Permission.INVENTORY.Production", DisplayValue = "Configuracion de Produccion" });
			permissions.Add(new Permission() { Group = "Inventario", Level = 3, GroupOrder = 8, Value = "Permission.INVENTORY.Damage", DisplayValue = "Desperdicio" });
			permissions.Add(new Permission() { Group = "Inventario", Level = 3, GroupOrder = 9, Value = "Permission.INVENTORY.PhysicalCount", DisplayValue = "Conteo Fisico" });
			permissions.Add(new Permission() { Group = "Inventario", Level = 3, GroupOrder = 10, Value = "Permission.INVENTORY.StockHistory", DisplayValue = "Historial de movimientos" });
			permissions.Add(new Permission() { Group = "Inventario", Level = 3, GroupOrder = 11, Value = "Permission.INVENTORY.StockHistory.Cost", DisplayValue = "Mostrar Costo en Tarjeta de Movimiento" });

			//menu
			permissions.Add(new Permission() { Group = "Configuracion de Menu", Level = 4, GroupOrder = 1, Value = "Permission.MENU.Group", DisplayValue = "Configuracion de Grupos" });
			permissions.Add(new Permission() { Group = "Configuracion de Menu", Level = 4, GroupOrder = 2, Value = "Permission.MENU.Category", DisplayValue = "Configuracion de Categorias" });
			permissions.Add(new Permission() { Group = "Configuracion de Menu", Level = 4, GroupOrder = 3, Value = "Permission.MENU.SubCategory", DisplayValue = "Configuracion de Sub Categorias" });
			permissions.Add(new Permission() { Group = "Configuracion de Menu", Level = 4, GroupOrder = 4, Value = "Permission.MENU.Product", DisplayValue = "Configuracion de Productos" });
			permissions.Add(new Permission() { Group = "Configuracion de Menu", Level = 4, GroupOrder = 5, Value = "Permission.MENU.MenuEditor", DisplayValue = "Menu Editor" });
			permissions.Add(new Permission() { Group = "Configuracion de Menu", Level = 4, GroupOrder = 6, Value = "Permission.MENU.MapTableEditor", DisplayValue = "Mapa de Mesas" });
			permissions.Add(new Permission() { Group = "Configuracion de Menu", Level = 4, GroupOrder = 7, Value = "Permission.MENU.Station", DisplayValue = "Configuracion de Estaciones" });
			permissions.Add(new Permission() { Group = "Configuracion de Menu", Level = 4, GroupOrder = 8, Value = "Permission.MENU.Discount", DisplayValue = "Descuentos" });
			permissions.Add(new Permission() { Group = "Configuracion de Menu", Level = 4, GroupOrder = 9, Value = "Permission.MENU.Promotion", DisplayValue = "Configuracion de Promociones" });

            permissions.Add(new Permission() { Group = "Tablero", Level = 5, GroupOrder = 1, Value = "Permission.DASHBOARD.TotalPurchase", DisplayValue = "Visualizar Total Ordenes de Compra" });
            permissions.Add(new Permission() { Group = "Tablero", Level = 5, GroupOrder = 2, Value = "Permission.DASHBOARD.TotalPurchaseTax", DisplayValue = "Visualizar Impuestos sobre la Compra" });
            permissions.Add(new Permission() { Group = "Tablero", Level = 5, GroupOrder = 3, Value = "Permission.DASHBOARD.TotalSales", DisplayValue = "Visualizar total de Ventas" });
            permissions.Add(new Permission() { Group = "Tablero", Level = 5, GroupOrder = 4, Value = "Permission.DASHBOARD.TotalSalesTax", DisplayValue = "Visualizar Impuestos sobre la Venta" });
            permissions.Add(new Permission() { Group = "Tablero", Level = 5, GroupOrder = 5, Value = "Permission.DASHBOARD.TotalSalesPropina", DisplayValue = "Visualizar Propina" });

            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 1, Value = "Permission.REPORT.SalesReport", DisplayValue = "Reporte de Ventas" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 2, Value = "Permission.REPORT.DetailedSalesReport", DisplayValue = "Reporte de Ventas Detalladas" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 3, Value = "Permission.REPORT.PurchaseReport", DisplayValue = "Reporte de Compras" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 4, Value = "Permission.REPORT.InventoryLevelReport", DisplayValue = "Reporte de Nivel de Inventario" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 5, Value = "Permission.REPORT.InventoryDepletionReport", DisplayValue = "Reporte de Agotamiento de Inventario" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 6, Value = "Permission.REPORT.CloseDrawerReport", DisplayValue = "Reporte de Cuadre de Caja" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 7, Value = "Permission.REPORT.PaymentMethodReport", DisplayValue = "Reporte de Metodos de Pago" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 8, Value = "Permission.REPORT.VoidOrdersReport", DisplayValue = "Reporte de Anulaciones de Ordenes" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 9, Value = "Permission.REPORT.VoidProductsReport", DisplayValue = "Reporte de Productos Anulados" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 10, Value = "Permission.REPORT.SalesInsightReport", DisplayValue = "Reporte de Ventas Semanales" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 11, Value = "Permission.REPORT.WorkDayReport", DisplayValue = "Reporte de Cierre del Dia" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 12, Value = "Permission.REPORT.ProductCost", DisplayValue = "Reporte de Costos Productos" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 13, Value = "Permission.REPORT.CostoDeVenta", DisplayValue = "Reporte de Costo de Ventas" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 14, Value = "Permission.REPORT.VarianceReport", DisplayValue = "Reporte de Varianza" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 15, Value = "Permission.REPORT.ParReport", DisplayValue = "Reporte de Minimos & Maximos" });
            permissions.Add(new Permission() { Group = "Reportes", Level = 6, GroupOrder = 15, Value = "Permission.REPORT.CxCReport", DisplayValue = "Reporte de Cuenta x Cobrar" });
            var currents = _dbContext.Permissions.ToList();
         
            //var maxid = currents.Max(s=>s.ID);

			foreach (var p in permissions)
            {               
                var exist = currents.FirstOrDefault(s=>s.Value == p.Value);
                if (exist == null)
                {
                    //maxid++;
                    //p.ID = maxid;
                    _dbContext.Permissions.Add(p);
                    _dbContext.SaveChanges();
                }
                else
                {
                    exist.Group = p.Group;
                    exist.DisplayValue = p.DisplayValue;
                }
            }

            _dbContext.SaveChanges();
           
            
        }
		public IActionResult AccessDenied()
        {
            return View();
        }
		[AllowAnonymous]
		public IActionResult Feed()
		{ 
			AddFeature();
			return View();
		}
		
		[HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromForm] LoginViewModel request)
		{
			if (!ModelState.IsValid)
            {
                return View(request);
            }
            request.UserName = request.UserName.Trim().ToLower();
            if (string.IsNullOrEmpty(request.Database))
            {
                if (!_captcha.Validate(request.CaptchaCode, HttpContext.Session))
                {
                    ModelState.AddModelError("CaptchaCode", "Code not valid");
                    return View(request);
                }
            }           

            var userCentral = _centralService.GetAllowedUser(request.UserName);
			if (userCentral == null)
			{
				ModelState.AddModelError("UserName" , "User not found");
				return View(request);
			}

			var valid = _centralService.ValidatePassword(userCentral, request.Password);
			if (!valid)
			{
                ModelState.AddModelError("Password", "Wrong password");
                return View(request);
            }
            else
            {
                if (!request.SelectDatabase && string.IsNullOrEmpty(request.Database))
                {
                    request.SelectDatabase = true;

                    var Central = new CentralService();
                    var lstCompanies = Central.GetAllowedCompanies(request.UserName);

                    if(lstCompanies.Any() && lstCompanies.Count > 1)
                    {
                        return View(request);
                    }

                    CookieOptions option = new CookieOptions();
                    option.Expires = DateTime.Now.AddDays(1);
                    Response.Cookies.Append("db", lstCompanies.First().Database, option);
                }
                else
                {
                    CookieOptions option = new CookieOptions();                    
                    option.Expires = DateTime.Now.AddDays(1);
                    Response.Cookies.Append("db", request.Database, option);
                }
            }

            //Si llego hasta aqui es que la validacion anterior fue exitosa, y recuperamos al usuario
            var user = _userService.GetAllowedUser(request.UserName);

            if ( user == null)
            {
                ModelState.AddModelError("UserName", "User not found");
                return View(request);
            }
            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FullName));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Username));

            var store = _dbContext.Preferences.FirstOrDefault();
            if (store != null)
            {
                AlfaHelper.Currency = ""+ store.Currency??"$";
            }

			if (user.Roles.Any())
			{
                foreach (var role in user.Roles)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role.RoleName));

					if (role.Permissions != null)
					{
                        foreach (var permission in role.Permissions)
                        {
                            identity.AddClaim(new Claim("Permission", permission.Value));
                        }
                    }
					
                }
            }            

            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
		}

        [AllowAnonymous]
        [HttpPost]
        [Route("/Account/POSLogin")]
        public async Task<IActionResult> POSLogin([FromBody] LoginViewModel request)
        {
            try
            {
                ModelsCentral.User userCentral = null;
                try
                {
                    userCentral = _centralService.GetAllowedUserByPin(request.Password);
                }
                catch (Exception ex)
                {
                    var m = ex;
                }

                if (userCentral == null)
                {
                    return Ok(new { status = 2 });
                }

                var Central = new CentralService();
                var lstCompanies = Central.GetAllowedCompanies(userCentral.Username);

                if (!lstCompanies.Any())
                {
                    return Ok(new { status = 2 });
                }

                CookieOptions option = new CookieOptions();
                option.Expires = DateTime.Now.AddDays(1);
                Response.Cookies.Append("db", lstCompanies.First().Database, option);


                var station = _dbContext.Stations.FirstOrDefault(s => "" + s.ID == request.UserName);
                if (station == null)
                {
                    return Ok(new { status = 2 });
                }


                var user = _dbContext.User.Include(s => s.Roles).ThenInclude(s => s.Permissions).FirstOrDefault(s => s.Username == userCentral.Username);
                if (user == null)
                {
                    return Ok(new { status = 1 });
                }


                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FullName));
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Username));

                var store = _dbContext.Preferences.FirstOrDefault();
                if (store != null)
                {
                    AlfaHelper.Currency = "" + store.Currency ?? "$";
                }
                var isAccessable = false;
                if (user.Roles.Any())
                {
                    foreach (var role in user.Roles)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role.RoleName));
                        if (role.Permissions != null)
                        {
                            foreach (var permission in role.Permissions)
                            {
                                identity.AddClaim(new Claim("Permission", permission.Value));
                                if (permission.Value == "Permission.POS")
                                {
                                    isAccessable = true;
                                }

                            }
                        }

                    }
                }

                if (!isAccessable)
                {
                    return Ok(new { status = 2 });
                }

                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                //HttpContext.Session.SetInt32("StationID", (int)station.ID);
                //HttpContext.Session.SetString("StationName", station.Name);


                option.Expires = DateTime.Now.AddDays(1);
                Response.Cookies.Append("StationID", station.ID.ToString(), option);
                Response.Cookies.Append("StationName", station.Name, option);

                return Ok(new { status = 0 });
            }
            catch (Exception ex){

                return Ok(new { status = 3 });
            }
           
        }


        [AllowAnonymous]
        [HttpPost]
        [Route("/Account/POSKitchenLogin")]
        public async Task<IActionResult> POSKitchenLogin([FromBody] LoginViewModel request)
        {
            ModelsCentral.User userCentral = null;
            try
            {
                userCentral = _centralService.GetAllowedUserByPin(request.Password);
            }
            catch (Exception ex)
            {
                var m = ex;
            }

            if (userCentral == null)
            {
                return Json(new { status = 2 });
            }

            var Central = new CentralService();
            var lstCompanies = Central.GetAllowedCompanies(userCentral.Username);

            if (!lstCompanies.Any())
            {
                return Json(new { status = 2 });
            }

            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddDays(1);
            Response.Cookies.Append("db", lstCompanies.First().Database, option);


            var kitchen = _dbContext.Kitchen.FirstOrDefault(s => "" + s.ID == request.UserName);
            if (kitchen == null)
            {
                return Json(new { status = 2 });
            }


            var user = _dbContext.User.Include(s => s.Roles).ThenInclude(s => s.Permissions).FirstOrDefault(s => s.Username == userCentral.Username);
            if (user == null)
            {
                return Json(new { status = 1 });
            }


            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FullName));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Username));

            var isAccessable = false;
            if (user.Roles.Any())
            {
                foreach (var role in user.Roles)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role.RoleName));
                    if (role.Permissions != null)
                    {
                        foreach (var permission in role.Permissions)
                        {
                            identity.AddClaim(new Claim("Permission", permission.Value));
                            if (permission.Value == "Permission.POS")
                            {
                                isAccessable = true;
                            }

                        }
                    }

                }
            }

            if (!isAccessable)
            {
                return Json(new { status = 2 });
            }

            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            //HttpContext.Session.SetInt32("StationID", (int)station.ID);
            //HttpContext.Session.SetString("StationName", station.Name);


            option.Expires = DateTime.Now.AddDays(1);
            Response.Cookies.Append("KitchenID", kitchen.ID.ToString(), option);
            Response.Cookies.Append("KitchenName", kitchen.Name, option);

            return Json(new { status = 0 });
        }


        [HttpPost]       
        public async Task<JsonResult> ClientLogin([FromForm] LoginViewModel request)
        {           
            var user = _userService.GetAllowedUser("waiter");
            
            var valid = _userService.ValidatePassword(user, request.Password);
            if (!valid)
            {
                ModelState.AddModelError("Password", "Wrong password");
                return Json(new { status = 1 });
            }

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FullName));
            identity.AddClaim(new Claim(ClaimTypes.Thumbprint, user.ProfileImage));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Username));

            if (user.Roles.Any())
            {
                foreach (var role in user.Roles)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role.RoleName));
                }
            }

            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return Json(new { status = 0 }) ;
        }


        public async Task<IActionResult> Logoff()
		{
			await HttpContext.SignOutAsync();
			return RedirectToAction("Login", new { page = "home" });
		}

        public string GetCookieValue(string key)
        {
            var cookieRequest = HttpContext.Request.Cookies[key];
            string cookieResponse = GetCookieValueFromResponse(HttpContext.Response, key);

            if (cookieResponse != null)
            {
                return (cookieResponse);
            }
            else
            {
                return (cookieRequest);
            }
        }

        public void DeleteCookie(string key)
        {
            var cookieRequest = HttpContext.Request.Cookies[key];
            string cookieResponse = GetCookieValueFromResponse(HttpContext.Response, key);

            if (cookieResponse != null)
            {
                HttpContext.Response.Cookies.Delete(key);
            }
            else
            {
                // HttpContext.Request.Cookies.Delete(key);

            }
        }

        string GetCookieValueFromResponse(HttpResponse response, string cookieName)
        {
            foreach (var headers in response.Headers.Values)
                foreach (var header in headers)
                    if (header.StartsWith($"{cookieName}="))
                    {
                        var p1 = header.IndexOf('=');
                        var p2 = header.IndexOf(';');
                        return header.Substring(p1 + 1, p2 - p1 - 1);
                    }
            return null;
        }
    }
}
