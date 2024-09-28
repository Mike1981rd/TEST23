using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using System.Net.Http.Json;
using Printer.Models;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Elements;
using static System.Net.Mime.MediaTypeNames;
using QuestPDF.Helpers;
using PdfiumViewer;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Extensions.Logging;

namespace Printer.Services
{
    public class PrinterService : BackgroundService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ServerIp = "127.0.0.1"; // Cambia a la IP real
        private const int ServerPort = 7205; // Cambia al puerto real
        private readonly ILogger<PrinterService> _logger; // Inyectar el logger

        public PrinterService(ILogger<PrinterService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("El servicio de impresión se ha iniciado.");

            try
            {
                _logger.LogInformation("Conectando a Aurora POS...");
                await StartService(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al conectar con Aurora POS.");
                _logger.LogError(ex, "Error en el servicio de impresión.");
            }
        }


        private async Task StartService(CancellationToken stoppingToken)
        {
            try
            {
                // Iniciar la conexión con el servidor TCP para recibir solicitudes de impresión en tiempo real
                _client = new TcpClient(ServerIp, ServerPort);
                _stream = _client.GetStream();

                // Iniciar la escucha de nuevas solicitudes de impresión
                await ListenForPrintRequests(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al conectar con Aurora POS: " + ex.Message);
            }
        }

        private async Task ListenForPrintRequests(CancellationToken stoppingToken)
        {
            byte[] buffer = new byte[1024];
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_stream.CanRead)
                    {
                        Console.WriteLine("Conexión perdida, intentando reconectar...");
                        await StartService(stoppingToken); // Intentar reconectar
                    }

                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);
                    if (bytesRead > 0)
                    {
                        string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine("Solicitud de impresión recibida: " + request);

                        // Verificar si el mensaje recibido es "PRINT"
                        if (request.Equals("PRINT", StringComparison.OrdinalIgnoreCase))
                        {
                            // Obtener los trabajos pendientes de impresión
                            var pendingJobs = await GetPendingPrintJobs();
                            if (pendingJobs != null && pendingJobs.Any())
                            {
                                // Procesar cada trabajo pendiente de impresión
                                foreach (var job in pendingJobs)
                                {
                                    Console.WriteLine("Procesando trabajo pendiente: " + job.JobId);

                                    // Llamar a la función que imprime el contenido HTML en la impresora
                                    await PrintToPrinter(job.PrinterName, job.HtmlContent); // Imprimir trabajo pendiente
                                }
                            }
                            else
                            {
                                Console.WriteLine("No hay trabajos pendientes.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Mensaje recibido desconocido: " + request);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al leer datos del socket: " + ex.Message);
                }
            }
        }

        private async Task<List<PrintJob>> GetPendingPrintJobs()
        {
            try
            {
                string apiUrl = $"http://localhost:7205/api/Printer/GetPendingPrintJobs";
                using (var client = _httpClientFactory.CreateClient())
                {
                    var response = await client.GetFromJsonAsync<List<PrintJob>>(apiUrl);
                    return response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener trabajos pendientes: {ex.Message}");
                return null;
            }
        }

        private async Task PrintToPrinter(string printerName, string htmlContent)
        {
            try
            {
                // Generar el PDF a partir del contenido HTML utilizando QuestPDF o iText.
                var pdfBytes = GeneratePdf(htmlContent);

                // Crear un archivo temporal para guardar el PDF generado.
                string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
                File.WriteAllBytes(tempFilePath, pdfBytes);


                // Configurar el proceso de impresión.
                using (var printDocument = new System.Drawing.Printing.PrintDocument())
                {
                    printDocument.PrinterSettings.PrinterName = printerName;  // Especificar la impresora por nombre.

                    // Configurar la acción que se llevará a cabo durante el evento PrintPage.
                    printDocument.PrintPage += (sender, e) =>
                    {
                        using (var pdfDocument = PdfiumViewer.PdfDocument.Load(tempFilePath))
                        {
                            var pageImage = pdfDocument.Render(0, e.PageBounds.Width, e.PageBounds.Height, true);
                            e.Graphics.DrawImage(pageImage, e.PageBounds);  // Dibujar el PDF en la página de impresión.
                        }
                    };

                    // Enviar el documento a la impresora.
                    printDocument.Print();
                }

                // Eliminar el archivo temporal después de la impresión.
                File.Delete(tempFilePath);

                Console.WriteLine($"Impresión completada en la impresora: {printerName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al imprimir en {printerName}: {ex.Message}");
            }
        }


        public byte[] GeneratePdf(string htmlContent)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new iText.Kernel.Pdf.PdfWriter(memoryStream)) // Asegúrate de usar iText
                {
                    using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                    {
                        var document = new iText.Layout.Document(pdf);
                        var lines = htmlContent.Split(new[] { "<br>", "<br/>", "<br />" }, StringSplitOptions.None);

                        foreach (var line in lines)
                        {
                            document.Add(new iText.Layout.Element.Paragraph(line)); // Asegúrate de usar el tipo correcto
                        }
                    }
                }
                return memoryStream.ToArray();
            }
        }

        // Detener el servicio y limpiar recursos
        public void StopService()
        {
            _stream?.Close();
            _client?.Close();
        }
    }
}

