//extern alias SK;
using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.ViewModels;
using iText.Kernel.Pdf;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Svg.Renderers.Impl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using OpenTK.Input;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Newtonsoft.Json;

namespace AuroraPOS.Services
{
    public interface IPrintService
    {
        void PrintKitchenItems(long stationId, long OrderID, List<OrderItem> items);
        bool PrintOrder(long stationId, long OrderID, int DivideNum, int SeatNum);
        bool PrintPaymentSummary(long stationId, long OrderID, int SeatNum, int DividerNum, bool reprint);
        bool PrintCxCSummary(long stationId, List<long> ReferenceIds, int SeatNum, int DividerNum, bool reprint);
        bool PrintCloseDrawerSummary(long stationId, CloseDrawerPrinterModel model);
        void PrintKitchenOrderItems(long kitchenId, long OrderID, List<OrderItem> items);
        bool PrintRecibo(long stationId, long ReciboID, string empresa, bool reprint);


    }

    public class PrintService : IPrintService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private const int LOGOWIDTH = 100;
        private readonly AppDbContext _dbContext;
        public PrintService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public bool PrintRecibo(long stationId, long ReciboID, string empresa, bool reprint)
        {

            var preference = _dbContext.Preferences.First();
            var order = _dbContext.Orders.Include(s => s.Table).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).FirstOrDefault(o => o.ID == ReciboID);
            var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
            order.GetTotalPrice(voucher);
            var items = order.Items.Where(s => !s.IsDeleted).ToList();
            var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order).ToList();

            SeatItem seat = null;
            var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);

            var station = _dbContext.Stations.Include(s => s.Printers).ThenInclude(s => s.PrinterChannel).Include(s => s.Printers).ThenInclude(s => s.Printer).FirstOrDefault(s => s.ID == stationId);
            var defaultPrinter = station.Printers.FirstOrDefault(s => s.PrinterChannel.IsDefault);

            DataSet ds = new DataSet("general");
            ds.Tables.Add("general");
            ds.Tables[0].Columns.Add("f_nombre");
            ds.Tables[0].Columns.Add("f_Qty");
            ds.Tables[0].Columns.Add("f_opciones");
            ds.Tables[0].Columns.Add("f_amt", typeof(float));
            foreach (var printerItem in items)
            {
                Debug.WriteLine(printerItem.Name);
                Debug.WriteLine(printerItem.Qty);
                Debug.WriteLine(printerItem.ID);
                Debug.WriteLine(printerItem);
                DataRow dr = ds.Tables[0].NewRow();
                var servingSize = "";
                if (!string.IsNullOrEmpty(printerItem.ServingSizeName))
                {
                    servingSize = $"({printerItem.ServingSizeName})";
                }


                dr["f_nombre"] = printerItem.Name + servingSize;
                dr["f_Qty"] = printerItem.Qty;
                dr["f_amt"] = printerItem.SubTotal;
                string srtOption = string.Empty;
                DivisorType divisor = 0;
                var questions = printerItem.Questions.OrderBy(s => s.Divisor).ToList();
                foreach (var q in questions)
                {
                    if (q.Divisor != divisor)
                    {
                        if (q.Divisor == DivisorType.Whole)
                        {
                            srtOption += "Completa :" + (char)10;
                        }
                        else if (q.Divisor == DivisorType.FirstHalf)
                        {
                            srtOption += "1 Mitad :" + (char)10;
                        }
                        else if (q.Divisor == DivisorType.SecondHalf)
                        {
                            srtOption += "2 Mitad :" + (char)10;
                        }

                        divisor = q.Divisor;
                    }

                    var strOption = "";
                    if (q.IsPreSelect)
                    {
                        if (!q.IsActive)
                        {
                            srtOption += q.Description + (char)10;
                        }
                    }
                    else
                    {
                        srtOption += q.Description + (char)10;
                        if (!string.IsNullOrEmpty(q.SubDescription))
                        {
                            strOption += " *" + q.SubDescription + (char)10;
                        }
                    }
                }
                if (srtOption.Length > 0)
                {
                    dr["f_opciones"] = srtOption.Substring(0, srtOption.Length - 1);
                }
                ds.Tables[0].Rows.Add(dr);

            }


            if (defaultPrinter != null && defaultPrinter.Printer != null)
            {
                var printer = _dbContext.Printers.First();
                if (printer != null)
                {
                    try
                    {

                        #region imprimiendo Sumary
                        //-----------------imprimiento la comanda------------//
                        /*
                        FastReport.Report Report1 = new FastReport.Report();
                        Report1.Load(GetReporte(6, 1)); //formato de recibo.
                        Report1.RegisterData(ds, "general");
                        Report1.GetDataSource("general").Enabled = true;

                        FastReport.DataBand data1 = (FastReport.DataBand)Report1.FindObject("Data1");
                        data1.DataSource = Report1.GetDataSource("general");
                        Report1.SetParameterValue("titulo", "Recibo");
                        Report1.SetParameterValue("empresa", preference.Name);
                        Report1.SetParameterValue("empresa1", preference.Company);
                        Report1.SetParameterValue("recibo", order.ID.ToString());

                        Report1.SetParameterValue("telefono", preference.Phone);
                        Report1.SetParameterValue("rnc", preference.RNC);
                        if (customer != null)
                        {
                            Report1.SetParameterValue("customerRNC", customer.RNC);
                            Report1.SetParameterValue("customername", customer.Name);
                        }

                        string metodo = string.Empty;
                        string cantidadp = string.Empty;
                        decimal paidAmount = 0;
                        decimal difference = 0;
                        foreach (var t in transactions)
                        {
                            metodo += t.Method + (char)10;
                            cantidadp += (t.Amount + t.Difference).ToString("0.00", CultureInfo.InvariantCulture) + (char)10;
                            paidAmount += t.Amount;
                            difference += t.Difference;
                            Console.WriteLine((t.Amount).ToString("0.00", CultureInfo.InvariantCulture) + " Cantidad dp");
                        }
                        if (metodo.Length > 0)
                        {
                            metodo = metodo.Substring(0, metodo.Length - 1);
                            cantidadp = cantidadp.Substring(0, cantidadp.Length - 1);
                        }
                        
                        Report1.SetParameterValue("metodo", metodo);
                        Report1.SetParameterValue("cantidaddp", cantidadp);
                        string descuentos = string.Empty;
                        string espacio = "    ";
                        foreach (var d in order.Discounts)
                        {
                            descuentos += d.Name + espacio + d.Amount + (char)10;
                        }
                        if (descuentos.Length > 0) { descuentos = descuentos.Substring(0, descuentos.Length - 1); }
                        Report1.SetParameterValue("descuentos", descuentos);
                        Report1.SetParameterValue("tax", order.Tax);
                        string taxes = string.Empty;
                        string taxes1 = string.Empty;
                        foreach (var d in order.Taxes)
                        {
                            taxes += d.Description + (char)10;
                            taxes1 += d.Amount.ToString("0.00", CultureInfo.InvariantCulture) + (char)10;

                        }
                        if (taxes.Length > 0)
                        {
                            taxes = taxes.Substring(0, taxes.Length - 1);
                            taxes1 = taxes1.Substring(0, taxes1.Length - 1);

                        }

                        Report1.SetParameterValue("total", order.TotalPrice);
                        Report1.SetParameterValue("devuelto", transactions.Sum(s => s.Difference));

                        if (transactions.Any())
                        {
                            Report1.SetParameterValue("cajero", transactions.First().CreatedBy);
                        }

                        var stream = new MemoryStream();
                        Report1.Prepare();
                        string nombre = "H" + GetHashCode().ToString() + "T" + DateTime.Now.Ticks + "Factura.fpx";
                        Report1.SavePrepared(stream);
                        GuardaImpresion(nombre, ".fpx", stream.ToArray(), defaultPrinter.Printer.PhysicalName, stationId.ToString(), 1 + station.PrintCopy, 1, station.IDSucursal, reprint);

                        */
                        #endregion
                        return true;
                    }
                    catch (Exception ex)
                    {
                        var m = 1;
                    }
                }
            }
            return false;
        }


        public void PrintKitchenItems(long stationId, long OrderID, List<OrderItem> items)
        {
            var order = _dbContext.Orders
                .Include(s => s.Station).ThenInclude(s => s.Printers)
                .Include(s => s.Area)
                .Include(s => s.Table)
                .Include(s => s.Taxes)
                .Include(s => s.Discounts)
                .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions)
                .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts)
                .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes)
                .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product)
                .FirstOrDefault(s => s.ID == OrderID);

            var station = _dbContext.Stations
                .Include(s => s.Printers).ThenInclude(s => s.PrinterChannel)
                .Include(s => s.Printers).ThenInclude(s => s.Printer)
                .FirstOrDefault(s => s.ID == stationId);

            var defaultPrinter = station?.Printers?.FirstOrDefault(s => s.PrinterChannel.IsDefault);

            if (defaultPrinter != null && defaultPrinter.Printer != null)
            {
                // 🚀 **Generar un nuevo job de impresión en la base de datos**

                var lstItemsIds = new List<long>();

                foreach (var item in items)
                {
                    lstItemsIds.Add(item.ID);
                }
                
                var objPrint = new PrinterTasks
                {
                    ObjectID = OrderID,
                    Status = (int)PrinterTasksStatus.Pendiente,
                    Type = (int)PrinterTasksType.TicketCocina, // Tipo de impresión para cocina
                    PhysicalName = defaultPrinter.Printer.PhysicalName,
                    StationId = stationId,
                    SucursalId = station.IDSucursal,
                    Items = JsonConvert.SerializeObject(lstItemsIds)
                };
                
                //string json = JsonConvert.SerializeObject(items);
                //items = JsonConvert.DeserializeObject<List<OrderItem>>(json);

                /*
                foreach (var printerItem in printerItems)
                        {
                            DataSet ds = new DataSet("general");
                            ds.Tables.Add("general");
                            ds.Tables[0].Columns.Add("f_nombre");
                            ds.Tables[0].Columns.Add("f_Qty");
                            ds.Tables[0].Columns.Add("f_opciones");
                            var pitems = printerItem.Value.OrderBy(s => s.CourseID);
                            long course = 0;
                            foreach (var item in pitems)
                            {
                                Debug.WriteLine(item.Name);
                                Debug.WriteLine(item.Qty);

                                if (course != item.CourseID)
                                {
                                    DataRow dr1 = ds.Tables[0].NewRow();
                    dr1["f_nombre"] = " - " + item.Course;
                            dr1["f_nombre"] = (char)10 + "       *** " + item.Course.ToUpper() + " *** " + (char)10;


                            ds.Tables[0].Rows.Add(dr1);
                            course = item.CourseID;
                    }*/

                _dbContext.PrinterTasks.Add(objPrint);
                _dbContext.SaveChanges(); // Guardar el job en la BD

            }
        }


        //public void PrintKitchenItems(long stationId, long OrderID, List<OrderItem> items, string empresa)
        //{
        //    if (!Directory.Exists("temp1"))
        //        Directory.CreateDirectory("temp1");
        //    var preference = _dbContext.Preferences.First();
        //    var order = _dbContext.Orders.Include(s => s.Station).ThenInclude(s => s.Printers).Include(s => s.Area).Include(s => s.Table).Include(s => s.Taxes).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).FirstOrDefault(s => s.ID == OrderID);
        //    var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);//cliente
        //    var station = _dbContext.Stations.Include(s => s.Printers).ThenInclude(s => s.PrinterChannel).Include(s => s.Printers).ThenInclude(s => s.Printer).FirstOrDefault(s => s.ID == stationId);
        //    var printerItems = GetPrintersOfItems(items);

        //    foreach (var printerItem in printerItems)
        //    {
        //        DataSet ds = new DataSet("general");
        //        ds.Tables.Add("general");
        //        ds.Tables[0].Columns.Add("f_nombre");
        //        ds.Tables[0].Columns.Add("f_Qty");
        //        ds.Tables[0].Columns.Add("f_opciones");
        //        var pitems = printerItem.Value.OrderBy(s => s.CourseID);
        //        long course = 0;
        //        foreach (var item in pitems)
        //        {
        //            Debug.WriteLine(item.Name);
        //            Debug.WriteLine(item.Qty);

        //            if (course != item.CourseID)
        //            {
        //                DataRow dr1 = ds.Tables[0].NewRow();
        //dr1["f_nombre"] = " - " + item.Course;
        //        dr1["f_nombre"] = (char)10 + "       *** " + item.Course.ToUpper() + " *** " + (char)10;


        //        ds.Tables[0].Rows.Add(dr1);
        //        course = item.CourseID;
        //    }

        //    var name = item.Product?.Printer;
        //    if (string.IsNullOrEmpty(name))
        //    {
        //        name = item.Name;
        //    }
        //    var servingSize = "";
        //    if (!  string.IsNullOrEmpty(item.ServingSizeName)) 
        //    {
        //        servingSize = $"({item.ServingSizeName})";
        //    }

        //    DataRow dr = ds.Tables[0].NewRow();
        //    dr["f_nombre"] = name + servingSize;
        //    dr["f_Qty"] = item.Qty;
        //    string srtOption = string.Empty;
        //    DivisorType divisor = 0;
        //    var questions = item.Questions.OrderBy(s => s.Divisor).ToList();
        //    foreach (var q in questions)
        //    {                        
        //        if (q.Divisor != divisor)
        //        {
        //            if (q.Divisor == DivisorType.Whole)
        //            {
        //                srtOption +=  "Completa :" + (char)10;
        //            }
        //            else if (q.Divisor == DivisorType.FirstHalf) {
        //                srtOption += "1 Mitad :" + (char)10;
        //            }
        //            else if (q.Divisor == DivisorType.SecondHalf)
        //            {
        //                srtOption += "2 Mitad :" + (char)10;
        //            }

        //            divisor = q.Divisor;
        //        }

        //        var strOption = "";
        //        if (q.IsPreSelect)
        //        {
        //            if (!q.IsActive)
        //            {
        //                srtOption += q.Description + (char)10;
        //            }
        //        }
        //        else
        //        {
        //            srtOption += q.Description + (char)10;
        //            if (!string.IsNullOrEmpty(q.SubDescription))
        //            {
        //                strOption += " *" + q.SubDescription + (char)10;
        //            }
        //        }                                               
        //    }
        //    if (srtOption.Length > 0)
        //    {
        //        dr["f_opciones"] = srtOption.Substring(0, srtOption.Length - 1);
        //    }
        //    ds.Tables[0].Rows.Add(dr);

        //    if (!string.IsNullOrEmpty(item.Note))
        //    {
        //        DataRow dr1 = ds.Tables[0].NewRow();
        //        dr1["f_nombre"] = " NOTE:" + item.Note;

        //        ds.Tables[0].Rows.Add(dr1);
        //    }
        //}

        //var printer = printerItem.Key;
        //var realprinter = station.Printers.FirstOrDefault(s => s.PrinterChannel == printer);
        //if (realprinter == null || realprinter.Printer == null) continue;
        //try
        //{
        //    Console.WriteLine("Creadno documento");
        /*
                            FastReport.Report Report1 = new FastReport.Report();
                            Report1.Load(GetReporte(2, 1));
                            Report1.RegisterData(ds, "general");
                            Report1.GetDataSource("general").Enabled = true;

                            FastReport.DataBand data1 = (FastReport.DataBand)Report1.FindObject("Data1");
                            data1.DataSource = Report1.GetDataSource("general");
                            Report1.SetParameterValue("titulo", printer.Name);
                            Report1.SetParameterValue("empresa", preference.Name);
                            Report1.SetParameterValue("order", order.ID.ToString());
                            string tableName = order.Table != null ? order.Table.Name : "";
                            Report1.SetParameterValue("table", tableName);
                            Report1.SetParameterValue("person", order.Person.ToString());
                            Report1.SetParameterValue("camarero", order.WaiterName);

                            if (customer != null)
                            {
                                Report1.SetParameterValue("customerRNC", customer.RNC);
                                Report1.SetParameterValue("customername", customer.Name);
                                Report1.SetParameterValue("customerAddress", customer.Address1);
                            }

                            if (order.OrderType != OrderType.Delivery)
                            {
                                Report1.SetParameterValue("oder_type", order.OrderType.ToString());
                            }
                            else
                            {
                                var prepareType = _dbContext.PrepareTypes.FirstOrDefault(s => s.ID == order.PrepareTypeID);
                                if (prepareType != null) {
                                    Report1.SetParameterValue("oder_type", prepareType.Name);
                                }
                                else {
                                    Report1.SetParameterValue("oder_type", "Para llevar");
                                }                        
                            }
                            var stream = new MemoryStream();
                            Report1.Prepare();
                            string nombre = "H" + GetHashCode().ToString() + "T" + DateTime.Now.Ticks + "Comanda.fpx";
                            //Console.WriteLine((Path.Combine(ruta, nombre)));
                            //Report1.SavePrepared(Path.Combine(ruta, nombre));
                            Report1.SavePrepared(stream);
                            //Report1.Save(stream);

                            GuardaImpresion(nombre, ".fpx", stream.ToArray(), realprinter.Printer.PhysicalName, stationId.ToString(), 1, 1, station.IDSucursal, false);

                            */
        //        }
        //        catch (Exception c)
        //        {
        //            Console.WriteLine(c.Message);
        //        }
        //    }

        //}

        public void PrintKitchenOrderItems(long kitchenId, long OrderID, List<OrderItem> items)
        {
            if (!Directory.Exists("temp1"))
                Directory.CreateDirectory("temp1");
            var preference = _dbContext.Preferences.First();
            var order = _dbContext.Orders.Include(s => s.Station).ThenInclude(s => s.Printers).Include(s => s.Area).Include(s => s.Table).Include(s => s.Taxes).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).FirstOrDefault(s => s.ID == OrderID);
            var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);//cliente
            var kitchen = _dbContext.Kitchen.FirstOrDefault(s => s.ID == kitchenId);
            var printerItems = GetPrintersOfItems(items);

            foreach (var printerItem in printerItems)
            {
                DataSet ds = new DataSet("general");
                ds.Tables.Add("general");
                ds.Tables[0].Columns.Add("f_nombre");
                ds.Tables[0].Columns.Add("f_Qty");
                ds.Tables[0].Columns.Add("f_opciones");

                var pitems = printerItem.Value.OrderBy(s => s.Course).ToList();
                long course = 0;
                foreach (var item in pitems)
                {
                    if (course != item.CourseID)
                    {
                        DataRow dr1 = ds.Tables[0].NewRow();
                        dr1["f_nombre"] = (char)10 + "       *** " + item.Course.ToUpper() + " *** " + (char)10;

 //                       dr1["f_nombre"] = " - " + item.Course;
                        ds.Tables[0].Rows.Add(dr1);
                        course = item.CourseID;
                    }

                    //Debug.WriteLine(printerItem.Value[i].Name);
                    //Debug.WriteLine(printerItem.Value[i].Qty);
                    DataRow dr = ds.Tables[0].NewRow();

                    var name = item.Product?.Printer;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = item.Name;
                    }
                    var servingSize = "";
                    if (!string.IsNullOrEmpty(item.ServingSizeName))
                    {
                        servingSize = $"({item.ServingSizeName})";
                    }

                    dr["f_nombre"] = name + servingSize;
                    dr["f_Qty"] = item.Qty;
                    string srtOption = string.Empty;
                    DivisorType divisor = 0;
                    var questions = item.Questions.OrderBy(s=>s.Divisor).ToList();
                    foreach (var q in questions)
                    {
                        if (q.Divisor != divisor)
                        {
                            if (q.Divisor == DivisorType.Whole)
                            {
                                srtOption += "Completa :" + (char)10;
                            }
                            else if (q.Divisor == DivisorType.FirstHalf)
                            {
                                srtOption += "1 Mitad :" + (char)10;
                            }
                            else if (q.Divisor == DivisorType.SecondHalf)
                            {
                                srtOption += "2 Mitad :" + (char)10;
                            }

                            divisor = q.Divisor;
                        }

                        var strOption = "";
                        if (q.IsPreSelect)
                        {
                            if (!q.IsActive)
                            {
                                srtOption += q.Description + (char)10;
                            }
                        }
                        else
                        {
                            srtOption += q.Description + (char)10;
                            if (!string.IsNullOrEmpty(q.SubDescription))
                            {
                                strOption += " *" + q.SubDescription + (char)10;
                            }
                        }
                    }
                    if (srtOption.Length > 0)
                    {
                        dr["f_opciones"] = srtOption.Substring(0, srtOption.Length - 1);
                    }
                    ds.Tables[0].Rows.Add(dr);
                    if (!string.IsNullOrEmpty(item.Note))
                    {
                        DataRow dr1 = ds.Tables[0].NewRow();
                        dr1["f_nombre"] = " NOTE:" + item.Note;

                        ds.Tables[0].Rows.Add(dr1);
                        course = item.CourseID;
                    }
                }

                var printer = printerItem.Key;
                var realprinter = _dbContext.Printers.FirstOrDefault(s => s.ID == kitchen.PrinterID);
                if (realprinter == null ) continue;
                try
                {
                    Console.WriteLine("Creando documento");
/*
                    FastReport.Report Report1 = new FastReport.Report();
                    Report1.Load(GetReporte(2, 1));
                    Report1.RegisterData(ds, "general");
                    Report1.GetDataSource("general").Enabled = true;

                    FastReport.DataBand data1 = (FastReport.DataBand)Report1.FindObject("Data1");
                    data1.DataSource = Report1.GetDataSource("general");
                    Report1.SetParameterValue("titulo", printer.Name);
                    Report1.SetParameterValue("empresa", preference.Name);
                    Report1.SetParameterValue("order", order.ID.ToString());
                    string tableName = order.Table != null ? order.Table.Name : "";
                    Report1.SetParameterValue("table", tableName);
                    Report1.SetParameterValue("person", order.Person.ToString());
                    Report1.SetParameterValue("camarero", order.WaiterName);
                    
                    if (customer != null)
                    {
                        Report1.SetParameterValue("customerRNC", customer.RNC);
                        Report1.SetParameterValue("customername", customer.Name);
                        Report1.SetParameterValue("customerAddress", customer.Address1);
                    }


                    if (order.OrderType != OrderType.Delivery)
                    {
                        Report1.SetParameterValue("oder_type", order.OrderType.ToString());
                    }
                    else
                    {
                        var prepareType = _dbContext.PrepareTypes.FirstOrDefault(s => s.ID == order.PrepareTypeID);
                        if (prepareType != null)
                        {
                            Report1.SetParameterValue("oder_type", prepareType.Name);
                        }
                        else
                        {
                            Report1.SetParameterValue("oder_type", "Para llevar");
                        }
                    }
                    var stream = new MemoryStream();
                    Report1.Prepare();
                    string nombre = "H" + GetHashCode().ToString() + "T" + DateTime.Now.Ticks + "Comanda.fpx";
                    //Console.WriteLine((Path.Combine(ruta, nombre)));
                    //Report1.SavePrepared(Path.Combine(ruta, nombre));
                    Report1.SavePrepared(stream);
                    //Report1.Save(stream);

                    GuardaImpresion(nombre, ".fpx", stream.ToArray(), realprinter.PhysicalName, kitchenId.ToString(), 1, 1, 1, false);
                    
                    */
                }
                catch (Exception c)
                {
                    Console.WriteLine(c.Message);
                }
            }

        }

        private Dictionary<PrinterChannel, List<OrderItem>> GetPrintersOfItems(List<OrderItem> items)
        {
            var result = new Dictionary<PrinterChannel, List<OrderItem>>();

            foreach (var item in items)
            {
                if (item.IsDeleted) continue;
                var product = _dbContext.Products.Include(s => s.PrinterChannels).FirstOrDefault(s => s.ID == item.Product.ID);
                foreach (var p in product.PrinterChannels)
                {
                    if (result.ContainsKey(p))
                    {
                        // Verificar si el elemento de la orden ya existe en la lista del canal de impresora
                        if (!result[p].Contains(item))
                        {
                            result[p].Add(item);
                        }
                    }
                    else
                    {
                        result[p] = new List<OrderItem>() { item };
                    }
                }
            }

            return result;
        }

        public bool PrintOrder(long stationId, long OrderID, int DivideNum, int SeatNum)
        {
            var preference = _dbContext.Preferences.First();
            var order = _dbContext.Orders.Include(s => s.Taxes).Include(s => s.Table).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).FirstOrDefault(o => o.ID == OrderID);
            /*var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order);
            var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
            var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);
            order.GetTotalPrice(voucher, DivideNum, SeatNum);
            */
            
            var station = _dbContext.Stations.Include(s => s.Printers).ThenInclude(s => s.PrinterChannel).Include(s => s.Printers).ThenInclude(s => s.Printer).FirstOrDefault(s => s.ID == stationId);
            var defaultPrinter = station.Printers.FirstOrDefault(s => s.PrinterChannel.IsDefault);
            var items = order.Items.Where(s => !s.IsDeleted).ToList();

            if (defaultPrinter != null && defaultPrinter.Printer != null)
            {
                //INICIA: Manda a imprimir, nuevo metodo
                var objPrint = new PrinterTasks();
                objPrint.ObjectID = OrderID;
                objPrint.Status = (int)PrinterTasksStatus.Pendiente;
                objPrint.Type = (int)PrinterTasksType.TicketOrden;
                objPrint.PhysicalName = defaultPrinter.Printer.PhysicalName;
                objPrint.StationId = stationId;
                objPrint.SucursalId = station.IDSucursal;
                objPrint.DivideNum = DivideNum;
                objPrint.SeatNum = SeatNum;
                _dbContext.PrinterTasks.Add(objPrint);
                _dbContext.SaveChanges();
                //TERMINA: Manda a imprimir, nuevo metodo

                /*{
                    try
                    {
                        DataSet ds = new DataSet("general");
                        ds.Tables.Add("general");
                        ds.Tables[0].Columns.Add("f_nombre");
                        ds.Tables[0].Columns.Add("f_Qty");
                        ds.Tables[0].Columns.Add("f_opciones");
                        ds.Tables[0].Columns.Add("f_amt", typeof(float));
                        foreach (var printerItem in items)
                        {
                            if (DivideNum > 0 && DivideNum != printerItem.DividerNum)
                            {
                                continue;
                            }
							if (SeatNum > 0 && SeatNum != printerItem.SeatNum)
							{
								continue;
							}
                            var servingSize = "";
                            if (!string.IsNullOrEmpty(printerItem.ServingSizeName))
                            {
                                servingSize = $"({printerItem.ServingSizeName})";
                            }
                            DataRow dr = ds.Tables[0].NewRow();
                            dr["f_nombre"] = printerItem.Name + servingSize;
                            dr["f_Qty"] = printerItem.Qty;
                            dr["f_amt"] = printerItem.SubTotal;
                            string srtOption = string.Empty;
                            DivisorType divisor = 0;
                            var questions = printerItem.Questions.OrderBy(s => s.Divisor).ToList();
                            foreach (var q in questions)
                            {
                                if (q.Divisor != divisor)
                                {
                                    if (q.Divisor == DivisorType.Whole)
                                    {
                                        srtOption += "Completa :" + (char)10;
                                    }
                                    else if (q.Divisor == DivisorType.FirstHalf)
                                    {
                                        srtOption += "1 Mitad :" + (char)10;
                                    }
                                    else if (q.Divisor == DivisorType.SecondHalf)
                                    {
                                        srtOption += "2 Mitad :" + (char)10;
                                    }

                                    divisor = q.Divisor;
                                }

                                var strOption = "";
                                if (q.IsPreSelect)
                                {
                                    if (!q.IsActive)
                                    {
                                        srtOption += q.Description + (char)10;
                                    }
                                }
                                else
                                {
                                    srtOption += q.Description + (char)10;
                                    if (!string.IsNullOrEmpty(q.SubDescription))
                                    {
                                        strOption += " *" + q.SubDescription + (char)10;
                                    }
                                }
                            }
                            if (srtOption.Length > 0)
                            {
                                dr["f_opciones"] = srtOption.Substring(0, srtOption.Length - 1);
                            }
                            ds.Tables[0].Rows.Add(dr);
                        }

                        #region Imprimiendo la Orden
                        //-----------------imprimiento la comanda------------//
                        FastReport.Report Report1 = new FastReport.Report();
                        //string ruta = Path.Combine(Environment.CurrentDirectory, "temp1");
                        Report1.Load(GetReporte(3, 1));
                        Report1.RegisterData(ds, "general");
                        Report1.GetDataSource("general").Enabled = true;

                        FastReport.DataBand data1 = (FastReport.DataBand)Report1.FindObject("Data1");
                        data1.DataSource = Report1.GetDataSource("general");
                        Report1.SetParameterValue("titulo", "PRE-CUENTA");
                        Report1.SetParameterValue("empresa", preference.Name);
                        Report1.SetParameterValue("empresa1", preference.Company);
                        Report1.SetParameterValue("order", order.ID.ToString());
                        string tableName = order.Table != null ? order.Table.Name : "";
                        Report1.SetParameterValue("table", tableName);
                        Report1.SetParameterValue("person", order.Person.ToString());
                        Report1.SetParameterValue("camarero", order.WaiterName);
                        Report1.SetParameterValue("rnc", preference.RNC);
                       
                        Report1.SetParameterValue("devuelto", transactions.Sum(s => s.Difference));
                        Report1.SetParameterValue("propina", order.Propina);


                        if (order.Delivery == null)
                        {
                            Report1.SetParameterValue("delivery", (decimal)0.00);
                        }
                        else
                        {
                            Report1.SetParameterValue("delivery", order.Delivery);
                        }

                       

                       // if(order.OrderType == OrderType.Delivery) {
                            Report1.SetParameterValue("promesapagomonto", order.PromesaPago);
                            Report1.SetParameterValue("promesapagodevuelto", order.PromesaPago - order.TotalPrice);
                       // }

                        Report1.SetParameterValue("subtotal", order.SubTotal);
                        Report1.SetParameterValue("descuento", order.Discount);
                        string descuentos = string.Empty;
                        string espacio = "    ";
                        foreach (var d in order.Discounts)
                        {
                            descuentos += d.Name + espacio + d.Amount + (char)10;
                        }
                        if (descuentos.Length > 0) { descuentos = descuentos.Substring(0, descuentos.Length - 1); }
                        Report1.SetParameterValue("descuentos", descuentos);
                        Report1.SetParameterValue("tax", order.Tax);
                        string taxes = string.Empty;
                        string taxes1 = string.Empty;
                        foreach (var d in order.Taxes)
                        {
                            taxes += d.Description + (char)10;
                            taxes1 += d.Amount.ToString("0.00", CultureInfo.CurrentCulture) + (char)10;

                        }
                        if (taxes.Length > 0)
                        {
                            taxes = taxes.Substring(0, taxes.Length - 1);
                            taxes1 = taxes1.Substring(0, taxes1.Length - 1);

                        }
                        Report1.SetParameterValue("taxes", taxes);
                        Report1.SetParameterValue("taxes1", taxes1);

                        Report1.SetParameterValue("total", order.TotalPrice);
                        Report1.SetParameterValue("type", order.PrepareType);

                        if (customer != null)
                        {
                            Report1.SetParameterValue("customerRNC", customer.RNC);
                            Report1.SetParameterValue("customername", customer.Name);
                            Report1.SetParameterValue("customerAddress", customer.Address1);
                            Report1.SetParameterValue("customerphone", customer.Phone);
                        }


                        var stream = new MemoryStream();
                        Report1.Prepare();
                        string nombre = "H" + GetHashCode().ToString() + "T" + DateTime.Now.Ticks + "PreCuenta.fpx";
                        //Console.WriteLine((Path.Combine(ruta, nombre)));
                        Report1.SavePrepared(stream);
                        //Report1.Save(stream);

                        //while(!File.Exists(pdocument.PrinterSettings.PrintFileName))
                        //{

                        //}
                        //AlfaHelper.guardaadb(nombre, ".fpx", System.IO.File.ReadAllBytes($@"Reports/" + nombre), "printertest", "1E", 1, 1);
                        //AlfaHelper.guardaadb(nombre, ".fpx", System.IO.File.ReadAllBytes(Path.Combine(ruta, nombre)), defaultPrinter.Printer.PhysicalName, stationID.ToString(), 1, 1, empresa);

                        GuardaImpresion(nombre, ".fpx", stream.ToArray(), defaultPrinter.Printer.PhysicalName, stationId.ToString(), 1, 1, station.IDSucursal, false);



                        //  pdocument.PrinterSettings.PrintToFile = true;
                        // string archivo = GetHashCode() + DateTime.Now.Ticks + "backup.pdf";
                        // pdocument.PrinterSettings.PrintFileName = archivo;
                        //  pdocument.Print();

                        // var stationID = order.Station.ID;

                        // AlfaHelper.guardaadb(archivo, ".pdf", File.ReadAllBytes(archivo), defaultPrinter.Printer.PhysicalName, stationID.ToString(), 1, 1);
                        //-------------------------end-----------------------//
                        #endregion



                        // pdocument.Print();
                        return true;
                    }
                    catch
                    {
                    }
                }*/
            }
            return false;
        }

        private System.Drawing.Image GetLogo(string logoBase64)
        {


            if (string.IsNullOrEmpty(logoBase64)) { return null; }
            var index = logoBase64.IndexOf("base64,");
            logoBase64 = logoBase64.Substring(index + 7);

            byte[] bytes = Convert.FromBase64String(logoBase64);

            System.Drawing.Image image;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                image = System.Drawing.Image.FromStream(ms);
            }

            return image;

        }

        public bool PrintPaymentSummary(long stationId, long OrderID, int SeatNum = 0, int DividerNum = 0, bool reprint = false)
        {

            var preference = _dbContext.Preferences.First();
            var order = _dbContext.Orders.Include(s => s.Table).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).FirstOrDefault(o => o.ID == OrderID);
            /*var voucher = _dbContext.Vouchers.Include(s => s.Taxes).FirstOrDefault(s => s.ID == order.ComprobantesID);
            order.GetTotalPrice(voucher);
            var items = order.Items.Where(s => !s.IsDeleted).ToList();
            var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order).ToList();

            SeatItem seat = null;
            var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);*/

            var station = _dbContext.Stations.Include(s => s.Printers).ThenInclude(s => s.PrinterChannel).Include(s => s.Printers).ThenInclude(s => s.Printer).FirstOrDefault(s => s.ID == stationId);
            var defaultPrinter = station.Printers.FirstOrDefault(s => s.PrinterChannel.IsDefault);

            /*DataSet ds = new DataSet("general");
            ds.Tables.Add("general");
            ds.Tables[0].Columns.Add("f_nombre");
            ds.Tables[0].Columns.Add("f_Qty");
            ds.Tables[0].Columns.Add("f_opciones");
            ds.Tables[0].Columns.Add("f_amt", typeof(float));
            foreach (var printerItem in items)
            {
                Debug.WriteLine(printerItem.Name);
                Debug.WriteLine(printerItem.Qty);
                Debug.WriteLine(printerItem.ID);
                Debug.WriteLine(printerItem);
                DataRow dr = ds.Tables[0].NewRow();
                var servingSize = "";
                if (!string.IsNullOrEmpty(printerItem.ServingSizeName))
                {
                    servingSize = $"({printerItem.ServingSizeName})";
                }


                dr["f_nombre"] = printerItem.Name + servingSize;
                dr["f_Qty"] = printerItem.Qty;
                dr["f_amt"] = printerItem.SubTotal;
                string srtOption = string.Empty;
                DivisorType divisor = 0;
                var questions = printerItem.Questions.OrderBy(s => s.Divisor).ToList();
                foreach (var q in questions)
                {
                    if (q.Divisor != divisor)
                    {
                        if (q.Divisor == DivisorType.Whole)
                        {
                            srtOption += "Completa :" + (char)10;
                        }
                        else if (q.Divisor == DivisorType.FirstHalf)
                        {
                            srtOption += "1 Mitad :" + (char)10;
                        }
                        else if (q.Divisor == DivisorType.SecondHalf)
                        {
                            srtOption += "2 Mitad :" + (char)10;
                        }

                        divisor = q.Divisor;
                    }

                    var strOption = "";
                    if (q.IsPreSelect)
                    {
                        if (!q.IsActive)
                        {
                            srtOption += q.Description + (char)10;
                        }
                    }
                    else
                    {
                        srtOption += q.Description + (char)10;
                        if (!string.IsNullOrEmpty(q.SubDescription))
                        {
                            strOption += " *" + q.SubDescription + (char)10;
                        }
                    }
                }
                if (srtOption.Length > 0)
                {
                    dr["f_opciones"] = srtOption.Substring(0, srtOption.Length - 1);
                }
                ds.Tables[0].Rows.Add(dr);

            }*/


            if (defaultPrinter != null && defaultPrinter.Printer != null)
            {
                var printer = _dbContext.Printers.First();
                if (printer != null)
                {
                    try
                    {
                        
                        //INICIA: Manda a imprimir, nuevo metodo
                        var objPrint = new PrinterTasks();
                        objPrint.ObjectID = OrderID;
                        objPrint.Status = (int)PrinterTasksStatus.Pendiente;

                        if (reprint)
                        {
                            objPrint.Type = (int)PrinterTasksType.TicketPaymentSummaryCopy;    
                        }
                        else
                        {
                            
                            objPrint.Type = (int)PrinterTasksType.TicketPaymentSummary;
                        }
                        
                        objPrint.PhysicalName = defaultPrinter.Printer.PhysicalName;
                        objPrint.StationId = stationId;
                        objPrint.SucursalId = station.IDSucursal;
                        objPrint.DivideNum = DividerNum;
                        objPrint.SeatNum = SeatNum;
                        _dbContext.PrinterTasks.Add(objPrint);
                        _dbContext.SaveChanges();
                        //TERMINA: Manda a imprimir, nuevo metodo

                        #region imprimiendo Sumary
                        
                        //-----------------imprimiento la comanda------------//
                        
                        /*
                        FastReport.Report Report1 = new FastReport.Report();
                        //string ruta = Path.Combine(Environment.CurrentDirectory, "temp1");
                        Report1.Load(GetReporte(4, 1));
                        Report1.RegisterData(ds, "general");
                        Report1.GetDataSource("general").Enabled = true;

                        FastReport.DataBand data1 = (FastReport.DataBand)Report1.FindObject("Data1");
                        data1.DataSource = Report1.GetDataSource("general");
                        Report1.SetParameterValue("titulo", "FACTURA");
                        Report1.SetParameterValue("empresa", preference.Name);
                        Report1.SetParameterValue("empresa1", preference.Company);
                        Report1.SetParameterValue("order", order.ID.ToString());

                        if (order.Table != null)
                        {
                            Report1.SetParameterValue("table", order.Table.Name);//es null en barcode error
                        }

                        Report1.SetParameterValue("person", order.Person.ToString());
                        Report1.SetParameterValue("telefono", preference.Phone);
                        Report1.SetParameterValue("camarero", order.WaiterName);
                        Report1.SetParameterValue("rnc", preference.RNC);
                        Report1.SetParameterValue("subtotal", order.SubTotal);
                        Report1.SetParameterValue("descuento", order.Discount);
                        if (order.OrderMode == OrderMode.Conduce && string.IsNullOrEmpty(order.ComprobanteNumber))
                        {
                            var conduceCount = _dbContext.Orders.Where(s=>s.IsConduce).Count();
							Report1.SetParameterValue("ncf", "Conduce #" + conduceCount);
							Report1.SetParameterValue("tiponcf", "");
						}
                        else
                        {
							Report1.SetParameterValue("ncf", order.ComprobanteNumber);
							Report1.SetParameterValue("tiponcf", order.ComprobanteName);
						}
                        
                       
                        Report1.SetParameterValue("seat", SeatNum);
                        Report1.SetParameterValue("DividerNum", DividerNum);
                        if (customer != null)
                        {
                            Report1.SetParameterValue("customerRNC", customer.RNC);
                            Report1.SetParameterValue("customername", customer.Name);
                        }

                        string metodo = string.Empty;
                        string cantidadp = string.Empty;
                        decimal paidAmount = 0;
                        decimal difference = 0;
                        foreach (var t in transactions)
                        {
                            metodo += t.Method + (char)10;
                            cantidadp += (t.Amount+t.Difference).ToString("0.00", CultureInfo.InvariantCulture) + (char)10;
                            paidAmount += t.Amount;
                            difference += t.Difference;
                            Console.WriteLine((t.Amount).ToString("0.00", CultureInfo.InvariantCulture) + " Cantidad dp");
                        }
                        if (metodo.Length > 0)
                        {
                            metodo = metodo.Substring(0, metodo.Length - 1);
                            cantidadp = cantidadp.Substring(0, cantidadp.Length - 1);
                        }
                        // Monto pagado 
                        // paidAmount

                        //Cambio

                        Report1.SetParameterValue("metodo", metodo);
                        Report1.SetParameterValue("cantidaddp", cantidadp);
                        string descuentos = string.Empty;
                        string espacio = "    ";
                        foreach (var d in order.Discounts)
                        {
                            descuentos += d.Name + espacio + d.Amount + (char)10;
                        }
                        if (descuentos.Length > 0) { descuentos = descuentos.Substring(0, descuentos.Length - 1); }
                        Report1.SetParameterValue("descuentos", descuentos);
                        Report1.SetParameterValue("tax", order.Tax);
                        string taxes = string.Empty;
                        string taxes1 = string.Empty;
                        foreach (var d in order.Taxes)
                        {
                            taxes += d.Description + (char)10;
                            taxes1 += d.Amount.ToString("0.00", CultureInfo.InvariantCulture) + (char)10;

                        }
                        if (taxes.Length > 0)
                        {
                            taxes = taxes.Substring(0, taxes.Length - 1);
                            taxes1 = taxes1.Substring(0, taxes1.Length - 1);

                        }
                        Report1.SetParameterValue("taxes", taxes);
                        Report1.SetParameterValue("taxes1", taxes1);

                        Report1.SetParameterValue("total", order.TotalPrice);
                        Report1.SetParameterValue("type", order.PrepareType);
                        Report1.SetParameterValue("devuelto", transactions.Sum(s => s.Difference));
                        Report1.SetParameterValue("propina", order.Propina);

                        if (transactions.Any())
                        {
                            Report1.SetParameterValue("cajero", transactions.First().CreatedBy);    
                        }
                        

                        if (order.Delivery == null)
                        {
                            Report1.SetParameterValue("delivery",(decimal) 0.00);
                        }
                        else
                        {
                            Report1.SetParameterValue("delivery", order.Delivery);
                        }
                        // if (order.OrderType == OrderType.Delivery)
                        // {
                        //     Report1.SetParameterValue("promesapagomonto", order.PromesaPago);
                        //     Report1.SetParameterValue("promesapagodevuelto", order.PromesaPago - order.TotalPrice);
                        // }

                        var stream = new MemoryStream();
                        Report1.Prepare();
                        string nombre = "H" + GetHashCode().ToString() + "T" + DateTime.Now.Ticks + "Factura.fpx";
                        //Console.WriteLine((Path.Combine(ruta, nombre)));
                        //Report1.SavePrepared(Path.Combine(ruta, nombre));
                        Report1.SavePrepared(stream);
                        //  pdocument.PrinterSettings.PrintToFile = true;
                        //string archivo = GetHashCode() + DateTime.Now.Ticks + "backup.pdf";
                        //   pdocument.PrinterSettings.PrintFileName = archivo;
                        //  pdocument.Print();


                        //AlfaHelper.guardaadb(nombre, ".fpx", System.IO.File.ReadAllBytes(Path.Combine(ruta, nombre)), defaultPrinter.Printer.PhysicalName, stationID.ToString(), 1, 1, empresa);
                        //AlfaHelper.guardaadb(nombre, ".fpx", stream.ToArray(), defaultPrinter.Printer.PhysicalName, stationID.ToString(), 1, 1, empresa);
                        //-------------------------end-----------------------//

                        GuardaImpresion(nombre, ".fpx", stream.ToArray(), defaultPrinter.Printer.PhysicalName, stationId.ToString(), 1 + station.PrintCopy, 1, station.IDSucursal, reprint);

                        */

                        // pdocument.Print();
                        #endregion
                        return true;
                    }
                    catch (Exception ex)
                    {
                        var m = 1;
                    }
                }
            }
            return false;
        }

		public bool PrintCxCSummary(long stationId, List<long> ReferenceIds, int SeatNum = 0, int DividerNum = 0, bool reprint = false)
        {
			var preference = _dbContext.Preferences.First();
			var cxcList = _dbContext.OrderTransactions.Include(ot => ot.Order).Where(ot => ReferenceIds.Contains(ot.ReferenceId)).ToList();

            List<Order> orders = new List<Order>();
            List<OrderItem> items = new List<OrderItem>();
            List<TaxItem> taxesitems = new List<TaxItem>();
            List<DiscountItem> discountsitems = new List<DiscountItem>();
            string strTable = "";
            foreach (var objCxc in cxcList)
            {
                var objTrans = _dbContext.OrderTransactions.Include(m=>m.Order).First(m => m.ID == objCxc.ReferenceId);
                var order = _dbContext.Orders.Include(s => s.Table).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.Discounts).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions).Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts).Include(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted)).FirstOrDefault(o => o.ID == objTrans.Order.ID);
                
                orders.Add(order);
                
                if (order.Table != null)
                {
                    if (!string.IsNullOrEmpty(strTable))
                    {
                        strTable = strTable + ", ";
                    }
                    strTable = strTable + order.Table.Name;
                }

                if (order.Items!= null && order.Items.Any())
                {
                    foreach (var objItem in order.Items)
                    {
                        items.Add(objItem);
                    }
                }

                if (order.Taxes != null && order.Taxes.Any())
                {
                    foreach (var objTax in order.Taxes)
                    {
                        taxesitems.Add(objTax);
                    }
                }

                if (order.Discounts != null && order.Discounts.Any())
                {
                    foreach (var objDiscount in order.Discounts)
                    {
                        discountsitems.Add(objDiscount);
                    }
                }

            }
            
			//var items = (from c in cxc.Order where !c.IsDeleted select c.Order.Items);
			SeatItem seat = null;
			var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == cxcList.First().Order.CustomerId);


			var station = _dbContext.Stations.Include(s => s.Printers).ThenInclude(s => s.PrinterChannel).Include(s => s.Printers).ThenInclude(s => s.Printer).FirstOrDefault(s => s.ID == stationId);
			var defaultPrinter = station.Printers.FirstOrDefault(s => s.PrinterChannel.IsDefault);

			DataSet ds = new DataSet("general");
			ds.Tables.Add("general");
			ds.Tables[0].Columns.Add("f_nombre");
			ds.Tables[0].Columns.Add("f_Qty");
			ds.Tables[0].Columns.Add("f_opciones");
			ds.Tables[0].Columns.Add("f_amt", typeof(float));

			foreach (var printerItem in items)
			{
				Debug.WriteLine(printerItem.Name);
				Debug.WriteLine(printerItem.Qty);
				Debug.WriteLine(printerItem.ID);
				Debug.WriteLine(printerItem);
				DataRow dr = ds.Tables[0].NewRow();
				var servingSize = "";
				if (!string.IsNullOrEmpty(printerItem.ServingSizeName))
				{
					servingSize = $"({printerItem.ServingSizeName})";
				}


				dr["f_nombre"] = printerItem.Name + servingSize;
				dr["f_Qty"] = printerItem.Qty;
				dr["f_amt"] = printerItem.SubTotal;
				string srtOption = string.Empty;
				DivisorType divisor = 0;
				var questions = printerItem.Questions.OrderBy(s => s.Divisor).ToList();
				foreach (var q in questions)
				{
					if (q.Divisor != divisor)
					{
						if (q.Divisor == DivisorType.Whole)
						{
							srtOption += "Completa :" + (char)10;
						}
						else if (q.Divisor == DivisorType.FirstHalf)
						{
							srtOption += "1 Mitad :" + (char)10;
						}
						else if (q.Divisor == DivisorType.SecondHalf)
						{
							srtOption += "2 Mitad :" + (char)10;
						}

						divisor = q.Divisor;
					}

					var strOption = "";
					if (q.IsPreSelect)
					{
						if (!q.IsActive)
						{
							srtOption += q.Description + (char)10;
						}
					}
					else
					{
						srtOption += q.Description + (char)10;
						if (!string.IsNullOrEmpty(q.SubDescription))
						{
							strOption += " *" + q.SubDescription + (char)10;
						}
					}
				}
				if (srtOption.Length > 0)
				{
					dr["f_opciones"] = srtOption.Substring(0, srtOption.Length - 1);
				}
				ds.Tables[0].Rows.Add(dr);

			}
			if (defaultPrinter != null && defaultPrinter.Printer != null)
			{
				var printer = _dbContext.Printers.First();
				if (printer != null)
				{
					try
					{

						#region imprimiendo Sumary
						//-----------------imprimiento la comanda------------//
                        
                        /*
						FastReport.Report Report1 = new FastReport.Report();
						//string ruta = Path.Combine(Environment.CurrentDirectory, "temp1");
						Report1.Load(GetReporte(4, 1));
						Report1.RegisterData(ds, "general");
						Report1.GetDataSource("general").Enabled = true;

						FastReport.DataBand data1 = (FastReport.DataBand)Report1.FindObject("Data1");
						data1.DataSource = Report1.GetDataSource("general");
						Report1.SetParameterValue("titulo", "FACTURA");
						Report1.SetParameterValue("empresa", preference.Name);
						Report1.SetParameterValue("empresa1", preference.Company);
						Report1.SetParameterValue("cxc",   string.Join(", ",cxcList.Select(p=>p.ReferenceId).ToList()));
                        
						if (!string.IsNullOrEmpty(strTable))
						{
							Report1.SetParameterValue("table", strTable);//es null en barcode error
						}

						Report1.SetParameterValue("person", string.Join(", ",cxcList.Select(p=>p.Order.Person).ToList()));
						Report1.SetParameterValue("telefono", preference.Phone);
						Report1.SetParameterValue("camarero", string.Join(", ",cxcList.Select(p=>p.Order.WaiterName).ToList()));
						Report1.SetParameterValue("rnc", preference.RNC);
						Report1.SetParameterValue("subtotal", cxcList.Select(p=>p.Order.SubTotal).Sum());
						Report1.SetParameterValue("descuento", cxcList.Select(p=>p.Order.Discount).Sum());
						Report1.SetParameterValue("ncf", string.Join(", ",cxcList.Select(p=>p.Order.ComprobanteNumber).ToList()));
						Report1.SetParameterValue("tiponcf", string.Join(", ",cxcList.Select(p=>p.Order.ComprobanteName).ToList()));
						Report1.SetParameterValue("seat", SeatNum);
						Report1.SetParameterValue("DividerNum", DividerNum);
						if (customer != null)
						{
							Report1.SetParameterValue("customerRNC", customer.RNC);
							Report1.SetParameterValue("customername", customer.Name);
						}

						string metodo = string.Empty;
						string cantidadp = string.Empty;
						decimal paidAmount = 0;
						decimal difference = 0;
						metodo += string.Join(", ",cxcList.Select(p=>p.Method).ToList()) + (char)10;
						cantidadp += (cxcList.Select(p=>p.Amount).Sum() + cxcList.Select(p=>p.Difference).Sum()).ToString("0.00", CultureInfo.InvariantCulture) + (char)10;
						paidAmount += cxcList.Select(p=>p.Amount).Sum();
						difference +=cxcList.Select(p=>p.Difference).Sum();
						Console.WriteLine((cxcList.Select(p=>p.Amount).Sum()).ToString("0.00", CultureInfo.InvariantCulture) + " Cantidad dp");
						if (metodo.Length > 0)
						{
							metodo = metodo.Substring(0, metodo.Length - 1);
							cantidadp = cantidadp.Substring(0, cantidadp.Length - 1);
						}
						// Monto pagado 
						// paidAmount

						//Cambio

						Report1.SetParameterValue("metodo", metodo);
						Report1.SetParameterValue("cantidaddp", cantidadp);
						string descuentos = string.Empty;
						string espacio = "    ";
						foreach (var d in discountsitems)
						{
							descuentos += d.Name + espacio + d.Amount + (char)10;
						}
						if (descuentos.Length > 0) { descuentos = descuentos.Substring(0, descuentos.Length - 1); }
						Report1.SetParameterValue("descuentos", descuentos);
						Report1.SetParameterValue("tax", cxcList.Select(p=>p.Order.Tax).Sum());
						string taxes = string.Empty;
						string taxes1 = string.Empty;
						foreach (var d in taxesitems)
						{
							taxes += d.Description + (char)10;
							taxes1 += d.Amount.ToString("0.00", CultureInfo.InvariantCulture) + (char)10;

						}
						if (taxes.Length > 0)
						{
							taxes = taxes.Substring(0, taxes.Length - 1);
							taxes1 = taxes1.Substring(0, taxes1.Length - 1);

						}
						Report1.SetParameterValue("taxes", taxes);
						Report1.SetParameterValue("taxes1", taxes1);

						Report1.SetParameterValue("total", cxcList.Select(p=>p.Order.TotalPrice).Sum());
						Report1.SetParameterValue("type", string.Join(", ",cxcList.Select(p=>p.Order.PrepareType).ToList()));
						Report1.SetParameterValue("devuelto", cxcList.Select(p=>p.Difference).Sum());
						Report1.SetParameterValue("propina", cxcList.Select(p=>p.Order.Propina).Sum());

						Report1.SetParameterValue("cajero", string.Join(", ",cxcList.Select(p=>p.CreatedBy).ToList()));


						//if (cxc.Order.Delivery == null)
						//{
						//	Report1.SetParameterValue("delivery", (decimal)0.00);
						//}
						//else
						//{
							Report1.SetParameterValue("delivery", cxcList.Select(p=>p.Order.Delivery).Sum());
						//}
						// if (order.OrderType == OrderType.Delivery)
						// {
						//     Report1.SetParameterValue("promesapagomonto", order.PromesaPago);
						//     Report1.SetParameterValue("promesapagodevuelto", order.PromesaPago - order.TotalPrice);
						// }

						var stream = new MemoryStream();
						Report1.Prepare();
						string nombre = "H" + GetHashCode().ToString() + "T" + DateTime.Now.Ticks + "Factura.fpx";
						//Console.WriteLine((Path.Combine(ruta, nombre)));
						//Report1.SavePrepared(Path.Combine(ruta, nombre));
						Report1.SavePrepared(stream);
						//  pdocument.PrinterSettings.PrintToFile = true;
						//string archivo = GetHashCode() + DateTime.Now.Ticks + "backup.pdf";
						//   pdocument.PrinterSettings.PrintFileName = archivo;
						//  pdocument.Print();


						//AlfaHelper.guardaadb(nombre, ".fpx", System.IO.File.ReadAllBytes(Path.Combine(ruta, nombre)), defaultPrinter.Printer.PhysicalName, stationID.ToString(), 1, 1, empresa);
						//AlfaHelper.guardaadb(nombre, ".fpx", stream.ToArray(), defaultPrinter.Printer.PhysicalName, stationID.ToString(), 1, 1, empresa);
						//-------------------------end-----------------------//

						GuardaImpresion(nombre, ".fpx", stream.ToArray(), defaultPrinter.Printer.PhysicalName, stationId.ToString(), 1 + station.PrintCopy, 1, station.IDSucursal, reprint);

                        */

						// pdocument.Print();
						#endregion
						return true;
					}
					catch (Exception ex)
					{
						var m = 1;
					}
				}
			}
			return false;

		}

        
        

		public bool PrintCloseDrawerSummary(long stationId, CloseDrawerPrinterModel model)
        {
            var preference = _dbContext.Preferences.First();

            var station = _dbContext.Stations.Include(s => s.Printers).ThenInclude(s => s.PrinterChannel).Include(s => s.Printers).ThenInclude(s => s.Printer).FirstOrDefault(s => s.ID == stationId);
            var defaultPrinter = station.Printers.FirstOrDefault(s => s.PrinterChannel.IsDefault);


            if (defaultPrinter != null && defaultPrinter.Printer != null)
            {
                var printer = _dbContext.Printers.First();
                if (printer != null)
                {
                    try
                    {

                        /*DataSet ds = new DataSet("general");
                        ds.Tables.Add("general");
                        ds.Tables[0].Columns.Add("f_nombre");
                        ds.Tables[0].Columns.Add("f_Qty");
                        ds.Tables[0].Columns.Add("f_amt", typeof(float));*/

                        var objResult = new CloseDrawerSummary();
                        objResult.denominations = new List<PCDSDenominations>();
                        objResult.formaPagos = new List<PCDSFormaPago>();

                        decimal subTotal = 0;
                        foreach (var d in model.Denominations)
                        {
                            var auxDenomination = new PCDSDenominations();
                            auxDenomination.name = "$" + d.Name;
                            auxDenomination.qty = d.Qty;
                            auxDenomination.amt = d.Amount.ToString("0.00", CultureInfo.CurrentCulture);
                            objResult.denominations.Add(auxDenomination);
                                
                            subTotal += d.Amount;
                            /*DataRow dr = ds.Tables[0].NewRow();
                            dr["f_nombre"] = d.Name;
                            dr["f_Qty"] = d.Qty;
                            dr["f_amt"] = d.Amount;
                            ds.Tables[0].Rows.Add(dr);*/

                        }
                        
                        

                        //-----------generando el reporte de cuadre de caja-----------//
                        /*
                        FastReport.Report Report1 = new FastReport.Report();
                        Report1.Load(GetReporte(5, 1));
                        Report1.RegisterData(ds, "general");
                        Report1.GetDataSource("general").Enabled = true;

                        FastReport.DataBand data1 = (FastReport.DataBand)Report1.FindObject("Data1");
                        data1.DataSource = Report1.GetDataSource("general");
                        Report1.SetParameterValue("titulo", "CUADRE DE CAJA");
                        Report1.SetParameterValue("empresa", preference.Name);
                        Report1.SetParameterValue("cajero", model.UserName);
                        Report1.SetParameterValue("total", subTotal);
                        */
                         
                        objResult.titulo="CUADRE DE CAJA";
                        objResult.empresa=preference.Name;
                        objResult.cajero=model.UserName;
                        objResult.total=subTotal;

                        //--------------formas de pago-------------//

                        string forma_pago = string.Empty;
                        string forma_pago_valor = string.Empty;

                        foreach (var p in model.PMethods)
                        {
                            var auxFormaPago = new PCDSFormaPago();
                            auxFormaPago.formaPago = p.Name;
                            auxFormaPago.valor = p.Amount.ToString("0.00", CultureInfo.CurrentCulture);
                            objResult.formaPagos.Add(auxFormaPago);
                            //forma_pago += p.Name + (char)10;
                            //forma_pago_valor += p.Amount.ToString("0.00", CultureInfo.CurrentCulture) + (char)10;

                        }
                        /*if (forma_pago.Length > 0)
                        {
                            forma_pago = forma_pago.Substring(0, forma_pago.Length - 1);
                            forma_pago_valor = forma_pago_valor.Substring(0, forma_pago_valor.Length - 1);

                        }*/
                        
                        //objResult.formaPago=forma_pago;
                        //objResult.formaPagoValor=forma_pago_valor;
                        
                        objResult.grandTotal=model.GrandTotal.ToString("0.00", CultureInfo.CurrentCulture);
                        objResult.expectedTotal=model.TransTotal.ToString("0.00", CultureInfo.CurrentCulture);
                        objResult.discrepancy=model.TransDifference.ToString("0.00", CultureInfo.CurrentCulture);
                        objResult.expectedTipTotal=model.TipTotal.ToString("0.00", CultureInfo.CurrentCulture);
                        objResult.tipDiscrepancy=model.TipDifference.ToString("0.00", CultureInfo.CurrentCulture);

                        /*

                        Report1.SetParameterValue("forma_pago", forma_pago);
                        Report1.SetParameterValue("forma_pago_valor", forma_pago_valor);

                        Report1.SetParameterValue("grand_total", model.GrandTotal.ToString("0.00", CultureInfo.InvariantCulture));
                        Report1.SetParameterValue("expected_total", model.TransTotal.ToString("0.00", CultureInfo.InvariantCulture));
                        Report1.SetParameterValue("discrepancy", model.TransDifference.ToString("0.00", CultureInfo.InvariantCulture));
                        Report1.SetParameterValue("expected_tip_total", model.TipTotal.ToString("0.00", CultureInfo.InvariantCulture));
                        Report1.SetParameterValue("tip_discrepancy", model.TipDifference.ToString("0.00", CultureInfo.InvariantCulture));

                        var stream = new MemoryStream();
                        Report1.Prepare();
                        string nombre = "H" + GetHashCode().ToString() + "T" + DateTime.Now.Ticks + "cuadre_caja.fpx";
                        Report1.SavePrepared(stream);

                        var stationID = stationId;

                        GuardaImpresion(nombre, ".fpx", stream.ToArray(), defaultPrinter.Printer.PhysicalName, stationID.ToString(), 1 + station.PrintCopy, 1, station.IDSucursal, false);
                        */
                        
                        var objPrint = new PrinterTasks
                        {
                            ObjectID = 0,
                            Status = (int)PrinterTasksStatus.Pendiente,
                            Type = (int)PrinterTasksType.TicketCloseDrawerSummary, // Tipo de impresión para cocina
                            PhysicalName = defaultPrinter.Printer.PhysicalName,
                            StationId = stationId,
                            SucursalId = station.IDSucursal,
                            Items = JsonConvert.SerializeObject(objResult)
                        };
                        
                        _dbContext.PrinterTasks.Add(objPrint);
                        _dbContext.SaveChanges();
                    }
                    catch { }
                }
            }

            return false;
        }

        public void GuardaImpresion(string nombre, string extencion, byte[] cuerpo, string impresora, string id_estacion, int numero_copias, int status_impresion, int id_sucursal, bool reprint)
        {
            try
            {
                var objImpresion = new t_impresion();

                objImpresion.nombre = nombre;
                objImpresion.extencion = extencion;
                objImpresion.cuerpo = System.Convert.ToBase64String(cuerpo);
                objImpresion.impresora = impresora;
                objImpresion.id_estacion = id_estacion;
                objImpresion.status_impresion = status_impresion;
                objImpresion.numero_copias = numero_copias;
                objImpresion.IDSucursal = id_sucursal;
                objImpresion.IsReprint = reprint;

                _dbContext.t_impresion.Add(objImpresion);
                _dbContext.SaveChanges();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        internal Stream GetReporte(int id, int tipo)
        {

            string strArchivo;

            switch (tipo)
            {
                case 1:
                    //qry = "select f_archivo from t_formato_impresion_general  where f_id=@ID";
                    strArchivo = _dbContext.t_formato_impresion_general.FirstOrDefault(s => s.f_id == id).f_archivo;

                    if (string.IsNullOrEmpty(strArchivo))
                    {
                        Console.WriteLine("No se encontro el reporte " + id + " t_formato_impresion_general");
                    }
                    break;

                default:
                    //qry = "select f_archivo from t_formato_impresion_reportes  where f_id=@ID";
                    strArchivo = _dbContext.t_formato_impresion_reportes.FirstOrDefault(s => s.f_id == id).f_archivo;
                    if (string.IsNullOrEmpty(strArchivo))
                    {
                        Console.WriteLine("No se encontro el reporte " + id + " t_formato_impresion_reportes");
                    }
                    break;
            }

            byte[] buffer = Convert.FromBase64String(strArchivo);
            MemoryStream stream = new MemoryStream(buffer);

            return stream;


        }


    }
}
