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
        private const string PrinterServiceIp = "127.0.0.1";
        private const int PrinterServicePort = 5000;

        // Método para enviar un trabajo de impresión al servicio Windows
        public async Task SendPrintJobToServiceAsync(PrintJob printJob)
        {
            // Asegúrate de que PrintJob tenga una propiedad HtmlContent
            if (printJob == null || string.IsNullOrEmpty(printJob.HtmlContent))
            {
                throw new ArgumentException("El trabajo de impresión no es válido.");
            }

            // Intentar enviar el trabajo al servicio de impresión
            await SendToPrinterServiceAsync(printJob.HtmlContent);
        }

        private async Task SendToPrinterServiceAsync(string htmlContent)
        {
            using (var client = new TcpClient())
            {
                int maxRetries = 3;
                int attempt = 0;
                bool isConnected = false;

                while (attempt < maxRetries && !isConnected)
                {
                    try
                    {
                        // Configura la dirección y el puerto del servicio de impresión
                        await client.ConnectAsync(PrinterServiceIp, PrinterServicePort); // Cambia la IP y el puerto según sea necesario
                        isConnected = true;
                    }
                    catch (SocketException)
                    {
                        attempt++;
                        if (attempt == maxRetries)
                        {
                            throw new Exception("No se pudo conectar al servicio de impresión después de varios intentos.");
                        }
                        await Task.Delay(1000); // Esperar 1 segundo antes de reintentar
                    }
                }

                // Convertir el contenido a bytes
                var data = Encoding.UTF8.GetBytes(htmlContent);

                try
                {
                    // Enviar los datos al socket
                    using (var stream = client.GetStream())
                    {
                        await stream.WriteAsync(data, 0, data.Length);

                        // Opcional: Leer la respuesta del servidor (confirmación de procesamiento)
                        byte[] response = new byte[256];
                        int bytesRead = await stream.ReadAsync(response, 0, response.Length);
                        string responseMessage = Encoding.UTF8.GetString(response, 0, bytesRead);

                        if (!string.IsNullOrEmpty(responseMessage))
                        {
                            // Log o manejar la respuesta (ej. "Impresión completada")
                            Console.WriteLine("Respuesta del servidor de impresión: " + responseMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al enviar los datos al servicio de impresión.", ex);
                }
            }
        }
    }
}