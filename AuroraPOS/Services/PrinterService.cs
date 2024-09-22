using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace AuroraPOS.Services
{
    public class PrinterService
    {
        // Método para procesar la impresión
        public async Task ProcessPrintJob(string printerName, string htmlContent)
        {
            string filePath = await GeneratePdfFromHtml(htmlContent);
            bool result = PrintToPrinter(printerName, filePath);

            if (result)
            {
                Console.WriteLine($"Impresión completada en {printerName}.");
            }
            else
            {
                Console.WriteLine($"Error al imprimir en {printerName}.");
            }
        }


        // Generar PDF desde contenido HTML
        private async Task<string> GeneratePdfFromHtml(string htmlContent)
        {
            await new BrowserFetcher().DownloadAsync();

            var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            await page.SetContentAsync(htmlContent);

            string filePath = $"C:\\temp\\printjob_{Guid.NewGuid()}.pdf";
            await page.PdfAsync(filePath);

            await browser.CloseAsync();
            return filePath;
        }


        // Imprimir el archivo PDF en la impresora especificada
        private bool PrintToPrinter(string printerName, string filePath)
        {
            try
            {
                string command;

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    // Lógica para Windows
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = filePath, // PDF a imprimir
                        Verb = "print",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments = $"\"{printerName}\"",
                    };
                    Process printProcess = Process.Start(psi);

                    if (printProcess != null)
                    {
                        printProcess.WaitForExit();
                        return true;
                    }
                    return false;
                }
                else if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    // Lógica para Linux/macOS
                    command = $"lp -d {printerName} {filePath}";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{command}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process process = Process.Start(psi);
                    process.WaitForExit();

                    return process.ExitCode == 0;
                }
                else
                {
                    throw new NotSupportedException("Sistema operativo no soportado.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al imprimir en la impresora {printerName}: {ex.Message}");
                return false;
            }
        }
    }

}


