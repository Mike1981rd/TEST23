using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PrintJobService
{
    public class PrintModel
    {
        public List<PrintJobModel> Lista { get; set; } = new List<PrintJobModel>();
        public int Cantidad { get; set; }
    }

    public class PrintJobModel
    {
        public long Id { get; set; }
        public string PhysicalName { get; set; }
        public PrintOrderModel printJobOrder { get; set; }
    }

    public class PrintOrderModel
    {
        public string Empresa { get; set; }
        public string Empresa1 { get; set; }
        public string Order { get; set; }
        public string Table { get; set; }
        public string Camarero { get; set; }
        public string Subtotal { get; set; }
        public string Taxes1 { get; set; }
        public string Propina { get; set; }
        public string Total { get; set; }
        public string Devuelto { get; set; }
        public List<PrintOrderItemModel> Items { get; set; } = new List<PrintOrderItemModel>();
    }

    public class PrintOrderItemModel
    {
        public string Nombre { get; set; }
        public string Qty { get; set; }
        public string Amount { get; set; }
    }
}
