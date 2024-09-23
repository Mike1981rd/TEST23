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
        // Método para obtener los detalles del trabajo de impresión
        [HttpGet("{jobId}")]
        public IActionResult GetPrintJob(string jobId)
        {
            // Aquí se deberían obtener los detalles de la impresión (de la base de datos)


            // Simulación de respuesta de la base de datos
            var printJob = new PrintJob
            {
                JobId = jobId,
                PrinterName = "Printer1",
                HtmlContent = "<h1>Impresión Test</h1>"
            };

            return Ok(printJob);
        }

        // Método para actualizar el estado de la impresión
        [HttpPost("update-status")]
        public IActionResult UpdatePrintStatus([FromBody] PrintJob statusUpdate)
        {
            // Aquí se debe actualizar el status en la base de datos
            return Ok(new { message = "Estado actualizado correctamente" });
        }


    }

}

