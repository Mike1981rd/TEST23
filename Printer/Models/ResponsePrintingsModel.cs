using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Printer.Models
{

    //public class PrintModel
    //{
    //    public long Id { get; set; }
    //    public string Html { get; set; }
    //    public string Impresora { get; set; }
    //}
    public class ResponsePrintingsModel
    {
        public List<PrintModel> lista { get; set; }
        public int cantidad { get; set; }
    }

    public class PrintModel
    {
        public long id { get; set; }
        public long objectId { get; set; }
        public int type { get; set; }
        public string physicalName { get; set; }
        public long stationId { get; set; }
        public long sucursalId { get; set; }
        public PrintOrderModel printJobOrder { get; set; }
    }

    public class PrintOrderModel
    {
        public string titulo { get; set; }
        public string empresa { get; set; }
        public string empresa1 { get; set; }
        public string order { get; set; }
        public string table { get; set; }
        public string person { get; set; }
        public string camarero { get; set; }
        public string rnc { get; set; }
        public string devuelto { get; set; }
        public string propina { get; set; }
        public string delivery { get; set; }
        public string promesaPagoMonto { get; set; }
        public string promesaPagoDevuelto { get; set; }
        public string subtotal { get; set; }
        public string descuento { get; set; }
        public string descuentos { get; set; }
        public string tax { get; set; }
        public string taxes { get; set; }
        public string taxes1 { get; set; }
        public string total { get; set; }
        public string? type { get; set; }
        public string? customerRNC { get; set; }
        public string? customerName { get; set; }
        public string? customerAddress { get; set; }
        public string? customerPhone { get; set; }
        public string? factura { get; set; }
        public string? tipoFactura { get; set; }
        public string? cajero { get; set; }
        public List<PrintOrderItemModel>? items { get; set; }

    }

    public class PrintOrderItemModel
    {
        public string nombre { get; set; }
        public string qty { get; set; }
        public string? opciones { get; set; }
        public string amount { get; set; }
    }
}