using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.Services;
using AuroraPOS.ViewModels;
using AuroraPOS.Hubs;
using Microsoft.AspNetCore.SignalR;
using FastReport;


namespace AuroraPOS.Controllers
{

    [Route("api/[controller]")]
    [ApiController]


    public class PrinterController : ControllerBase
    {
        private readonly IHubContext<PrintHub> _hubContext;
        private readonly PrinterService _printerService;

        public PrinterController(IHubContext<PrintHub> hubContext, PrinterService printerService)
        {
            _hubContext = hubContext;
            _printerService = printerService;
        }

        // Este método será llamado cuando haya un trabajo de impresión pendiente
        [HttpPost]
        public async Task<IActionResult> NotifyPrint([FromBody] PrintJob job)
        {
            if (job == null || string.IsNullOrEmpty(job.PrinterName) || string.IsNullOrEmpty(job.HtmlContent))
            {
                return BadRequest("Datos de impresión inválidos.");
            }

            // Procesa el trabajo de impresión
            await _printerService.ProcessPrintJob(job.PrinterName, job.HtmlContent);

            // Notifica a los clientes conectados que el trabajo de impresión se completó
            await _hubContext.Clients.All.SendAsync("PrintComplete", job.PrinterName);

            return Ok(new { message = "Impresión completada", printerName = job.PrinterName });
        }


    }

}

