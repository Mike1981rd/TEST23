using Microsoft.AspNetCore.Mvc;
using AuroraPOS.Data;
using AuroraPOS.Services;
using AuroraPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using NPOI.SS.Formula.Functions;

namespace AuroraPOS.Core;

public class POSCore
{
    private readonly IUserService _userService;
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _context;
    public POSCore(IUserService userService, AppDbContext dbContext, IHttpContextAccessor context)
    {
        _userService = userService;
        _dbContext = dbContext;
        _context = context;
    }
    
    public Area? GetArea(long areaID, string db)
		{
            var request = _context.HttpContext.Request;
            var _baseURL = $"https://{request.Host}";
            
			var area = _dbContext.Areas.Include(s => s.AreaObjects.Where(s=>!s.IsDeleted)).FirstOrDefault(s => s.ID == areaID);

            //Obtenemos la URL de la imagen del archivo            
            string pathFile = Environment.CurrentDirectory + "/wwwroot" + "/localfiles/" + db + "/area/" + area.ID.ToString() + ".png";
            
            if (System.IO.File.Exists(pathFile))
            {
                var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                area.BackImage = _baseURL + "/localfiles/" + db + "/area/" + area.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second;
            }
            else
            {
                area.BackImage = _baseURL + "/localfiles/" + db + "/area/" + "empty.png";
            }

            //Obtenemos las urls de las imagenes            
            if (area.AreaObjects != null && area.AreaObjects.Any())
            {
                foreach (var item in area.AreaObjects)
                {
                    pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", db, "areaobject", item.ID.ToString() + ".png");
                    if (System.IO.File.Exists(pathFile))
                    {
                        var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                        item.BackImage = Path.Combine(_baseURL, "localfiles", db, "areaobject", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                    }
                    else
                    {
                        item.BackImage = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", "empty.png");
                    }
                }
            }

            /*var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                //MaxDepth = 10, // Fixed
                ReferenceHandler = ReferenceHandler.Preserve
            };*/
            
            return area;
		}

    public List<Area> GetAreasInStation(Station station, string db)
    {
        var request = _context.HttpContext.Request;
        var _baseURL = $"https://{request.Host}";
        if (station.Areas != null && station.Areas.Any())
        {
            foreach (var item in station.Areas)
            {
                var pathFile = Path.Combine(Environment.CurrentDirectory, "wwwroot", "localfiles", db, "area", item.ID.ToString() + ".png");
                if (System.IO.File.Exists(pathFile))
                {
                    var fechaModificacion = System.IO.File.GetLastWriteTime(pathFile);
                    item.BackImage = Path.Combine(_baseURL, "localfiles", db, "area", item.ID.ToString() + ".png?v=" + fechaModificacion.Minute + fechaModificacion.Second);
                }
                else
                {
                    item.BackImage = null; // Path.Combine(_baseURL, "localfiles", Request.Cookies["db"], "areaobject", "empty.png");
                }
            }
        }

        return station.Areas;
    }
}