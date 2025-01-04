using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuroraPOS.Data;
using AuroraPOS.ModelsJWT;
using AuroraPOS.Services;
using Edi.Captcha;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using AuroraPOS.Models;
using AuroraPOS.ViewModels;

namespace AuroraPOS.ControllersJWT;

[Route("jwt/[controller]")]
public class AccountController : Controller
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
    
    // GET
    // Este responde OK siempre por que no tiene autenticación
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return Ok("OK");
    }
    
    // GET
    // Solo responde OK cuando viene el Bearer Token en el Header
    [HttpGet("TestToken")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult TestToken()
    {
        return Ok("OK");
    }

    // Crea un Bearer Token solo para pruebas
    /*[HttpGet("Login")]
    public IActionResult Login()
    {
        return Ok(BuildToken());
    }*/

    //Login con PIN
   [AllowAnonymous]
   [HttpPost("POSLogin")]
    public IActionResult POSLogin(POSLoginRequest request)
    {
        var objResponse = new POSLoginResponse();
        objResponse.status = 2;

        try
        {
            ModelsCentral.User userCentral = null;
            try
            {
                userCentral = _centralService.GetAllowedUserByPin(request.pin);
            }
            catch (Exception ex)
            {
                var m = ex;
            }

            if (userCentral == null)
            {
                objResponse.status = 2;
                return StatusCode(200, objResponse);
            }

            var Central = new CentralService();
            var lstCompanies = Central.GetAllowedCompanies(userCentral.Username);

            if (!lstCompanies.Any())
            {
                objResponse.status = 2;
                return Ok(objResponse);
            }

            objResponse.db = lstCompanies.First().Database;

            var station = _dbContext.Stations.FirstOrDefault(s => "" + s.ID == request.stationId);
            if (station == null)
            {
                objResponse.status = 2;
                return Ok(objResponse);
            }


            var user = _dbContext.User.Include(s => s.Roles).ThenInclude(s => s.Permissions).FirstOrDefault(s => s.Username == userCentral.Username);
            if (user == null)
            {
                objResponse.status = 1;
                return Ok(objResponse);
            }

            List<Claim> claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.GivenName, user.FullName));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Username));

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
                    claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
                    if (role.Permissions != null)
                    {
                        foreach (var permission in role.Permissions)
                        {
                            claims.Add(new Claim("Permission", permission.Value));
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
                objResponse.status = 2;
                return Ok(objResponse);
            }

            objResponse.token = BuildToken(user, claims);
            objResponse.stationId = station.ID.ToString();
            objResponse.stationName = station.Name;

            objResponse.status = 0;
            return Ok(objResponse);
        }
        catch (Exception ex)
        {

            objResponse.status = 3;
            return Ok(objResponse);
        }

        return Ok(objResponse);
    }

    private string BuildToken(User user, List<Claim> claims, DateTime? SetExpiration = null, bool Vacio = false)
        {
            // Cuando tiempo durara nuestro Token
            DateTime expiration;
            if (SetExpiration != null)
            {
                expiration = SetExpiration.Value;
            }
            else
            {
                expiration = DateTime.UtcNow.AddHours(8);
            }
            
            // Creamos Claim (Conjunto de informacion en la cual podemos confiar)
            List<Claim> claim= new List<Claim>();

            foreach (var objClaim in claims)
            {
                claim.Append(objClaim);    
            }

            if (Vacio)
            {
                claim.Append(new Claim(ClaimTypes.PrimarySid, Guid.NewGuid().ToString()));
            }
            else
            {
                
                claim.Append(new Claim(ClaimTypes.Expired, expiration.ToString()));
                claim.Append(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
                
                /*claim = new[]
            {
                new Claim(ClaimTypes.PrimarySid, "001"),
                new Claim(ClaimTypes.Name, "Rafa"),
                new Claim(ClaimTypes.NameIdentifier, "Rafa"),
                new Claim(ClaimTypes.GivenName, "Rafael"),
                new Claim(ClaimTypes.Actor, "Admin"),
                new Claim(ClaimTypes.Email, "a@a.com"),
                new Claim(ClaimTypes.UserData, "otro dato"),
                new Claim(ClaimTypes.Expired, expiration.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };*/
            }
            
            // Creamos nuestra llave de seguridad
            string password = AppConfiguration.Get()["Token:Environment"] == "True" ? AppConfiguration.Get()["super_secret_key"] : AppConfiguration.Get()["Token:Password"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(password));

            // Creamos nuestras credenciales
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Vamos a crear el JSON WEB TOKEN
            JwtSecurityToken token = new JwtSecurityToken(
                issuer: "aurorapos.com",
                audience: "aurorapos.com",
                claims: claim,
                expires: expiration,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

}