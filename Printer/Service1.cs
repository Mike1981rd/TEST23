using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Printer.Services;

namespace Printer
{
    public partial class Service1 : ServiceBase
    {
        private PrinterService _printerService;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _printerService = new PrinterService();
            _printerService.StartService(); // Inicia el servicio de impresión
        }

        protected override void OnStop()
        {
            _printerService?.StopService(); // Detiene el servicio de impresión si está en ejecución
        }
    }
}
