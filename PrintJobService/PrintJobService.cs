using System;
using System.Collections.Generic;
using System.Net.Http;
using System.ServiceProcess;
using System.Text.Json;
using System.Threading;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace PrintJobService
{
    public partial class PrintJobService : ServiceBase
    {
        private Timer _timer;
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "http:/api/GetPendingPrintJobs";

        public PrintJobService()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
        }

        protected override void OnStart(string[] args)
        {
            _timer = new Timer(async _ => await CheckForPrintJobs(), null, 0, 2000); // Ejecutar cada 2 segundos
        }

        protected override void OnStop()
        {
            _timer?.Dispose();
        }

        private async Task CheckForPrintJobs()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(ApiUrl);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    PrintModel printModel = JsonSerializer.Deserialize<PrintModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new PrintModel();

                    if (printModel?.Lista != null && printModel.Lista.Count > 0)
                    {
                        foreach (var job in printModel.Lista)
                        {
                            await PrintToPrinter(job.PhysicalName, job.printJobOrder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("C:\\Logs\\PrintService.log", $"{DateTime.Now}: Error - {ex.Message}\n");
            }
        }

        private async Task PrintToPrinter(string printerName, PrintOrderModel order)
        {
            try
            {
                TicketPrinter ticketPrinter = new TicketPrinter();
                ticketPrinter.SetPrinter(printerName);

                // Encabezado
                ticketPrinter.AddLine(order.Empresa + ", " + order.Empresa1, new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold), TextAlign.Center);
                ticketPrinter.AddLine("Fecha: " + DateTime.Now.ToString(), new System.Drawing.Font("Arial", 8), TextAlign.Center);
                ticketPrinter.AddEmptyLine();

                // Columnas
                ticketPrinter.AddColumns(new[] { "Descripción", "Cant.", "Precio", "Total" },
                                         new[] { 110f, 30f, 60f, 60f },
                                         new System.Drawing.Font("Arial", 7, System.Drawing.FontStyle.Bold),
                                         new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });

                ticketPrinter.AddLine("--------------------------------------------------------");

                // Agregar productos
                foreach (var item in order.Items)
                {
                    decimal total = decimal.Parse(item.Amount) * int.Parse(item.Qty);
                    ticketPrinter.AddColumns(new[] { item.Nombre, item.Qty, item.Amount, total.ToString("C") },
                                             new[] { 110f, 30f, 60f, 60f },
                                             new System.Drawing.Font("Arial", 7, System.Drawing.FontStyle.Regular),
                                             new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });
                }

                ticketPrinter.AddLine("--------------------------------------------------------");

                // Totales
                ticketPrinter.AddColumns(new[] { "Subtotal", "", "", order.Subtotal },
                                         new[] { 110f, 30f, 60f, 60f },
                                         new System.Drawing.Font("Arial", 7, System.Drawing.FontStyle.Bold),
                                         new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });

                ticketPrinter.AddColumns(new[] { "Taxes", "", "", order.Taxes1 },
                                         new[] { 110f, 30f, 60f, 60f },
                                         new System.Drawing.Font("Arial", 7, System.Drawing.FontStyle.Bold),
                                         new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });

                ticketPrinter.AddColumns(new[] { "Total", "", "", order.Total },
                                         new[] { 110f, 30f, 60f, 60f },
                                         new System.Drawing.Font("Arial", 7, System.Drawing.FontStyle.Bold),
                                         new[] { TextAlign.Left, TextAlign.Center, TextAlign.Right, TextAlign.Right });

                // Mensaje de agradecimiento
                ticketPrinter.AddLine("¡Gracias por su compra!", new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Italic), TextAlign.Center);

                // Imprimir
                ticketPrinter.Print(printerName);
            }
            catch (Exception ex)
            {
                File.AppendAllText("C:\\Logs\\PrintService.log", $"{DateTime.Now}: Error de impresión - {ex.Message}\n");
            }
        }
    }
}
