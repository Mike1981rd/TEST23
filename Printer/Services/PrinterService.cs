using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Http.Json;
using Printer.Models;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Text.RegularExpressions;
using System.Drawing;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;

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
        private System.Threading.Timer _timer;
        private readonly string _apiUrl;
        private readonly string _getPendingPrintJobsEndpoint;
        private readonly string _updatePrintJobStatusEndpoint;
        private NotifyIcon _notifyIcon;

        public PrinterService(ILogger<PrinterService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiUrl = "https://875a-189-176-106-110.ngrok-free.app/api/Printer"; // configuration["PrinterSettings:ApiUrl"];
            _getPendingPrintJobsEndpoint = "GetPendingPrintJobs"; // configuration["PrinterSettings:GetPendingPrintJobsEndpoint"];
            _updatePrintJobStatusEndpoint = configuration["PrinterSettings:UpdatePrintJobStatusEndpoint"];
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("El servicio de impresión se ha iniciado.");

            try
            {
                // Iniciar la aplicación de la bandeja del sistema
                //await StartTrayApplication();
                StartTrayIcon();

                // Configurar el Timer para ejecutar el método cada 2 segundos
                _timer = new System.Threading.Timer(state => ConsultaImpresionEImprime(state), null, 0, 20000); // Intervalo en milisegundos
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar iniciar el servicio de impresión.");
            }

            // Mantener el servicio ejecutándose hasta que se solicite la cancelación
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken); // Usar Task.Delay en lugar de Thread.Sleep
            }

            // Detener el Timer cuando el servicio se detenga
            _timer?.Change(Timeout.Infinite, 0);
            _logger.LogInformation("El servicio de impresión se ha detenido.");
        }

        private void StartTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon("Resources/favicon.ico"), // Usar un icono personalizado
                Visible = true,
                Text = "Servicio de impresión en ejecución"
            };

            // Opcionalmente, agregar un menú contextual al icono
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Salir", null, (sender, e) => Application.Exit());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        //Iniciar el programa de fondo que contiene la lógica del icono en UI
        private async Task StartTrayApplication()
        {
            try
            {
                string projectDirectory = AppDomain.CurrentDomain.BaseDirectory; // Directorio base del servicio
                string iconsExePath = Path.Combine(projectDirectory, "Resources", "Icons.exe");

                if (System.IO.File.Exists(iconsExePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = iconsExePath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(iconsExePath)
                    });
                    _logger.LogInformation("Aplicación de bandeja del sistema iniciada correctamente.");
                    //bool success = UserSession.LaunchProcessInUserSession(iconsExePath);
                    //if (success)
                    //{
                    //    _logger.LogInformation("Aplicación de bandeja del sistema iniciada correctamente.");
                    //}
                    //else
                    //{
                    //    _logger.LogError("No se pudo iniciar la aplicación de bandeja en la sesión del usuario.");
                    //}
                }
                else
                {
                    _logger.LogError($"No se encontró el archivo {iconsExePath}. Asegúrate de que el .exe esté en el directorio correcto.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar la aplicación de bandeja del sistema.");
            }
        }


        private async Task ConsultaImpresionEImprime(object state)
        {
            try
            {
                // Solicitar trabajos pendientes desde el servidor.
                var client = _httpClientFactory.CreateClient();
                var url = $"{_apiUrl}/{_getPendingPrintJobsEndpoint}";                
                _logger.LogWarning(url);
                var response = await client.GetAsync(url);
                

                // Asegúrate de que la respuesta fue exitosa.
                if (response.IsSuccessStatusCode)
                {
                    var respuesta = await response.Content.ReadFromJsonAsync<ResponsePrintingsModel>();

                    if (respuesta?.cantidad > 0 || respuesta?.lista != null)
                    {
                        foreach (var objImpresion in respuesta.lista)
                        {
                            var order = objImpresion.printJobOrder;
                            await PrintToPrinter(objImpresion.physicalName, order);

                            //actualizar el estado del trabajo de impresión a Impreso
                            var body = new StringContent(
                                JsonSerializer.Serialize(new UpdatePrintJobRequest { Id = objImpresion.id }),
                                Encoding.UTF8,
                                "application/json"
                            );

                            var url_upd = $"{_apiUrl}{_updatePrintJobStatusEndpoint}";
                            var res = await client.PutAsync(url_upd, body);
                            var info = await response.Content.ReadAsStringAsync();
                            _logger.LogInformation(info);

                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"No se pudieron obtener las impresiones pendientes. Código de estado: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar impresiones pendientes.");
            }
    }



        private async Task PrintToPrinter(string printerName, PrintOrderModel order)
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
                string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string logoPath = Path.Combine(projectDirectory, "Resources", "logo.png");
                Image logo = Image.FromFile(logoPath); // Asegúrate de que el archivo exista
                //Image logo = Image.FromFile("logo.png"); // Asegúrate de que el archivo exista
                ticketPrinter.AddImage(logo); // Tamaño: ancho = 100, alto = 50                

                // Agregar líneas de texto con alineación
                //ticketPrinter.AddLine("TIENDA XYZ", new Font("Arial", 10, FontStyle.Bold), TextAlign.Center);
                ticketPrinter.AddLine(order.empresa + "," + order.empresa1, new Font("Arial", 10, FontStyle.Bold), TextAlign.Center);
                ticketPrinter.AddLine("Fecha: " + DateTime.Now.ToString(), new Font("Arial", 8), TextAlign.Center);
                ticketPrinter.AddEmptyLine();

                // Agregar columnas con alineación
                ticketPrinter.AddColumns(new[] { "Descripción", "Cant.", "Precio", "Total" }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                
                ticketPrinter.AddLine("--------------------------------------------------------");
                
                // Agregar filas de tabla
                foreach (PrintOrderItemModel item in order.items)
                {
                    decimal total = 0.0M;
                    string amountString = item.amount.Substring(1);
                    if (decimal.TryParse(amountString, out decimal amount))
                    {
                        if (int.TryParse(item.qty, out int quantity))
                        {
                            total = amount * quantity;
                        }
                    }

                    ticketPrinter.AddColumns(new[] { item.nombre, item.qty, item.amount, total.ToString("C") }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Regular), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                }
                //ticketPrinter.AddColumns(new[] { "Artículo 1", "2", "$10.00", "$10.00" }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Regular), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                //ticketPrinter.AddColumns(new[] { "Artículo 2 Artículo 2 Artículo 2 Artículo 2", "1", "$15.00", "$10.00" }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Regular), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                
                ticketPrinter.AddLine("--------------------------------------------------------");

                // Total
                ticketPrinter.AddColumns(new[] { "Subtotal", "", "", order.subtotal }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                ticketPrinter.AddColumns(new[] { "Taxes", "", "", order.taxes1 }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                ticketPrinter.AddColumns(new[] { "Propina", "", "", order.propina }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                ticketPrinter.AddColumns(new[] { "Total", "", "", order.total }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                ticketPrinter.AddLine("--------------------------------------------------------");
                ticketPrinter.AddColumns(new[] { "Devuelto", "", "", order.devuelto }, new[] { 110f, 30f, 60f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });

                ticketPrinter.AddEmptyLine();

                // Agregar texto multilínea
                //string longText = "Este es un texto muy largo que necesita dividirse en varias  quitarle el primerolíneas para ajustarse al ancho del ticket.";
                //ticketPrinter.AddMultilineText(longText, new Font("Arial", 10));

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


        private async Task StartService(CancellationToken stoppingToken)
        {
            bool isBusy = false;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!isBusy)
                    {
                        isBusy = true;
                        ConsultaImpresionEImprime(null);
                        isBusy = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message); 
                }

                await Task.Delay(1500);
            }
        }


        private async Task<List<PrintJob>> GetPendingPrintJobs()
        {
            try
            {
                var url = $"{_apiUrl}{_getPendingPrintJobsEndpoint}";
                using (var client = _httpClientFactory.CreateClient())
                {
                    var response = await client.GetFromJsonAsync<List<PrintJob>>(url);
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


        // Detener el servicio y limpiar recursos
        public void StopService()
        {
            _stream?.Close();
            _client?.Close();
        }
    }
}

