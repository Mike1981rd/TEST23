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
using Microsoft.Extensions.Logging;
using System.Text.Json;
using RestSharp;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Printing;

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

            //PrintToPrinter("POS-80", "");
            PrintToPrinter("Microsoft Print to PDF", "");

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
            /*try
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
            }*/

            bool isBusy = false;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!isBusy)
                    {
                        isBusy = true;
                        ConsultaImpresionEImprime();
                        isBusy = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.Write("Error: " + ex.Message);
                }

                await Task.Delay(1500);
            }
        }

        private void ConsultaImpresionEImprime()
        {
            // send POST request with RestSharp
            var client = new RestClient("https://localhost:7205");
            var request = new RestRequest("pendingimpressions");
            /*request.AddBody(new
            {
                sucursal = "1"
            });*/

            var response = client.ExecutePost(request);

            // deserialize json string response to Product object
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var respuesta = JsonSerializer.Deserialize<ResponsePrintingsModel>(response.Content, options);

            if (respuesta.Cantidad > 0)
            {
                foreach (var objImpresion in respuesta.Impresiones)
                {
                    Imprimir(objImpresion);
                }
            }
        }

        private void Imprimir(PrintModel objImpresion )
        {
            PrintToPrinter(objImpresion.Impresora, objImpresion.Html); // Imprimir trabajo pendiente
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

        private string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }

        private async Task PrintToPrinter(string printerName, string htmlContent)
        {
            try
            {
                // Generar el PDF a partir del contenido HTML utilizando QuestPDF o iText.
                //var pdfBytes = GeneratePdf(htmlContent);
                //string str = HtmlToPlainText(htmlContent);

                // Crear un archivo temporal para guardar el PDF generado.
                //string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
                //File.WriteAllBytes(tempFilePath, pdfBytes);

                //HtmlConverter.ConvertToPdf(htmlContent, new FileStream(tempFilePath, FileMode.Create));


                // Configurar el proceso de impresión.
                


                TicketPrinter ticketPrinter = new TicketPrinter();

                // Agregar un logotipo
                Image logo = Image.FromFile("logo.png"); // Asegúrate de que el archivo exista
                ticketPrinter.AddImage(logo); // Tamaño: ancho = 100, alto = 50                

                // Agregar líneas de texto con alineación
                ticketPrinter.AddLine("TIENDA XYZ", new Font("Arial", 10, FontStyle.Bold), TextAlign.Center);
                ticketPrinter.AddLine("Fecha: " + DateTime.Now.ToString(), new Font("Arial", 8), TextAlign.Center);
                ticketPrinter.AddEmptyLine();

                // Agregar columnas con alineación
                ticketPrinter.AddColumns(new[] { "Descripción", "Cant.", "Precio", "Total" }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                ticketPrinter.AddLine("--------------------------------------------------------");

                // Agregar filas de tabla
                ticketPrinter.AddColumns(new[] { "Artículo 1", "2", "$10.00", "$10.00" }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Regular), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                ticketPrinter.AddColumns(new[] { "Artículo 2 Artículo 2 Artículo 2 Artículo 2", "1", "$15.00", "$10.00" }, new[] { 110f, 30f, 60f, 60f },new Font("Arial", 7, FontStyle.Regular), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                ticketPrinter.AddLine("--------------------------------------------------------");

                // Total
                ticketPrinter.AddColumns(new[] { "Total", "","", "$35.00" }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                ticketPrinter.AddEmptyLine();

                // Agregar texto multilínea
                string longText = "Este es un texto muy largo que necesita dividirse en varias líneas para ajustarse al ancho del ticket.";
                ticketPrinter.AddMultilineText(longText, new Font("Arial", 10));

                // Mensaje de agradecimiento
                ticketPrinter.AddLine("¡Gracias por su compra!", new Font("Arial", 10, FontStyle.Italic), TextAlign.Center);

                // Imprimir
                //string printerName = "NombreDeLaImpresora"; // Cambia esto por el nombre de tu impresora
                ticketPrinter.Print(printerName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al imprimir en {printerName}: {ex.Message}");
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

