using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Printer.Converters;
using DinkToPdf.Contracts;
using DinkToPdf;
using Printer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Printer
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseWindowsService() // Necesario para que funcione como un servicio de Windows
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IConverter, PdfConverter>();
                    services.AddHttpClient();
                    services.AddHostedService<PrinterService>();
                })
                .Build();

            host.Run();
        }

    }
}
