using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Printer.Models
{

    //public class PrintModel
    //{
    //    public long Id { get; set; }
    //    public string Html { get; set; }
    //    public string Impresora { get; set; }
    //}
    public class UpdatePrintJobRequest
    {
        [JsonPropertyName("Id")]
        public long Id { get; set; }
    }
}