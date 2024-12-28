using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AuroraPOS.ControllersJWT;

[Route("jwt/[controller]")]
public class AccountController : Controller
{
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
    
    // Crea un Bearer Token
    [HttpGet("Login")]
    public IActionResult Login()
    {
        return Ok(BuildToken());
    }
    
    private string BuildToken(DateTime? SetExpiration = null, bool Vacio = false)
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
            Claim[] claim;

            if (Vacio)
            {
                claim = new[] { new Claim(ClaimTypes.PrimarySid, Guid.NewGuid().ToString()) };
            }
            else
            {
                claim = new[]
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
            };
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