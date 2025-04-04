using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.Services;
using AuroraPOS.ViewModels;
using AuroraPOS.Hubs;
using Microsoft.AspNetCore.SignalR;
//using FastReport;
using Microsoft.EntityFrameworkCore;
using AuroraPOS.ModelsCentral;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;


namespace AuroraPOS.Controllers
{
    [Route("api/[controller]")]
    public class PrinterController : Controller
    {
        private readonly PrinterService _printerService;
        private AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        private readonly DbAlfaCentralContext _dbCentralContext;

        public PrinterController(PrinterService printerService, AppDbContext dbContext)
        {
            _printerService = printerService;
            _dbContext = dbContext;
        }

        // Método para crear y enviar un trabajo de impresión
        [HttpPost]
        public async Task<IActionResult> PrintJobs()
        {
            // Obtiene la lista de trabajos pendientes desde la base de datos
            var pendingJobs = await _dbContext.PrintJobs
                .Where(job => job.Status == "Pendiente")
                .ToListAsync();

            // Verifica si hay trabajos pendientes
            if (pendingJobs == null || !pendingJobs.Any())
            {
                return NotFound("No hay trabajos de impresión pendientes.");
            }

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // Enviar cada trabajo al servicio de impresión
                    foreach (var job in pendingJobs)
                    {
                        await _printerService.SendPrintJobToServiceAsync(job);
                        job.Status = "En Progreso"; // Cambia el estado a "En Progreso"
                    }

                    await _dbContext.SaveChangesAsync(); // Guarda los cambios en la base de datos
                    await transaction.CommitAsync(); // Confirma la transacción

                    return Ok(new { message = "Trabajos de impresión enviados." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(); // Revertir en caso de error
                    return StatusCode(500, $"Error al enviar trabajos de impresión: {ex.Message}");
                }
            }
        }

        [HttpGet("GetPendingPrintJobs")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPendingPrintJobs()
        {
            // Obtiene la lista de trabajos pendientes desde la base de datos
            var pendingJobs = await _dbContext.PrinterTasks
                .Where(job => job.Status == (int)PrinterTasksStatus.Pendiente)
                .ToListAsync();

            var objResult = new PrintModel();

            if (pendingJobs.Any())
            {
                objResult.Lista = new List<PrintJobModel>();

                foreach (var objPending in pendingJobs)
                {
                    if (objPending.Type == 1)
                    {
                        var preference = _dbContext.Preferences.First();
                        var order = _dbContext.Orders.Include(s => s.Station).ThenInclude(s => s.Printers)
                            .Include(s => s.Area).Include(s => s.Table).Include(s => s.Taxes).Include(s => s.Discounts)
                            .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions)
                            .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts)
                            .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes)
                            .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product)
                            .FirstOrDefault(s => s.ID == objPending.ObjectID);
                        var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId); //cliente
                        var station = _dbContext.Stations.Include(s => s.Printers).ThenInclude(s => s.PrinterChannel)
                            .Include(s => s.Printers).ThenInclude(s => s.Printer)
                            .FirstOrDefault(s => s.ID == objPending.StationId);

                        var lstItemsIds = JsonConvert.DeserializeObject<List<long>>(objPending.Items);

                        var items = new List<OrderItem>();
                        foreach (var itemId in lstItemsIds)
                        {
                            items.Add(order.Items.Where(s => s.ID == itemId).First());
                        }

                        var printerItems = GetPrintersOfItems(items);

                        foreach (var printerItem in printerItems)
                        {
                            var objPrintJob = new PrintJobModel();
                            
                            var objRealPrinter = station.Printers.FirstOrDefault(s => s.PrinterChannel == printerItem.Key);
                            
                            //objPrintJob.printJobOrders = new List<PrintOrderModel>();
                            objPrintJob.Id = objPending.ID;
                            objPrintJob.Type = objPending.Type.Value;
                            objPrintJob.PhysicalName = objRealPrinter.Printer.PhysicalName;
                            objPrintJob.StationId = objPending.StationId.HasValue ? objPending.StationId.Value : -1;
                            objPrintJob.SucursalId = objPending.SucursalId.HasValue ? objPending.SucursalId.Value : -1;
                            objPrintJob.ObjectId = objPending.ObjectID.HasValue ? objPending.ObjectID.Value : -1;


                            //DataSet ds = new DataSet("general");
                            //ds.Tables.Add("general");
                            //ds.Tables[0].Columns.Add("f_nombre");
                            //ds.Tables[0].Columns.Add("f_Qty");
                            //ds.Tables[0].Columns.Add("f_opciones");
                            var pitems = printerItem.Value.OrderBy(s => s.CourseID);
                            long course = 0;

                            var objPrintOrden = new PrintOrderModel();
                            objPrintOrden.Items = new List<PrintOrderItemModel>();
                            foreach (var item in pitems)
                            {
                                var objItem = new PrintOrderItemModel();
                                //Debug.WriteLine(item.Name);
                                //Debug.WriteLine(item.Qty);

                                if (course != item.CourseID)
                                {
                                    //DataRow dr1 = ds.Tables[0].NewRow();
                                    objItem.Nombre = " - " + item.Course;
                                    objItem.Nombre = (char)10 + "       *** " + item.Course.ToUpper() + " *** " +
                                                     (char)10;
                                    course = item.CourseID;
                                }

                                objPrintOrden.Items.Add(objItem);


                                objItem = new PrintOrderItemModel();


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

                                //DataRow dr = ds.Tables[0].NewRow();
                                objItem.Nombre = name + servingSize;
                                objItem.Qty = item.Qty.ToString();
                                string srtOption = string.Empty;
                                DivisorType divisor = 0;
                                var questions = item.Questions.OrderBy(s => s.Divisor).ToList();
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
                                    objItem.Opciones = srtOption.Substring(0, srtOption.Length - 1);
                                }

                                objPrintOrden.Items.Add(objItem);
                                //ds.Tables[0].Rows.Add(dr);

                                if (!string.IsNullOrEmpty(item.Note))
                                {
                                    objItem = new PrintOrderItemModel();
                                    //DataRow dr1 = ds.Tables[0].NewRow();
                                    objItem.Nombre = " NOTE:" + item.Note;

                                    objPrintOrden.Items.Add(objItem);
                                    //ds.Tables[0].Rows.Add(dr1);
                                }
                            }

                            var printer = printerItem.Key;
                            var realprinter = station.Printers.FirstOrDefault(s => s.PrinterChannel == printer);
                            if (realprinter == null || realprinter.Printer == null) continue;
                            try
                            {
                                //Console.WriteLine("Creadno documento");

                                //FastReport.Report Report1 = new FastReport.Report();
                                //Report1.Load(GetReporte(2, 1));
                                //Report1.RegisterData(ds, "general");
                                //Report1.GetDataSource("general").Enabled = true;

                                //FastReport.DataBand data1 = (FastReport.DataBand)Report1.FindObject("Data1");
                                //data1.DataSource = Report1.GetDataSource("general");
                                //Report1.SetParameterValue("titulo", printer.Name);
                                //Report1.SetParameterValue("empresa", preference.Name);
                                //Report1.SetParameterValue("order", order.ID.ToString());
                                //string tableName = order.Table != null ? order.Table.Name : "";
                                //Report1.SetParameterValue("table", tableName);
                                //Report1.SetParameterValue("person", order.Person.ToString());
                                //Report1.SetParameterValue("camarero", order.WaiterName);

                                objPrintOrden.Titulo = printer.Name;
                                objPrintOrden.Empresa = preference.Name;
                                objPrintOrden.Empresa1 = preference.Company;
                                objPrintOrden.Order = order.ID.ToString();
                                string tableName = order.Table != null ? order.Table.Name : "";
                                objPrintOrden.Table = tableName;
                                objPrintOrden.Person = order.Person.ToString();
                                objPrintOrden.Camarero = order.WaiterName;
                                objPrintOrden.Rnc = preference.RNC;

                                if (customer != null)
                                {
                                    objPrintOrden.CustomerRNC = customer.RNC;
                                    objPrintOrden.CustomerName = customer.Name;
                                    objPrintOrden.CustomerAddress = customer.Address1;
                                    //Report1.SetParameterValue("customerRNC", customer.RNC);
                                    //Report1.SetParameterValue("customername", customer.Name);
                                    //Report1.SetParameterValue("customerAddress", customer.Address1);
                                }

                                objPrintOrden.OrderType = "";
                                if (order.OrderType != OrderType.Delivery)
                                {
                                    objPrintOrden.OrderType = order.OrderType.ToString();
                                    //Report1.SetParameterValue("oder_type", order.OrderType.ToString());
                                }
                                else
                                {
                                    var prepareType =
                                        _dbContext.PrepareTypes.FirstOrDefault(s => s.ID == order.PrepareTypeID);
                                    if (prepareType != null)
                                    {
                                        objPrintOrden.OrderType = prepareType.Name;
                                        //Report1.SetParameterValue("oder_type", prepareType.Name);
                                    }
                                    else
                                    {
                                        objPrintOrden.OrderType = "Para llevar";
                                        //Report1.SetParameterValue("oder_type", "Para llevar");
                                    }
                                }
                                //var stream = new MemoryStream();
                                //Report1.Prepare();
                                //string nombre = "H" + GetHashCode().ToString() + "T" + DateTime.Now.Ticks + "Comanda.fpx";
                                //Console.WriteLine((Path.Combine(ruta, nombre)));
                                //Report1.SavePrepared(Path.Combine(ruta, nombre));
                                //Report1.SavePrepared(stream);
                                //Report1.Save(stream);

                                //GuardaImpresion(nombre, ".fpx", stream.ToArray(), realprinter.Printer.PhysicalName, stationId.ToString(), 1, 1, station.IDSucursal, false);

                                objPrintJob.printJobOrder = objPrintOrden;
                                objResult.Lista.Add(objPrintJob);
                            }
                            catch (Exception c)
                            {
                                Console.WriteLine(c.Message);
                            }
                        }
                    }
                    else
                    {
                        var objPrintJob = new PrintJobModel();
                        //objPrintJob.printJobOrders = new List<PrintOrderModel>();
                        objPrintJob.Id = objPending.ID;
                        objPrintJob.Type = objPending.Type.Value;
                        objPrintJob.PhysicalName = objPending.PhysicalName;
                        objPrintJob.StationId = objPending.StationId.HasValue ? objPending.StationId.Value : -1;
                        objPrintJob.SucursalId = objPending.SucursalId.HasValue ? objPending.SucursalId.Value : -1;
                        objPrintJob.ObjectId = objPending.ObjectID.HasValue ? objPending.ObjectID.Value : -1;

                        if (objPending.Type == 5)
                        {
                            objPrintJob.ObjectData = objPending.Items;
                        }
                        else
                        {
                            var preference = _dbContext.Preferences.First();
                            var order = _dbContext.Orders.Include(s => s.Taxes).Include(s => s.Table)
                                .Include(s => s.Propinas).Include(s => s.Discounts)
                                .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Taxes)
                                .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Propinas)
                                .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Product)
                                .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Questions)
                                .Include(s => s.Items.Where(s => !s.IsDeleted)).ThenInclude(s => s.Discounts)
                                .Include(s => s.Seats).ThenInclude(s => s.Items.Where(s => !s.IsDeleted))
                                .FirstOrDefault(o => o.ID == objPending.ObjectID);
                            var transactions = _dbContext.OrderTransactions.Where(s => s.Order == order);
                            var voucher = _dbContext.Vouchers.Include(s => s.Taxes)
                                .FirstOrDefault(s => s.ID == order.ComprobantesID);
                            var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == order.CustomerId);
                            order.GetTotalPrice(voucher, objPending.DivideNum != null ? objPending.DivideNum.Value : 0,
                                objPending.SeatNum != null ? objPending.SeatNum.Value : 0);
                            var station = _dbContext.Stations.Include(s => s.Printers)
                                .ThenInclude(s => s.PrinterChannel)
                                .Include(s => s.Printers).ThenInclude(s => s.Printer)
                                .FirstOrDefault(s => s.ID == objPending.StationId);
                            var defaultPrinter = station.Printers.FirstOrDefault(s => s.PrinterChannel.IsDefault);

                            var items = order.Items.Where(s => !s.IsDeleted).ToList();

                            if (defaultPrinter != null && defaultPrinter.Printer != null)
                            {
                                {
                                    try
                                    {
                                        var objPrintOrden = new PrintOrderModel();
                                        objPrintOrden.Items = new List<PrintOrderItemModel>();

                                        foreach (var printerItem in items)
                                        {
                                            var objItem = new PrintOrderItemModel();

                                            if (objPending.DivideNum > 0 &&
                                                objPending.DivideNum != printerItem.DividerNum)
                                            {
                                                continue;
                                            }

                                            if (objPending.SeatNum > 0 && objPending.SeatNum != printerItem.SeatNum)
                                            {
                                                continue;
                                            }

                                            var servingSize = "";
                                            if (!string.IsNullOrEmpty(printerItem.ServingSizeName))
                                            {
                                                servingSize = $"({printerItem.ServingSizeName})";
                                            }

                                            objItem.Nombre = printerItem.Name + servingSize;
                                            objItem.Qty = printerItem.Qty.ToString();
                                            objItem.Amount = printerItem.SubTotal.ToString("C",
                                                (CultureInfo)CultureInfo.InvariantCulture);
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
                                                objItem.Opciones = srtOption.Substring(0, srtOption.Length - 1);
                                            }

                                            objPrintOrden.Items.Add(objItem);
                                        }

                                        #region Imprimiendo la Orden

                                        switch (objPending.Type)
                                        {
                                            case 0:
                                                objPrintOrden.Titulo = "PRE-CUENTA";
                                                break;
                                            case 1:
                                                objPrintOrden.Titulo = "COCINA";
                                                break;
                                            case 2:
                                                objPrintOrden.Titulo = "FACTURA";
                                                break;
                                            case 4:
                                                objPrintOrden.Titulo = "FACTURA";
                                                break;
                                            default:
                                                objPrintOrden.Titulo = "PRE-CUENTA";
                                                break;
                                        }


                                        objPrintOrden.Empresa = preference.Name;
                                        objPrintOrden.Empresa1 = preference.Company;
                                        objPrintOrden.Order = order.ID.ToString();
                                        string tableName = order.Table != null ? order.Table.Name : "";
                                        objPrintOrden.Table = tableName;
                                        objPrintOrden.Person = order.Person.ToString();
                                        objPrintOrden.Camarero = order.WaiterName;
                                        objPrintOrden.Rnc = preference.RNC;

                                        if (order.OrderType != OrderType.Delivery)
                                        {
                                            objPrintOrden.OrderType = order.OrderType.ToString();
                                        }
                                        else
                                        {
                                            var prepareType =
                                                _dbContext.PrepareTypes.FirstOrDefault(s =>
                                                    s.ID == order.PrepareTypeID);
                                            if (prepareType != null)
                                            {
                                                objPrintOrden.OrderType = prepareType.Name;
                                            }
                                            else
                                            {
                                                objPrintOrden.OrderType = "Para llevar";
                                            }
                                        }

                                        objPrintOrden.Propina =
                                            order.Propina.ToString("C", (CultureInfo)CultureInfo.InvariantCulture);

                                        if (transactions.Any())
                                        {
                                            objPrintOrden.Cajero = transactions.First().CreatedBy;
                                        }

                                        if (order.OrderMode == OrderMode.Conduce &&
                                            string.IsNullOrEmpty(order.ComprobanteNumber))
                                        {
                                            var conduceCount = _dbContext.Orders.Where(s => s.IsConduce).Count();
                                            objPrintOrden.Nfc = "Conduce #" + conduceCount;
                                            objPrintOrden.TipoFactura = "";
                                        }
                                        else
                                        {
                                            objPrintOrden.Nfc = order.ComprobanteNumber;
                                            objPrintOrden.TipoFactura = order.ComprobanteName;
                                        }

                                        if (order.Delivery == null)
                                        {
                                            objPrintOrden.Delivery = ((decimal)0.00).ToString("C",
                                                (CultureInfo)CultureInfo.InvariantCulture);
                                        }
                                        else
                                        {
                                            objPrintOrden.Delivery = order.Delivery.Value.ToString("C",
                                                (CultureInfo)CultureInfo.InvariantCulture);
                                        }

                                        string metodo = string.Empty;
                                        string cantidadp = string.Empty;
                                        decimal paidAmount = 0;
                                        decimal difference = 0;
                                        foreach (var t in transactions)
                                        {
                                            metodo += t.Method + (char)10;
                                            cantidadp +=
                                                (t.Amount + t.Difference).ToString("0.00",
                                                    CultureInfo.InvariantCulture) +
                                                (char)10;
                                            paidAmount += t.Amount;
                                            difference += t.Difference;
                                            Console.WriteLine(
                                                (t.Amount).ToString("0.00", CultureInfo.InvariantCulture) +
                                                " Cantidad dp");
                                        }

                                        if (metodo.Length > 0)
                                        {
                                            metodo = metodo.Substring(0, metodo.Length - 1);
                                            cantidadp = cantidadp.Substring(0, cantidadp.Length - 1);
                                        }
                                        // Monto pagado 
                                        // paidAmount

                                        //Cambio
                                        objPrintOrden.Metodo = metodo;
                                        objPrintOrden.Cantidaddp = cantidadp;
                                        objPrintOrden.Devuelto = transactions.Sum(s => s.Difference)
                                            .ToString("0.00", CultureInfo.InvariantCulture);

                                        objPrintOrden.PromesaPagoMonto =
                                            order.PromesaPago.ToString("C", (CultureInfo)CultureInfo.InvariantCulture);
                                        objPrintOrden.PromesaPagoDevuelto =
                                            (order.PromesaPago - order.TotalPrice).ToString("C",
                                                (CultureInfo)CultureInfo.InvariantCulture);
                                        objPrintOrden.Subtotal =
                                            order.SubTotal.ToString("C", (CultureInfo)CultureInfo.InvariantCulture);
                                        objPrintOrden.Descuento =
                                            order.Discount.ToString("C", (CultureInfo)CultureInfo.InvariantCulture);


                                        string descuentos = string.Empty;
                                        string espacio = "    ";
                                        foreach (var d in order.Discounts)
                                        {
                                            descuentos += d.Name + espacio + d.Amount + (char)10;
                                        }

                                        if (descuentos.Length > 0)
                                        {
                                            descuentos = descuentos.Substring(0, descuentos.Length - 1);
                                        }

                                        objPrintOrden.Descuentos = descuentos;
                                        objPrintOrden.Tax = order.Tax.ToString();


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

                                        objPrintOrden.Taxes = taxes;
                                        objPrintOrden.Taxes1 = taxes1;
                                        objPrintOrden.Total =
                                            order.TotalPrice.ToString("0.00", CultureInfo.CurrentCulture);
                                        objPrintOrden.Type = order.PrepareType?.ToString();


                                        if (customer != null)
                                        {
                                            objPrintOrden.CustomerRNC = customer.RNC;
                                            objPrintOrden.CustomerName = customer.Name;
                                            objPrintOrden.CustomerAddress = customer.Address1;
                                            objPrintOrden.CustomerPhone = customer.Phone;
                                        }

                                        #endregion


                                        //objPrintJob.Json = JsonSerializer.Serialize(objPrintOrden);
                                        objPrintJob.printJobOrder = objPrintOrden;
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        }

                        objResult.Lista.Add(objPrintJob);
                    }
                }
            }

            return Ok(objResult);
        }

        private Dictionary<PrinterChannel, List<OrderItem>> GetPrintersOfItems(List<OrderItem> items)
        {
            var result = new Dictionary<PrinterChannel, List<OrderItem>>();

            foreach (var item in items)
            {
                if (item.IsDeleted) continue;
                var product = _dbContext.Products.Include(s => s.PrinterChannels)
                    .FirstOrDefault(s => s.ID == item.Product.ID);
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


        [HttpGet("GetPreference")]
        [AllowAnonymous]
        public IActionResult GetPreference()
        {
            var preference = _dbContext.Preferences.FirstOrDefault();

            if (preference == null)
            {
                return NotFound(new { message = "No se encontró la configuración de la empresa." });
            }

            return Ok(new
            {
                Name = preference.Name,
                Company = preference.Company,
                Logo = preference.Logo,
                RNC = preference.RNC,
                Email = preference.Email,
                Phone = preference.Phone,
                Address1 = preference.Address1,
                Address2 = preference.Address2,
                City = preference.City,
                State = preference.State,
                PostalCode = preference.PostalCode,
                Country = preference.Country,
                Currency = preference.Currency,
                SecondCurrency = preference.SecondCurrency,
                Tasa = preference.Tasa
            });
        }

        public class Data
        {
            public long Id { get; set; }
        }

        [HttpPost("updatePrintJobStatus")]
        [AllowAnonymous]
        public async Task<IActionResult> updatePrintJobStatus([FromBody] Data data)
        {
            var pendingJob = await _dbContext.PrinterTasks.FirstOrDefaultAsync(s => s.ID == data.Id);

            if (pendingJob == null)
            {
                return NotFound("Trabajo de impresión no encontrado.");
            }

            // Actualizar el estado del trabajo de impresión aquí
            pendingJob.Status = (int)PrinterTasksStatus.Impreso;

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Estado del trabajo de impresión actualizado." });
        }
    }
}