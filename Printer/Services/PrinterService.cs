using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using Printer.Models;
using DinkToPdf.Contracts;
using DinkToPdf;
using PdfiumViewer;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;

namespace Printer.Services
{
    public class PrinterService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly IConverter _pdfConverter;

        [DllImport("wkhtmltox.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void wkhtml_init(); // Función para inicializar wkhtmltox

        [DllImport("wkhtmltox.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void wkhtml_cleanup(); // Función para limpiar recursos al final

        public PrinterService()
        {
            // Inicializar el conversor de PDF
            wkhtml_init(); // Llama a la función de inicialización
            _pdfConverter = new SynchronizedConverter(new PdfTools());
        }

        public async Task StartService()
        {
            try
            {
                // Establecer la conexión con el servidor Aurora POS
                _client = new TcpClient("IP", 5000);  
                _stream = _client.GetStream();

                // Mantener la escucha de nuevas impresiones
                await ListenForPrintRequests();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al conectar con Aurora POS: " + ex.Message);
            }
        }

        private async Task ListenForPrintRequests()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine("Solicitud de impresión recibida: " + request);

                        // Procesar la solicitud de impresión
                        await ProcessPrintJob(request);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al leer datos del socket: " + ex.Message);
                }
            }
        }

        private async Task ProcessPrintJob(string jobId)
        {
            // Llamar a la API de Aurora POS para obtener los detalles del trabajo de impresión
            string apiUrl = $"http://IP:PORT/api/PrinterController/GetPrintJob/{jobId}"; 

            // Obtener los detalles del trabajo de impresión (nombre de la impresora, HTML, etc.)
            var printJob = await GetPrintJobDetails(apiUrl);

            // Enviar a imprimir
            if (printJob != null)
            {
                await PrintToPrinter(printJob.PrinterName, printJob.HtmlContent);
            }
        }

        private async Task<PrintJob> GetPrintJobDetails(string apiUrl)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Hacer la solicitud GET para obtener los detalles del trabajo de impresión
                    var printJob = await client.GetFromJsonAsync<PrintJob>(apiUrl);
                    return printJob;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener detalles de impresión: {ex.Message}");
                return null;
            }
        }

        private async Task PrintToPrinter(string printerName, string htmlContent)
        {
            try
            {
                // Generar el PDF a partir del contenido HTML
                var pdfBytes = ConvertHtmlToPdf(htmlContent);

                // Crear un archivo temporal para almacenar el PDF
                string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
                File.WriteAllBytes(tempFilePath, pdfBytes);

                // Cargar el archivo PDF con PdfiumViewer
                using (var pdfDocument = PdfDocument.Load(tempFilePath))
                {
                    // Crear un PrintDocument a partir del PDF
                    using (var printDocument = pdfDocument.CreatePrintDocument())
                    {
                        printDocument.PrinterSettings.PrinterName = printerName;

                        // Configurar la impresión si es necesario (ajustar márgenes, calidad, etc.)
                        printDocument.DefaultPageSettings.Landscape = false;
                        printDocument.Print();
                    }
                }

                // Eliminar el archivo temporal después de la impresión
                File.Delete(tempFilePath);

                Console.WriteLine("Impresión completada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al imprimir en {printerName}: {ex.Message}");
            }
        }

        private byte[] ConvertHtmlToPdf(string htmlContent)
        {
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                     PaperSize = DinkToPdf.PaperKind.A4
                },
                Objects = {
                    new ObjectSettings
                    {
                        PagesCount = true,
                        HtmlContent = htmlContent,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            // Convertir el HTML a PDF y devolver los bytes
            return _pdfConverter.Convert(doc);
        }
        public void StopService()
        {
            _stream?.Close();
            _client?.Close();
        }

    }
}



