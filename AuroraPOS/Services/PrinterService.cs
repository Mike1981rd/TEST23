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
using AuroraPOS.Models;
using DinkToPdf.Contracts;
using DinkToPdf;
using PdfiumViewer;
using System.Drawing.Printing;
using System.IO;

namespace AuroraPOS.Services
{
    public class PrinterService
    {
        // Método para enviar un trabajo de impresión al servicio Windows
        public async Task SendPrintJobToServiceAsync(PrintJob printJob)
        {
            // Asegúrate de que PrintJob tenga una propiedad HtmlContent
            if (printJob == null || string.IsNullOrEmpty(printJob.HtmlContent))
            {
                throw new ArgumentException("El trabajo de impresión no es válido.");
            }

            // Enviar el trabajo al servicio de impresión
            await SendToPrinterServiceAsync(printJob.HtmlContent);
        }

        private async Task SendToPrinterServiceAsync(string htmlContent)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    // Configura la dirección y el puerto del servicio de impresión
                    await client.ConnectAsync("127.0.0.1", 5000); // Cambia la IP y el puerto según sea necesario

                    // Convertir el contenido a bytes
                    var data = Encoding.UTF8.GetBytes(htmlContent);

                    // Enviar los datos al socket
                    using (var stream = client.GetStream())
                    {
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                }
                catch (SocketException ex)
                {
                    throw new Exception("No se pudo conectar al servicio de impresión.", ex);
                }
            }
        }
    }
}