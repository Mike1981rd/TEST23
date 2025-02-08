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


    namespace AuroraPOS.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        public class PrinterController : ControllerBase
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

            [HttpGet]
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
                        var objPrintJob = new PrintJobModel();
                        objPrintJob.Id = objPending.ID;
                        objPrintJob.Type =  objPending.Type.Value;
                        objPrintJob.PhysicalName =  objPending.PhysicalName;
                        objPrintJob.StationId =  objPending.StationId.HasValue ? objPending.StationId.Value : -1;
                        objPrintJob.SucursalId = objPending.SucursalId.HasValue ? objPending.SucursalId.Value : -1;
                        objPrintJob.ObjectId =  objPending.ObjectID.HasValue ? objPending.ObjectID.Value : -1;

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
                        order.GetTotalPrice(voucher, objPending.DivideNum.Value, objPending.SeatNum.Value);
                        var station = _dbContext.Stations.Include(s => s.Printers).ThenInclude(s => s.PrinterChannel)
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

                                        if (objPending.DivideNum > 0 && objPending.DivideNum != printerItem.DividerNum)
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

                                    objPrintOrden.Titulo = "PRE-CUENTA";
                                    objPrintOrden.Empresa = preference.Name;
                                    objPrintOrden.Empresa1 = preference.Company;
                                    objPrintOrden.Order = order.ID.ToString();
                                    string tableName = order.Table != null ? order.Table.Name : "";
                                    objPrintOrden.Table = tableName;
                                    objPrintOrden.Person = order.Person.ToString();
                                    objPrintOrden.Camarero = order.WaiterName;
                                    objPrintOrden.Rnc = preference.RNC;
                                    objPrintOrden.Devuelto = transactions.Sum(s => s.Difference)
                                        .ToString("C", (CultureInfo)CultureInfo.InvariantCulture);
                                    objPrintOrden.Propina =
                                        order.Propina.ToString("C", (CultureInfo)CultureInfo.InvariantCulture);



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
                                    objPrintOrden.Total = order.TotalPrice.ToString("0.00", CultureInfo.CurrentCulture);
                                    objPrintOrden.Type = order.PrepareType.ToString();


                                    if (customer != null)
                                    {
                                        objPrintOrden.CustomerRNC = customer.RNC;
                                        objPrintOrden.CustomerName = customer.Name;
                                        objPrintOrden.CustomerAddress = customer.Address1;
                                        objPrintOrden.CustomerPhone = customer.Phone;
                                    }

                                    #endregion

                                    objPrintJob.Json = JsonSerializer.Serialize(objPrintOrden);

                                }
                                catch
                                {
                                }
                            }

                            objResult.Lista.Add(objPrintJob);
                        }
                    }


                }

                return Ok(objResult); 
            }
            
        

    }
    }

