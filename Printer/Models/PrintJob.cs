﻿namespace Printer.Models
{
    public class PrintJob
    {
        public string JobId { get; set; }
        public string PrinterName { get; set; }
        public string HtmlContent { get; set; }
        public string Status { get; set; } // Pendiente, En Progreso, Impreso, etc.
    }
}
