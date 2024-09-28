using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.Services;
using AuroraPOS.ViewModels;
using AuroraPOS.Hubs;
using Microsoft.AspNetCore.SignalR;
using FastReport;
using Microsoft.EntityFrameworkCore;
using AuroraPOS.ModelsCentral;


namespace AuroraPOS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrinterController : ControllerBase
    {
        private readonly PrinterService _printerService;
        private AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        private readonly DbAlfaCentralContext _dbCentralContext;

        public PrinterController(PrinterService printerService, AppDbContext dbContext)
        {
            _printerService = printerService;
            _dbContext = dbContext;
        }

        // Método para crear y enviar un trabajo de impresión
        [HttpPost]
        public async Task<IActionResult> PrintJobs()
        {
            // Obtiene la lista de trabajos pendientes desde la base de datos
            var pendingJobs = await _dbContext.PrintJobs
                .Where(job => job.Status == "Pendiente")
                .ToListAsync();

            // Verifica si hay trabajos pendientes
            if (pendingJobs == null || !pendingJobs.Any())
            {
                return NotFound("No hay trabajos de impresión pendientes.");
            }

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // Enviar cada trabajo al servicio de impresión
                    foreach (var job in pendingJobs)
                    {
                        await _printerService.SendPrintJobToServiceAsync(job);
                        job.Status = "En Progreso"; // Cambia el estado a "En Progreso"
                    }

                    await _dbContext.SaveChangesAsync(); // Guarda los cambios en la base de datos
                    await transaction.CommitAsync(); // Confirma la transacción

                    return Ok(new { message = "Trabajos de impresión enviados." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(); // Revertir en caso de error
                    return StatusCode(500, $"Error al enviar trabajos de impresión: {ex.Message}");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingPrintJobs()
        {
            // Obtiene la lista de trabajos pendientes desde la base de datos
            var pendingJobs = await _dbContext.PrintJobs
                .Where(job => job.Status == "Pendiente")
                .ToListAsync();

            return Ok(pendingJobs);
        }
    }

}

