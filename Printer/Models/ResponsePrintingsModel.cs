using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Printer.Models
{
    public class ResponsePrintingsModel
    {
        public List<PrintModel> Impresiones { get; set; }
        public int Cantidad { get; set; }
    }

    public class PrintModel
    {
        public long Id { get; set; }
        public string Html { get; set; }
        public string Impresora { get; set; }
    }
}
