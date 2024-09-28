using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using DinkToPdf.Contracts;
using DinkToPdf;
using Printer.Services;
using System.Threading;
using Printer.Converters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Printer
{
    public partial class Service1 : ServiceBase
    {
        private IHost _host; // Almacenar el host

        public Service1()
        {
            InitializeComponent();
            // Inicializar el host
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IConverter, PdfConverter>(); // Registrar el conversor
                    services.AddHostedService<PrinterService>(); // Registrar PrinterService
                })
                .Build();
        }

        protected override void OnStart(string[] args)
        {
            // Iniciar el host
            _host.StartAsync().GetAwaiter().GetResult();
        }

        protected override void OnStop()
        {
            // Detener el host
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }
    }

}
