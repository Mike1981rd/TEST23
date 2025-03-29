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
using System.Security.Policy;
using System.Globalization;

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
        private readonly SemaphoreSlim _semaphore = new(1, 1); // Permite solo una ejecución concurrente
        private PreferenceModel _preferenceCache;

        public PrinterService(ILogger<PrinterService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            // Cargar valores desde appsettings.json
            _apiUrl = configuration["PrinterSettings:ApiUrl"];
            _getPendingPrintJobsEndpoint = configuration["PrinterSettings:GetPendingPrintJobsEndpoint"];
            _updatePrintJobStatusEndpoint = configuration["PrinterSettings:UpdatePrintJobStatusEndpoint"];

            _logger.LogInformation($"API URL: {_apiUrl}");
            _logger.LogInformation($"Pending Print Jobs Endpoint: {_getPendingPrintJobsEndpoint}");
            _logger.LogInformation($"Update Print Job Status Endpoint: {_updatePrintJobStatusEndpoint}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("El servicio de impresión se ha iniciado.");

            try
            {
                // Iniciar la aplicación de la bandeja del sistema
                //await StartTrayApplication();
                StartTrayIcon();

                // Configurar el Timer para ejecutar el método cada 1.5 segundos
                _timer = new System.Threading.Timer(state => ConsultaImpresionEImprime(state), null, 0, 2000); // Intervalo en milisegundos
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

        private async Task<PreferenceModel> GetPreferenceData()
        {
            try
            {
                if (_preferenceCache != null)
                {
                    _logger.LogInformation(" Usando configuración de Preference desde caché.");
                    return _preferenceCache;
                }

                var client = _httpClientFactory.CreateClient();
                var url = $"{_apiUrl}/GetPreference";
                _logger.LogInformation($" Consultando: {url}");

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($" Error en la petición: {response.StatusCode}");
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($" Respuesta cruda de la API: {jsonResponse}");

                if (jsonResponse.Trim().StartsWith("<"))
                {
                    _logger.LogError(" La API devolvió HTML en lugar de JSON.");
                    return null;
                }

                // Deserializar JSON y guardar en caché
                _preferenceCache = JsonSerializer.Deserialize<PreferenceModel>(jsonResponse);

                // 🔥 Verificar si los datos están completos
                if (_preferenceCache == null)
                {
                    _logger.LogError(" La API devolvió una respuesta vacía.");
                    return null;
                }
                _logger.LogInformation($" PreferenceModel Deserializado: Name={_preferenceCache.Name}, Company={_preferenceCache.Company}, Logo={(_preferenceCache.Logo != null ? "Sí tiene logo" : "No tiene logo")}");

                return _preferenceCache;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error al obtener la configuración de Preference.");
                return null;
            }
        }





        private async Task ConsultaImpresionEImprime(object state)
        {
            if (!_semaphore.Wait(0)) // Si ya se está ejecutando, no lo ejecuta de nuevo
            {
                _logger.LogWarning("El proceso de impresión aún está en ejecución. Esperando...");
                return;
            }

            try
            {
                _logger.LogInformation("Consultando impresiones pendientes...");

                var client = _httpClientFactory.CreateClient();
                var url = $"{_apiUrl}/{_getPendingPrintJobsEndpoint}";
                _logger.LogInformation($"Consultando: {url}");

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var respuesta = await response.Content.ReadFromJsonAsync<ResponsePrintingsModel>();

                    if (respuesta?.cantidad > 0 || respuesta?.lista != null)
                    {
                        foreach (var objImpresion in respuesta.lista)
                        {
                            var order = objImpresion.printJobOrder;

                            // 🔥 Detectar tipo de impresión
                          

                            await PrintToPrinter(objImpresion.physicalName, order, objImpresion.type);

                            // Actualizar estado de impresión a "Impreso"
                            var body = new StringContent(
                                JsonSerializer.Serialize(new { Id = objImpresion.id }),
                                Encoding.UTF8,
                                "application/json"
                            );

                            var url_upd = $"{_apiUrl}/{_updatePrintJobStatusEndpoint}";
                            var res = await client.PostAsync(url_upd, body);
                            var info = await res.Content.ReadAsStringAsync();
                            _logger.LogInformation($"Estado de impresión actualizado: {info}");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"No se pudieron obtener las impresiones pendientes. Código: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar trabajos de impresión.");
            }
            finally
            {
                _semaphore.Release(); // Libera el semáforo para permitir nuevas ejecuciones
            }
        }


        string CleanCurrency(string value)
        {
            return Regex.Replace(value, @"[^\d.,]", "").Trim();
        }

        private async Task UpdatePrintJobStatus(string jobId)
        {
            try
            {
                var url = $"{_apiUrl}{_updatePrintJobStatusEndpoint}";
                using (var client = _httpClientFactory.CreateClient())
                {
                    var content = JsonContent.Create(jobId);
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($" Estado del trabajo de impresión {jobId} actualizado a 'Impreso'.");
                    }
                    else
                    {
                        Console.WriteLine($" Error al actualizar estado de impresión {jobId}: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error en la actualización del estado de impresión: {ex.Message}");
            }
        }

        private async Task PrintToPrinter(string printerName, PrintOrderModel order, int type)
        {
            try
            {
                //TicketPrinter ticketPrinter = new TicketPrinter();
                var preference = await GetPreferenceData();

                if (preference == null)
                {
                    _logger.LogError("No se pudo obtener la información de Preference.");
                    return;
                }

                TicketPrinter ticketPrinter = new TicketPrinter();

                // 🔥 Convertir Base64 a Imagen
                if (!string.IsNullOrEmpty(preference.Logo))
                {
                    try
                    {
                        byte[] imageBytes = Convert.FromBase64String(preference.Logo.Split(',')[1]); // Removemos el prefijo "data:image/png;base64,"
                        using (MemoryStream ms = new MemoryStream(imageBytes))
                        {
                            Image logo = Image.FromStream(ms);
                            ticketPrinter.AddImage(logo);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al procesar el logo en Base64: {ex.Message}");
                    }
                }


                //// Agregar logotipo
                //string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                //string resourcesDirectory = Path.Combine(Directory.GetParent(baseDirectory).FullName, "Resources");
                //string logoPath = Path.Combine(resourcesDirectory, "logo.png");
                //Image logo = Image.FromFile(logoPath);
                //ticketPrinter.AddImage(logo);

                // 🔥 **Diseño según tipo de impresión**
                if (type == 0 || type == 2)
                {
                    ticketPrinter.AddLine("ORDEN", new Font("Arial", 12, FontStyle.Bold), TextAlign.Center);
                    ticketPrinter.AddLine(preference.Name + "," + preference.Company, new Font("Arial", 10, FontStyle.Bold), TextAlign.Center);
                    ticketPrinter.AddLine(preference.Address1, new Font("Arial", 8), TextAlign.Center);
                    ticketPrinter.AddLine("Tel." + preference.Phone, new Font("Arial", 8), TextAlign.Center);
                    ticketPrinter.AddLine("RNC:" + preference.RNC, new Font("Arial", 8), TextAlign.Center);
                    ticketPrinter.AddEmptyLine();

                    ticketPrinter.AddLine(order.tipoFactura, new Font("Arial", 8, FontStyle.Bold), TextAlign.Center);
                    ticketPrinter.AddLine(order.factura, new Font("Arial", 8), TextAlign.Center);
                    ticketPrinter.AddLine("Fecha: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt"), new Font("Arial", 8), TextAlign.Center);
                    ticketPrinter.AddEmptyLine();

                    ticketPrinter.AddLine("--------------------------------------------------------");

                    ticketPrinter.AddColumns(new[] { "Cliente:", order.customerName, "Mesa:", order.table },
                                 new[] { 50f, 90f, 50f, 90f },
                                 new Font("Arial", 8, FontStyle.Bold),
                                 new[] { TextAlign.Left, TextAlign.Left, TextAlign.Left, TextAlign.Left });


                    ticketPrinter.AddColumns(new[] { "RNC:", order.customerRNC, "Orden:", order.order },
                                 new[] { 50f, 90f, 50f, 90f },
                                 new Font("Arial", 8, FontStyle.Bold),
                                 new[] { TextAlign.Left, TextAlign.Left, TextAlign.Left, TextAlign.Left });

                    ticketPrinter.AddColumns(new[] { "Factura:", order.factura, "Cajero:", order.cajero },
                                 new[] { 50f, 90f, 50f, 90f },
                                 new Font("Arial", 8, FontStyle.Bold),
                                 new[] { TextAlign.Left, TextAlign.Left, TextAlign.Left, TextAlign.Left });

                    

                    ticketPrinter.AddEmptyLine();

                    ticketPrinter.AddEmptyLine();
                }
                else if (type == 1)
                {
                    ticketPrinter.AddLine("COCINA", new Font("Arial", 12, FontStyle.Bold), TextAlign.Center);
                    ticketPrinter.AddLine($"#{order.order}", new Font("Arial", 12, FontStyle.Bold), TextAlign.Center);
                    ticketPrinter.AddLine(order.delivery.ToUpper(), new Font("Arial", 10, FontStyle.Bold), TextAlign.Center);
                    ticketPrinter.AddEmptyLine();
                    ticketPrinter.AddLine("Fecha: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt"), new Font("Arial", 8), TextAlign.Center);
                    ticketPrinter.AddLine("--------------------------------------------------------");
                    ticketPrinter.AddColumns(new[] { "M:", order.table, "Camarero:", order.camarero },
                                            new[] { 50f, 90f, 50f, 90f },
                                            new Font("Arial", 8, FontStyle.Bold),
                                            new[] { TextAlign.Left, TextAlign.Left, TextAlign.Left, TextAlign.Left });
                    ticketPrinter.AddLine("--------------------------------------------------------");

                    ticketPrinter.AddEmptyLine();
                }

                ticketPrinter.AddEmptyLine();

                // 🔥 **Impresión de Ítems según tipo de ticket**
                if (type == 1)
                {
                    // Categorizar los platillos por tipo (Ej: Entrada, Plato Fuerte, etc.)
                    string currentCategory = "";
                    foreach (var item in order.items)
                    {
                       

                        ticketPrinter.AddColumns(new[] { item.qty, item.nombre },
                                                 new[] { 30f, 200f },
                                                 new Font("Arial", 8, FontStyle.Bold),
                                                 new[] { TextAlign.Center, TextAlign.Left });

                        if (!string.IsNullOrEmpty(item.opciones))
                        {
                            ticketPrinter.AddLine(item.opciones, new Font("Arial", 7, FontStyle.Italic), TextAlign.Left);
                        }
                    }
                }
                else
                {
                    ticketPrinter.AddLine("--------------------------------------------------------");
                    // Impresión para Ticket de Orden normal
                    ticketPrinter.AddColumns(new[] { "Descripción", "Cant.", "Precio", "Total" },
                                             new[] { 110f, 30f, 60f, 60f },
                                             new Font("Arial", 7, FontStyle.Bold),
                                             new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });

                    ticketPrinter.AddLine("--------------------------------------------------------");

                    foreach (PrintOrderItemModel item in order.items)
                    {
                        decimal total = 0.0M;                        
                        string amountString = Regex.Replace(item.amount, @"[^\d.,]", "").Trim();
                        if (decimal.TryParse(amountString, out decimal amount))
                        {
                            if (int.TryParse(item.qty, out int quantity))
                            {
                                total = amount * quantity;                                
                            }
                        }

                        ticketPrinter.AddColumns(new[] { item.nombre, item.qty, amount.ToString("C"), total.ToString("C") },
                                                 new[] { 110f, 30f, 60f, 60f },
                                                 new Font("Arial", 7, FontStyle.Regular),
                                                 new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                    }
                    ticketPrinter.AddLine("--------------------------------------------------------");
                    decimal subtotal = decimal.TryParse(CleanCurrency(order.subtotal), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal sub) ? sub : 0M;
                    decimal discount = decimal.TryParse(CleanCurrency(order.descuento), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal disc) ? disc : 0M;
                    decimal tax = decimal.TryParse(CleanCurrency(order.tax), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal t) ? t : 0M;
                    decimal tip = decimal.TryParse(CleanCurrency(order.propina), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tipAmount) ? tipAmount : 0M;
                    decimal delivery = decimal.TryParse(CleanCurrency(order.delivery), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal del) ? del : 0M;
                    decimal total2 = decimal.TryParse(CleanCurrency(order.total), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tot) ? tot : 0M;

                    // **Resumen de costos**
                    //ticketPrinter.AddColumns(new[] { "Alimentos:", "$" + order.subtotal.ToString("0.00") }, new[] { 110f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Right });
                    //ticketPrinter.AddColumns(new[] { "Alcohol:", "$" + order.alcoholTotal.ToString("0.00") }, new[] { 110f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Right });
                    ticketPrinter.AddColumns(new[] { "", "Sub Total:", subtotal.ToString("C") },
                                             new[] { 110f, 80f, 70f },
                                             new Font("Arial", 7, FontStyle.Bold),
                                             new[] { TextAlign.Left, TextAlign.Left, TextAlign.Right });

                    ticketPrinter.AddColumns(new[] {"", "Descuento:", discount.ToString("C") },
                                             new[] { 110f, 80f, 70f },
                                             new Font("Arial", 5, FontStyle.Bold),
                                             new[] { TextAlign.Left, TextAlign.Left, TextAlign.Right });

                    ticketPrinter.AddColumns(new[] { "", "ITBIS 18%:", tax.ToString("C") },
                                             new[] { 110f, 80f, 70f },
                                             new Font("Arial", 5, FontStyle.Bold),
                                             new[] { TextAlign.Left, TextAlign.Left, TextAlign.Right });

                    ticketPrinter.AddColumns(new[] { "", "10% Propina:", tip.ToString("C") },
                                             new[] { 110f, 80f, 70f },
                                             new Font("Arial", 5, FontStyle.Bold),
                                             new[] { TextAlign.Left, TextAlign.Left, TextAlign.Right });

                    ticketPrinter.AddColumns(new[] { "", "Domicilio:", delivery.ToString("C") },
                                             new[] { 110f, 80f, 70f },
                                             new Font("Arial", 5, FontStyle.Bold),
                                             new[] { TextAlign.Left, TextAlign.Left, TextAlign.Right });

                    ticketPrinter.AddLine("--------------------------------------------------------");

                    ticketPrinter.AddColumns(new[] { "", "Total:", total2.ToString("C") },
                                             new[] { 110f, 80f, 70f },
                                             new Font("Arial", 7, FontStyle.Bold),
                                             new[] { TextAlign.Left, TextAlign.Left, TextAlign.Right });

                    



                    ticketPrinter.AddEmptyLine();

                    // **Detalles de pago**
                    //ticketPrinter.AddColumns(new[] { "Pagado:", "$" + order..ToString("0.00") }, new[] { 110f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Right });
                    //ticketPrinter.AddColumns(new[] { "Cambio:", "$" + order.change.ToString("0.00") }, new[] { 110f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Right });
                    //ticketPrinter.AddColumns(new[] { "Forma:", order.paymentMethod }, new[] { 110f, 60f }, new Font("Arial", 7, FontStyle.Bold), new[] { TextAlign.Left, TextAlign.Right });

                  
                }

                ticketPrinter.AddEmptyLine();
                ticketPrinter.AddLine("Le Atendió: " + order.camarero, new Font("Arial", 8, FontStyle.Bold), TextAlign.Center);

                ticketPrinter.AddEmptyLine();
                ticketPrinter.AddLine("¡Gracias por su compra!", new Font("Arial", 10, FontStyle.Italic), TextAlign.Center);
                ticketPrinter.AddEmptyLine();
                ticketPrinter.AddLine("--------------------------------------------------------");
                ticketPrinter.AddEmptyLine();


                // Imprimir

                ticketPrinter.Print(printerName);

                //cortar papel
                string GS = Convert.ToString((char)29);
                string ESC = Convert.ToString((char)27);
                string COMMAND = "";
                COMMAND = ESC + "@";
                COMMAND += GS + "V" + (char)48;
                RawPrinterHelper.SendStringToPrinter(printerName, COMMAND);
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

