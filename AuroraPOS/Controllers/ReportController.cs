using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.Services;
using AuroraPOS.ViewModels;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Dynamic.Core;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Border = iText.Layout.Borders.Border;
using iText.Barcodes;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace AuroraPOS.Controllers
{
    [Authorize]
    public class ReportController : BaseController
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly AppDbContext _dbContext;

        public ReportController(ExtendedAppDbContext dbContext, IWebHostEnvironment hostingEnvironment)
        {
            _dbContext = dbContext._context;
            _hostingEnvironment = hostingEnvironment;
        }
        [HttpPost]
        public JsonResult GetAllBranchs()
        {
            return Json(_dbContext.t_sucursal.ToList());
        }
        public IActionResult Index()
        {
            return View();
        }
		[Authorize(Policy = "Permission.REPORT.SalesReport")]
		public IActionResult SalesReport()
        {
            ViewBag.Branchs = _dbContext.t_sucursal.ToList();
            ViewBag.Products = _dbContext.Products.Where(s=>s.IsActive && !s.IsDeleted).ToList();
            return View();
        }
		[Authorize(Policy = "Permission.REPORT.PurchaseReport")]
		public IActionResult PurchaseReport()
        {
			ViewBag.Groups = _dbContext.Groups.ToList();
			return View();
        }
		[Authorize(Policy = "Permission.REPORT.InventoryLevelReport")]
		public IActionResult InventoryLevelReport()
        {
			ViewBag.Groups = _dbContext.Groups.ToList();
			return View();
        }
		[Authorize(Policy = "Permission.REPORT.InventoryDepletionReport")]
		public IActionResult InventoryDepletionReport()
        {
			ViewBag.Groups = _dbContext.Groups.ToList();
			ViewBag.Warehouses = _dbContext.Warehouses.Where(s => s.IsActive).ToList();
            return View();
        }
		[Authorize(Policy = "Permission.REPORT.DetailedSalesReport")]
		public IActionResult DetailedSalesReport()
        {
            ViewBag.Branchs = _dbContext.t_sucursal.ToList();
            ViewBag.Users = _dbContext.User.Where(m => m.IsActive == true).ToList();
            ViewBag.Vouchers = _dbContext.Vouchers.Where(m => m.IsActive == true).ToList();
            return View();
        }
		[Authorize(Policy = "Permission.REPORT.PaymentMethodReport")]
		public IActionResult PaymentMethodReport()
        {
            return View();
        }
		[Authorize(Policy = "Permission.REPORT.VoidOrdersReport")]
		public IActionResult VoidOrdersReport()
        {
            return View();
        }
		[Authorize(Policy = "Permission.REPORT.VoidProductsReport")]
		public IActionResult VoidProductsReport()
        {
            return View();
        }
		[Authorize(Policy = "Permission.REPORT.CloseDrawerReport")]
		public IActionResult CloseDrawerReport()
        {
            ViewBag.Users = _dbContext.User.Where(s => s.IsActive).ToList();
            return View();
        }
		[Authorize(Policy = "Permission.REPORT.SalesInsightReport")]
		public IActionResult SalesInsightReport()
        {
            ViewBag.Branchs = _dbContext.t_sucursal.ToList();
            return View();
        }
		[Authorize(Policy = "Permission.REPORT.WorkDayReport")]
		public IActionResult WorkDayReport()
        {
            ViewBag.Users = _dbContext.User.Where(m => m.IsActive == true).ToList();
            ViewBag.Branchs = _dbContext.t_sucursal.ToList();
            return View();
        }


		[Authorize(Policy = "Permission.REPORT.OrdersReport")]
		public IActionResult OrdersReport()
        {
            ViewBag.Customers = _dbContext.Customers.Where(m => m.IsActive == true).ToList();
            ViewBag.Branchs = _dbContext.t_sucursal.ToList();
            return View();
        }

        [Authorize(Policy = "Permission.REPORT.ProductCost")]
        public IActionResult PercentCostReport()
		{
			return View();
		}

		[Authorize(Policy = "Permission.REPORT.CxCReport")]
		public IActionResult CXCReport()
        {
            return View();
        }
		[Authorize(Policy = "Permission.REPORT.CostoDeVenta")]
		public IActionResult InventoryByGroupReport()
        {
            ViewBag.Groups = _dbContext.Groups.ToList();
            ViewBag.Warehouses = _dbContext.Warehouses.Where(s => s.IsActive).ToList();
            return View();
		}
		[Authorize(Policy = "Permission.REPORT.VarianceReport")]
		public IActionResult VarianceReport()
        {
            ViewBag.Groups = _dbContext.Groups.ToList();
            ViewBag.Warehouses = _dbContext.Warehouses.Where(s => s.IsActive).ToList();
            return View();
        }

		[Authorize(Policy = "Permission.REPORT.ParReport")]
		public IActionResult ParReport()
        {
            return View();
        }

        public string GetMoneyString(decimal amount)
        {
            return /*AlfaHelper.Currency +*/ amount.ToString("N2", CultureInfo.InvariantCulture);
        }

        private List<SalesDetailReportModel> GenerateSalesDetailsReportModel( DateTime fromDate, DateTime toDate, int sucursal, int user, int voucher)
        {
            var result = new List<SalesDetailReportModel>();
            
            string UserFullName = "";

            if (user != 0)
            {
                UserFullName = _dbContext.User.Where(s => s.ID == user).First().FullName;
            }

            List<OrderTransaction> transactions;
            if (sucursal != 0)
            {
                transactions = _dbContext.OrderTransactions
                    .Include(s => s.Order)
                    .ThenInclude(o => o.Station)
                    .Where(s =>

                            s.PaymentDate.Date >= fromDate.Date &&
                            s.PaymentDate.Date <= toDate.Date &&
                                _dbContext.Orders.Any(o =>
                                    o.Station == s.Order.Station &&
                                    (o.Station.IDSucursal == sucursal || sucursal == 0) &&
                                    (o.CreatedBy == UserFullName || string.IsNullOrEmpty(UserFullName)) &&
                                    (o.ComprobantesID == voucher || voucher == 0)
                                    )
                                )
                    .OrderByDescending(s => s.PaymentDate)
                    .ToList();


            }
            else
            {
                transactions = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.PaymentDate.Date >= fromDate.Date && s.PaymentDate.Date <= toDate.Date &&
                    (s.Order.CreatedBy == UserFullName || string.IsNullOrEmpty(UserFullName)) &&
                    (s.Order.ComprobantesID == voucher || voucher == 0)).OrderByDescending(s => s.PaymentDate).ToList();
            }

            return(GenerateListDetailReportModel(transactions));
        }
        
        private void SetBorder(ExcelRange Celdas)
        {
            Celdas.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            Celdas.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            Celdas.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            Celdas.Style.Border.Left.Style = ExcelBorderStyle.Thin;

        }

        [Authorize(Policy = "Permission.REPORT.DetailedSalesReport")]
        [SuppressMessage("ReSharper.DPA", "DPA0007: Large number of DB records", MessageId = "count: 223")]
        public IActionResult GenerateSalesDetailsReportXLS(string from, string to, int sucursal, int user, int voucher)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            
            var result = GenerateSalesDetailsReportModel(fromDate,toDate,sucursal, user, voucher);
            int iFila = 1;
            
            using (ExcelPackage package = new ExcelPackage())
                {
                    // Los datos estaran en la primera hoja
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Reporte");
                    
                    var store = _dbContext.Preferences.First();
                    if (store != null)
                    {

                        worksheet.Cells["A" + iFila.ToString()].Value = store.Name;
                        worksheet.Cells["I" + iFila.ToString()].Value = "Fecha: " + DateTime.Today.ToString("MM/dd/yy");
                        iFila = iFila + 1;

                        worksheet.Cells["A" + iFila.ToString()].Value = store.Address1;
                        worksheet.Cells["I" + iFila.ToString()].Value = "Hora: " + DateTime.Now.ToString("hh:mm tt");
                        iFila = iFila + 1;

                        worksheet.Cells["A" + iFila.ToString()].Value = "RNC: " + store.RNC;
                        worksheet.Cells["I" + iFila.ToString()].Value = "Usuario: " + User.Identity.GetName();
                        iFila = iFila + 1;

                        worksheet.Cells["A" + iFila.ToString()].Value = "Telefono:" + store.Phone;
                        iFila = iFila + 2;

                    }

                    //var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                    //var time = "Fecha: " + DateTime.Today.ToString("MM/dd/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();
                    
                    worksheet.Cells["E" + iFila.ToString()].Value = "Venta Detallada";
                    worksheet.Cells["E" + iFila.ToString()].Style.Font.Bold = true;
                    iFila = iFila + 2;
                    
                    worksheet.Cells["A" + iFila.ToString()].Value = "Numero";
                    worksheet.Cells["A" + iFila.ToString()].Style.Font.Bold = true;
                    SetBorder(worksheet.Cells["A" + iFila.ToString()]);
                    worksheet.Cells["B" + iFila.ToString()].Value = "SubTotal";
                    worksheet.Cells["B" + iFila.ToString()].Style.Font.Bold = true;
                    SetBorder(worksheet.Cells["B" + iFila.ToString()]);
                    worksheet.Cells["C" + iFila.ToString()].Value = "Discount";
                    worksheet.Cells["C" + iFila.ToString()].Style.Font.Bold = true;
                    SetBorder(worksheet.Cells["C" + iFila.ToString()]);
                    worksheet.Cells["D" + iFila.ToString()].Value = "Itbis";
                    worksheet.Cells["D" + iFila.ToString()].Style.Font.Bold = true;
                    SetBorder(worksheet.Cells["D" + iFila.ToString()]);
                    worksheet.Cells["E" + iFila.ToString()].Value = "Propina";
                    worksheet.Cells["E" + iFila.ToString()].Style.Font.Bold = true;
                    SetBorder(worksheet.Cells["E" + iFila.ToString()]);
                    worksheet.Cells["F" + iFila.ToString()].Value = "Delivery";
                    worksheet.Cells["F" + iFila.ToString()].Style.Font.Bold = true;
                    SetBorder(worksheet.Cells["F" + iFila.ToString()]);
                    worksheet.Cells["G" + iFila.ToString()].Value = "Tip";
                    worksheet.Cells["G" + iFila.ToString()].Style.Font.Bold = true;
                    SetBorder(worksheet.Cells["G" + iFila.ToString()]);
                    worksheet.Cells["H" + iFila.ToString()].Value = "Total";
                    worksheet.Cells["H" + iFila.ToString()].Style.Font.Bold = true;
                    SetBorder(worksheet.Cells["H" + iFila.ToString()]);
                    worksheet.Cells["I" + iFila.ToString()].Value = "Metodo";
                    worksheet.Cells["I" + iFila.ToString()].Style.Font.Bold = true;
                    SetBorder(worksheet.Cells["I" + iFila.ToString()]);
                    worksheet.Cells["J" + iFila.ToString()].Value = "Client";
                    worksheet.Cells["J" + iFila.ToString()].Style.Font.Bold = true;
                    SetBorder(worksheet.Cells["J" + iFila.ToString()]);
                    worksheet.Cells["K" + iFila.ToString()].Value = "RNC";
                    worksheet.Cells["K" + iFila.ToString()].Style.Font.Bold = true;
                    SetBorder(worksheet.Cells["K" + iFila.ToString()]);
                    iFila = iFila + 1;


                    foreach (var objFila in result)
                    {
                        worksheet.Cells["A" + iFila.ToString()].Value = objFila.ComprobanteNumber;
                        worksheet.Cells["B" + iFila.ToString()].Value = objFila.SubTotal;
                        worksheet.Cells["C" + iFila.ToString()].Value = objFila.Discount;
                        worksheet.Cells["D" + iFila.ToString()].Value = objFila.Tax;
                        worksheet.Cells["E" + iFila.ToString()].Value = objFila.Propina;
                        worksheet.Cells["F" + iFila.ToString()].Value = objFila.Delivery;
                        worksheet.Cells["G" + iFila.ToString()].Value = objFila.Tip;                        
                        worksheet.Cells["H" + iFila.ToString()].Value = objFila.Amount;
                        worksheet.Cells["I" + iFila.ToString()].Value = objFila.PaymentMethod;
                        worksheet.Cells["J" + iFila.ToString()].Value = objFila.ClientName;
                        worksheet.Cells["K" + iFila.ToString()].Value = objFila.ClientRNC;
                        worksheet.Cells["B" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                        worksheet.Cells["C" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                        worksheet.Cells["D" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                        worksheet.Cells["E" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                        worksheet.Cells["F" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                        worksheet.Cells["G" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                        worksheet.Cells["H" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                        iFila = iFila + 1;
                    }
                    
                    worksheet.Cells["A" + iFila.ToString()].Value = "Totales:";
                    SetBorder(worksheet.Cells["A" + iFila.ToString()]);
                    worksheet.Cells["B" + iFila.ToString()].Value = result.Sum(s=>s.SubTotal);
                    SetBorder(worksheet.Cells["B" + iFila.ToString()]);
                    worksheet.Cells["C" + iFila.ToString()].Value = result.Sum(s=>s.Discount);
                    SetBorder(worksheet.Cells["C" + iFila.ToString()]);
                    worksheet.Cells["D" + iFila.ToString()].Value = result.Sum(s=>s.Tax);;
                    SetBorder(worksheet.Cells["D" + iFila.ToString()]);
                    worksheet.Cells["E" + iFila.ToString()].Value = result.Sum(s=>s.Propina);;
                    SetBorder(worksheet.Cells["E" + iFila.ToString()]);
                    worksheet.Cells["F" + iFila.ToString()].Value = result.Sum(s=>s.Delivery);;
                    SetBorder(worksheet.Cells["F" + iFila.ToString()]);
                    worksheet.Cells["G" + iFila.ToString()].Value = result.Sum(s=>s.Tip);;
                    SetBorder(worksheet.Cells["G" + iFila.ToString()]);
                    worksheet.Cells["H" + iFila.ToString()].Value = result.Sum(s=>s.Amount);;
                    SetBorder(worksheet.Cells["H" + iFila.ToString()]);
                    worksheet.Cells["I" + iFila.ToString()].Value = "";
                    SetBorder(worksheet.Cells["I" + iFila.ToString()]);
                    worksheet.Cells["J" + iFila.ToString()].Value = "";
                    SetBorder(worksheet.Cells["J" + iFila.ToString()]);
                    worksheet.Cells["K" + iFila.ToString()].Value = "";
                    SetBorder(worksheet.Cells["K" + iFila.ToString()]);
                    worksheet.Cells["B" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                    worksheet.Cells["C" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                    worksheet.Cells["D" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                    worksheet.Cells["E" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                    worksheet.Cells["F" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                    worksheet.Cells["G" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                    worksheet.Cells["H" + iFila.ToString()].Style.Numberformat.Format = "$###,###,##0.00";
                    
                    worksheet.Column(1).Width = 18;
                    worksheet.Column(2).Width = 18;
                    worksheet.Column(3).Width = 18;
                    worksheet.Column(4).Width = 18;
                    worksheet.Column(5).Width = 18;
                    worksheet.Column(6).Width = 18;
                    worksheet.Column(7).Width = 18;
                    worksheet.Column(8).Width = 18;
                    worksheet.Column(9).Width = 18;
                    worksheet.Column(10).Width = 18;
                    worksheet.Column(11).Width = 18;

                    string strNombre = "Reporte" + DateTime.Now.ToString("ddMMyyyy_HHmmss");

                    //convert the excel package to a byte array
                    byte[] bin = package.GetAsByteArray();

                    //clear the buffer stream
                    //HttpContext.Response.ClearHeaders();
                    HttpContext.Response.Clear();
                    //HttpContext.Response.Buffer = true;

                    //set the correct contenttype
                    HttpContext.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    //set the correct length of the data being send
                    HttpContext.Response.Headers.Add("content-length", bin.Length.ToString());

                    //set the filename for the excel package
                    HttpContext.Response.Headers.Add("content-disposition", "attachment; filename=\"" + strNombre + ".xlsx\"");

                    //send the byte array to the browser
                    return File(bin, "application/vnd.ms-excel");
                }
            
        }

        [Authorize(Policy = "Permission.REPORT.DetailedSalesReport")]
		[HttpPost]
        [SuppressMessage("ReSharper.DPA", "DPA0007: Large number of DB records", MessageId = "count: 223")]
        public JsonResult GenerateSalesDetailsReport(string from, string to, int sucursal, int user, int voucher)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            
            var result = GenerateSalesDetailsReportModel(fromDate,toDate,sucursal, user, voucher);

            // write report
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "SalesDetailsReport_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;


            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
                // REPORT HEADER
                {
                    string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                    var store = _dbContext.Preferences.First();
                    if (store != null)
                    {
                        var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();


                        var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;


                        var time = "Fecha: " + DateTime.Today.ToString("MM/dd/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));
                        Cell cell2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        Image img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cell2.Add(img.ScaleToFit(100, 60));

                        }

                        var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cell1);
                        headertable.AddCell(cell2);
                        headertable.AddCell(cell3);

                        doc.Add(headertable);
                    }
                }

                Paragraph header = new Paragraph("Venta Detallada")
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(25);
                doc.Add(header);
                Paragraph header1 = new Paragraph(fromDate.ToString("dd-MM-yyyy") + " to " + toDate.ToString("dd-MM-yyyy"))
                              .SetTextAlignment(TextAlignment.CENTER)
                              .SetFontColor(ColorConstants.GRAY)
                              .SetFontSize(12);
                doc.Add(header1);
                decimal totalAmount = 0;
                decimal totalSubTotal = 0;
                decimal totalTax = 0;
                decimal totalPropina = 0;
                decimal totalDelivery = 0;
                decimal totalTip = 0;
                decimal totalDiscount = 0;

                {
                    var salesTable = new iText.Layout.Element.Table(new float[] { 2, 1, 1, 1, 1, 1, 1, 1, 1 });
                    salesTable.SetWidth(UnitValue.CreatePercentValue(100));
                  
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Numero").SetFontSize(12));

                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("SubTotal").SetFontSize(12));
                    var cell10 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Descuento").SetFontSize(12));
                    var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Itbis").SetFontSize(12));

                    var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Propina").SetFontSize(12));
                    var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Domicilio").SetFontSize(12));
                    var cell65 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Propina Vol").SetFontSize(12));
                    var cell7 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Total").SetFontSize(12));
                    var cell8 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Metodo").SetFontSize(12));
                    //var cell9 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Forma").SetFontSize(12));

                    salesTable.AddCell(cell2).AddCell(cell3).AddCell(cell10).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell65).AddCell(cell7).AddCell(cell8); //.AddCell(cell9);


                    foreach (var sale in result)
                    {
  
                        var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + sale.ComprobanteNumber).SetFontSize(11));
                        var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.SubTotal)).SetFontSize(11));
                        var cell210 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.Discount)).SetFontSize(11));
                        var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.Tax)).SetFontSize(11));
                        var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.Propina)).SetFontSize(11));
                        var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.Delivery)).SetFontSize(11));
                        var cell265 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.Tip)).SetFontSize(11));
                        var cell27 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.Amount)).SetFontSize(11));
                        var cell28 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + sale.PaymentMethod).SetFontSize(11));
                        //var cell29 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + sale.PaymentType).SetFontSize(11));

                        salesTable.AddCell(cell22).AddCell(cell23).AddCell(cell210).AddCell(cell24).AddCell(cell25).AddCell(cell26).AddCell(cell265).AddCell(cell27).AddCell(cell28); //.AddCell(cell29);
                        totalAmount += sale.Amount;
                        totalSubTotal += sale.SubTotal;
                        totalTax += sale.Tax;
                        totalPropina += sale.Propina;
                        totalDelivery += sale.Delivery;
                        totalTip += sale.Tip;
                        totalDiscount += sale.Discount;

                    }
                    var cell31 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Total").SetFontSize(13));
       
                    var cell34 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalSubTotal)).SetFontSize(13));
                    var cell310 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalDiscount)).SetFontSize(13));
                    var cell35 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalTax)).SetFontSize(13));

                    var cell36 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalPropina)).SetFontSize(13));
                    var cell365 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalTip)).SetFontSize(13));
                    var cell37 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalDelivery)).SetFontSize(13));
                    var cell38 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalAmount)).SetFontSize(13));
                    salesTable.AddCell(cell31).AddCell(cell34).AddCell(cell310).AddCell(cell35).AddCell(cell36).AddCell(cell37).AddCell(cell365).AddCell(cell38); ;

                    doc.Add(salesTable);

                }

                doc.Close();

            }
            return Json(new { status = 0, url = uploadsUrl });
        }


        public List<SalesDetailReportModel> GenerateListDetailReportModel(List<OrderTransaction> transactions) {

            var lstOrdenes = new List<Order>();
            var result = new List<SalesDetailReportModel?>();

            foreach (var tran in transactions)
            {

                if (tran.Order == null) continue;

                var comprobantes = _dbContext.OrderComprobantes.Where(s => s.OrderId == tran.Order.ID).ToList();

                foreach (var comp in comprobantes)
                {
                    var d = new SalesDetailReportModel();
                    if (comp.ComprobanteNumber == tran.Order.ComprobanteNumber)
                    {

                        if (!lstOrdenes.Contains(tran.Order))
                        {

                            if (tran.Type != TransactionType.Tip)
                            {
                                lstOrdenes.Add(tran.Order);
                            }

                            d.SubTotal = (tran.Order.SubTotal);
                            d.Tax = tran.Order.Tax;
                            d.Propina = tran.Order.Propina;
                            d.Discount = tran.Order.Discount;
                            d.Delivery = tran.Order.Delivery ?? 0;
                        }
                        else
                        {
                            d.SubTotal = 0;
                            d.Tax = 0;
                            d.Propina = 0;
                            d.Discount = 0;
                            d.Delivery = 0;
                        }

                        d.ClientName = tran.Order.ClientName ?? "";

                        if (tran.Order.CustomerId > 0)
                        {
                            var objClient = _dbContext.Customers.Where(s => s.ID == tran.Order.CustomerId).FirstOrDefault();

                            if (objClient != null)
                            {
                                d.ClientName = objClient.Name;
                                d.ClientRNC =  objClient.RNC;    
                            }
                        }
                        
                        d.PayDate = tran.PaymentDate;
                        d.Amount = tran.Amount;
                        d.PaymentMethod = tran.Method;
                        d.PaymentType = tran.PaymentType;
                        d.Comprobante = tran.Order.ComprobanteName;
                        d.ComprobanteNumber = tran.Order.ComprobanteNumber;

                        d.Order = tran.Order.ID;
                        d.UsuarioCreacion = tran.Order.CreatedBy;
                        d.FechaCreacion = (new DateTime(tran.UpdatedDate.Year, tran.UpdatedDate.Month, tran.UpdatedDate.Day, tran.Order.CreatedDate.Hour, tran.Order.CreatedDate.Minute, tran.Order.CreatedDate.Second)).ToString("g");
                        d.UsuarioPago = tran.CreatedBy;
                        d.FechaPago = tran.PaymentDate.ToString("g");

                        if (tran.Type == TransactionType.Refund)
                        {
                            d.SubTotal = -(tran.Order.SubTotal);
                            d.Tax = -tran.Order.Tax;
                            d.Propina = -tran.Order.Propina;
                            d.Discount = -tran.Order.Discount;

                        }

                        if (tran.Type == TransactionType.Tip)
                        {
                            d.Tip = tran.Amount;
                            d.Tax = 0;
                            d.Amount = 0;
                            d.SubTotal = 0;
                            d.Discount = 0;
                            d.Propina = 0;
                        }

                        d.SubTotal = Math.Round(d.SubTotal, 2, MidpointRounding.AwayFromZero);
                        d.Tax = Math.Round(d.Tax, 2, MidpointRounding.AwayFromZero);
                        d.Propina = Math.Round(d.Propina, 2, MidpointRounding.AwayFromZero);
                        d.Amount = Math.Round(d.Amount, 2, MidpointRounding.AwayFromZero);

                        result.Add(d);
                    }
                    else
                    {

                        d.PayDate = comp.CreatedDate;
                        d.ClientName = "";
                        d.ClientRNC = "";
                        d.Amount = 0;
                        d.SubTotal = 0;
                        d.Tax = 0;
                        d.Propina = 0;
                        d.Discount = 0;
                        d.Delivery = 0;
                        d.PaymentMethod = "";
                        d.PaymentType = "";
                        d.Comprobante = comp.ComprobanteName;
                        d.ComprobanteNumber = comp.ComprobanteNumber;

                        if (tran.Type == TransactionType.Tip)
                        {
                            d.Tip = tran.Amount;
                            d.Tax = 0;
                            d.Amount = 0;
                            d.SubTotal = 0;
                            d.Discount = 0;
                            d.Propina = 0;
                        }

                        d.SubTotal = Math.Round(d.SubTotal, 2, MidpointRounding.AwayFromZero);
                        d.Tax = Math.Round(d.Tax, 2, MidpointRounding.AwayFromZero);
                        d.Propina = Math.Round(d.Propina, 2, MidpointRounding.AwayFromZero);
                        d.Amount = Math.Round(d.Amount, 2, MidpointRounding.AwayFromZero);

                        result.Add(d);
                    }
                }


            }

            return result;
        }


        [Authorize(Policy = "Permission.REPORT.SalesReport")]
        [HttpPost]
        public JsonResult GenerateSalesReport(string from, string to, int sucursal, int productId)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            List<OrderItem> items;
            if (sucursal != 0)
            {
                items = _dbContext.OrderItems
                    .Include(s => s.Order)
                    .Include(s => s.Product)
                    .ThenInclude(s => s.Category).Include(s => s.Product)
                    .ThenInclude(s => s.ServingSizes)
                    .Include(s => s.Taxes)
                    .Where(s =>
                        s.Status == OrderItemStatus.Paid &&
                        s.UpdatedDate.Date >= fromDate.Date &&
                        s.UpdatedDate.Date <= toDate.Date &&
                        _dbContext.Orders.Any(o =>
                        o.Station == s.Order.Station &&
                        (o.Station.IDSucursal == sucursal || sucursal == 0)
                        &&
                        (s.Product.ID == productId || productId == 0))
                        )
                        .ToList();

            }
            else
            {
                items = _dbContext.OrderItems.Include(s => s.Order).Include(s => s.Product).ThenInclude(s => s.ServingSizes).Include(s => s.Product).ThenInclude(s => s.Category).Include(s => s.Taxes).Where(s => s.Status == OrderItemStatus.Paid && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date && (productId == 0 || s.Product.ID == productId)).ToList();
            }
            var groups = _dbContext.MenuGroups.ToList();
            var categories = _dbContext.MenuCategories.ToList();


            var sales = new List<SalesReportGroup>();
            var taxes = new List<SalesReportTaxModel>();
            var propinas = new List<SalesReportPropinaModel>();

            var lstOrdenes = new List<long>();

            foreach (var item in items)
            {
                var menuProduct = _dbContext.MenuProducts.FirstOrDefault(s => s.ID == item.MenuProductID);                

                if (!lstOrdenes.Contains(item.Order.ID)) {
                    item.SubTotal = item.SubTotal - item.Order.Discount;
                    lstOrdenes.Add(item.Order.ID);
                }

                var groupName = "";
                var categoryName = "";
                if (menuProduct != null)
                {
                    var group = groups.FirstOrDefault(s => s.ID == menuProduct.GroupID);
                    var category = categories.FirstOrDefault(s => s.ID == menuProduct.CategoryID);

                    if (group != null)
                    {
                        groupName = group.Name;
                        categoryName = category.Name;
                    }
                    else
                    {
                        groupName = "N/A";
                        categoryName = "N/A";
                    }
                }
                else
                {
                    groupName = "N/A";
                    categoryName = "N/A";
                }

                var existgroup = sales.FirstOrDefault(s => s.Group == groupName);
                if (existgroup == null)
                {
                    existgroup = new SalesReportGroup()
                    {
                        Group = groupName,
                        Categories = new List<SalesReportCategory>()
                    };
                    sales.Add(existgroup);
                }

                var existcategory = existgroup.Categories.FirstOrDefault(s => s.Category == categoryName);
                if (existcategory == null)
                {
                    existcategory = new SalesReportCategory() { Category = categoryName, Products = new List<SalesReportProductModel>() };
                    existgroup.Categories.Add(existcategory);
                }
                var servingSize = new ProductServingSize();
                if (item.Product.HasServingSize)
                {
                    servingSize = item.Product.ServingSizes.FirstOrDefault(s => s.ServingSizeID == item.ServingSizeID);
                }
                var existproduct = existcategory.Products.FirstOrDefault(s => s.ProductId == item.Product.ID && s.ServingSizeID == item.ServingSizeID);
                
                if (existproduct == null)
                {
                    var m = new SalesReportProductModel()
                    {
                        Group = groupName,
                        Category = categoryName,
                        ProductName = item.Product.Name,
                        ProductId = item.Product.ID,
                        ServingSizeID = servingSize == null? 0: servingSize.ID,
                        ServingSizeName = servingSize?.ServingSizeName,
                        Qty = item.Qty,
                        Sales = item.SubTotal,
                        Cost = item.Product.ProductCost * item.Qty,
                        Profit = item.SubTotal - item.Product.ProductCost * item.Qty
                    };
                    existcategory.Products.Add(m);
                }
                else
                {
                    existproduct.Qty += item.Qty;
                    existproduct.Sales += item.SubTotal ;
                    existproduct.Cost += item.Product.ProductCost * item.Qty;
                    existproduct.Profit += item.SubTotal - item.Product.ProductCost * item.Qty;
                }

                foreach (var t in item.Taxes)
                {
                    var existtax = taxes.FirstOrDefault(s => s.TaxName == t.Description);
                    if (existtax == null)
                    {
                        var tax = new SalesReportTaxModel()
                        {
                            TaxName = t.Description,
                            Percent = t.Percent,
                            Taxable = item.SubTotal
                        };
                        if (t.IsExempt)
                        {
                            tax.TaxExempt = t.Amount;
                        }
                        else
                        {
                            tax.Tax = t.Amount;
                        }

                        taxes.Add(tax);
                    }
                    else
                    {
                        existtax.Taxable += item.SubTotal;
                        if (t.IsExempt)
                        {
                            existtax.TaxExempt += t.Amount;
                        }
                        else
                        {
                            existtax.Tax += t.Amount;
                        }
                    }
                }

                if (item.Propinas != null && item.Propinas.Any())
                {
                    foreach (var t in item.Propinas)
                    {
                        var existtax = propinas.FirstOrDefault(s => s.PropinaName == t.Description);
                        if (existtax == null)
                        {
                            var tax = new SalesReportPropinaModel()
                            {
                                PropinaName = t.Description,
                                Percent = t.Percent,
                            };
                            if (t.IsExempt)
                            {
                                tax.PropinaExempt = t.Amount;
                            }
                            else
                            {
                                tax.Propina = t.Amount;
                            }

                            propinas.Add(tax);
                        }
                        else
                        {
                            if (t.IsExempt)
                            {
                                existtax.PropinaExempt += t.Amount;
                            }
                            else
                            {
                                existtax.Propina += t.Amount;
                            }
                        }
                    }
                }


            }

            sales = sales.OrderBy(s => s.Group).ToList();
            foreach (var g in sales)
            {
                foreach (var c in g.Categories)
                {
                    foreach (var p in c.Products)
                    {
                        g.Qty += p.Qty;
                        g.Sales += p.Sales;
                        g.Cost += p.Cost;
                    }
                }

                g.CostPercent = g.Cost / g.Sales * 100.0m;
                g.Profit = g.Sales - g.Cost;
            }


            // write report
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "SalesReport_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;


            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
                // REPORT HEADER
                {
                    string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");

                    var store = _dbContext.Preferences.First();


                    if (store != null)
                    {
                        var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();


                        var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;


                        var time = "Fecha: " + DateTime.Today.ToString("MM/dd/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));
                        Cell cell2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        Image img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cell2.Add(img.ScaleToFit(100, 60));

                        }

                        var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cell1);
                        headertable.AddCell(cell2);
                        headertable.AddCell(cell3);

                        doc.Add(headertable);
                    }
                }

                Paragraph header = new Paragraph("Reporte de Ventas")
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(25);
                doc.Add(header);
                Paragraph header1 = new Paragraph(fromDate.ToString("dd-MM-yyyy") + " to " + toDate.ToString("dd-MM-yyyy"))
                              .SetTextAlignment(TextAlignment.CENTER)
                              .SetFontColor(ColorConstants.GRAY)
                              .SetFontSize(12);
                doc.Add(header1);
                decimal totalQty = 0;
                decimal totalSale = 0;
                decimal totalCost = 0;
                {
                    Paragraph subheader = new Paragraph(" - Group Summary")
                               .SetTextAlignment(TextAlignment.LEFT)
                               .SetFontSize(15);
                    doc.Add(subheader);
                    var grouptable = new iText.Layout.Element.Table(new float[] { 3, 1, 1, 1, 1, 1 });
                    grouptable.SetWidth(UnitValue.CreatePercentValue(100));

                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Descripcion").SetFontSize(12));
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Qty").SetFontSize(12));
                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Valor").SetFontSize(12));
                    var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Costo").SetFontSize(12));
                    var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Costo %").SetFontSize(12));
                    var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Rentabilidad").SetFontSize(12));
                    grouptable.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6);


                    foreach (var sale in sales)
                    {
                        var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(sale.Group).SetFontSize(11));
                        var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + sale.Qty).SetFontSize(11));
                        var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.Sales)).SetFontSize(11));
                        var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.Cost)).SetFontSize(11));
                        var cell15 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.CostPercent)).SetFontSize(11));
                        var cell16 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.Profit)).SetFontSize(11));
                        grouptable.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16);
                        totalQty += sale.Qty;
                        totalSale += sale.Sales;
                        totalCost += sale.Cost;
                    }
                    decimal totalProfit = totalSale - totalCost;
                    decimal totalCostPercent = 0;
                    if (totalSale > 0)
                        totalCostPercent = totalCost / totalSale * 100;
                    var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Grand Total").SetFontSize(13));
                    var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalQty)).SetFontSize(13));
                    var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalSale)).SetFontSize(13));
                    var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalCost)).SetFontSize(13));
                    var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalCostPercent)).SetFontSize(13));
                    var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalProfit)).SetFontSize(13));
                    grouptable.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25).AddCell(cell26);

                    doc.Add(grouptable);

                }

                {
                    doc.Add(new Paragraph(""));
                    Paragraph subheader = new Paragraph(" - Tax Summary")
                               .SetTextAlignment(TextAlignment.LEFT)
                               .SetFontSize(15);
                    doc.Add(subheader);

                    var taxtable = new iText.Layout.Element.Table(new float[] { 4, 1, 1, 1 });
                    taxtable.SetWidth(UnitValue.CreatePercentValue(100));
                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Descripcion").SetFontSize(12));
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Imponible").SetFontSize(12));
                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Exento").SetFontSize(12));
                    var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Impuesto").SetFontSize(12));
                    taxtable.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4);

                    decimal totalTaxable = 0;
                    decimal totalTaxExempt = 0;
                    decimal totalTax = 0;
                    foreach (var tax in taxes)
                    {
                        var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(tax.TaxName).SetFontSize(11));
                        var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(tax.Taxable)).SetFontSize(11));
                        var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(tax.TaxExempt)).SetFontSize(11));
                        var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(tax.Tax)).SetFontSize(11));
                        taxtable.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14);
                        totalTax += tax.Tax;
                        totalTaxable += tax.Taxable;
                        totalTaxExempt += tax.TaxExempt;
                    }
                    var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Total").SetFontSize(13));
                    var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("").SetFontSize(13));
                    var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalTaxExempt)).SetFontSize(13));
                    var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalTax)).SetFontSize(13));
                    taxtable.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24);

                    doc.Add(taxtable);
                }

                {
                    doc.Add(new Paragraph(""));
                    Paragraph subheader = new Paragraph(" - Propina Summary")
                               .SetTextAlignment(TextAlignment.LEFT)
                               .SetFontSize(15);
                    doc.Add(subheader);

                    var taxtable = new iText.Layout.Element.Table(new float[] { 4, 1, 1 });
                    taxtable.SetWidth(UnitValue.CreatePercentValue(100));
                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Descripcion").SetFontSize(12));

                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Propina exempt").SetFontSize(12));
                    var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Propina").SetFontSize(12));
                    taxtable.AddCell(cell1).AddCell(cell3).AddCell(cell4);

                    decimal totalPropinaExempt = 0;
                    decimal totalPropina = 0;
                    foreach (var tax in propinas)
                    {
                        var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(tax.PropinaName).SetFontSize(11));

                        var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(tax.PropinaExempt)).SetFontSize(11));
                        var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(tax.Propina)).SetFontSize(11));
                        taxtable.AddCell(cell11).AddCell(cell13).AddCell(cell14);
                        totalPropina += tax.Propina;
                        totalPropinaExempt += tax.PropinaExempt;
                    }
                    var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Total").SetFontSize(13));

                    var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalPropinaExempt)).SetFontSize(13));
                    var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalPropina)).SetFontSize(13));
                    taxtable.AddCell(cell21).AddCell(cell23).AddCell(cell24);

                    doc.Add(taxtable);
                }
                {
                    doc.Add(new Paragraph(""));
                    Paragraph subheader = new Paragraph(" - Product Summary")
                               .SetTextAlignment(TextAlignment.LEFT)
                               .SetFontSize(15);
                    doc.Add(subheader);

                    var prodtable = new iText.Layout.Element.Table(new float[] { 2, 1, 1, 1, 1, 1, 1, 1 });
                    prodtable.SetWidth(UnitValue.CreatePercentValue(100));

                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Producto").SetFontSize(12));
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("ID #").SetFontSize(12));
                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Vendidos #").SetFontSize(12));
                    var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Ventas").SetFontSize(12));
                    var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("% Ventas").SetFontSize(12));
                    var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Costo").SetFontSize(12));
                    var cell7 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Rentabilidad").SetFontSize(12));
                    var cell8 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("% Rentabilidad").SetFontSize(12));
                    prodtable.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell7).AddCell(cell8);

                    foreach (var sale in sales)
                    {
                        foreach (var c in sale.Categories)
                        {
                            if (c.Products.Count == 0) continue;
                            var cell31 = new Cell(1, 8).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(c.Category).SetBold().SetFontSize(13));
                            prodtable.AddCell(cell31);
                            decimal categoryTotal = 0;
                            decimal profitTotal = 0;
                            foreach (var p in c.Products)
                            {
                                if (p.Sales == 0) continue;
                                decimal salespercent = 0;
                                if (totalSale > 0)
                                    salespercent = p.Sales / totalSale;

                                categoryTotal += p.Sales;
                                var profit = p.Sales - p.Cost;
                                profitTotal += profit;
                                var profitPercent = profit / p.Sales;
                                var productName = p.ProductName;
                                if (!string.IsNullOrEmpty(p.ServingSizeName))
                                {
                                    productName += "(" + p.ServingSizeName + ")";
                                }
                                var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(productName).SetFontSize(11));
                                var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.ProductId).SetFontSize(11));
                                var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(p.Qty.ToString("0")).SetFontSize(11));
                                var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(p.Sales)).SetFontSize(11));
                                var cell15 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(salespercent.ToString("0.00%")).SetFontSize(11));
                                var cell16 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(p.Cost)).SetFontSize(11));
                                var cell17 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(profit)).SetFontSize(11));
                                var cell18 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(profitPercent.ToString("0.00%")).SetFontSize(11));
                                prodtable.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16).AddCell(cell17).AddCell(cell18);
                            }
                            var cell111 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(11));
                            var cell112 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(11));
                            var cell113 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(11));
                            var cell114 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetBorderTop(new SolidBorder(0.5f)).SetBold().Add(new Paragraph(GetMoneyString(categoryTotal)).SetFontSize(11));
                            var cell115 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(11));
                            var cell116 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph().SetFontSize(11));
                            var cell117 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetBorderTop(new SolidBorder(0.5f)).SetBold().Add(new Paragraph(GetMoneyString(profitTotal)).SetFontSize(11));
                            var cell118 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph().SetFontSize(11));
                            prodtable.AddCell(cell111).AddCell(cell112).AddCell(cell113).AddCell(cell114).AddCell(cell115).SetBorderTop(new SolidBorder(0.5f)).AddCell(cell116).AddCell(cell117).AddCell(cell118);

                        }
                    }

                    doc.Add(prodtable);
                }

                doc.Close();

            }
            return Json(new { status = 0, url = uploadsUrl });
        }

        [Authorize(Policy = "Permission.REPORT.SalesReport")]
        [HttpPost]
        public JsonResult GenerateSalesExcelReport(string from, string to, int sucursal, int productId)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            List<OrderItem> items;
            if (sucursal != 0)
            {
                items = _dbContext.OrderItems
                    .Include(s => s.Order)
                    .Include(s => s.Product)
                    .ThenInclude(s => s.Category).Include(s => s.Product)
                    .ThenInclude(s => s.ServingSizes)
                    .Include(s => s.Taxes)
                    .Where(s =>
                        s.Status == OrderItemStatus.Paid &&
                        s.UpdatedDate.Date >= fromDate.Date &&
                        s.UpdatedDate.Date <= toDate.Date &&
                        _dbContext.Orders.Any(o =>
                        o.Station == s.Order.Station &&
                        (o.Station.IDSucursal == sucursal || sucursal == 0)
                        &&
                        (s.Product.ID == productId || productId == 0))
                        )
                        .ToList();

            }
            else
            {
                items = _dbContext.OrderItems.Include(s => s.Order).Include(s => s.Product)
                    .ThenInclude(s => s.ServingSizes).Include(s => s.Product).ThenInclude(s => s.Category).Include(s => s.Taxes).Where(s => s.Status == OrderItemStatus.Paid && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date && (productId == 0 || s.Product.ID == productId)).ToList();
            }
            var groups = _dbContext.MenuGroups.ToList();
            var categories = _dbContext.MenuCategories.ToList();


            var sales = new List<SalesReportGroup>();
            var taxes = new List<SalesReportTaxModel>();
            var propinas = new List<SalesReportPropinaModel>();

            var lstOrdenes = new List<long>();

            foreach (var item in items)
            {
                var menuProduct = _dbContext.MenuProducts.FirstOrDefault(s => s.ID == item.MenuProductID);

                if (!lstOrdenes.Contains(item.Order.ID))
                {
                    item.SubTotal = item.SubTotal - item.Order.Discount;
                    lstOrdenes.Add(item.Order.ID);
                }

                var groupName = "";
                var categoryName = "";
                if (menuProduct != null)
                {
                    var group = groups.FirstOrDefault(s => s.ID == menuProduct.GroupID);
                    var category = categories.FirstOrDefault(s => s.ID == menuProduct.CategoryID);

                    if (group != null)
                    {
                        groupName = group.Name;
                        categoryName = category.Name;
                    }
                    else
                    {
                        groupName = "N/A";
                        categoryName = "N/A";
                    }
                }
                else
                {
                    groupName = "N/A";
                    categoryName = "N/A";
                }

                var existgroup = sales.FirstOrDefault(s => s.Group == groupName);
                if (existgroup == null)
                {
                    existgroup = new SalesReportGroup()
                    {
                        Group = groupName,
                        Categories = new List<SalesReportCategory>()
                    };
                    sales.Add(existgroup);
                }

                var existcategory = existgroup.Categories.FirstOrDefault(s => s.Category == categoryName);
                if (existcategory == null)
                {
                    existcategory = new SalesReportCategory() { Category = categoryName, Products = new List<SalesReportProductModel>() };
                    existgroup.Categories.Add(existcategory);
                }

                var servingSize = new ProductServingSize();
                if (item.Product.HasServingSize)
                {
                    servingSize = item.Product.ServingSizes.FirstOrDefault(s => s.ID == item.ServingSizeID);
                }
                var existproduct = existcategory.Products.FirstOrDefault(s => s.ProductId == item.Product.ID && s.ServingSizeID == item.ServingSizeID);
                if (existproduct == null)
                {
                    var m = new SalesReportProductModel()
                    {
                        Group = groupName,
                        Category = categoryName,
                        ProductName = item.Product.Name,
                        ProductId = item.Product.ID,
                        ServingSizeID = servingSize == null ? 0 : servingSize.ID,
                        ServingSizeName = servingSize?.ServingSizeName,
                        Qty = item.Qty,
                        Sales = item.SubTotal,
                        Cost = item.Product.ProductCost * item.Qty,
                        Profit = item.SubTotal - item.Product.ProductCost * item.Qty
                    };
                    existcategory.Products.Add(m);
                }
                else
                {
                    existproduct.Qty += item.Qty;
                    existproduct.Sales += item.SubTotal;
                    existproduct.Cost += item.Product.ProductCost * item.Qty;
                    existproduct.Profit += item.SubTotal - item.Product.ProductCost * item.Qty;
                }

                foreach (var t in item.Taxes)
                {
                    var existtax = taxes.FirstOrDefault(s => s.TaxName == t.Description);
                    if (existtax == null)
                    {
                        var tax = new SalesReportTaxModel()
                        {
                            TaxName = t.Description,
                            Percent = t.Percent,
                            Taxable = item.SubTotal
                        };
                        if (t.IsExempt)
                        {
                            tax.TaxExempt = t.Amount;
                        }
                        else
                        {
                            tax.Tax = t.Amount;
                        }

                        taxes.Add(tax);
                    }
                    else
                    {
                        existtax.Taxable += item.SubTotal;
                        if (t.IsExempt)
                        {
                            existtax.TaxExempt += t.Amount;
                        }
                        else
                        {
                            existtax.Tax += t.Amount;
                        }
                    }
                }

                if (item.Propinas != null && item.Propinas.Any())
                {
                    foreach (var t in item.Propinas)
                    {
                        var existtax = propinas.FirstOrDefault(s => s.PropinaName == t.Description);
                        if (existtax == null)
                        {
                            var tax = new SalesReportPropinaModel()
                            {
                                PropinaName = t.Description,
                                Percent = t.Percent,
                            };
                            if (t.IsExempt)
                            {
                                tax.PropinaExempt = t.Amount;
                            }
                            else
                            {
                                tax.Propina = t.Amount;
                            }

                            propinas.Add(tax);
                        }
                        else
                        {
                            if (t.IsExempt)
                            {
                                existtax.PropinaExempt += t.Amount;
                            }
                            else
                            {
                                existtax.Propina += t.Amount;
                            }
                        }
                    }
                }


            }

            sales = sales.OrderBy(s => s.Group).ToList();
            foreach (var g in sales)
            {
                foreach (var c in g.Categories)
                {
                    foreach (var p in c.Products)
                    {
                        g.Qty += p.Qty;
                        g.Sales += p.Sales;
                        g.Cost += p.Cost;
                    }
                }

                g.CostPercent = g.Cost / g.Sales * 100.0m;
                g.Profit = g.Sales - g.Cost;
            }


            // write report
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "SalesReport_" + "_" + DateTime.Now.Ticks + ".xlsx";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;


            IWorkbook workbook = new XSSFWorkbook();
            decimal totalQty = 0;
            decimal totalSale = 0;
            decimal totalCost = 0;
            {
                ISheet sheet = workbook.CreateSheet("Group Summary");
                var row0 = sheet.CreateRow(0);

                row0.CreateCell(0).SetCellValue("Description");
                row0.CreateCell(1).SetCellValue("Qty");
                row0.CreateCell(2).SetCellValue("Value");
                row0.CreateCell(3).SetCellValue("Cost");
                row0.CreateCell(4).SetCellValue("Cost %");
                row0.CreateCell(5).SetCellValue("Profit");
           
                var index = 1;
                foreach (var sale in sales)
                {
                    var row = sheet.CreateRow(index);
                    row.CreateCell(0).SetCellValue(sale.Group);
                    row.CreateCell(1).SetCellValue("" + sale.Qty);
                    row.CreateCell(2).SetCellValue("" + sale.Sales.ToString("N2", CultureInfo.InvariantCulture));
                    row.CreateCell(3).SetCellValue("" + sale.Cost.ToString("N2", CultureInfo.InvariantCulture));
                    row.CreateCell(4).SetCellValue("" + sale.CostPercent.ToString("N2", CultureInfo.InvariantCulture));
                    row.CreateCell(5).SetCellValue("" + sale.Profit.ToString("N2", CultureInfo.InvariantCulture));
                   
                    totalQty += sale.Qty;
                    totalSale += sale.Sales;
                    totalCost += sale.Cost;
                    index++;
                }
                decimal totalProfit = totalSale - totalCost;
                decimal totalCostPercent = 0;
                if (totalSale > 0)
                    totalCostPercent = totalCost / totalSale * 100;
                var row1 = sheet.CreateRow(index);
                row1.CreateCell(0).SetCellValue("Grand Total");
                row1.CreateCell(1).SetCellValue(totalQty.ToString("N2", CultureInfo.InvariantCulture));
                row1.CreateCell(2).SetCellValue(totalSale.ToString("N2", CultureInfo.InvariantCulture));
                row1.CreateCell(3).SetCellValue(totalCost.ToString("N2", CultureInfo.InvariantCulture));
                row1.CreateCell(4).SetCellValue(totalCostPercent.ToString("N2", CultureInfo.InvariantCulture));
                row1.CreateCell(5).SetCellValue(totalProfit.ToString("N2", CultureInfo.InvariantCulture));
            }

            {
                ISheet sheet = workbook.CreateSheet("Tax Summary");
                var row0 = sheet.CreateRow(0);

                row0.CreateCell(0).SetCellValue("Description");
                row0.CreateCell(1).SetCellValue("Taxable");
                row0.CreateCell(2).SetCellValue("Tax exempt");
                row0.CreateCell(3).SetCellValue("Tax");

                decimal totalTaxable = 0;
                decimal totalTaxExempt = 0;
                decimal totalTax = 0;
                var index = 1;
                foreach (var tax in taxes)
                {
                    var row = sheet.CreateRow(index);
                    row.CreateCell(0).SetCellValue(tax.TaxName);
                    row.CreateCell(1).SetCellValue(tax.Taxable.ToString("N2", CultureInfo.InvariantCulture));
                    row.CreateCell(2).SetCellValue(tax.TaxExempt.ToString("N2", CultureInfo.InvariantCulture));
                    row.CreateCell(3).SetCellValue(tax.Tax.ToString("N2", CultureInfo.InvariantCulture));
                    totalTax += tax.Tax;
                    totalTaxable += tax.Taxable;
                    totalTaxExempt += tax.TaxExempt;
                    index++;
                }

                var row1 = sheet.CreateRow(index);
                row1.CreateCell(0).SetCellValue("Total");
                row1.CreateCell(1).SetCellValue("");
                row1.CreateCell(2).SetCellValue(totalTaxExempt.ToString("N2", CultureInfo.InvariantCulture));
                row1.CreateCell(3).SetCellValue(totalTax.ToString("N2", CultureInfo.InvariantCulture));
            }
            {
                ISheet sheet = workbook.CreateSheet("Propina Summary");
                var row0 = sheet.CreateRow(0);

                row0.CreateCell(0).SetCellValue("Description");
                row0.CreateCell(1).SetCellValue("Propina exempt");
                row0.CreateCell(2).SetCellValue("Propina");

                decimal totalPropinaExempt = 0;
                decimal totalPropina = 0;
                int index = 1;
                foreach (var tax in propinas)
                {
                    var row = sheet.CreateRow(index);
                    row.CreateCell(0).SetCellValue(tax.PropinaName);
                    row.CreateCell(1).SetCellValue(tax.PropinaExempt.ToString("N2", CultureInfo.InvariantCulture));
                    row.CreateCell(2).SetCellValue(tax.Propina.ToString("N2", CultureInfo.InvariantCulture));
                   
                    totalPropina += tax.Propina;
                    totalPropinaExempt += tax.PropinaExempt;
                    index++;
                }
                var row1 = sheet.CreateRow(index);
                row1.CreateCell(0).SetCellValue("Total");
                row1.CreateCell(1).SetCellValue(totalPropinaExempt.ToString("N2", CultureInfo.InvariantCulture));
                row1.CreateCell(2).SetCellValue(totalPropina.ToString("N2", CultureInfo.InvariantCulture));
            }
            {
                ISheet sheet = workbook.CreateSheet("Product Summary");
                var row0 = sheet.CreateRow(0);

                row0.CreateCell(0).SetCellValue("Category");
                row0.CreateCell(1).SetCellValue("Product Name");
                row0.CreateCell(2).SetCellValue("ID #");
                row0.CreateCell(3).SetCellValue("Sold #");
                row0.CreateCell(4).SetCellValue("Sales");
                row0.CreateCell(5).SetCellValue("Sales %");
                row0.CreateCell(6).SetCellValue("Cost");
                row0.CreateCell(7).SetCellValue("Profit");
                row0.CreateCell(8).SetCellValue("Profit %");

                var index = 1;
                foreach (var sale in sales)
                {
                    foreach (var c in sale.Categories)
                    {
                        foreach (var p in c.Products)
                        {
                            if (p.Sales == 0) continue;
                            decimal salespercent = 0;
                            if (totalSale > 0)
                                salespercent = p.Sales / totalSale;
                            var profit = p.Sales - p.Cost;
                            var profitPercent = profit / p.Sales;
                            var row = sheet.CreateRow(index);
                            var productName = p.ProductName;
                            if (!string.IsNullOrEmpty(p.ServingSizeName))
                            {
                                productName += "(" + p.ServingSizeName + ")";
                            }
                            row.CreateCell(0).SetCellValue(c.Category);
                            row.CreateCell(1).SetCellValue(productName);
                            row.CreateCell(2).SetCellValue("" + p.ProductId);
                            row.CreateCell(3).SetCellValue(p.Qty.ToString("0"));
                            row.CreateCell(4).SetCellValue(p.Sales.ToString("N2", CultureInfo.InvariantCulture));
                            row.CreateCell(5).SetCellValue(salespercent.ToString("0.00%"));
                            row.CreateCell(6).SetCellValue(p.Cost.ToString("N2", CultureInfo.InvariantCulture));
                            row.CreateCell(7).SetCellValue(profit.ToString("N2", CultureInfo.InvariantCulture));
                            row.CreateCell(8).SetCellValue(profitPercent.ToString("0.00%"));
                        }
                    }
                }
            }

            FileStream sw = System.IO.File.Create(uploadsFile);
            workbook.Write(sw);
            sw.Close();

            return Json(new { status = 0, url = uploadsUrl });
        }

        [Authorize(Policy = "Permission.REPORT.PurchaseReport")]
		[HttpPost]
        public JsonResult GeneratePurchaseReport(string from, string to, int group)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var purchaseOrders = _dbContext.PurchaseOrders.Include(s=>s.Warehouse).Include(s=>s.Items).ThenInclude(s=>s.Item).Include(s => s.Supplier).Where(s => s.Warehouse.IsActive && s.OrderTime.Date >= fromDate.Date && s.OrderTime.Date <= toDate.Date && s.Status == PurchaseOrderStatus.Received).ToList();

            var sorders = new List<PurchaseSupplierModel>();
            foreach (var porder in purchaseOrders)
            {              
                if (group > 0)
				{
					decimal subTotal = 0;
					decimal subTax = 0;
					decimal subSubTotal = 0;
					foreach (var a in porder.Items)
                    {
                        var article = _dbContext.Articles.Include(s => s.Category).ThenInclude(s => s.Group).Include(s=>s.Items).FirstOrDefault(s => s.ID == a.Item.ID && s.IsActive);
                        var unit = article.Items.FirstOrDefault(s => s.Number == a.UnitNum);
                        if (article.Category.Group.ID == group)
                        {
                            subSubTotal += a.Qty * a.UnitCost;
                            subTax += subSubTotal * (decimal)a.TaxRate / 100.0m;
                        }
                    }

                    subTotal = subSubTotal + subTax;
                    if (subTotal == 0) continue;
					var exist = sorders.FirstOrDefault(s => s.Supplier == porder.Supplier.Name);
					var item = new PurchaseItemModel()
					{
						Supplier = porder.Supplier.Name,
						SubTotal = subSubTotal,
						Tax = subTax,
						Total = subTotal,
						NCF = porder.NCF,
						PurchaseId = porder.ID,
						PurchaseDate = porder.OrderTime,
						Exempt = 0
					};
					if (exist == null)
					{
						exist = new PurchaseSupplierModel() { Supplier = porder.Supplier.Name, Items = new List<PurchaseItemModel>() };

						exist.Total = subTotal;
						exist.SubTotal = subSubTotal;
						exist.Tax = subTax;

						exist.Items.Add(item);
						sorders.Add(exist);
					}
					else
					{
						exist.Total += subTotal;
						exist.SubTotal += subSubTotal;
						exist.Tax += subTax;

						exist.Items.Add(item);
					}


				}
                else
                {
					var exist = sorders.FirstOrDefault(s => s.Supplier == porder.Supplier.Name);
					var item = new PurchaseItemModel()
					{
						Supplier = porder.Supplier.Name,
						SubTotal = porder.SubTotal,
						Tax = porder.TaxTotal,
						Total = porder.Total,
						NCF = porder.NCF,
						PurchaseId = porder.ID,
						PurchaseDate = porder.OrderTime,
						Exempt = 0
					};
					if (exist == null)
					{
						exist = new PurchaseSupplierModel() { Supplier = porder.Supplier.Name, Items = new List<PurchaseItemModel>() };

						exist.Total = porder.Total;
						exist.SubTotal = porder.SubTotal;
						exist.Tax = porder.TaxTotal;

						exist.Items.Add(item);
						sorders.Add(exist);
					}
					else
					{
						exist.Total += porder.Total;
						exist.SubTotal += porder.SubTotal;
						exist.Tax += porder.TaxTotal;

						exist.Items.Add(item);
					}
				}
                
            }
            // write report
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "PurchaseReport_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;

            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);

                // REPORT HEADER
                {
                    string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                    var store = _dbContext.Preferences.First();
                    if (store != null)
                    {
                        var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();

                        var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                        var time = "Fecha: " + DateTime.Today.ToString("dd/MM/yyyy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                        var cellh1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));


                        Cell cellh2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        Image img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cellh2.Add(img.ScaleToFit(100, 60));

                        }

                        var cellh3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cellh1);
                        headertable.AddCell(cellh2);
                        headertable.AddCell(cellh3);

                        doc.Add(headertable);
                    }
                }

                Paragraph header = new Paragraph("Reporte de Compras")
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(25);
                doc.Add(header);
                Paragraph header1 = new Paragraph(fromDate.ToString("dd-MM-yyyy") + " to " + toDate.ToString("dd-MM-yyyy"))
                          .SetTextAlignment(TextAlignment.CENTER)
                          .SetFontColor(ColorConstants.GRAY)
                          .SetFontSize(12);
                doc.Add(header1);
                var table = new iText.Layout.Element.Table(new float[] { 1, 1, 1, 1, 1, 1, 1 });
                table.SetWidth(UnitValue.CreatePercentValue(100));

                var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Fecha").SetFontSize(12));
                var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Factura").SetFontSize(12));
                var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("NCF").SetFontSize(12));
                var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Monto Extento").SetFontSize(12));
                var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Base Imponible").SetFontSize(12));
                var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("ITBIS").SetFontSize(12));
                var cell7 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Monto").SetBold().SetFontSize(12));
                table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell7);

                decimal total = 0;
                decimal taxTotal = 0;
                decimal subTotal = 0;
                int count = sorders.Count;

                foreach (var s in sorders)
                {
                    var cell31 = new Cell(1, 7).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(s.Supplier).SetBold().SetFontSize(13));
                    table.AddCell(cell31);

                    foreach (var p in s.Items)
                    {
                        var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.PurchaseDate.ToString("dd-MM-yyyy")).SetFontSize(11));
                        var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.PurchaseId).SetFontSize(11));
                        var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.NCF).SetFontSize(11));
                        var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(p.Exempt)).SetFontSize(11));
                        var cell15 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(p.SubTotal)).SetFontSize(11));
                        var cell16 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(p.Tax)).SetFontSize(11));
                        var cell17 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(p.Total)).SetFontSize(11));
                        table.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16).AddCell(cell17);
                    }

                    var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Items : " + s.Items.Count()).SetBold().SetFontSize(12));
                    var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("").SetBold().SetFontSize(12));
                    var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Totales ").SetBold().SetFontSize(12));
                    var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(s.Exempt)).SetBold().SetFontSize(12));
                    var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(s.SubTotal)).SetBold().SetFontSize(12));
                    var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(s.Tax)).SetBold().SetFontSize(12));
                    var cell27 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(s.Total)).SetBold().SetFontSize(12));
                    table.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25).AddCell(cell26).AddCell(cell27);
                    total += s.Total;
                    subTotal += s.SubTotal;
                    taxTotal += s.Tax;
                }

                var cell121 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Total Orders : " + count).SetBold().SetFontSize(13));
                var cell122 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("").SetBold().SetFontSize(12));
                var cell123 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Totales ").SetBold().SetFontSize(13));
                var cell124 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("").SetBold().SetFontSize(12));
                var cell125 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(subTotal)).SetBold().SetFontSize(13));
                var cell126 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(taxTotal)).SetBold().SetFontSize(13));
                var cell127 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(total)).SetBold().SetFontSize(12));
                table.AddCell(cell121).AddCell(cell122).AddCell(cell123).AddCell(cell124).AddCell(cell125).AddCell(cell126).AddCell(cell127);

                doc.Add(table);

                doc.Close();
            }
          


            return Json(new { status = 0, url = uploadsUrl });
        }
        
		[Authorize(Policy = "Permission.REPORT.InventoryLevelReport")]
		[HttpPost]
        public JsonResult GenerateInventoryLevelReport(long warehouse, int group, string date)
		{
            try
            {
                var rdate = DateTime.Now;
                if (!string.IsNullOrEmpty(date))
                {
                    try
                    {
                        rdate = DateTime.ParseExact(date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                    }
                    catch { }
                }

                var stocks = _dbContext.WarehouseStocks.Include(s => s.Warehouse).Where(s => s.Warehouse.IsActive).ToList();
                if (warehouse > 0)
                {
                    stocks = stocks.Where(s => s.Warehouse.ID == warehouse).ToList();
                }
                var stockChanges = _dbContext.WarehouseStockChangeHistory.Include(s => s.Warehouse).Where(s => s.Warehouse.IsActive && s.CreatedDate.Date > rdate.Date && (warehouse == 0 || s.Warehouse.ID == warehouse)).ToList();

                var warehouseItem = _dbContext.Warehouses.FirstOrDefault(s => s.ID == warehouse);

                var stockLevels = new List<InventoryLevelModel>();

                foreach (var stock in stocks)
                {
                    var category = "";
                    var sub = new InventoryStockSub();
                    if (stock.ItemType == ItemType.Article)
                    {
                        var article = _dbContext.Articles.Include(s => s.Brand).Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Items).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                        if (article == null) continue;
                        if (group > 0)
                        {
                            if (article.Category != null && article.Category.Group.ID != group)
                            {
                                continue;
                            }
                        }
                        category = article.Category?.Name;

                        var stockArtcles = stockChanges.Where(s => s.ItemType == ItemType.Article && s.ItemId == stock.ItemId && s.Warehouse == stock.Warehouse).ToList();
                        foreach (var ss in stockArtcles)
                        {
                            stock.Qty -= ss.Qty;
                        }

                        sub = GetInventorySubFromArticle(article, stock.Qty, 1);
                    }
                    else
                    {
                        var subRecipe = _dbContext.SubRecipes.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                        if (subRecipe == null) continue;
                        category = subRecipe.Category?.Name;
                        if (group > 0)
                        {
                            if (subRecipe.Category != null && subRecipe.Category.Group.ID != group)
                            {
                                continue;
                            }
                        }

                        var stockArtcles = stockChanges.Where(s => s.ItemType == ItemType.SubRecipe && s.ItemId == stock.ItemId && s.Warehouse == stock.Warehouse).ToList();
                        foreach (var ss in stockArtcles)
                        {
                            stock.Qty -= ss.Qty;
                        }

                        sub = GetInventorySubFromSubRecipe(subRecipe, stock.Qty, 1);
                    }
                    if (string.IsNullOrEmpty(sub.ItemName)) continue;

                    var exist = stockLevels.FirstOrDefault(s => s.Category == category);
                    if (exist != null)
                    {
                        AddInventorySubToModel1(exist, sub);
                    }
                    else
                    {
                        var level = new InventoryLevelModel();
                        level.Category = category == null ? "" : category;
                        level.Items = new List<InventoryStockSub>()
                    {
                        sub
                    };
                        stockLevels.Add(level);
                    }
                }
                // write report
                string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
                var uniqueFileName = "InventoryStockReport_" + "_" + DateTime.Now.Ticks + ".pdf";
                var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
                var uploadsUrl = "/temp/" + uniqueFileName;
                var paperSize = iText.Kernel.Geom.PageSize.A4;

                using (var writer = new PdfWriter(uploadsFile))
                {
                    var pdf = new PdfDocument(writer);
                    var doc = new iText.Layout.Document(pdf);
                    // REPORT HEADER
                    {
                        string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                        var store = _dbContext.Preferences.First();
                        if (store != null)
                        {
                            var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                            headertable.SetWidth(UnitValue.CreatePercentValue(100));
                            headertable.SetFixedLayout();

                            var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                            var time = "Fecha: " + DateTime.Today.ToString("dd/MM/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                            var cellh1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));


                            Cell cellh2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                            Image img = AlfaHelper.GetLogo(store.Logo);
                            if (img != null)
                            {
                                cellh2.Add(img.ScaleToFit(100, 60));

                            }
                            var cellh3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                            headertable.AddCell(cellh1);
                            headertable.AddCell(cellh2);
                            headertable.AddCell(cellh3);

                            doc.Add(headertable);
                        }
                    }
                    Paragraph header = new Paragraph("Nivel de Inventario")
                                   .SetTextAlignment(TextAlignment.CENTER)
                                   .SetFontSize(25);
                    doc.Add(header);
                    if (warehouseItem != null)
                    {
                        Paragraph header1 = new Paragraph(warehouseItem.WarehouseName + "   " + rdate.ToString("dd-MM-yyyy"))
                          .SetTextAlignment(TextAlignment.CENTER)
                          .SetFontColor(ColorConstants.GRAY)
                          .SetFontSize(12);
                        doc.Add(header1);
                    }
                    else
                    {
                        Paragraph header1 = new Paragraph("Todo el almacén" + "   " + rdate.ToString("dd-MM-yyyy"))
                         .SetTextAlignment(TextAlignment.CENTER)
                         .SetFontColor(ColorConstants.GRAY)
                         .SetFontSize(12);
                        doc.Add(header1);
                    }
                    var table = new iText.Layout.Element.Table(new float[] { 2, 1, 1, 2, 1, 2, 1, 2, 1 });
                    table.SetWidth(UnitValue.CreatePercentValue(100));

                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Nombre").SetFontSize(12));
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Brand").SetFontSize(12));
                    var cell3 = new Cell(1, 2).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("Unidad 1").SetFontSize(11));
                    var cell5 = new Cell(1, 2).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("Unidad 2").SetFontSize(11));
                    var cell7 = new Cell(1, 2).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("Unidad 3").SetFontSize(11));
                    var cell8 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Total").SetFontSize(12));
                    table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell5).AddCell(cell7).AddCell(cell8);

                    decimal totalCost = 0;
                    foreach (var c in stockLevels)
                    {
                        if (c.Items.Count == 0) continue;
                        var cell21 = new Cell(1, 9).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(c.Category).SetBold().SetFontSize(11));
                        table.AddCell(cell21);
                        decimal categorytotal = 0;
                        foreach (var p in c.Items)
                        {
                            if (p.Qty1 == 0) continue;
                            var total = p.TotalCost;
                            categorytotal += total;
                            totalCost += total;
                            var cell31 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.ItemName).SetFontSize(10));
                            var cell39 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Brand).SetFontSize(10));
                            var cell32 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + p.Qty1.ToString("0.00", CultureInfo.InvariantCulture)).SetFontSize(10));
                            var cell33 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Unit1).SetFontSize(10));
                            var cell34 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + p.Qty2.ToString("0.00", CultureInfo.InvariantCulture)).SetFontSize(10));
                            var cell35 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Unit2).SetFontSize(10));
                            var cell36 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + p.Qty3.ToString("0.00", CultureInfo.InvariantCulture)).SetFontSize(10));
                            var cell37 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Unit3).SetFontSize(10));
                            var cell38 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(total)).SetFontSize(10));

                            table.AddCell(cell31).AddCell(cell39).AddCell(cell32).AddCell(cell33).AddCell(cell34).AddCell(cell35).AddCell(cell36).AddCell(cell37).AddCell(cell38);
                        }

                        // category total
                        var cell131 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(10));
                        var cell139 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(10));
                        var cell132 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(10));
                        var cell133 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(10));
                        var cell134 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(10));
                        var cell135 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(10));
                        var cell136 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(10));
                        var cell137 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).SetBorderTop(new SolidBorder(0.5f)).Add(new Paragraph("").SetFontSize(10));
                        var cell138 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetBorderTop(new SolidBorder(0.5f)).SetBold().Add(new Paragraph(GetMoneyString(categorytotal)).SetFontSize(10));

                        table.AddCell(cell131).AddCell(cell139).AddCell(cell132).AddCell(cell133).AddCell(cell134).AddCell(cell135).AddCell(cell136).AddCell(cell137).AddCell(cell138);

                    }

                    var cell41 = new Cell(1, 3).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Grand Total : ").SetFontSize(13));
                    var cell42 = new Cell(1, 2).SetBorder(Border.NO_BORDER).SetBorderTop(new SolidBorder(0.5f)).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalCost)).SetBold().SetFontSize(13));
                    table.AddCell(cell41).AddCell(cell42);

                    doc.Add(table);
                    doc.Close();
                }

                return Json(new { status = 0, url = uploadsUrl });
            }
            catch(Exception ex)
            {
                _dbContext.ErrorOuts.Add(new ErrorOut() { Name = "Inventory level error", Description = ex.Message + ex.StackTrace });
                _dbContext.SaveChanges();
                return Json(new {status = 1, message = ex.Message + ex.StackTrace });
            }
			
        }
		[Authorize(Policy = "Permission.REPORT.InventoryLevelReport")]
		[HttpPost]
		public JsonResult GenerateExcelInventoryLevelReport(long warehouse, int group, string date)
		{
			var rdate = DateTime.Now;
			if (!string.IsNullOrEmpty(date))
			{
				try
				{
					rdate = DateTime.ParseExact(date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}
			var stocks = _dbContext.WarehouseStocks.Include(s => s.Warehouse).Where(s=>s.Warehouse.IsActive).ToList();
			if (warehouse > 0)
			{
				stocks = stocks.Where(s => s.Warehouse.ID == warehouse).ToList();
			}
			var stockChanges = _dbContext.WarehouseStockChangeHistory.Include(s => s.Warehouse).Where(s => s.Warehouse.IsActive && s.CreatedDate.Date > rdate.Date && (warehouse == 0 || s.Warehouse.ID == warehouse)).ToList();

			var stockLevels = new List<InventoryLevelModel>();

			foreach (var stock in stocks)
			{
				var category = "";
				var sub = new InventoryStockSub();
				if (stock.ItemType == ItemType.Article)
				{
					var article = _dbContext.Articles.Include(s => s.Brand).Include(s => s.Category).Include(s => s.Items).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
					if (article == null) continue;
					if (group > 0)
					{
						if (article.Category != null && article.Category.Group.ID != group)
						{
							continue;
						}
					}
					category = article.Category.Name;

					var stockArtcles = stockChanges.Where(s => s.ItemType == ItemType.Article && s.ItemId == stock.ItemId).ToList();
					foreach (var ss in stockArtcles)
					{
						stock.Qty -= ss.Qty;
					}

					sub = GetInventorySubFromArticle(article, stock.Qty, 1);
				}
				else
				{
					var subRecipe = _dbContext.SubRecipes.Include(s => s.Category).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == stock.ItemId && s.IsDeleted);
					if (subRecipe == null) continue;
					category = subRecipe.Category.Name;
					if (group > 0)
					{
						if (subRecipe.Category != null && subRecipe.Category.Group.ID != group)
						{
							continue;
						}
					}
					sub = GetInventorySubFromSubRecipe(subRecipe, stock.Qty, 1);
				}
				if (string.IsNullOrEmpty(sub.ItemName)) continue;

				var exist = stockLevels.FirstOrDefault(s => s.Category == category);
				if (exist != null)
				{
					AddInventorySubToModel1(exist, sub);
				}
				else
				{
					var level = new InventoryLevelModel();
					level.Category = category== null ? "" : category;
					level.Items = new List<InventoryStockSub>()
					{
						sub
					};
					stockLevels.Add(level);
				}
			}
			// write report
			string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
			var uniqueFileName = "InventoryLevelReport_" + "_" + DateTime.Now.Ticks + ".xlsx";
			var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
			var uploadsUrl = "/temp/" + uniqueFileName;


			IWorkbook workbook = new XSSFWorkbook();
			ISheet sheet = workbook.CreateSheet("Report");

            var row0 = sheet.CreateRow(0);

            row0.CreateCell(0).SetCellValue("Category");
			row0.CreateCell(1).SetCellValue("Name");
			row0.CreateCell(2).SetCellValue("Brand");
			row0.CreateCell(3).SetCellValue("QTY 1");
			row0.CreateCell(4).SetCellValue("Unit 1");
			row0.CreateCell(5).SetCellValue("QTY 2");
			row0.CreateCell(6).SetCellValue("Unit 2");
			row0.CreateCell(7).SetCellValue("QTY 3");
			row0.CreateCell(8).SetCellValue("Unit 3");
			row0.CreateCell(9).SetCellValue("Total");

            int index = 1;
            decimal totalCost = 0;
            foreach (var stock in stockLevels)
            {
                foreach(var item in stock.Items)
				{
					if (item.Qty1 == 0) continue;
					var total = item.Qty1 * item.Cost;
					totalCost += total;
                    var row = sheet.CreateRow(index);

                    row.CreateCell(0).SetCellValue(stock.Category);
					row.CreateCell(1).SetCellValue(item.ItemName);
					row.CreateCell(2).SetCellValue(item.Brand);
                    if (!string.IsNullOrEmpty(item.Unit1))
                    { 
                        row.CreateCell(3).SetCellValue("" + item.Qty1.ToString("0.00", CultureInfo.InvariantCulture)); 
                        row.CreateCell(4).SetCellValue(item.Unit1); 
                    }
                    if (!string.IsNullOrEmpty(item.Unit2))
                    {
                        row.CreateCell(5).SetCellValue("" + item.Qty2.ToString("0.00", CultureInfo.InvariantCulture));
                        row.CreateCell(6).SetCellValue(item.Unit2);
                    }                       
                    if (!string.IsNullOrEmpty(item.Unit3))
                    {
                        row.CreateCell(7).SetCellValue("" + item.Qty3.ToString("0.00", CultureInfo.InvariantCulture));
                        row.CreateCell(8).SetCellValue(item.Unit3);
                    }
                        
					row.CreateCell(9).SetCellValue(total.ToString("N2", CultureInfo.InvariantCulture));

					index++;
                }
            }

			sheet.CreateRow(index).CreateCell(8).SetCellValue("Grand Total");
			sheet.CreateRow(index).CreateCell(9).SetCellValue(totalCost.ToString("N2", CultureInfo.InvariantCulture));

			FileStream sw = System.IO.File.Create(uploadsFile);
			workbook.Write(sw);
			sw.Close();

			return Json(new { status = 0, url = uploadsUrl });
		}

		[Authorize(Policy = "Permission.REPORT.InventoryDepletionReport")]
        [HttpPost]
        public JsonResult InventoryDepletionReportExcel(string from, string to, int almacen, int group)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var stockChange = _dbContext.WarehouseStockChangeHistory.Include(s => s.Warehouse).Where(s => s.ReasonType == StockChangeReason.Kitchen && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date && (s.Warehouse.ID == almacen || almacen == 0)).ToList();
            var depletions = new List<InventoryDepletionModel>();

            foreach (var p in stockChange)
            {
                var orderItem = _dbContext.OrderItems.Include(s => s.Product).FirstOrDefault(s => s.ID == p.ReasonId);
                if (orderItem == null) continue;

                var sub = new InventoryStockSub();

                if (p.ItemType == ItemType.Article)
                {
                    var article = _dbContext.Articles.Include(s => s.Brand).Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Items).FirstOrDefault(s => s.ID == p.ItemId);
                    if (group > 0)
                    {
                        if (article.Category != null && article.Category.Group.ID != group)
                        {
                            continue;
                        }
                    }
                    sub = GetInventorySubFromArticle(article, p.Qty, p.UnitNum);
                }
                else
                {
                    var subrecipe = _dbContext.SubRecipes.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == p.ItemId);
                    if (subrecipe.Category != null && subrecipe.Category.Group.ID != group)
                    {
                        continue;
                    }
                    sub = GetInventorySubFromSubRecipe(subrecipe, p.Qty, p.UnitNum);
                }
                var exist = depletions.FirstOrDefault(s => s.Warehouse == p.Warehouse.WarehouseName && s.ProductId == orderItem.Product.ID && s.ProductId == orderItem.Product.ID);
                if (exist == null)
                {
                    exist = new InventoryDepletionModel()
                    {
                        Warehouse = p.Warehouse.WarehouseName,
                        ProductId = orderItem.Product.ID,
                        ItemId = new List<long>() { orderItem.ID },
                        Product = orderItem.Product.Name,
                        SalesQty = orderItem.Qty,
                        Items = new List<InventoryStockSub>()
                        {
                            sub
                        }
                    };
                    depletions.Add(exist);
                }
                else
                {
                    if (!exist.ItemId.Contains(orderItem.ID))
                    {
                        exist.ItemId.Add(orderItem.ID);
                        exist.SalesQty += orderItem.Qty;
                    }

                    AddInventorySubToModel(exist, sub);
                }
            }

            var output = new List<InventoryDepletionModelExcel>();
           

            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "InventoryDepletionReport_" + "_" + DateTime.Now.Ticks + ".xlsx";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;

            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Report");

            var row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("Product");
            row.CreateCell(1).SetCellValue("QTY");
            row.CreateCell(2).SetCellValue("Item Name");
            row.CreateCell(3).SetCellValue("Qty 1");
            row.CreateCell(4).SetCellValue("Unit 1");
            row.CreateCell(5).SetCellValue("Qty 2");
            row.CreateCell(6).SetCellValue("Unit 2");
            row.CreateCell(7).SetCellValue("Qty 3");
            row.CreateCell(8).SetCellValue("Unit 3");

            int index = 1;
            foreach (var d in depletions)
            {
                foreach (var item in d.Items)
                {
                    if (item.Qty1 == 0) continue;
                    var row1 = sheet.CreateRow(index);
                    row1.CreateCell(0).SetCellValue(d.Product);
                    row1.CreateCell(1).SetCellValue((double)d.SalesQty);
                    row1.CreateCell(2).SetCellValue(item.ItemName);
                    if (!string.IsNullOrEmpty(item.Unit1))
                    {
                        row1.CreateCell(3).SetCellValue("" + item.Qty1.ToString("0.00", CultureInfo.InvariantCulture));
                        row1.CreateCell(4).SetCellValue(item.Unit1);
                    }
                     
                    if (!string.IsNullOrEmpty(item.Unit2))
                    {
                        row1.CreateCell(5).SetCellValue("" + item.Qty2.ToString("0.00", CultureInfo.InvariantCulture));
                        row1.CreateCell(6).SetCellValue(item.Unit2);
                    }
                      
                    if (!string.IsNullOrEmpty(item.Unit3))
                    {
                        row1.CreateCell(7).SetCellValue("" + item.Qty3.ToString("0.00", CultureInfo.InvariantCulture));
                        row1.CreateCell(8).SetCellValue(item.Unit3);
                    }
                      

                    index++;
                }
            }

            FileStream sw = System.IO.File.Create(uploadsFile);
            workbook.Write(sw);
            sw.Close();

            return Json(new { status = 0, url = uploadsUrl });
        }

		[Authorize(Policy = "Permission.REPORT.CostoDeVenta")]
		[HttpPost]
        public JsonResult InventoryByGroupReportExcel(InventoryByGroupRequest request)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(request.to))
            {
                try
                {
                    toDate = DateTime.ParseExact(request.to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(request.from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(request.from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var stockChanges = _dbContext.WarehouseStockChangeHistory.Include(s => s.Warehouse).Where(s => s.CreatedDate.Date > fromDate.Date && s.Warehouse.IsActive && (request.WarehouseID == 0 || s.Warehouse.ID == request.WarehouseID)).OrderByDescending(s => s.CreatedDate).ToList();

            var warehouse = _dbContext.Warehouses.FirstOrDefault(s => s.ID == request.WarehouseID);
            var stocks = _dbContext.WarehouseStocks.Include(s => s.Warehouse).Where(s => s.Warehouse.IsActive && (warehouse == null || s.Warehouse == warehouse)).ToList();

            var result = new List<InventoryGroupModel>();
            foreach (var stock in stocks)
            {
                if (stock.ItemType == ItemType.Article)
                {
                    var article = _dbContext.Articles.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Items).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                    if (article == null) continue;
                    if (!request.GroupIDs.Contains(article.Category.Group.ID)) continue;

                    var initQty = stock.Qty;
                    var stockArtcles = stockChanges.Where(s => s.ItemType == ItemType.Article && s.ItemId == stock.ItemId && s.Warehouse == stock.Warehouse).ToList();
                    foreach (var ss in stockArtcles)
                    {
                        initQty -= ss.Qty;
                    }

                    var unit = article.Items.FirstOrDefault(s => s.Number == 1);
                    var exist = result.FirstOrDefault(s => s.GroupID == article.Category.Group.ID);
                    if (exist == null)
                    {
                        result.Add(new InventoryGroupModel()
                        {
                            GroupID = article.Category.Group.ID,
                            GroupName = article.Category.Group.GroupName,
                            Initial = initQty * unit.Cost,
                            Final = stock.Qty * unit.Cost
                        });
                    }
                    else
                    {
                        exist.Initial += initQty * unit.Cost;
                        exist.Final += stock.Qty * unit.Cost;
                    }
                }
                else if (stock.ItemType == ItemType.SubRecipe)
                {
                    var subRecipe = _dbContext.SubRecipes.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                    if (subRecipe == null) continue;
                    if (!request.GroupIDs.Contains(subRecipe.Category.Group.ID)) continue;

                    var initQty = stock.Qty;
                    var stockArtcles = stockChanges.Where(s => s.ItemType == ItemType.SubRecipe && s.ItemId == stock.ItemId && s.Warehouse == stock.Warehouse).ToList();
                    foreach (var ss in stockArtcles)
                    {
                        initQty -= ss.Qty;
                    }

                    var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == 1);
                    var exist = result.FirstOrDefault(s => s.GroupID == subRecipe.Category.Group.ID);
                    if (exist == null)
                    {
                        result.Add(new InventoryGroupModel()
                        {
                            GroupID = subRecipe.Category.Group.ID,
                            GroupName = subRecipe.Category.Group.GroupName,
                            Initial = initQty * unit.Cost,
                            Final = stock.Qty * unit.Cost
                        });
                    }
                    else
                    {
                        exist.Initial += initQty * unit.Cost;
                        exist.Final += stock.Qty * unit.Cost;
                    }
                }
            }

            foreach (var stock in stockChanges)
            {
                if (stock.ItemType == ItemType.Article)
                {
                    var article = _dbContext.Articles.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Items).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                    if (article == null) continue;
                    if (!request.GroupIDs.Contains(article.Category.Group.ID)) continue;

                    var unit = article.Items.FirstOrDefault(s => s.Number == 1);
                    var exist = result.FirstOrDefault(s => s.GroupID == article.Category.Group.ID);
                    var amount = stock.Qty * unit.Cost;
                    if (stock.CreatedDate.Date > toDate.Date)
                    {
                        //exist.Final -= amount;
                    }
                    else
                    {
                        
                        //exist.Initial -= amount;
                        if (stock.ReasonType == StockChangeReason.PurchaseOrder)
                        {
                            exist.Purchase += amount;
                        }
                        //else if (stock.ReasonType == StockChangeReason.Kitchen)
                        //{
                        //    var orderItem = _dbContext.OrderItems.FirstOrDefault(s => !s.IsDeleted && s.ID == stock.ReasonId && s.Status == OrderItemStatus.Paid);
                        //    if (orderItem != null)
                        //        exist.Sales += orderItem.SubTotal;
                        //}
                    }

                    exist.Items.Add(new InventoryGroupSubModel()
                    {
                        Reason = stock.ReasonType,
                        ChangeDate = stock.CreatedDate,
                        Amount = stock.Qty * unit.Cost
                    });

                }
                else if (stock.ItemType == ItemType.SubRecipe)
                {
                    var subRecipe = _dbContext.SubRecipes.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                    if (subRecipe == null) continue;
                    if (!request.GroupIDs.Contains(subRecipe.Category.Group.ID)) continue;

                    var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == 1);
                    var exist = result.FirstOrDefault(s => s.GroupID == subRecipe.Category.Group.ID);
                    var amount = stock.Qty * unit.Cost;
                    if (stock.CreatedDate.Date > toDate.Date)
                    {
                        //exist.Final -= amount;
                    }
                    else
                    {
                       
                        //exist.Initial -= amount;
                        if (stock.ReasonType == StockChangeReason.PurchaseOrder)
                        {
                            exist.Purchase += amount;
                        }
                        //else if (stock.ReasonType == StockChangeReason.Kitchen)
                        //{
                        //    var orderItem = _dbContext.OrderItems.FirstOrDefault(s => !s.IsDeleted && s.ID == stock.ReasonId && s.Status == OrderItemStatus.Paid);
                        //    if (orderItem != null)
                        //        exist.Sales += orderItem.SubTotal;
                        //}
                    }
                    exist.Items.Add(new InventoryGroupSubModel()
                    {
                        Reason = stock.ReasonType,
                        ChangeDate = stock.CreatedDate,
                        Amount = stock.Qty * unit.Cost
                    });
                }
            }
            var items = _dbContext.OrderItems.Include(s => s.Product).ThenInclude(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Taxes).Include(s => s.Order).Where(s => s.Status == OrderItemStatus.Paid && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date).ToList();

            foreach (var item in items)
            {
                try
                {
                    var groupId = item.Product.Category.Group.ID;
                    var exist = result.FirstOrDefault(s => s.GroupID == groupId);
                    if (exist != null)
                    {
                        exist.Sales += item.SubTotal;
                    }
                }
                catch { }
            }


            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "Costo de ventaReport_" + "_" + DateTime.Now.Ticks + ".xlsx";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;

            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Report");

            var row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("Group");
            row.CreateCell(1).SetCellValue("Initial Inventory");
            row.CreateCell(2).SetCellValue("Purchase");
            row.CreateCell(3).SetCellValue("Final Inventory");
            row.CreateCell(4).SetCellValue("Costa de Venta");
            row.CreateCell(4).SetCellValue("Sales");
            row.CreateCell(5).SetCellValue("Percent(%)");

            int index = 1;
            foreach (var d in result)
            {
				var costDeventa = d.Initial + d.Purchase - d.Final;
				decimal percent = 0; 
                if (d.Sales > 0)
                    percent = (d.Initial + d.Purchase - d.Final) / d.Sales * 100.0m;
                var row1 = sheet.CreateRow(index);
                row1.CreateCell(0).SetCellValue(d.GroupName);
                row1.CreateCell(1).SetCellValue((double)d.Initial);
                row1.CreateCell(2).SetCellValue((double)d.Purchase);
                row1.CreateCell(3).SetCellValue((double)d.Final);
                row1.CreateCell(4).SetCellValue((double)costDeventa);
                row1.CreateCell(4).SetCellValue((double)d.Sales);
                row1.CreateCell(5).SetCellValue((double)Math.Round(percent, 2));
               
                index++;
              
            }

            FileStream sw = System.IO.File.Create(uploadsFile);
            workbook.Write(sw);
            sw.Close();

            return Json(new { status = 0, url = uploadsUrl });
        }

		[Authorize(Policy = "Permission.REPORT.ProductCost")]
		[HttpPost]
		public JsonResult CostPercentReportExcel(int from, int to)
		{

			var result = new List<ProductCostPercentModel>();
			var products = _dbContext.Products.Include(s => s.ServingSizes).Where(s => s.IsActive).ToList();
			foreach (var product in products)
			{
				if (product.HasServingSize)
				{
					foreach (var serving in product.ServingSizes)
					{
						if (serving.Cost == 0) continue;
						var percent = serving.Price[0] / serving.Cost * 100;
						if (percent >= from && percent <= to)
						{
							result.Add(new ProductCostPercentModel()
							{
								ProductID = product.ID,
								ProductName = product.Name + "(" + serving.ServingSizeName + ")",
								Percent = Math.Round(percent, 2),
								Price = serving.Price[0],
								Cost = serving.Cost
							});
						}
					}
				}
				else
				{
					if (product.ProductCost == 0) continue;
					var percent = product.Price[0] / product.ProductCost * 100.0m;
					if (percent >= from && percent <= to)
					{
						result.Add(new ProductCostPercentModel()
						{
							ProductID = product.ID,
							ProductName = product.Name,
							Percent = Math.Round(percent, 2),
							Price = product.Price[0],
							Cost = product.ProductCost
						});
					}
				}
			}



			string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
			var uniqueFileName = "CostPercentReport_" + "_" + DateTime.Now.Ticks + ".xlsx";
			var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
			var uploadsUrl = "/temp/" + uniqueFileName;

			IWorkbook workbook = new XSSFWorkbook();
			ISheet sheet = workbook.CreateSheet("Report");

			var row = sheet.CreateRow(0);
			row.CreateCell(0).SetCellValue("ID");
			row.CreateCell(1).SetCellValue("Name");
			row.CreateCell(2).SetCellValue("Price");
			row.CreateCell(3).SetCellValue("Cost");
			row.CreateCell(4).SetCellValue("Percent (%)");

			int index = 1;
			foreach (var d in result)
			{
				var row1 = sheet.CreateRow(index);
				row1.CreateCell(0).SetCellValue(d.ProductID);
				row1.CreateCell(1).SetCellValue(d.ProductName);
				row1.CreateCell(2).SetCellValue((double)d.Price);
				row1.CreateCell(2).SetCellValue((double)d.Cost);
				row1.CreateCell(2).SetCellValue((double)d.Percent);
			}

			FileStream sw = System.IO.File.Create(uploadsFile);
			workbook.Write(sw);
			sw.Close();

			return Json(new { status = 0, url = uploadsUrl });
		}

		[Authorize(Policy = "Permission.REPORT.InventoryDepletionReport")]
		[HttpPost]
        public JsonResult GenerateInventoryDepletionReport(string from, string to, int almacen, int group)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var stockChange = _dbContext.WarehouseStockChangeHistory.Include(s => s.Warehouse).Where(s => s.ReasonType == StockChangeReason.Kitchen && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date && (s.Warehouse.ID == almacen || almacen == 0)).ToList();

            var depletions = new List<InventoryDepletionModel>();
            foreach (var p in stockChange)
            {
                var orderItem = _dbContext.OrderItems.Include(s => s.Product).FirstOrDefault(s => s.ID == p.ReasonId);
                if (orderItem == null) continue;

                var sub = new InventoryStockSub();

                if (p.ItemType == ItemType.Article)
                {
                    var article = _dbContext.Articles.Include(s => s.Brand).Include(s=>s.Category).ThenInclude(s=>s.Group).Include(s => s.Items).FirstOrDefault(s => s.ID == p.ItemId);
                    if (group > 0)
                    {
                        if (article.Category != null && article.Category.Group.ID != group)
                        {
                            continue;
                        }
                    }

                    sub = GetInventorySubFromArticle(article, p.Qty, p.UnitNum);
                }
                else
                {
                    var subrecipe = _dbContext.SubRecipes.Include(s=>s.Category).ThenInclude(s=>s.Group).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == p.ItemId);
					if (group > 0 && subrecipe.Category != null && subrecipe.Category.Group.ID != group)
					{
						continue;
					}
					sub = GetInventorySubFromSubRecipe(subrecipe, p.Qty, p.UnitNum);
                }

                var exist = depletions.FirstOrDefault(s => s.Warehouse == p.Warehouse.WarehouseName && s.ProductId == orderItem.Product.ID);
                if (exist == null)
                {
                    exist = new InventoryDepletionModel()
                    {
                        Warehouse = p.Warehouse.WarehouseName,
                        ProductId = orderItem.Product.ID,
                        ItemId = new List<long>() { orderItem.ID },
                        Product = orderItem.Product.Name,
                        SalesQty = orderItem.Qty,
                        Items = new List<InventoryStockSub>()
                        {
                            sub
                        }
                    };
                    depletions.Add(exist);
                }
                else
                {
                    if (!exist.ItemId.Contains(orderItem.ID))
                    {
                        exist.ItemId.Add(orderItem.ID);
                        exist.SalesQty += orderItem.Qty;
                    }

                    AddInventorySubToModel(exist, sub);
                }

            }

            // write report
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "InventoryDepletionReport_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;

            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
                // REPORT HEADER
                {
                    string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                    var store = _dbContext.Preferences.First();
                    if (store != null)
                    {
                        var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();

                        var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                        var time = "Fecha: " + DateTime.Today.ToString("dd/MM/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                        var cellh1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));

                        Cell cellh2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        Image img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cellh2.Add(img.ScaleToFit(100, 60));

                        }
                        var cellh3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cellh1);
                        headertable.AddCell(cellh2);
                        headertable.AddCell(cellh3);

                        doc.Add(headertable);
                    }
                }
                Paragraph header = new Paragraph("Agotamiento de Inventario")
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(25);
                doc.Add(header);
                Paragraph header1 = new Paragraph(fromDate.ToString("dd-MM-yyyy") + " to " + toDate.ToString("dd-MM-yyyy"))
                          .SetTextAlignment(TextAlignment.CENTER)
                          .SetFontColor(ColorConstants.GRAY)
                          .SetFontSize(12);
                doc.Add(header1);
                var table = new iText.Layout.Element.Table(new float[] { 2, 1, 1, 1, 1, 3 });
                table.SetWidth(UnitValue.CreatePercentValue(100));

                var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Producto").SetFontSize(12));
                var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("Qty").SetFontSize(12));
                var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Unidad 1").SetFontSize(12));
                var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Unidad 2").SetFontSize(12));
                var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Unidad 3").SetFontSize(12));
                var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Barcode").SetFontSize(12));
                table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6);

                foreach (var d in depletions)
                {
                    var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(d.Product).SetFontSize(11));
                    var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("" + d.SalesQty.ToString()).SetFontSize(11));
                    var cell23 = new Cell(1, 4).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("").SetFontSize(11));
                    table.AddCell(cell21).AddCell(cell22).AddCell(cell23);

                    foreach (var p in d.Items)
                    {
                        if (p.Qty1 == 0) continue;
                        var unit1 = "";
                        var unit2 = "";
                        var unit3 = "";
                        if (!string.IsNullOrEmpty(p.Unit1)) unit1 = "" + p.Qty1.ToString("0.00", CultureInfo.InvariantCulture) + " " + p.Unit1;
                        if (!string.IsNullOrEmpty(p.Unit2)) unit1 = "" + p.Qty2.ToString("0.00", CultureInfo.InvariantCulture) + " " + p.Unit2;
                        if (!string.IsNullOrEmpty(p.Unit3)) unit1 = "" + p.Qty3.ToString("0.00", CultureInfo.InvariantCulture) + " " + p.Unit3;
                        var cell31 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("  " + p.ItemName).SetFontSize(10));
                        var cell32 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("").SetFontSize(10));
                        var cell33 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(unit1).SetFontSize(10));
                        var cell34 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(unit2).SetFontSize(10));
                        var cell35 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(unit3).SetFontSize(10));
                        var cell36 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("")).SetFontSize(10);
                        if (!string.IsNullOrEmpty(p.Barcode))
                        {
                            Barcode128 code128 = new Barcode128(pdf);
                            code128.SetCode(p.Barcode);

                            //Here's how to add barcode to PDF with IText7
                            var barcodeImg = new iText.Layout.Element.Image(code128.CreateFormXObject(pdf));
                            barcodeImg.SetWidth(120);
                            barcodeImg.SetHeight(40);
                            cell36 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(barcodeImg).SetFontSize(10);
                        }

                        table.AddCell(cell31).AddCell(cell32).AddCell(cell33).AddCell(cell34).AddCell(cell35).AddCell(cell36);
                    }
                }

                doc.Add(table);
                doc.Close();
            }

            return Json(new { status = 0, url = uploadsUrl });
        }

		[Authorize(Policy = "Permission.REPORT.CostoDeVenta")]
		[HttpPost]
        public JsonResult GenerateInventoryByGroupReport([FromBody] InventoryByGroupRequest request)
        {
            try
            {
                var toDate = DateTime.Now;
                if (!string.IsNullOrEmpty(request.to))
                {
                    try
                    {
                        toDate = DateTime.ParseExact(request.to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                    }
                    catch { }
                }
                var fromDate = DateTime.Now;
                if (!string.IsNullOrEmpty(request.from))
                {
                    try
                    {
                        fromDate = DateTime.ParseExact(request.from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                    }
                    catch { }
                }

                var stockChange = _dbContext.WarehouseStockChangeHistory.Include(s => s.Warehouse).Where(s => s.CreatedDate.Date > fromDate.Date && s.Warehouse.IsActive && (request.WarehouseID == 0 || s.Warehouse.ID == request.WarehouseID)).OrderByDescending(s => s.CreatedDate).ToList();

                var warehouse = _dbContext.Warehouses.FirstOrDefault(s => s.ID == request.WarehouseID);
                var stocks = _dbContext.WarehouseStocks.Include(s => s.Warehouse).Where(s => s.Warehouse.IsActive && (warehouse == null || s.Warehouse == warehouse)).ToList();

                var result = new List<InventoryGroupModel>();
                foreach (var stock in stocks)
                {
                    if (stock.ItemType == ItemType.Article)
                    {
                        var article = _dbContext.Articles.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Items).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                        if (article == null) continue;
                        if (!request.GroupIDs.Contains(article.Category.Group.ID)) continue;

                        var unit = article.Items.FirstOrDefault(s => s.Number == 1);
                        var exist = result.FirstOrDefault(s => s.GroupID == article.Category.Group.ID);
                        if (exist == null)
                        {

                            result.Add(new InventoryGroupModel()
                            {
                                GroupID = article.Category.Group.ID,
                                GroupName = article.Category.Group.GroupName,
                                Initial = stock.Qty * unit.Cost,
                                Final = stock.Qty * unit.Cost
                            });
                        }
                        else
                        {
                            exist.Initial += stock.Qty * unit.Cost;
                            exist.Final += stock.Qty * unit.Cost;
                        }
                    }
                    else if (stock.ItemType == ItemType.SubRecipe)
                    {
                        var subRecipe = _dbContext.SubRecipes.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                        if (subRecipe == null) continue;
                        if (!request.GroupIDs.Contains(subRecipe.Category.Group.ID)) continue;

                        var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == 1);
                        var exist = result.FirstOrDefault(s => s.GroupID == subRecipe.Category.Group.ID);
                        if (exist == null)
                        {
                            result.Add(new InventoryGroupModel()
                            {
                                GroupID = subRecipe.Category.Group.ID,
                                GroupName = subRecipe.Category.Group.GroupName,
                                Initial = stock.Qty * unit.Cost,
                                Final = stock.Qty * unit.Cost
                            });
                        }
                        else
                        {
                            exist.Initial += stock.Qty * unit.Cost;
                            exist.Final += stock.Qty * unit.Cost;
                        }
                    }
                }

                foreach (var stock in stockChange)
                {
                    if (stock.ItemType == ItemType.Article)
                    {
                        var article = _dbContext.Articles.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Items).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                        if (article == null) continue;
                        if (!request.GroupIDs.Contains(article.Category.Group.ID)) continue;

                        var unit = article.Items.FirstOrDefault(s => s.Number == 1);
                        var exist = result.FirstOrDefault(s => s.GroupID == article.Category.Group.ID);
                        var amount = stock.Qty * unit.Cost;
                        if (stock.CreatedDate.Date > toDate.Date)
                        {
                            exist.Final -= amount;
                            exist.Initial -= amount;
                        }
                        else
                        {
                            //exist.Final -= amount;
                            exist.Initial -= amount;
                            if (stock.ReasonType == StockChangeReason.PurchaseOrder)
                            {
                                exist.Purchase += amount;
                            }
                            //else if (stock.ReasonType == StockChangeReason.Kitchen)
                            //{
                            //    var orderItem = _dbContext.OrderItems.FirstOrDefault(s => !s.IsDeleted && s.ID == stock.ReasonId && s.Status == OrderItemStatus.Paid);
                            //    if (orderItem != null)
                            //    {
                            //        var existOrderItem = exist.OrderItems.FirstOrDefault(s => s.ID == orderItem.ID);
                            //        if (existOrderItem == null)
                            //        {
                            //            exist.Sales += orderItem.SubTotal;
                            //            exist.OrderItems.Add(orderItem);
                            //        }
                            //    }

                            //}
                        }

                        exist.Items.Add(new InventoryGroupSubModel()
                        {
                            Reason = stock.ReasonType,
                            ChangeDate = stock.CreatedDate,
                            Amount = stock.Qty * unit.Cost
                        });
                    }
                    else if (stock.ItemType == ItemType.SubRecipe)
                    {
                        var subRecipe = _dbContext.SubRecipes.Include(s => s.Category).ThenInclude(s => s.Group).Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                        if (subRecipe == null) continue;
                        if (!request.GroupIDs.Contains(subRecipe.Category.Group.ID)) continue;

                        var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == 1);
                        var exist = result.FirstOrDefault(s => s.GroupID == subRecipe.Category.Group.ID);
                        var amount = stock.Qty * unit.Cost;
                        if (stock.CreatedDate.Date > toDate.Date)
                        {
                            exist.Final -= amount;
                            exist.Initial -= amount;
                        }
                        else
                        {
                            //exist.Final -= amount;
                            exist.Initial -= amount;
                            if (stock.ReasonType == StockChangeReason.PurchaseOrder)
                            {
                                exist.Purchase += amount;
                            }

                        }
                        exist.Items.Add(new InventoryGroupSubModel()
                        {
                            Reason = stock.ReasonType,
                            ChangeDate = stock.CreatedDate,
                            Amount = stock.Qty * unit.Cost
                        });
                    }
                }
                var items = _dbContext.OrderItems.Include(s => s.Product).ThenInclude(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Taxes).Include(s => s.Order).Where(s => s.Status == OrderItemStatus.Paid && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date).ToList();

                foreach (var item in items)
                {
                    try
                    {
                        var groupId = item.Product.Category.Group.ID;
                        var exist = result.FirstOrDefault(s => s.GroupID == groupId);
                        if (exist != null)
                        {
                            exist.Sales += item.SubTotal;
                        }
                    }
                    catch { }
                }

                // write report
                string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
                var uniqueFileName = "Costo de ventaReport_" + "_" + DateTime.Now.Ticks + ".pdf";
                var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
                var uploadsUrl = "/temp/" + uniqueFileName;
                var paperSize = iText.Kernel.Geom.PageSize.A4;

                using (var writer = new PdfWriter(uploadsFile))
                {
                    var pdf = new PdfDocument(writer);
                    var doc = new iText.Layout.Document(pdf);
                    // REPORT HEADER
                    {
                        string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                        var store = _dbContext.Preferences.First();
                        if (store != null)
                        {
                            var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                            headertable.SetWidth(UnitValue.CreatePercentValue(100));
                            headertable.SetFixedLayout();

                            var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                            var time = "Fecha: " + DateTime.Today.ToString("dd/MM/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                            var cellh1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));

                            Cell cellh2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                            Image img = AlfaHelper.GetLogo(store.Logo);
                            if (img != null)
                            {
                                cellh2.Add(img.ScaleToFit(100, 60));

                            }
                            var cellh3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                            headertable.AddCell(cellh1);
                            headertable.AddCell(cellh2);
                            headertable.AddCell(cellh3);

                            doc.Add(headertable);
                        }
                    }
                    Paragraph header = new Paragraph("Costo de venta")
                                   .SetTextAlignment(TextAlignment.CENTER)
                                   .SetFontSize(25);
                    doc.Add(header);
                    Paragraph header1 = new Paragraph(fromDate.ToString("dd-MM-yyyy") + " to " + toDate.ToString("dd-MM-yyyy"))
                              .SetTextAlignment(TextAlignment.CENTER)
                              .SetFontColor(ColorConstants.GRAY)
                              .SetFontSize(12);
                    doc.Add(header1);
                    var table = new iText.Layout.Element.Table(new float[] { 2, 1, 1, 1, 1, 1, 1 });
                    table.SetWidth(UnitValue.CreatePercentValue(100));

                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Grupo").SetFontSize(12));
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("Inventario Inicial").SetFontSize(12));
                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Compras").SetFontSize(12));
                    var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Inventario Final").SetFontSize(12));
                    var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Costo de Ventas").SetFontSize(12));
                    var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Ventas").SetFontSize(12));
                    var cell7 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("% Costo de Ventas").SetFontSize(12));
                    table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell7);

                    foreach (var d in result)
                    {
                        var costDeventa = d.Initial + d.Purchase - d.Final;
                        decimal percent = 0;
                        if (d.Sales > 0)
                            percent = (costDeventa) / d.Sales * 100.0m;


                        var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(d.GroupName).SetFontSize(11));
                        var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.Initial)).SetFontSize(11));
                        var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.Purchase)).SetFontSize(11));
                        var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.Final)).SetFontSize(11));
                        var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(costDeventa)).SetFontSize(11));
                        var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.Sales)).SetFontSize(11));
                        var cell27 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + Math.Round(percent, 2) + "%").SetFontSize(11));
                        table.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25).AddCell(cell26).AddCell(cell27);
                    }

                    doc.Add(table);
                    doc.Close();
                }

                return Json(new { status = 0, url = uploadsUrl });
            }
            catch(Exception ex)
            {
                return Json(new { status = 1, message = ex.Message + ex.StackTrace });
            }
            
        }

		[Authorize(Policy = "Permission.REPORT.ProductCost")]
		[HttpPost]
        public JsonResult GenerateCostPercentReport(int from, int to)
        {
            var result = new List<ProductCostPercentModel>();
            var products = _dbContext.Products.Include(s => s.ServingSizes).Where(s => s.IsActive).ToList();
            foreach(var product in products)
            {
                try
                {
					if (product.HasServingSize)
					{
						foreach (var serving in product.ServingSizes)
						{
							if (serving.Price[0] == 0) continue;
							var percent = serving.Cost / serving.Price[0] * 100;
							if (percent >= from && percent <= to)
							{
								result.Add(new ProductCostPercentModel()
								{
									ProductID = product.ID,
									ProductName = product.Name + "(" + serving.ServingSizeName + ")",
									Percent = Math.Round(percent, 2),
									Price = serving.Price[0],
									Cost = serving.Cost
								});
							}
						}
					}
					else
					{
						if (product.Price[0] == 0) continue;
						var percent = product.ProductCost / product.Price[0] * 100.0m;
						if (percent >= from && percent <= to)
						{
							result.Add(new ProductCostPercentModel()
							{
								ProductID = product.ID,
								ProductName = product.Name,
								Percent = Math.Round(percent, 2),
								Price = product.Price[0],
								Cost = product.ProductCost
							});
						}
					}
				}
                catch { }
              
            }


            // write report
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "ProductPercentReport_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;

            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
                // REPORT HEADER
                {
                    string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                    var store = _dbContext.Preferences.First();
                    if (store != null)
                    {
                        var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();

                        var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                        var time = "Fecha: " + DateTime.Today.ToString("dd/MM/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                        var cellh1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));

                        Cell cellh2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        Image img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cellh2.Add(img.ScaleToFit(100, 60));

                        }
                        var cellh3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cellh1);
                        headertable.AddCell(cellh2);
                        headertable.AddCell(cellh3);

                        doc.Add(headertable);
                    }
                }
                Paragraph header = new Paragraph("Costo de Productos")
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(25);
                doc.Add(header);
                Paragraph header1 = new Paragraph("" + from + "% to " + to + "%")
                          .SetTextAlignment(TextAlignment.CENTER)
                          .SetFontColor(ColorConstants.GRAY)
                          .SetFontSize(12);
                doc.Add(header1);
                var table = new iText.Layout.Element.Table(new float[] { 1, 3, 1, 1, 1 });
                table.SetWidth(UnitValue.CreatePercentValue(100));

                var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("ID").SetFontSize(12));
                var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("Nombre").SetFontSize(12));
                var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Precio").SetFontSize(12));
                var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Costo").SetFontSize(12));
                var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Porcentaje").SetFontSize(12));
                table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5);

                foreach (var d in result)
                {
                    var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + d.ProductID).SetFontSize(11));
                    var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("" + d.ProductName).SetFontSize(11));
                    var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.Price.ToString("0.00", CultureInfo.InvariantCulture)).SetFontSize(11));
                    var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.Cost.ToString("0.00", CultureInfo.InvariantCulture)).SetFontSize(11));
                    var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.Percent.ToString("0.00", CultureInfo.InvariantCulture) + "%").SetFontSize(11));
                    table.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25);
                }

                doc.Add(table);
                doc.Close();
            }

            return Json(new { status = 0, url = uploadsUrl });
        }

        [Authorize(Policy = "Permission.REPORT.CloseDrawerReport")]
        [HttpPost]
        public JsonResult GetCloseDrawerReport(string from, string to, int userId)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var user = _dbContext.User.FirstOrDefault(s => s.ID == userId);

            var username = "";
            if (user != null)
            {
                username = user.FullName;
            }

            var closeDrawers = _dbContext.CloseDrawers.Where(s => s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date && (username == "" || username == s.UpdatedBy)).Select(s => new CloseDrawerReportModel()
            {
                WaiterName = s.Username,
                CloseDate = s.CreatedDate,
                CloseDateStr = s.CreatedDate.ToString("dd-MM-yyyy HH:mm:ss"),
                GrandTotal = s.GrandTotal,
                TransTotal = s.TransTotal,
                TransDifference = s.TransDifference,
                TipTotal = s.TipTotal,
                TipDifference = s.TipDifference,
                Dominations = JsonConvert.DeserializeObject<List<CloseDrawerDominationModel>>(s.Denominations),
                Payments = JsonConvert.DeserializeObject<List<CloseDrawerPaymentModel>>(s.PaymentMethods),

            }).ToList();

            return Json(new { status = 0, closeDrawers });
        }

		[HttpPost]
		[Authorize(Policy = "Permission.REPORT.CxCReport")]
		public JsonResult GetCXCReport(string from, string to, int customerId)
		{
			var toDate = DateTime.Now;
			if (!string.IsNullOrEmpty(to))
			{
				try
				{
					toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}
			var fromDate = DateTime.Now.AddDays(30);
			if (!string.IsNullOrEmpty(from))
			{
				try
				{
					fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}
            var customers = _dbContext.Customers.Where(s => s.IsActive).ToList();

			var customer = customers.FirstOrDefault(s => s.ID == customerId);

            
			var trans = _dbContext.OrderTransactions
						.Include(ot => ot.Order)
						.Where(ot => ot.Order != null &&
									 (ot.Order.CustomerId == customerId || customerId == 0) &&
									 ot.Method != null &&
									 ot.PaymentDate.Date >= fromDate.Date && ot.PaymentDate.Date <= toDate.Date &&
									 ot.PaymentType != null &&
									 ot.PaymentType.ToUpper() == "C X C")
						.Select(ot => new CxCReportModel
						{
							ID = ot.ID,
                            ClientName = ot.Order.ClientName,
							Amount = ot.Amount,
							Method = ot.Method,
							PaymentDate = ot.PaymentDate,
							PaymentType = ot.PaymentType,
							BeforeBalance = ot.BeforeBalance, // Asumiendo que este campo existe
							AfterBalance = ot.AfterBalance // Asumiendo que este campo existe

						})
						.ToList();

			foreach (var order in trans)
			{
				// Obtener órdenes asociadas al ReferenceId
				var associatedOrders = _dbContext.OrderTransactions
					.Where(ot => ot.ReferenceId == order.ID)
					.ToList();

				// Calcular la diferencia y almacenarla en la propiedad Difference
				order.TemporaryDifference = associatedOrders.Sum(ao => ao.Amount);
				order.AfterBalance = order.Amount - order.TemporaryDifference;
			}

			//Quitamos las que tengan en su balance 0

			List<OrderTransaction> lstFinal = new List<OrderTransaction>();
			foreach (var order in trans)
			{
				if (order.AfterBalance > 0)
				{
					lstFinal.Add(order);

				}
			}

			if (lstFinal != null && lstFinal.Any())
			{
				foreach (var objCxc in lstFinal)
				{
					objCxc.Amount = Math.Round(objCxc.Amount, 2, MidpointRounding.AwayFromZero);
					objCxc.AfterBalance = Math.Round(objCxc.AfterBalance, 2, MidpointRounding.AwayFromZero);
				}
			}
            lstFinal = lstFinal.OrderBy(s => s.PaymentDate).ToList();
            return Json(new { status = 0, trans = lstFinal, customer });
		}

		[Authorize(Policy = "Permission.REPORT.CxCReport")]
		[HttpPost]
		public JsonResult GenerateCXCReport(string from, string to, int customerId)
		{
			var toDate = DateTime.Now;
			if (!string.IsNullOrEmpty(to))
			{
				try
				{
					toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}
			var fromDate = DateTime.Now.AddDays(30);
			if (!string.IsNullOrEmpty(from))
			{
				try
				{
					fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}

			var customer = _dbContext.Customers.FirstOrDefault(s => s.ID == customerId);
			if (customer == null)
			{
				return Json(new { status = 1 });
			}
			var trans = _dbContext.OrderTransactions
						.Include(ot => ot.Order)
						.Where(ot => ot.Order != null &&
									 (ot.Order.CustomerId == customerId || customerId == 0) &&
									 ot.Method != null &&
                                     ot.PaymentDate.Date >= fromDate.Date && ot.PaymentDate.Date <= toDate.Date &&
									 ot.PaymentType != null &&
									 ot.PaymentType.ToUpper() == "C X C")
						.Select(ot => new CxCReportModel
						{
							ID = ot.ID,
							ClientName = ot.Order.ClientName,
							Amount = ot.Amount,
							Method = ot.Method,
							PaymentDate = ot.PaymentDate,
							PaymentType = ot.PaymentType,
							BeforeBalance = ot.BeforeBalance, // Asumiendo que este campo existe
							AfterBalance = ot.AfterBalance // Asumiendo que este campo existe
						})
						.ToList();

			foreach (var order in trans)
			{
				// Obtener órdenes asociadas al ReferenceId
				var associatedOrders = _dbContext.OrderTransactions
					.Where(ot => ot.ReferenceId == order.ID)
					.ToList();

				// Calcular la diferencia y almacenarla en la propiedad Difference
				order.TemporaryDifference = associatedOrders.Sum(ao => ao.Amount);
				order.AfterBalance = order.Amount - order.TemporaryDifference;
			}

			//Quitamos las que tengan en su balance 0

			List<OrderTransaction> lstFinal = new List<OrderTransaction>();
			foreach (var order in trans)
			{
				if (order.AfterBalance > 0)
				{
					lstFinal.Add(order);

				}
			}

			if (lstFinal != null && lstFinal.Any())
			{
				foreach (var objCxc in lstFinal)
				{
					objCxc.Amount = Math.Round(objCxc.Amount, 2, MidpointRounding.AwayFromZero);
					objCxc.AfterBalance = Math.Round(objCxc.AfterBalance, 2, MidpointRounding.AwayFromZero);
				}
			}
            lstFinal = lstFinal.OrderBy(s=>s.PaymentDate).ToList();

			// write report
			string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
			var uniqueFileName = "CXCReport_" + "_" + DateTime.Now.Ticks + ".pdf";
			var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
			var uploadsUrl = "/temp/" + uniqueFileName;
			var paperSize = iText.Kernel.Geom.PageSize.A4;

			using (var writer = new PdfWriter(uploadsFile))
			{
				var pdf = new PdfDocument(writer);
				var doc = new iText.Layout.Document(pdf);
				// REPORT HEADER
				{
					string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
					var store = _dbContext.Preferences.First();
					if (store != null)
					{
						var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
						headertable.SetWidth(UnitValue.CreatePercentValue(100));
						headertable.SetFixedLayout();

						var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
						var time = "Fecha: " + DateTime.Today.ToString("dd/MM/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

						var cellh1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));

						Cell cellh2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
						Image img = AlfaHelper.GetLogo(store.Logo);
						if (img != null)
						{
							cellh2.Add(img.ScaleToFit(100, 60));

						}
						var cellh3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

						headertable.AddCell(cellh1);
						headertable.AddCell(cellh2);
						headertable.AddCell(cellh3);

						doc.Add(headertable);
					}
				}
				Paragraph header = new Paragraph("CxC Reporte")
							   .SetTextAlignment(TextAlignment.CENTER)
							   .SetFontSize(25);
				doc.Add(header);
				Paragraph header1 = new Paragraph(fromDate.ToString("dd-MM-yyyy") + " to " + toDate.ToString("dd-MM-yyyy"))
						  .SetTextAlignment(TextAlignment.CENTER)
						  .SetFontColor(ColorConstants.GRAY)
						  .SetFontSize(12);
				doc.Add(header1);
                if (customer != null)
                {
					Paragraph header2 = new Paragraph(customer.Name)
					  .SetTextAlignment(TextAlignment.CENTER)
					  .SetFontColor(ColorConstants.GRAY)
					  .SetFontSize(12);
					doc.Add(header2);
				}
              
				
				var table = new iText.Layout.Element.Table(new float[] { 2, 1, 1, 1, 1, 1, 1 });
				table.SetWidth(UnitValue.CreatePercentValue(100));

				var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("CxC").SetFontSize(12));
				var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("Fecha").SetFontSize(12));
				var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Vence").SetFontSize(12));
				var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Dias vencimiento").SetFontSize(12));
				var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Monto").SetFontSize(12));
				var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Abono").SetFontSize(12));
				var cell7 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Balance").SetFontSize(12));

				table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell7);

                decimal totalAmount = 0;
                decimal totalDiff = 0;
                decimal totalBalance = 0;
				foreach (var d in lstFinal)
				{

                    var adjustdate = d.PaymentDate;
                    if (customer != null)
                    {
						adjustdate = d.PaymentDate.AddDays(customer.CreditDays);
					}
                    var diffdays = Math.Round((DateTime.Today - adjustdate).TotalDays) + 1 ;
					var cell31 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("   " + d.ID).SetFontSize(10));
					var cell32 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.PaymentDate.ToString("dd-MM-yyyy")).SetFontSize(10));
					var cell33 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.PaymentDate.AddDays(customer.CreditDays).ToString("dd-MM-yyyy")).SetFontSize(10));
					var cell34 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + diffdays).SetFontSize(10));
					var cell35 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.BeforeBalance)).SetFontSize(10));
					var cell36 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.TemporaryDifference)).SetFontSize(10));
					var cell37 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.AfterBalance)).SetFontSize(10));
                    totalAmount += d.BeforeBalance;
                    totalDiff += d.TemporaryDifference;
                    totalBalance += d.AfterBalance;

					table.AddCell(cell31).AddCell(cell32).AddCell(cell33).AddCell(cell34).AddCell(cell35).AddCell(cell36).AddCell(cell37);
				}
				var cell41 = new Cell(1, 4).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Total").SetFontSize(10));
				var cell42 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalAmount)).SetFontSize(12));
				var cell43 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalDiff)).SetFontSize(12));
				var cell44 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(totalBalance)).SetFontSize(12));
                table.AddCell(cell41).AddCell(cell42).AddCell(cell43).AddCell(cell44);

				doc.Add(table);
				doc.Close();
			}

			return Json(new { status = 0, url = uploadsUrl });
		}

		[Authorize(Policy = "Permission.REPORT.CloseDrawerReport")]
		[HttpPost]
        public JsonResult GenerateCloseDrawerReport(string from, string to, int userId)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var user = _dbContext.User.FirstOrDefault(s => s.ID == userId);

            var username = "";
            if (user != null)
            {
                username = user.Username;
            }

            var closeDrawers = _dbContext.CloseDrawers.Where(s => s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date && (username == "" || username == s.UpdatedBy)).Select(s => new CloseDrawerReportModel()
            {
                WaiterName = s.Username,
                CloseDate = s.CreatedDate,
                GrandTotal = s.GrandTotal,
                TransTotal = s.TransTotal,
                TransDifference = s.TransDifference,
                TipTotal = s.TipTotal,
                TipDifference = s.TipDifference,
                Dominations = JsonConvert.DeserializeObject<List<CloseDrawerDominationModel>>(s.Denominations),
                Payments = JsonConvert.DeserializeObject<List<CloseDrawerPaymentModel>>(s.PaymentMethods),

            }).ToList();


            // write report
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "CloseDrawerReport_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;

            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
                // REPORT HEADER
                {
                    string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                    var store = _dbContext.Preferences.First();
                    if (store != null)
                    {
                        var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();

                        var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                        var time = "Fecha: " + DateTime.Today.ToString("dd/MM/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                        var cellh1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));

                        Cell cellh2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        Image img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cellh2.Add(img.ScaleToFit(100, 60));

                        }
                        var cellh3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cellh1);
                        headertable.AddCell(cellh2);
                        headertable.AddCell(cellh3);

                        doc.Add(headertable);
                    }
                }
                Paragraph header = new Paragraph("Cuadre de Caja")
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(25);
                doc.Add(header);
                Paragraph header1 = new Paragraph(fromDate.ToString("dd-MM-yyyy") + " to " + toDate.ToString("dd-MM-yyyy"))
                          .SetTextAlignment(TextAlignment.CENTER)
                          .SetFontColor(ColorConstants.GRAY)
                          .SetFontSize(12);
                doc.Add(header1);
                var table = new iText.Layout.Element.Table(new float[] { 2, 1, 1, 1, 1, 1, 1 });
                table.SetWidth(UnitValue.CreatePercentValue(100));

                var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Fecha").SetFontSize(12));
                var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("Usuario").SetFontSize(12));
                var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Conteo Fisico").SetFontSize(12));
                var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Sistema").SetFontSize(12));
                var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Diferencia").SetFontSize(12));
                var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Propina").SetFontSize(12));
                var cell7 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Dif.Prop.").SetFontSize(12));

                table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell7);

                foreach (var d in closeDrawers)
                {
                    var cell31 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("   " + d.CloseDate.ToString("yyyy/MM/dd HH:mm:ss")).SetFontSize(10));
                    var cell32 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.WaiterName).SetFontSize(10));
                    var cell33 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.GrandTotal)).SetFontSize(10));
                    var cell34 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.TransTotal)).SetFontSize(10));
                    var cell35 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.TransDifference)).SetFontSize(10));
                    var cell36 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.TipTotal)).SetFontSize(10));
                    var cell37 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(d.TipDifference)).SetFontSize(10));

                    table.AddCell(cell31).AddCell(cell32).AddCell(cell33).AddCell(cell34).AddCell(cell35).AddCell(cell36).AddCell(cell37);

                    foreach (var p in d.Dominations)
                    {
                        var cell41 = new Cell(1, 3).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("   ").SetFontSize(10));
                        var cell42 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Name).SetFontSize(9));
                        var cell43 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Qty.ToString("0")).SetFontSize(9));
                        var cell44 = new Cell(1, 2).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(GetMoneyString(p.Amount)).SetFontSize(9));
                        table.AddCell(cell41).AddCell(cell42).AddCell(cell43).AddCell(cell44);
                    }

                    foreach (var p in d.Payments)
                    {
                        var cell41 = new Cell(1, 3).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("   ").SetFontSize(10));
                        var cell42 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Name).SetFontSize(9));
                        var cell43 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("").SetFontSize(9));
                        var cell44 = new Cell(1, 2).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(GetMoneyString(p.Amount)).SetFontSize(9));
                        table.AddCell(cell41).AddCell(cell42).AddCell(cell43).AddCell(cell44);
                    }


                }

                doc.Add(table);
                doc.Close();
            }


            return Json(new { status = 0, url = uploadsUrl });
        }

		[Authorize(Policy = "Permission.REPORT.PaymentMethodReport")]
		[HttpPost]
        public JsonResult GenerateSalesByMethodReport(string from, string to)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var result = new List<SalesReportByMethodModel>();

            var transactions = _dbContext.OrderTransactions.Include(s => s.Order).Where(s => s.Type != TransactionType.CloseDrawer && s.Status == TransactionStatus.Closed && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date).ToList();
            decimal Total = 0;
            foreach (var transaction in transactions)
            {
                Total += transaction.Amount;
                var exist = result.FirstOrDefault(s => s.PaymentMethods == transaction.Method);
                if (exist != null)
                {
                    exist.Count++;
                    exist.Amount += transaction.Amount;
                    exist.Sales.Add(new SalesByMethodsDetail()
                    {
                        ClientName = transaction.Order?.ClientName,
                        Amount = transaction.Amount,
                        PayDate = transaction.PaymentDate
                    });

                }
                else
                {
                    result.Add(new SalesReportByMethodModel()
                    {
                        Count = 1,
                        Amount = transaction.Amount,
                        PaymentMethods = transaction.Method,
                        Sales = new List<SalesByMethodsDetail>()
                        {
                            new SalesByMethodsDetail()
                            {
                                ClientName = transaction.Order?.ClientName,
                                Amount = transaction.Amount,
                                PayDate = transaction.PaymentDate
                            }
                        }
                    });
                }
            }

            // write report
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "SalesDetailsReport_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;

            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
                // REPORT HEADER
                {
                    string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                    var store = _dbContext.Preferences.First();
                    if (store != null)
                    {
                        var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();

                        var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                        var time = "Fecha: " + DateTime.Today.ToString("MM/dd/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));
                        Cell cell2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        Image img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cell2.Add(img.ScaleToFit(100, 60));

                        }

                        var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cell1);
                        headertable.AddCell(cell2);
                        headertable.AddCell(cell3);

                        doc.Add(headertable);
                    }
                }

                Paragraph header = new Paragraph("Ventas por Metodo de Pago")
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(25);
                doc.Add(header);
                Paragraph header1 = new Paragraph(fromDate.ToString("dd-MM-yyyy") + " to " + toDate.ToString("dd-MM-yyyy"))
                              .SetTextAlignment(TextAlignment.CENTER)
                              .SetFontColor(ColorConstants.GRAY)
                              .SetFontSize(12);
                doc.Add(header1);


                {
                    var prodtable = new iText.Layout.Element.Table(new float[] { 2, 1, 1 });
                    prodtable.SetWidth(UnitValue.CreatePercentValue(100));

                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Metodo de Pago").SetFontSize(12));
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Count").SetFontSize(12));
                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Monto").SetFontSize(12));

                    prodtable.AddCell(cell1).AddCell(cell2).AddCell(cell3);

                    foreach (var sale in result)
                    {
                        var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(sale.PaymentMethods).SetFontSize(11));
                        var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + sale.Count.ToString("0")).SetFontSize(11));
                        var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(sale.Amount)).SetFontSize(11));
                        prodtable.AddCell(cell11).AddCell(cell12).AddCell(cell13);
                    }
                    var cell111 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Total").SetFontSize(12));
                    var cell112 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("").SetFontSize(12));
                    var cell113 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(GetMoneyString(Total)).SetFontSize(12));
                    prodtable.AddCell(cell111).AddCell(cell112).AddCell(cell113);
                    doc.Add(prodtable);
                }

                doc.Close();

            }
            return Json(new { status = 0, url = uploadsUrl });
        }
		[Authorize(Policy = "Permission.REPORT.VoidOrdersReport")]
		[HttpPost]
        public JsonResult GenerateVoidOrdersReport(string from, string to)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var voidProducts = _dbContext.CanceledItems.Include(s => s.Product).Include(s => s.Reason).Include(s => s.Item).ThenInclude(s => s.Order).Where(s => s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date).OrderByDescending(s => s.UpdatedDate).ToList();

            var result = new List<VoidOrderReportModel>();

            foreach (var p in voidProducts)
            {
                if (p.Item.Order != null && p.Item.Order.Status == OrderStatus.Void)
                {
                    var exist = result.FirstOrDefault(s => s.OrderId == p.Item.Order.ID);
                    if (exist == null)
                    {
                        result.Add(new VoidOrderReportModel()
                        {
                            OrderId = p.Item.Order.ID,
                            Reason = p.Reason.Reason,
                            VoidDate = p.UpdatedDate,
                            OrderAmount = p.Item.Order.TotalPrice,
                            Waitername = p.Item.Order.WaiterName,
                            VoidBy = p.Item.Order.UpdatedBy
                        });
                    }
                }
            }


            // write report
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "VoidProducts_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;

            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
                // REPORT HEADER
                {
                    string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                    var store = _dbContext.Preferences.First();
                    if (store != null)
                    {
                        var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();

                        var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                        var time = "Fecha: " + DateTime.Today.ToString("MM/dd/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));
                        Cell cell2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        Image img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cell2.Add(img.ScaleToFit(100, 60));

                        }

                        var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cell1);
                        headertable.AddCell(cell2);
                        headertable.AddCell(cell3);

                        doc.Add(headertable);
                    }
                }

                Paragraph header = new Paragraph("Ordenes Anuladas")
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(25);
                doc.Add(header);
                Paragraph header1 = new Paragraph(fromDate.ToString("dd-MM-yyyy") + " to " + toDate.ToString("dd-MM-yyyy"))
                              .SetTextAlignment(TextAlignment.CENTER)
                              .SetFontColor(ColorConstants.GRAY)
                              .SetFontSize(12);
                doc.Add(header1);

                {
                    var prodtable = new iText.Layout.Element.Table(new float[] { 1, 1, 1, 3, 1, 1 });
                    prodtable.SetWidth(UnitValue.CreatePercentValue(100));

                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Orden #").SetFontSize(12));
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Monto").SetFontSize(12));
                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Fecha").SetFontSize(12));
                    var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Razon").SetFontSize(12));
                    var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Usuario").SetFontSize(12));
                    var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Void by").SetFontSize(12));

                    prodtable.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6);

                    foreach (var p in result)
                    {
                        var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.OrderId).SetFontSize(11));
                        var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(GetMoneyString(p.OrderAmount)).SetFontSize(11));
                        var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.VoidDate.ToString("MM/dd/yyyy")).SetFontSize(11));
                        var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.Reason).SetFontSize(11));
                        var cell15 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.Waitername != null ? p.Waitername : "").SetFontSize(11));
                        var cell16 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.VoidBy).SetFontSize(11));
                        prodtable.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16);
                    }

                    doc.Add(prodtable);
                }

                doc.Close();

            }
            return Json(new { status = 0, url = uploadsUrl ,});
        }
		[Authorize(Policy = "Permission.REPORT.VoidProductsReport")]
		[HttpPost]
        public JsonResult ViewVoidProductsReport(string from, string to)
        {
			var toDate = DateTime.Now;
			if (!string.IsNullOrEmpty(to))
			{
				try
				{
					toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}
			var fromDate = DateTime.Now;
			if (!string.IsNullOrEmpty(from))
			{
				try
				{
					fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}
			var voidProducts = _dbContext.CanceledItems.Include(s => s.Product).Include(s => s.Reason).Include(s => s.Item).ThenInclude(s => s.Order).Where(s => s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date).OrderByDescending(s => s.UpdatedDate).ToList();

            List<VoidOrderReportViewModel> Listmodelo = new List<VoidOrderReportViewModel>();
			string sumaAmount = "0.0";
			decimal sumaAmountdes = 0.0m;
			bool resultado = false;
            if (voidProducts.Count()>0)
            {
                resultado = true;
				foreach (var item in voidProducts)
				{
					var existe = Listmodelo.FirstOrDefault(s => s.Id.Equals(item.ID.ToString()));

					if (existe == null)
					{
						VoidOrderReportViewModel items = new VoidOrderReportViewModel();

						items.Id = item.ID.ToString();
						items.Order = item.Item.Order.ID.ToString();
						items.Amount = GetMoneyString(item.Item.SubTotal);
						sumaAmountdes = sumaAmountdes + item.Item.SubTotal;
						items.Product = item.Product.Name;
						items.Qty = item.Item.Qty.ToString("0");
						items.Fecha = item.UpdatedDate.ToString("MM/dd/yyyy H:mm:ss");
						items.FechaCreado = item.Item.CreatedDate.ToString("MM/dd/yyyy H:mm:ss");
						items.Razon = item.Reason.Reason;				                        
                        items.Usuario = item.Item.Order.WaiterName;
                        items.Void_By = item.UpdatedBy;
                        Listmodelo.Add(items);
					}
				}

			}
			
			sumaAmount = GetMoneyString(sumaAmountdes);
			return Json(new { resultado = resultado, Listmodelo = Listmodelo.OrderByDescending(a => a.Order), sumaAmount= sumaAmount });
		}
		[Authorize(Policy = "Permission.REPORT.VoidOrdersReport")]
		[HttpPost]
		public JsonResult ViewVoidOrdersReport(string from, string to)
		{
			var toDate = DateTime.Now;
			if (!string.IsNullOrEmpty(to))
			{
				try
				{
					toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}
			var fromDate = DateTime.Now;
			if (!string.IsNullOrEmpty(from))
			{
				try
				{
					fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				}
				catch { }
			}
			var voidProducts = _dbContext.CanceledItems.Include(s => s.Product).Include(s => s.Reason).Include(s => s.Item).ThenInclude(s => s.Order).Where(s => s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date).OrderByDescending(s => s.UpdatedDate).ToList();

			List<VoidOrderReportViewModel> Listmodelo = new List<VoidOrderReportViewModel>();
            string sumaAmount = "0.0";
            decimal sumaAmountdes = 0.0m;
            bool resultado=false;

            List<long> lstOrdersID= new List<long>();

            if (voidProducts.Count()>0)
			{
				foreach (var item in voidProducts)
				{
					if (item.Item.Order != null && item.Item.Order.Status == OrderStatus.Void)
					{
                           //.FirstOrDefault(s => s.Order.Contains(item.Item.Order.ID.ToString()));

						VoidOrderReportViewModel items = new VoidOrderReportViewModel();
                        if (!lstOrdersID.Contains(item.Item.Order.ID))// existe == null)
                        {

                            productospopup productositems = new productospopup();
                            items.listaProductos = new List<productospopup>();

							productositems.Product = item.Product.Name.ToString();
                            productositems.Productprecio = GetMoneyString(item.Item.SubTotal);  

                            items.listaProductos.Add(productositems);


                            items.Id = item.ID.ToString();
                            items.Order = item.Item.Order.ID.ToString();

                            lstOrdersID.Add(item.Item.Order.ID);

                            items.Amount = GetMoneyString(item.Item.Order.TotalPrice);
                            sumaAmountdes = sumaAmountdes + item.Item.Order.TotalPrice;
                            items.Fecha = item.UpdatedDate.ToString("MM/dd/yyyy H:mm:ss");
                            items.FechaCreado = item.Item.Order.CreatedDate.ToString("MM/dd/yyyy H:mm:ss");
                            items.Razon = item.Reason.Reason;
                            items.Usuario = item.UpdatedBy;
                            items.Void_By = item.Item.Order.UpdatedBy;
                            Listmodelo.Add(items);
                        }
                        else
						{
                            items = Listmodelo.Where(s => s.Order == item.Item.Order.ID.ToString()).First();

                            productospopup productositems = new productospopup();

							productositems.Product = item.Product.Name.ToString();
							productositems.Productprecio = GetMoneyString(item.Item.SubTotal);


                            items.listaProductos.Add(productositems);
                            //existe.listaProductos.Add(productositems);


                            /*items.Id = item.ID.ToString();
							items.Order = item.Item.Order.ID.ToString();
						items.Amount = item.Item.Order.TotalPrice.ToString("N2");
						sumaAmountdes = sumaAmountdes + item.Item.Order.TotalPrice;
						items.Fecha = item.UpdatedDate.ToString("MM/dd/yyyy H:mm:ss");
						items.FechaCreado = item.Item.Order.CreatedDate.ToString("MM/dd/yyyy H:mm:ss");
						items.Razon = item.Reason.Reason;
						items.Void_By = item.UpdatedBy;
						items.Usuario  = item.Item.Order.CreatedBy;
						Listmodelo.Add(items);*/

						}
					}

				}
                resultado = true;
			}
		


            sumaAmount = GetMoneyString(sumaAmountdes);
			return Json(new { resultado = resultado, Listmodelo = Listmodelo.OrderByDescending(a=>a.Order).Distinct(), sumaAmount = sumaAmount });
		}
		[Authorize(Policy = "Permission.REPORT.VoidProductsReport")]
		[HttpPost]
        public JsonResult GenerateVoidProductsReport(string from, string to)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var voidProducts = _dbContext.CanceledItems.Include(s => s.Product).Include(s => s.Reason).Include(s => s.Item).ThenInclude(s => s.Order).Where(s => s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date).OrderByDescending(s => s.UpdatedDate).ToList();


            // write report
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "VoidProducts_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;

            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
                // REPORT HEADER
                {
                    string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                    var store = _dbContext.Preferences.First();
                    if (store != null)
                    {
                        var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();

                        var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                        var time = "Fecha: " + DateTime.Today.ToString("MM/dd/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                        var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));
                        Cell cell2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        Image img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cell2.Add(img.ScaleToFit(100, 60));

                        }

                        var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cell1);
                        headertable.AddCell(cell2);
                        headertable.AddCell(cell3);

                        doc.Add(headertable);
                    }
                }

                Paragraph header = new Paragraph("Productos Anulados")
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(25);
                doc.Add(header);
                Paragraph header1 = new Paragraph(fromDate.ToString("dd-MM-yyyy") + " to " + toDate.ToString("dd-MM-yyyy"))
                              .SetTextAlignment(TextAlignment.CENTER)
                              .SetFontColor(ColorConstants.GRAY)
                              .SetFontSize(12);
                doc.Add(header1);

                {
                    var prodtable = new iText.Layout.Element.Table(new float[] { 2, 1, 1, 3, 1, 1 });
                    prodtable.SetWidth(UnitValue.CreatePercentValue(100));

                    var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Producto").SetFontSize(12));
                    var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Qty").SetFontSize(12));
                    var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Fecha").SetFontSize(12));
                    var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Razon").SetFontSize(12));
                    var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Usuario").SetFontSize(12));
                    var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Void by").SetFontSize(12));

                    prodtable.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6);

                    foreach (var p in voidProducts)
                    {
                        var cell11 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.Product.Name).SetFontSize(11));
                        var cell12 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("" + p.Item.Qty.ToString("0")).SetFontSize(11));
                        var cell13 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.UpdatedDate.ToString("MM/dd/yyyy")).SetFontSize(11));
                        var cell14 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.Reason.Reason).SetFontSize(11));
                        var cell15 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.Item.Order.CreatedBy ).SetFontSize(11));
                        var cell16 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(p.UpdatedBy).SetFontSize(11));
                        prodtable.AddCell(cell11).AddCell(cell12).AddCell(cell13).AddCell(cell14).AddCell(cell15).AddCell(cell16); ;
                    }

                    doc.Add(prodtable);
                }

                doc.Close();

            }
            return Json(new { status = 0, url = uploadsUrl });
        }

        private void AddInventorySubToModel(InventoryDepletionModel model, InventoryStockSub sub)
        {
            var existsub = model.Items.FirstOrDefault(s => s.ItemId == sub.ItemId && s.ItemName == sub.ItemName);
            if (existsub == null)
            {
                model.Items.Add(sub);
            }
            else
            {
                existsub.Qty1 += sub.Qty1;
                existsub.Qty2 += sub.Qty2;
                existsub.Qty3 += sub.Qty3;
                existsub.TotalCost += sub.TotalCost;
            }
        }

        private void AddInventorySubToModel1(InventoryLevelModel model, InventoryStockSub sub)
        {
            var existsub = model.Items.FirstOrDefault(s => s.ItemId == sub.ItemId && s.ItemName == sub.ItemName);
            if (existsub == null)
            {
                model.Items.Add(sub);
            }
            else
            {
                existsub.Qty1 += sub.Qty1;
                existsub.Qty2 += sub.Qty2;
                existsub.Qty3 += sub.Qty3;
                existsub.TotalCost += sub.TotalCost;
            }
        }

        private InventoryStockSub GetInventorySubFromArticle(InventoryItem article, decimal qty, int unitnum)
        {
            var result = new InventoryStockSub();
            result.ItemId = article.ID;
            result.ItemName = article.Name;
            result.Brand = article.Brand.Name;
            result.Barcode = article.Barcode;   

            var units = article.Items.OrderBy(s => s.Number).ToList();

            var qty1 = AlfaHelper.ConvertQtyToBase(qty, unitnum, units); result.Qty1 = qty1; result.Unit1 = units[0].Name;

            try
            {
                var qty2 = AlfaHelper.ConvertBaseQtyTo(qty1, 2, units); result.Qty2 = qty2; result.Unit2 = units[1].Name;
            }
            catch { }
            try
            {
                var qty3 = AlfaHelper.ConvertBaseQtyTo(qty1, 3, units); result.Qty3 = qty3; result.Unit3 = units[2].Name;
            }
            catch { }

            result.TotalCost = qty1 * units[0].Cost;
            result.Cost = units[0].Cost;
            return result;
        }

        private InventoryStockSub GetInventorySubFromSubRecipe(SubRecipe article, decimal qty, int unitnum)
        {
            var result = new InventoryStockSub();
            result.ItemId = article.ID;
            result.ItemName = article.Name;
            result.Barcode = article.Barcode;
            result.Brand = "SubRecipe";

            var units = article.ItemUnits.OrderBy(s => s.Number).ToList();

            var qty1 = AlfaHelper.ConvertQtyToBase(qty, unitnum, units); result.Qty1 = qty1; result.Unit1 = units[0].Name;

            try
            {
                var qty2 = AlfaHelper.ConvertBaseQtyTo(qty1, 2, units); result.Qty2 = qty2; result.Unit2 = units[1].Name;
            }
            catch { }
            try
            {
                var qty3 = AlfaHelper.ConvertBaseQtyTo(qty1, 3, units); result.Qty3 = qty3; result.Unit3 = units[2].Name;
            }
            catch { }

            result.TotalCost = qty1 * units[0].Cost;
            result.Cost = units[0].Cost;
            return result;
        }
		[Authorize(Policy = "Permission.REPORT.WorkDayReport")]
		[HttpPost]
        public JsonResult GetWeekOfDayReport(string from, string to, int sucursal)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var transactions = _dbContext.OrderTransactions.Include(s => s.Order).Include(s => s.Order.Station).Include(s => s.Order.Items).Where(s => s.Type != TransactionType.CloseDrawer && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date && (s.Order.Station.IDSucursal == sucursal || sucursal == 0)).ToList();

            var results = new List<WeekOfDayReportModel>();
            for (var day = fromDate.Date; day.Date <= toDate.Date; day = day.AddDays(1))
            {
                var dayStr = day.ToString("dddd");
                var exist = results.FirstOrDefault(s => s.Day == (int)day.DayOfWeek);
                if (exist == null)
                {
                    results.Add(new WeekOfDayReportModel()
                    {
                        Day = (int)day.DayOfWeek,
                        DayStr = day.DayOfWeek.ToString(),
                        WeekCount = 1
                    });
                }
                else
                {
                    exist.WeekCount++;
                }
            }

            results = results.OrderBy(s => s.Day).ToList();

            foreach (var trans in transactions)
            {
                var day = trans.UpdatedDate;
                var exist = results.FirstOrDefault(s => s.Day == (int)day.DayOfWeek);
                //if (exist == null)
                //{
                //	results.Add(new WeekOfDayReportModel 
                //	{  
                //		Transactions = new List<OrderTransaction>() { trans }
                //	});
                //}
                if (exist != null)
                {
                    exist.Transactions.Add(trans);
                }
            }
            decimal TotalAvg = 0;
            foreach (var tran in results)
            {
                try
                {
                    tran.TotalTrans = tran.Transactions.Count;
                    if (tran.TotalTrans > 0)
                    {
                        tran.TotalSales = Math.Round(tran.Transactions.Sum(s => s.Amount), 2);
                        tran.AvgSales = Math.Round(tran.TotalSales / tran.WeekCount, 2);
                        tran.AvgTrans = (int)Math.Floor((decimal)tran.TotalTrans / (decimal)tran.WeekCount);
                        tran.AvgAvgSales = Math.Round(tran.AvgSales / tran.AvgTrans, 2);
                        TotalAvg += tran.AvgSales;
                    }
                    //No se neesita enviar la data a la vista
                    tran.Transactions = null;
                }
                catch { }

            }

            return Json(new { trans = results, total = TotalAvg });
        }
		[Authorize(Policy = "Permission.REPORT.OrdersReport")]
		[HttpPost]
        public JsonResult GetWorkDayReportOrders(string from, string to, int user, int sucursal, int orden)
        {


            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            string UserFullName = "";

            if (user != 0)
            {
                UserFullName = _dbContext.Customers.Where(s => s.ID == user).First().Name;
            }

            var Listorders = _dbContext.Orders.Include(s=>s.Items).Include(s=>s.PrepareType).Where(a => a.Items.Any()  && a.Items.First().UpdatedDate.Date >= fromDate.Date && a.Items.First().UpdatedDate.Date <= toDate.Date && !a.IsDeleted && (a.Station.IDSucursal == sucursal || sucursal == 0) && (a.ClientName == UserFullName || UserFullName == "") && (a.ID == orden || orden==0)).ToList();

            string subtotal_ = "0";
            string descuento_ = "0";
            string itbis_ = "0";
            string propuba_ = "0";
            string propinavoluntaria_ = "0";
            string delivery_ = "0";
            string total_ = "0";
            string costo_ = "0";
            string ganancia_ = "0";

            List<LOrdersModel> Modelo = new List<LOrdersModel>();
            foreach (var item in Listorders)
            {
                //var costumber_ = _dbContext.Customers.Where(a => a.ID == item.CustomerId).FirstOrDefault();
                var orderTransactions_ = _dbContext.OrderTransactions.Include(a=>a.Order).Where(a => a.Order.ID == item.ID);
                var listOrdersFirst = item.Items.First();

                LOrdersModel addItems = new LOrdersModel();
                addItems.Fecha = listOrdersFirst.UpdatedDate.ToShortDateString();
                if (item.ClientName!= null)
                {
                    addItems.ClienteName = item.ClientName;
                }
                else
                {

                    addItems.ClienteName = "Sin Cliente";
                }

                addItems.Numero = item.ComprobanteNumber;
                addItems.OrderNum = item.ID.ToString();
                addItems.Abierta = listOrdersFirst.Order.OrderTime.ToString("g");
                addItems.AbiertaName = item.CreatedBy.ToString();
                if (orderTransactions_.Any())
                {
                    var orderTransactions_First = orderTransactions_.First();
                    addItems.Abierta = (new DateTime(orderTransactions_First.UpdatedDate.Year, orderTransactions_First.UpdatedDate.Month, orderTransactions_First.UpdatedDate.Day, listOrdersFirst.Order.CreatedDate.Hour, listOrdersFirst.Order.CreatedDate.Minute, listOrdersFirst.Order.CreatedDate.Second)).ToString("g");
                    addItems.Cerrada = orderTransactions_First.CreatedDate.ToString("g");
                    addItems.CerradaName = orderTransactions_First.CreatedBy;

                }
                else
                {
                    addItems.Cerrada = "";
                    addItems.CerradaName = "";

                }

                addItems.SubTotal = GetMoneyString(item.SubTotal);
                addItems.Descuento = GetMoneyString(item.Discount);
                addItems.Itbis = GetMoneyString(item.Tax);
                
                addItems.Delivery = GetMoneyString(item.Delivery != null ? (decimal)item.Delivery : 0.0m);
                addItems.Tipo = (item.PrepareType != null ? item.PrepareType.Name : "");                
                addItems.Total = GetMoneyString(item.TotalPrice);

                if (orderTransactions_.Any()) {
                    addItems.PropinaVoluntaria = GetMoneyString(orderTransactions_.Where(a => a.Type == TransactionType.Tip).Sum(a => a.Amount));
                }
                else {
                    addItems.PropinaVoluntaria = "";
                }

                //addItems.Propina = (item.Propina).ToString("N2");

                var ListMetodoItems = orderTransactions_; // _dbContext.OrderTransactions.Include(a => a.Order).Where(a => a.Order.ID == item.ID).ToList();
                addItems.ListmetodoPago = new List<MetodoPagoModelo>();

                foreach (var itemww in ListMetodoItems)
                {
                    MetodoPagoModelo itemsAdds = new MetodoPagoModelo();

                    if (addItems.ListmetodoPago.Count() > 0 )
                    {
                    var precioExistente = addItems.ListmetodoPago.Where(a => a.Tipo == itemww.Method).FirstOrDefault();

                        if (precioExistente != null)
                        {
                            precioExistente.Cantidad = GetMoneyString(decimal.Parse(precioExistente.Cantidad) + itemww.Amount);
                        }
                        else
                        {
                            if (itemsAdds.Cantidad == null)
                            {
                                itemsAdds.Cantidad = "0";
                            }
                            if (precioExistente != null)
                            {
                                itemsAdds.Cantidad = GetMoneyString((decimal.Parse(precioExistente.Cantidad) + itemww.Amount));
                            }
                            else
                              {
                                itemsAdds.Cantidad = GetMoneyString(itemww.Amount);
                               }
                                itemsAdds.Tipo = itemww.Method.ToString();
                            addItems.ListmetodoPago.Add(itemsAdds);
                        }

                    }
                    else
                    {
                        if (itemsAdds.Cantidad == null)
                        {
                            itemsAdds.Cantidad = "0";
                        }
                        itemsAdds.Cantidad = GetMoneyString((decimal.Parse(itemsAdds.Cantidad) + itemww.Amount)); 
                        itemsAdds.Tipo = itemww.Method.ToString();
                        addItems.ListmetodoPago.Add(itemsAdds);

                    }
                  
                }

                // Procesamos los items

                var ListOrderItems = _dbContext.OrderItems.Include(s => s.Product).ThenInclude(s => s.RecipeItems).Where(a => a.OrderID == item.ID && !a.IsDeleted).OrderBy(a=>a.OrderID).ToList();
                addItems.ListOrderItems = new List<OrdersItemsModel>();


                foreach (var itemdos in ListOrderItems.OrderBy(a => a.OrderID).ToList())
                {
                    OrdersItemsModel ordersItems = new OrdersItemsModel();



                    ordersItems.Producto = itemdos.Name;
                    ordersItems.Precio = GetMoneyString(((itemdos.Price)));
                    ordersItems.QTY = itemdos.Qty.ToString();
                    ordersItems.SubTotal = GetMoneyString(itemdos.SubTotal);
                    ordersItems.Descuento = itemdos.Discount.ToString();
                    ordersItems.Itbis = GetMoneyString(itemdos.Tax);
                    ordersItems.Propina = GetMoneyString(itemdos.Propina);
                    if (addItems.Propina != null)
                    {
                        addItems.Propina = GetMoneyString((decimal.Parse(addItems.Propina) + itemdos.Propina));
                    }
                    else
                    {
                        addItems.Propina = GetMoneyString(itemdos.Propina);

                    }
                    
                    addItems.Costo = GetMoneyString(decimal.Parse(addItems.Costo != null ? addItems.Costo : "0") + itemdos.Costo);

                  
                    ordersItems.Total = GetMoneyString((itemdos.Qty * itemdos.Price) + itemdos.Tax);
                    ordersItems.Costo = GetMoneyString(itemdos.Costo);
                    ordersItems.Ganancia = GetMoneyString((decimal.Parse(ordersItems.SubTotal) - itemdos.Costo));
                    if (ordersItems.Ganancia.Contains("-"))
                    {
                        ordersItems.Ganancia = "0";
                    }
                    ordersItems.CostoPor = ((itemdos.SubTotal != 0.0m) ? GetMoneyString((itemdos.Costo / itemdos.SubTotal) * 100) : "0");
                    ;



                    ordersItems.ListRecetasItems = new List<ProductoArticuloModelo>();


                    //var product = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions.OrderBy(s => s.DisplayOrder)).FirstOrDefault(s => s.ID == itemdos.Product.ID);
                    var product = _dbContext.Products.Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions.OrderBy(s => s.DisplayOrder)).FirstOrDefault(s => s.ID == itemdos.Product.ID);


                    if (product.HasServingSize)
                    {

                        foreach (var ss in product.ServingSizes)
                        {
                            var ret = new ProductServingSizeViewModel(ss);

                            var items = product.RecipeItems.Where(s => s.ServingSizeID == ss.ServingSizeID);



                            foreach (var itemProducto in items)
                            {

                                if (ret.ServingSizeID == itemdos.ServingSizeID)
                                {
                                    var subRecipeItem = new ProductRecipeItemViewModel(itemProducto);
                                    if (itemProducto.Type == ItemType.Article)
                                    {
                                        var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                        var unit = article.Items.FirstOrDefault(s => s.Number == itemProducto.UnitNum);
                                        subRecipeItem.Article = article;

                                        decimal itemCost = (from m in article.Items where m.Number == itemProducto.UnitNum select m.Cost).FirstOrDefault();

                                        decimal itemPrice = (from m in article.Items where m.Number == itemProducto.UnitNum select m.Price).FirstOrDefault();
                                        ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                        ordersIte.articulo = article.Name;
                                        ordersIte.marca = article.Brand.Name;
                                        ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                        ordersIte.unidad = unit.Name;
                                        ordersIte.costo = GetMoneyString(itemCost);
                                        ordersIte.total = GetMoneyString(decimal.Parse(ordersIte.qty) * itemCost);

                                        ordersItems.ListRecetasItems.Add(ordersIte);
                                    }
                                    else if (itemProducto.Type == ItemType.Product)
                                    {
                                        var product1 = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                        subRecipeItem.Product = product1;

                                        if (product1.HasServingSize)
                                        {
                                            decimal itemCost = (from m in product1.ServingSizes where m.ServingSizeID == itemProducto.UnitNum select m.Cost).FirstOrDefault();

                                            ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                            ordersIte.articulo = product1.Name;
                                            ordersIte.marca = product1.Barcode;
                                            ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                            ordersIte.unidad = "N/A";
                                            ordersIte.costo = GetMoneyString(itemCost);
                                            ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemCost));
                                            ordersItems.ListRecetasItems.Add(ordersIte);
                                        }
                                        else
                                        {
                                            ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                            ordersIte.articulo = product.Name;
                                            ordersIte.marca = product.Barcode;
                                            ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                            ordersIte.unidad = "N/A";
                                            ordersIte.costo = GetMoneyString(product.ProductCost);
                                            ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemProducto.Qty));

                                            ordersItems.ListRecetasItems.Add(ordersIte);

                                        }


                                    }
                                    else
                                    {
                                        var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                        var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == itemProducto.UnitNum);
                                        subRecipeItem.SubRecipe = subRecipe;

                                        decimal itemCost = (from m in subRecipe.ItemUnits where m.Number == itemProducto.UnitNum select m.Cost).FirstOrDefault();
                                        ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                        ordersIte.articulo = subRecipe.Name;
                                        ordersIte.marca = subRecipe.Name;
                                        ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                        ordersIte.unidad = unit.Name;
                                        ordersIte.costo = GetMoneyString(itemCost);
                                        ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemCost));

                                        ordersItems.ListRecetasItems.Add(ordersIte);
                                    }

                                }

                            }
                        }
                    }
                    else
                    {
                        foreach (var recipe in product.RecipeItems)
                        {
                            var itemProducto = _dbContext.ProductItems.FirstOrDefault(s => s.ID == recipe.ID);

                            var subRecipeItem = new ProductRecipeItemViewModel(itemProducto);

                            if (itemProducto.Type == ItemType.Article)
                            {

                                var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                subRecipeItem.Article = article;

                                decimal itemCost = (from m in article.Items where m.Number == itemProducto.UnitNum select m.Cost).FirstOrDefault();
                                decimal itemPrice = (from m in article.Items where m.Number == itemProducto.UnitNum select m.Price).FirstOrDefault();

                                var unit = article.Items.FirstOrDefault(s => s.Number == itemProducto.UnitNum);

                                ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                ordersIte.articulo = article.Name;
                                ordersIte.marca = article.Brand.Name;
                                ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                ordersIte.unidad = unit.Name;
                                ordersIte.costo = GetMoneyString(itemCost);
                                ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemCost));

                                ordersItems.ListRecetasItems.Add(ordersIte);


                            }
                            else if (itemProducto.Type == ItemType.Product)
                            {
                                //var productRecipe = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                var productRecipe = _dbContext.Products.Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                subRecipeItem.Product = productRecipe;

                                if (productRecipe.HasServingSize)
                                {
                                    decimal itemCost = (from m in productRecipe.ServingSizes where m.ServingSizeID == itemProducto.UnitNum select m.Cost).FirstOrDefault();
                                    ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                    ordersIte.articulo = productRecipe.Name;
                                    ordersIte.marca = productRecipe.Barcode;
                                    ordersIte.qty = GetMoneyString((itemProducto.Qty * itemdos.Qty));
                                    ordersIte.unidad = "N/A";
                                    ordersIte.costo = GetMoneyString(itemCost);
                                    ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemCost));
                                    ordersItems.ListRecetasItems.Add(ordersIte);
                                }
                                else
                                {

                                    ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                    ordersIte.articulo = product.Name;
                                    ordersIte.marca = product.Barcode;
                                    ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                    ordersIte.unidad ="N/A";
                                    ordersIte.costo = GetMoneyString(product.ProductCost);
                                    ordersIte.total = GetMoneyString(decimal.Parse(ordersIte.qty) * itemProducto.Qty);

                                    ordersItems.ListRecetasItems.Add(ordersIte);
                                }

                            }
                            else
                            {
                                var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                subRecipeItem.SubRecipe = subRecipe;

                                decimal itemCost = (from m in subRecipe.ItemUnits where m.Number == itemProducto.UnitNum select m.Cost).FirstOrDefault();
                                var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == itemProducto.UnitNum);
                                ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                ordersIte.articulo = subRecipe.Name;
                                ordersIte.marca = subRecipe.Name;
                                ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                ordersIte.unidad = unit.Name;
                                ordersIte.costo = GetMoneyString(itemCost);
                                ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemCost));

                                ordersItems.ListRecetasItems.Add(ordersIte);
                            }
                        }
                    }

                    addItems.ListOrderItems.Add(ordersItems);

                    // Procesamos los answer

                    var ListQuestionItems = _dbContext.QuestionItems.Include(s => s.Answer).Where(a => a.OrderItemID == itemdos.ID).ToList();

                    
                    foreach (var itemQuestionItem in ListQuestionItems)
                    {
                        var itemtres = itemQuestionItem.Answer;

                        ordersItems = new OrdersItemsModel();

                        ordersItems = CalculaTotalAnswer(ordersItems, itemtres, itemdos);

                        ordersItems.Producto = itemtres.Product.Name;
                        
                        ordersItems.QTY = itemdos.Qty.ToString();
                      
                        ordersItems.Ganancia = GetMoneyString(decimal.Parse(ordersItems.SubTotal) - decimal.Parse(ordersItems.Costo));
                        if (ordersItems.Ganancia.Contains("-")) {
                            ordersItems.Ganancia = "0";
                        }

                        ordersItems.CostoPor = ((decimal.Parse(ordersItems.SubTotal) != 0.0m) ? GetMoneyString(((decimal.Parse(ordersItems.Costo) / decimal.Parse(ordersItems.SubTotal)) * 100)) : "0");

                        // --- Iniciamos: Canculamos precios

                        // --- Terminamos:  Canculamos precios


                        ordersItems.ListRecetasItems = new List<ProductoArticuloModelo>();


                        //product = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions.OrderBy(s => s.DisplayOrder)).FirstOrDefault(s => s.ID == itemtres.Product.ID);
                        product = _dbContext.Products.Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions.OrderBy(s => s.DisplayOrder)).FirstOrDefault(s => s.ID == itemtres.Product.ID);
                        if (product.HasServingSize)
                        {

                            //product = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions).FirstOrDefault(s => s.ID == itemtres.Product.ID);



                            foreach (var ss in product.ServingSizes)
                            {
                                var ret = new ProductServingSizeViewModel(ss);

                                var items = product.RecipeItems.Where(s => s.ServingSizeID == ss.ServingSizeID);



                                foreach (var itemProducto in items)
                                {

                                    if (ret.ServingSizeID == itemtres.ServingSizeID)
                                    {
                                        var subRecipeItem = new ProductRecipeItemViewModel(itemProducto);
                                        if (itemProducto.Type == ItemType.Article)
                                        {
                                            var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                            var unit = article.Items.FirstOrDefault(s => s.Number == itemProducto.UnitNum);
                                            subRecipeItem.Article = article;

                                            decimal itemCost = unit.Cost; // (from m in article.Items where m.Number == itemProducto.UnitNum select m.Cost).FirstOrDefault();

                                            decimal itemPrice = unit.Price; // (from m in article.Items where m.Number == itemProducto.UnitNum select m.Price).FirstOrDefault();
                                            ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                            ordersIte.articulo = article.Name;
                                            ordersIte.marca = article.Brand.Name;
                                            ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                            ordersIte.unidad = unit.Name;
                                            ordersIte.costo = GetMoneyString(itemCost);
                                            ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemCost));

                                            ordersItems.ListRecetasItems.Add(ordersIte);
                                        }
                                        else if (itemProducto.Type == ItemType.Product)
                                        {
                                            //var product1 = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                            var product1 = _dbContext.Products.Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                            subRecipeItem.Product = product1;

                                            if (product1.HasServingSize)
                                            {
                                                decimal itemCost = (from m in product1.ServingSizes where m.ServingSizeID == itemProducto.UnitNum select m.Cost).FirstOrDefault();
                                                
                                                ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                                ordersIte.articulo = product1.Name;
                                                ordersIte.marca = product1.Barcode;
                                                ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                                ordersIte.unidad = "N/A";
                                                ordersIte.costo = GetMoneyString(itemCost);
                                                ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemCost));
                                                ordersItems.ListRecetasItems.Add(ordersIte);
                                            }
                                            else
                                            {
                                                ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                                ordersIte.articulo = product.Name;
                                                ordersIte.marca = product.Barcode;
                                                ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                                ordersIte.unidad = "N/A";
                                                ordersIte.costo = GetMoneyString(product.ProductCost);
                                                ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemProducto.Qty));

                                                ordersItems.ListRecetasItems.Add(ordersIte);

                                            }


                                        }
                                        else
                                        {
                                            var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                            var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == itemProducto.UnitNum);
                                            subRecipeItem.SubRecipe = subRecipe;

                                            decimal itemCost = unit.Cost; // (from m in subRecipe.ItemUnits where m.Number == itemProducto.UnitNum select m.Cost).FirstOrDefault();
                                            ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                            ordersIte.articulo = subRecipe.Name;
                                            ordersIte.marca = subRecipe.Name;
                                            ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                            ordersIte.unidad = unit.Name;
                                            ordersIte.costo = GetMoneyString(itemCost);
                                            ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemCost));

                                            ordersItems.ListRecetasItems.Add(ordersIte);
                                        }

                                    }

                                }
                            }
                        }
                        else
                        {
                            foreach (var recipe in product.RecipeItems)
                            {
                                var itemProducto = _dbContext.ProductItems.FirstOrDefault(s => s.ID == recipe.ID);

                                var subRecipeItem = new ProductRecipeItemViewModel(itemProducto);

                                if (itemProducto.Type == ItemType.Article)
                                {

                                    var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                    subRecipeItem.Article = article;
                                    var articleItemFirst = article.Items.FirstOrDefault(s => s.Number == itemProducto.UnitNum);

                                    decimal itemCost = articleItemFirst.Cost; //(from m in article.Items where m.Number == itemProducto.UnitNum select m.Cost).FirstOrDefault();
                                    decimal itemPrice = articleItemFirst.Price; //(from m in article.Items where m.Number == itemProducto.UnitNum select m.Price).FirstOrDefault();

                                    var unit = article.Items.FirstOrDefault(s => s.Number == itemProducto.UnitNum);

                                    ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                    ordersIte.articulo = article.Name;
                                    ordersIte.marca = article.Brand.Name;
                                    ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                    ordersIte.unidad = unit.Name;
                                    ordersIte.costo = GetMoneyString(itemCost);
                                    ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemCost));

                                    ordersItems.ListRecetasItems.Add(ordersIte);


                                }
                                else if (itemProducto.Type == ItemType.Product)
                                {
                                    //var productRecipe = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                    var productRecipe = _dbContext.Products.Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                    subRecipeItem.Product = productRecipe;

                                    if (productRecipe.HasServingSize)
                                    {
                                        decimal itemCost = (from m in productRecipe.ServingSizes where m.ServingSizeID == itemProducto.UnitNum select m.Cost).FirstOrDefault();
                                        ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                        ordersIte.articulo = productRecipe.Name;
                                        ordersIte.marca = productRecipe.Barcode;
                                        ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                        ordersIte.unidad = "N/A";
                                        ordersIte.costo = GetMoneyString(itemCost);
                                        ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemCost));
                                        ordersItems.ListRecetasItems.Add(ordersIte);
                                    }
                                    else
                                    {

                                        ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                        ordersIte.articulo = product.Name;
                                        ordersIte.marca = product.Barcode;
                                        ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                        ordersIte.unidad = "N/A";
                                        ordersIte.costo = GetMoneyString(product.ProductCost);
                                        ordersIte.total = (decimal.Parse(ordersIte.qty) * itemProducto.Qty).ToString();

                                        ordersItems.ListRecetasItems.Add(ordersIte);
                                    }

                                }
                                else
                                {
                                    var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == itemProducto.ItemID);
                                    subRecipeItem.SubRecipe = subRecipe;

                                    decimal itemCost = (from m in subRecipe.ItemUnits where m.Number == itemProducto.UnitNum select m.Cost).FirstOrDefault();
                                    var unit = subRecipe.ItemUnits.FirstOrDefault(s => s.Number == itemProducto.UnitNum);
                                    ProductoArticuloModelo ordersIte = new ProductoArticuloModelo();

                                    ordersIte.articulo = subRecipe.Name;
                                    ordersIte.marca = subRecipe.Name;
                                    ordersIte.qty = (itemProducto.Qty * itemdos.Qty).ToString();
                                    ordersIte.unidad = unit.Name;
                                    ordersIte.costo = GetMoneyString(itemCost);
                                    ordersIte.total = GetMoneyString((decimal.Parse(ordersIte.qty) * itemCost));

                                    ordersItems.ListRecetasItems.Add(ordersIte);
                                }
                            }
                        }

                        addItems.ListOrderItems.Add(ordersItems);

                    }

                }

                decimal? costo = 1.0m;
                if (addItems.Costo != null)
                {

                    costo = decimal.Parse(addItems.Costo);
                }

                decimal? costoParaSubtotal = 1.0m;
                if (item.SubTotal != null)
                {

                    costoParaSubtotal = decimal.Parse(addItems.SubTotal);
                }
                //decimal? costoParaCalculo = costo != 0 ? costo : 1.0m;
                costoParaSubtotal = costoParaSubtotal != 0 ? costoParaSubtotal : 1.0m;


                if (costo != 0.0m)
                {
                    addItems.CostoPor = GetMoneyString(((costo / costoParaSubtotal) * 100).Value);
                }
                else
                {
                    addItems.CostoPor ="0.0";
                }

                if (addItems.Costo != null)
                {
                    
                    addItems.Ganancia = GetMoneyString((decimal.Parse(addItems.SubTotal) - decimal.Parse(addItems.Descuento) - decimal.Parse(addItems.Costo)));
                    if (addItems.Ganancia.Contains("-")) {
                        addItems.Ganancia = "0";
                    }

                }
                else
                {

                    addItems.Ganancia = GetMoneyString((decimal.Parse(addItems.SubTotal) - decimal.Parse(addItems.Descuento) - 0.0m));
                    if (addItems.Ganancia.Contains("-"))
                    {
                        addItems.Ganancia = "0";
                    }
                }


                  subtotal_ = (decimal.Parse(subtotal_) + decimal.Parse(addItems.SubTotal != null ? addItems.SubTotal : "0")).ToString();
                  descuento_ = (decimal.Parse(descuento_) + decimal.Parse(addItems.Descuento != null ? addItems.Descuento : "0")).ToString();
                  itbis_ = (decimal.Parse(itbis_)+ decimal.Parse(addItems.Itbis != null ? addItems.Itbis : "0")).ToString();
                  propuba_ = (decimal.Parse(propuba_) + decimal.Parse(addItems.Propina!= null?addItems.Propina:"0")).ToString();
                propinavoluntaria_ = (decimal.Parse(propinavoluntaria_) + decimal.Parse(!string.IsNullOrEmpty(addItems.PropinaVoluntaria) ? addItems.PropinaVoluntaria : "0")).ToString();
                delivery_ = (decimal.Parse(delivery_) + decimal.Parse(addItems.Delivery != null ? addItems.Delivery : "0")).ToString();
                  total_ = (decimal.Parse(total_) + decimal.Parse(addItems.Total != null ? addItems.Total : "0")).ToString();
                  costo_ = (decimal.Parse(costo_ )+ decimal.Parse(addItems.Costo != null ? addItems.Costo : "0")).ToString(); 
                  ganancia_ = (decimal.Parse(ganancia_) + decimal.Parse(addItems.Ganancia != null ? addItems.Ganancia : "0")).ToString();

                Modelo.Add(addItems);
            }




                subtotal_ = GetMoneyString(decimal.Parse(subtotal_));
            descuento_ = GetMoneyString(decimal.Parse(descuento_));
            itbis_ = GetMoneyString(decimal.Parse(itbis_));
            propuba_ = GetMoneyString(decimal.Parse(propuba_));
            propinavoluntaria_ = GetMoneyString(decimal.Parse(propinavoluntaria_));
            delivery_ = GetMoneyString(decimal.Parse(delivery_));
            total_ = GetMoneyString(decimal.Parse(total_));
                costo_ = GetMoneyString(decimal.Parse(costo_));
            ganancia_ = GetMoneyString(decimal.Parse(ganancia_));
            return Json(new
            {
                ListaModelo = Modelo.OrderByDescending(a=>a.OrderNum).ToList(),
                resultado = true,
                subtotal_ = subtotal_,
                descuento_ = descuento_ ,
                itbis_ = itbis_ ,
                propuba_ = propuba_,
                propinavoluntaria_ = propinavoluntaria_,
                delivery_ = delivery_,
                total_ = total_,
                costo_ = costo_,
                ganancia_ = ganancia_
            });
            //}
            /*else {
                return Json(new {  resultado = false });
            }*/


        }

        private OrdersItemsModel CalculaTotalAnswer(OrdersItemsModel orderItemModel,Answer answer,OrderItem orderItem) {

            var index = 0;
            var question = _dbContext.Questions.Include(s => s.Answers).ThenInclude(s => s.Product).ThenInclude(s => s.ServingSizes).FirstOrDefault(s => s.ID == answer.QuestionID);
            var a = answer;
            decimal answerTotalVenta = 0;
            decimal answerTotalCosto = 0;


            var servingsizeName = "";
            int servingsizeId = 0;
            var price = 0.0m;
            var canRoll = false;
            if (answer.RollPrice > 0)
            {
                price = answer.RollPrice;
                canRoll = true;
            }
            else if (answer.FixedPrice > 0)
            {
                price = answer.FixedPrice;
            }
            else
            {
                try
                {
                    price = answer.Product.Price[(int)answer.PriceType - 1];
                    if (answer.MatchSize && orderItem.ServingSizeID > 0)
                    {
                        if (answer.Product.HasServingSize)
                        {
                            var servingsize = answer.Product.ServingSizes.FirstOrDefault(s => s.ServingSizeID == orderItem.ServingSizeID);
                            if (servingsize != null)
                            {
                                price = servingsize.Price[(int)answer.PriceType - 1];
                                servingsizeName = servingsize.ServingSizeName;
                                servingsizeId = orderItem.ServingSizeID;
                            }
                        }
                    }
                    else
                    {
                        if (answer.Product.HasServingSize && answer.ServingSizeID > 0)
                        {
                            var servingsize = answer.Product.ServingSizes.FirstOrDefault(s => s.ServingSizeID == answer.ServingSizeID);
                            if (servingsize != null)
                            {
                                price = servingsize.Price[(int)answer.PriceType - 1];
                                servingsizeName = servingsize.ServingSizeName;
                                servingsizeId = answer.ServingSizeID;
                            }
                        }
                    }

                }
                catch { }
            }

            if (orderItem.Questions == null)
                orderItem.Questions = new List<QuestionItem>();

            if (a != null)
            {


                var questionItem = new QuestionItem()
                {
                    Answer = answer,
                    Description = answer.Product.Name,
                    Price = price,
                    CanRoll = canRoll,
                    Qty = orderItem.Qty,
                    Divisor = DivisorType.None, //(DivisorType)a.HasDivisor,
                    ServingSizeID = servingsizeId,
                    ServingSizeName = servingsizeName,
                    IsPreSelect = answer.IsPreSelected
                };
                if (answer.FixedPrice == 0 && index < question.FreeChoice)
                {
                    for (int j = 0; j < orderItem.Qty; j++)
                    {
                        if (index < question.FreeChoice)
                        {
                            questionItem.FreeChoice++;
                            index++;
                        }
                    }
                }
                if (answer.HasQty)
                {
                    questionItem.Description = questionItem.Description;
                    if (orderItem.Qty > 1)
                        questionItem.Description = questionItem.Description + " x " + questionItem.Qty;
                }


                /*if (!string.IsNullOrEmpty(a.SubAnswers))
                {
                    var subanswers = a.SubAnswers.Split(",").ToList();
                    var subdescription = "";
                    var subprice = 0;
                    var subquestion = _dbContext.Questions.Include(s => s.Answers).ThenInclude(s => s.Product).Include(s => s.Products).FirstOrDefault(s => s.ID == answer.ForcedQuestionID);
                    foreach (var sa in subquestion.Answers)
                    {
                        if (subanswers.Contains("" + sa.ID))
                        {
                            subdescription += sa.Product.Name + "<br />";

                        }
                    }
                    questionItem.SubDescription = subdescription;
                }
                orderItem.Questions.Add(questionItem);*/

                if (questionItem.FreeChoice <= 0)
                {
                    answerTotalVenta = answerTotalVenta + price;
                }


                if (answer.Product.HasServingSize)
                {
                    answerTotalCosto = answerTotalCosto + GetCostOrderItem(answer.Product.ID, orderItem.Qty, orderItem.ServingSizeID);
                }
                else
                {
                    answerTotalCosto = answerTotalCosto + GetCostOrderItem(answer.Product.ID, orderItem.Qty);
                }
            }
            else if (a == null && answer.IsPreSelected)
            {
                var questionItem = new QuestionItem()
                {
                    Answer = answer,
                    Description = "No " + answer.Product.Name,
                    IsPreSelect = answer.IsPreSelected,
                    IsActive = false
                };
                orderItem.Questions.Add(questionItem);

                answerTotalVenta = answerTotalVenta + price;

                if (answer.Product.HasServingSize)
                {
                    answerTotalCosto = answerTotalCosto + GetCostOrderItem(answer.Product.ID, 1, orderItem.ServingSizeID);
                }
                else
                {
                    answerTotalCosto = answerTotalCosto + GetCostOrderItem(answer.Product.ID, 1);
                }
            }

            orderItemModel.Precio = GetMoneyString(price);
            orderItemModel.SubTotal = GetMoneyString(answerTotalVenta);
            orderItemModel.Total = GetMoneyString(answerTotalVenta);
            orderItemModel.Costo = GetMoneyString(answerTotalCosto);

            return orderItemModel;
        }

        private decimal GetCostOrderItem(long productID, decimal cantidad, int size = 0)
        {
            decimal costoTotal = 0;

            var product = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions.OrderBy(s => s.DisplayOrder)).FirstOrDefault(s => s.ID == productID);

            if (product.HasServingSize)
            {

                product = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.ServingSizes).Include(s => s.Questions).FirstOrDefault(s => s.ID == productID);

                foreach (var ss in product.ServingSizes)
                {
                    var ret = new ProductServingSizeViewModel(ss);

                    var items = product.RecipeItems.Where(s => s.ServingSizeID == ss.ServingSizeID);

                    foreach (var item in items)
                    {

                        if (ret.ServingSizeID == size)
                        {
                            var subRecipeItem = new ProductRecipeItemViewModel(item);
                            if (item.Type == ItemType.Article)
                            {
                                var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == item.ItemID);
                                subRecipeItem.Article = article;

                                decimal itemCost = (from m in article.Items where m.Number == item.UnitNum select m.Cost).FirstOrDefault();
                                costoTotal = costoTotal + (itemCost * item.Qty);
                            }
                            else if (item.Type == ItemType.Product)
                            {
                                var product1 = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == item.ItemID);
                                subRecipeItem.Product = product1;

                                if (product1.HasServingSize)
                                {
                                    decimal itemCost = (from m in product1.ServingSizes where m.ServingSizeID == item.UnitNum select m.Cost).FirstOrDefault();
                                    costoTotal = costoTotal + (itemCost * item.Qty);
                                }
                                else
                                {
                                    costoTotal = costoTotal + (product1.ProductCost * item.Qty);
                                }


                            }
                            else
                            {
                                var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == item.ItemID);
                                subRecipeItem.SubRecipe = subRecipe;

                                decimal itemCost = (from m in subRecipe.ItemUnits where m.Number == item.UnitNum select m.Cost).FirstOrDefault();
                                costoTotal = costoTotal + (itemCost * item.Qty);
                            }

                        }

                    }
                }
            }
            else
            {
                foreach (var recipe in product.RecipeItems)
                {
                    var item = _dbContext.ProductItems.FirstOrDefault(s => s.ID == recipe.ID);

                    var subRecipeItem = new ProductRecipeItemViewModel(item);

                    if (item.Type == ItemType.Article)
                    {
                        var article = _dbContext.Articles.Include(s => s.Items.OrderBy(s => s.Number)).Include(s => s.Brand).FirstOrDefault(s => s.ID == item.ItemID);
                        subRecipeItem.Article = article;

                        decimal itemCost = (from m in article.Items where m.Number == item.UnitNum select m.Cost).FirstOrDefault();
                        costoTotal = costoTotal + (itemCost * item.Qty);
                    }
                    else if (item.Type == ItemType.Product)
                    {
                        var productRecipe = _dbContext.Products.Include(s => s.Category).Include(s => s.SubCategory).Include(s => s.Taxes).Include(s => s.Propinas).Include(s => s.PrinterChannels).Include(s => s.RecipeItems).Include(s => s.Questions).Include(s => s.ServingSizes).FirstOrDefault(s => s.ID == item.ItemID);
                        subRecipeItem.Product = productRecipe;

                        if (productRecipe.HasServingSize)
                        {
                            decimal itemCost = (from m in productRecipe.ServingSizes where m.ServingSizeID == item.UnitNum select m.Cost).FirstOrDefault();
                            costoTotal = costoTotal + (itemCost * item.Qty);
                        }
                        else
                        {
                            costoTotal = costoTotal + (product.ProductCost * item.Qty);
                        }

                    }
                    else
                    {
                        var subRecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits.OrderBy(s => s.Number)).FirstOrDefault(s => s.ID == item.ItemID);
                        subRecipeItem.SubRecipe = subRecipe;

                        decimal itemCost = (from m in subRecipe.ItemUnits where m.Number == item.UnitNum select m.Cost).FirstOrDefault();
                        costoTotal = costoTotal + (itemCost * item.Qty);
                    }
                }
            }

            return costoTotal * cantidad;
        }
		[Authorize(Policy = "Permission.REPORT.WorkDayReport")]
		[HttpPost]
        public JsonResult GetWorkDayReport(string from, string to, int user, int sucursal, int cierre)
        {
            /*var fromDate = DateTime.Now;
            var toDate = DateTime.Now.AddDays(1).AddMinutes(-1);
            if (!string.IsNullOrEmpty(date))
            {
                try
                {
                    fromDate = DateTime.ParseExact(date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
					toDate = fromDate.AddDays(1).AddMinutes(-1);
                }
                catch { }
            }*/

            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,0,0,0);
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            string UserFullName = "";
            string UserFullNameCierre = "";

            if (user != 0)
            {
                UserFullName = _dbContext.User.Where(s => s.ID == user).First().FullName;
            }

            if (cierre != 0)
            {
                UserFullNameCierre = _dbContext.User.Where(s => s.ID == cierre).First().FullName;
            }

            var transactions = _dbContext.OrderTransactions.Include(s => s.Order).Include(s => s.Order.Station).Include(s => s.Order.Items).Include(s => s.Order.PrepareType).Where(s => s.Type != TransactionType.CloseDrawer && s.Order.OrderType != OrderType.FastExpress && (s.ReferenceId==null || s.ReferenceId == 0) && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date && (s.Order.Station.IDSucursal == sucursal || sucursal == 0) && (s.CreatedBy == UserFullName || UserFullName == "" || (s.Order.CreatedBy == UserFullName || UserFullName == "")) && (s.UpdatedBy==UserFullNameCierre || UserFullNameCierre=="")).ToList();

            //if (transactions.Any()) {
            //Sacamos las transacciones

            int NumeroTrasacciones = 0;
            decimal AverageTransaccion = 0;
            decimal AveragePersonas = 0;

            var lstOrders = new List<Order>();

            foreach (var trans in transactions)
            {
                NumeroTrasacciones++;
                AverageTransaccion = AverageTransaccion + trans.Amount;


                if (!lstOrders.Contains(trans.Order) && (trans.CreatedBy == UserFullName || UserFullName == "" || trans.Order.CreatedBy == UserFullName || UserFullName == ""))
                {
                    lstOrders.Add(trans.Order);

                }
            }

            int NumeroPersonas = lstOrders.Sum(p => p.Person);
            AveragePersonas = AverageTransaccion > 0 && NumeroPersonas > 0 ? AverageTransaccion / NumeroPersonas : 0;
            AverageTransaccion = AverageTransaccion > 0 && NumeroTrasacciones > 0 ? AverageTransaccion / NumeroTrasacciones : 0;

            var lstTransacciones = new List<WDItemModel>();
            lstTransacciones.Add(new WDItemModel() { Descripcion = "No. de Transacciones", Valor = NumeroTrasacciones.ToString() });
            lstTransacciones.Add(new WDItemModel() { Descripcion = "No. de Personas", Valor = NumeroPersonas.ToString() });
            lstTransacciones.Add(new WDItemModel() { Descripcion = "Average por Transacción", Valor = "" + string.Format("{0:N2}", AverageTransaccion) });
            lstTransacciones.Add(new WDItemModel() { Descripcion = "Average por Persona", Valor = "" + string.Format("{0:N2}", AveragePersonas) });

            //Sacamos los descuentos

            decimal DescuentosOrdenes = 0;
            decimal DescuentosOrdenesItems = 0;
            foreach (var trans in transactions)
            {
                DescuentosOrdenes = DescuentosOrdenes + trans.Order.Discount;
                foreach (var objItem in trans.Order.Items)
                {
                    DescuentosOrdenesItems = DescuentosOrdenesItems + objItem.Discount;

                }

            }

            var lstDescuentos = new List<WDItemModel>();
            lstDescuentos.Add(new WDItemModel() { Descripcion = "Descuentos Ordenes", Valor = DescuentosOrdenes.ToString() });
            lstDescuentos.Add(new WDItemModel() { Descripcion = "Descuentos Items", Valor = DescuentosOrdenesItems.ToString() });
            lstDescuentos.Add(new WDItemModel() { Descripcion = "Total Descuentos", Valor = "" + string.Format("{0:N2}", DescuentosOrdenes + DescuentosOrdenesItems) });

            //Sacamos las ventas al contado y otras ventas

            var lstMethodContado = _dbContext.PaymentMethods.Where(s => s.PaymentType == "Effectivo").Select(s => s.Name).ToList();
            var lstMethodTarjeta = _dbContext.PaymentMethods.Where(s => s.PaymentType == "Tarjeta").Select(s => s.Name).ToList();
            var lstMethodOtros = _dbContext.PaymentMethods.Where(s => s.PaymentType != "Tarjeta" && s.PaymentType != "Effectivo").Select(s => s.Name).ToList();

            int CantidadAlContado = 0;
            int CantidadTarjeta = 0;
            int CantidadOtros = 0;
            decimal TotalContado = 0;
            decimal TotalCuentaXCobrar = 0;
            decimal TotalCuentaCasa = 0;

            List<KeyValuePair<string, decimal>> lstMethodOtrosValores = new List<KeyValuePair<string, decimal>>();

            var lstTransaccionesAlContado = new List<OrderTransaction>();
            var lstTransaccionesOtras = new List<OrderTransaction>();

            foreach (var trans in transactions)
            {
                if (lstMethodOtros.Contains(trans.Method))
                {

                    var objMethodExiste = (from t in lstMethodOtrosValores where t.Key == trans.Method select t).FirstOrDefault();
                    int indexSelected = lstMethodOtrosValores.IndexOf(objMethodExiste);

                    if (string.IsNullOrEmpty(objMethodExiste.Key))
                    {
                        lstMethodOtrosValores.Add(new KeyValuePair<string, decimal>(trans.Method, trans.Amount));
                        lstTransaccionesOtras.Add(trans);
                    }
                    else
                    {
                        lstMethodOtrosValores[indexSelected] = new KeyValuePair<string, decimal>(trans.Method, objMethodExiste.Value + trans.Amount);
                    }
                }



                if (lstMethodContado.Contains(trans.Method))
                {
                    CantidadAlContado++;
                    TotalContado = TotalContado + trans.Amount;
                    lstTransaccionesAlContado.Add(trans);
                }

            }

            var lstVentasContadoDetalle = GenerateListDetailReportModel(lstTransaccionesAlContado);
            var lstVentasOtrasDetalle = GenerateListDetailReportModel(lstTransaccionesOtras);

            var lstVentasContado = new List<WDItemModel>();
            lstVentasContado.Add(new WDItemModel() { Descripcion = "Qty", Valor = CantidadAlContado.ToString() });
            lstVentasContado.Add(new WDItemModel() { Descripcion = "Total", Valor = "" + string.Format("{0:N2}", TotalContado) });

            var lstOtrasVentas = new List<WDItemModel>();            
            foreach (var objTemp in lstMethodOtrosValores)
            {
                CantidadOtros++;
                lstOtrasVentas.Add(new WDItemModel() { Descripcion = objTemp.Key, Valor = "" + string.Format("{0:N2}", objTemp.Value) });
            }
            lstOtrasVentas.Add(new WDItemModel() { Descripcion = "Qty", Valor = CantidadOtros.ToString() });
            lstOtrasVentas.Add(new WDItemModel() { Descripcion = "Total", Valor = "" + string.Format("{0:N2}", lstMethodOtrosValores.Sum(s => s.Value)) });

            //Sacamos las transacciones de tarjetas de credito

            List<KeyValuePair<string, decimal>> lstTarjetasValores = new List<KeyValuePair<string, decimal>>();

            var lstTransaccionesTarjeta = new List<OrderTransaction>();

            var lstPropinaOrders = new List<Order>();
            foreach (var trans in transactions.Where(s => lstMethodTarjeta.Contains(s.Method)))
            {
                var objTarjetaExiste = (from t in lstTarjetasValores where t.Key == trans.Method select t).FirstOrDefault();
                int indexSelected = lstTarjetasValores.IndexOf(objTarjetaExiste);


                if (string.IsNullOrEmpty(objTarjetaExiste.Key))
                {
                    lstTarjetasValores.Add(new KeyValuePair<string, decimal>(trans.Method, trans.Amount));
                }
                else
                {
                    lstTarjetasValores[indexSelected] = new KeyValuePair<string, decimal>(trans.Method, objTarjetaExiste.Value + trans.Amount);
                }

                if (!lstPropinaOrders.Contains(trans.Order))
                {
                    lstPropinaOrders.Add(trans.Order);
                    lstTransaccionesTarjeta.Add(trans);
                    CantidadTarjeta++;
                }

            }

            var lstVentasTarjetaDetalle = GenerateListDetailReportModel(lstTransaccionesTarjeta);

            var lstTarjetas = new List<WDItemModel>();
            foreach (var objTemp in lstTarjetasValores)
            {
                lstTarjetas.Add(new WDItemModel() { Descripcion = objTemp.Key, Valor = "" + string.Format("{0:N2}", objTemp.Value) });
            }
            lstTarjetas.Add(new WDItemModel() { Descripcion = "Propina Legal", Valor = "" + string.Format("{0:N2}", lstPropinaOrders.Sum(s => s.Propina)) });
            lstTarjetas.Add(new WDItemModel() { Descripcion = "Qty", Valor = CantidadTarjeta.ToString() });
            lstTarjetas.Add(new WDItemModel() { Descripcion = "Total", Valor = "" + string.Format("{0:N2}", lstTarjetasValores.Sum(s => s.Value)) });

            // Sacamos las transaccciones de propina legal

            List<KeyValuePair<string, decimal>> lstPropinaLegalValores = new List<KeyValuePair<string, decimal>>();

            lstPropinaOrders = new List<Order>();

            foreach (var trans in transactions.Where(t => t.Type != TransactionType.Tip))
            {
                if (!lstPropinaOrders.Contains(trans.Order))
                {

                    var objPropinaLegalExiste = (from t in lstPropinaLegalValores where t.Key == trans.PaymentType select t).FirstOrDefault();
                    int indexSelected = lstPropinaLegalValores.IndexOf(objPropinaLegalExiste);                    

                    if (string.IsNullOrEmpty(objPropinaLegalExiste.Key))
                    {
                        lstPropinaLegalValores.Add(new KeyValuePair<string, decimal>(trans.PaymentType, trans.Order.Propina));
                    }
                    else
                    {
                        lstPropinaLegalValores[indexSelected] = new KeyValuePair<string, decimal>(trans.PaymentType, objPropinaLegalExiste.Value + trans.Order.Propina);
                    }

                    lstPropinaOrders.Add(trans.Order);
                }

            }

            var lstPropinasLegal = new List<WDItemModel>();
            decimal totalPropinasLegal = 0;
            foreach (var objTemp in lstPropinaLegalValores)
            {
                totalPropinasLegal = totalPropinasLegal + objTemp.Value;
                lstPropinasLegal.Add(new WDItemModel() { Descripcion = objTemp.Key, Valor = "" + string.Format("{0:N2}", objTemp.Value) });
            }

            lstPropinasLegal.Add(new WDItemModel() { Descripcion = "Total", Valor = "" + string.Format("{0:N2}", totalPropinasLegal) });

            // Sacamos las transaccciones de propina voluntaria

            List<KeyValuePair<string, decimal>> lstPropinaVoluntariaValores = new List<KeyValuePair<string, decimal>>();

            foreach (var trans in transactions.Where(t => t.Type == TransactionType.Tip))
            {
                var objPropinaVoluntariaExiste = (from t in lstPropinaVoluntariaValores where t.Key == trans.Order.CreatedBy select t).FirstOrDefault();
                int indexSelected = lstPropinaVoluntariaValores.IndexOf(objPropinaVoluntariaExiste);


                if (string.IsNullOrEmpty(objPropinaVoluntariaExiste.Key))
                {
                    lstPropinaVoluntariaValores.Add(new KeyValuePair<string, decimal>(trans.Order.CreatedBy, trans.Amount));
                }
                else
                {
                    lstPropinaVoluntariaValores[indexSelected] = new KeyValuePair<string, decimal>(trans.Order.CreatedBy, objPropinaVoluntariaExiste.Value + trans.Amount);
                }
            }

            var lstPropinasVoluntaria = new List<WDItemModel>();
            decimal totalPropinasVoluntaria = 0;
            foreach (var objTemp in lstPropinaVoluntariaValores)
            {
                totalPropinasVoluntaria = totalPropinasVoluntaria + objTemp.Value;
                lstPropinasVoluntaria.Add(new WDItemModel() { Descripcion = objTemp.Key, Valor = "" + string.Format("{0:N2}", objTemp.Value) });
            }

            lstPropinasVoluntaria.Add(new WDItemModel() { Descripcion = "Total", Valor = "" + string.Format("{0:N2}", totalPropinasVoluntaria) });

            //Sacamos las transacciones de cargos domicilio                				
            var lstDomicilio = new List<WDItemModel>();

            lstDomicilio.Add(new WDItemModel() { Descripcion = "Domicilio", Valor = "" + string.Format("{0:N2}", transactions.Sum(s => s.Order.Delivery)) });
            lstDomicilio.Add(new WDItemModel() { Descripcion = "Total", Valor = "" + string.Format("{0:N2}", transactions.Sum(s => s.Order.Delivery)) });

            //Sacamos la venta por categoria
            var items = _dbContext.OrderItems.Include(s => s.Product).ThenInclude(s => s.Category).ThenInclude(s => s.Group).Include(s => s.Taxes).Include(s => s.Order).Where(s => s.Status == OrderItemStatus.Paid && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date &&
                    _dbContext.Orders.Any(o =>
                    o.Station == s.Order.Station &&
                    (o.Station.IDSucursal == sucursal || sucursal == 0 && (o.CreatedBy == UserFullName || UserFullName == "" || lstOrders.Contains(o))) && (s.CreatedBy == UserFullName || UserFullName == "" || lstOrders.Contains(s.Order)) && (s.UpdatedBy == UserFullNameCierre || UserFullNameCierre == ""))).ToList();

            var groups = _dbContext.MenuGroups.ToList();
            var categories = _dbContext.MenuCategories.ToList();


            decimal TotalVoid = 0;
            var sales = new List<SalesReportGroup>();
            var taxes = new List<SalesReportTaxModel>();
            var propinas = new List<SalesReportPropinaModel>();
            var lstProductos = new List<SalesReportProductModel>();

            var lstOrdenes = new List<long>();

            foreach (var item in items)
            {
                var menuProduct = _dbContext.MenuProducts.FirstOrDefault(s => s.ID == item.MenuProductID);

                if (!lstOrdenes.Contains(item.Order.ID))
                {
                    item.SubTotal = item.SubTotal - item.Order.Discount;
                    lstOrdenes.Add(item.Order.ID);
                }

                var groupName = "";
                var categoryName = "";
                if (menuProduct != null)
                {
                    var group = groups.FirstOrDefault(s => s.ID == menuProduct.GroupID);
                    var category = categories.FirstOrDefault(s => s.ID == menuProduct.CategoryID);

                    if (group != null)
                    {
                        groupName = group.Name;
                        categoryName = category.Name;
                    }
                    else
                    {
                        groupName = "N/A";
                        categoryName = "N/A";
                    }
                }
                else
                {
                    groupName = "N/A";
                    categoryName = "N/A";
                }

                var existgroup = sales.FirstOrDefault(s => s.Group == groupName);
                if (existgroup == null)
                {
                    existgroup = new SalesReportGroup()
                    {
                        Group = groupName,
                        Categories = new List<SalesReportCategory>()
                    };
                    sales.Add(existgroup);
                }

                var existcategory = existgroup.Categories.FirstOrDefault(s => s.Category == categoryName);
                if (existcategory == null)
                {
                    existcategory = new SalesReportCategory() { Category = categoryName, Products = new List<SalesReportProductModel>() };
                    existgroup.Categories.Add(existcategory);
                }

                var existproduct = existcategory.Products.FirstOrDefault(s => s.ProductId == item.Product.ID);
                if (existproduct == null)
                {
                    var m = new SalesReportProductModel()
                    {
                        Group = groupName,
                        Category = categoryName,
                        ProductName = item.Product.Name,
                        ProductId = item.Product.ID,
                        GroupName = item.Product.Category.Group.GroupName,
                        Qty = item.Qty,
                        Sales = item.SubTotal,
                        Tax = 0, //item.Taxes!=null ? item.Taxes.Where(m=>!m.IsExempt).Sum(m=>m.Amount) : 0, //item.Tax,
                        Hour = item.CreatedDate.Hour,
                        Cost = item.Product.ProductCost * item.Qty,
                        Profit = item.SubTotal - item.Product.ProductCost * item.Qty
                    };
                    lstProductos.Add(m);
                    existcategory.Products.Add(m);
                    existproduct = m;
                }
                else
                {
                    existproduct.Qty += item.Qty;
                    existproduct.Sales += item.SubTotal;
                    existproduct.Cost += item.Product.ProductCost * item.Qty;
                    existproduct.Profit += item.SubTotal - item.Product.ProductCost * item.Qty;
                }

                foreach (var t in item.Taxes)
                {
                    var existtax = taxes.FirstOrDefault(s => s.TaxName == t.Description);
                    if (existtax == null)
                    {
                        var tax = new SalesReportTaxModel()
                        {
                            TaxName = t.Description,
                            Percent = t.Percent,
                            Taxable = item.SubTotal
                        };
                        if (t.IsExempt)
                        {
                            tax.TaxExempt = t.Amount;
                        }
                        else
                        {
                            tax.Tax = t.Amount;
                            existproduct.Tax += t.Amount;
                        }

                        taxes.Add(tax);
                    }
                    else
                    {
                        existtax.Taxable += item.SubTotal;
                        if (t.IsExempt)
                        {
                            existtax.TaxExempt += t.Amount;
                        }
                        else
                        {
                            existtax.Tax += t.Amount;
                            existproduct.Tax += t.Amount;
                        }
                    }
                }

                if (item.Propinas != null && item.Propinas.Any())
                {
                    foreach (var t in item.Propinas)
                    {
                        var existtax = propinas.FirstOrDefault(s => s.PropinaName == t.Description);
                        if (existtax == null)
                        {
                            var tax = new SalesReportPropinaModel()
                            {
                                PropinaName = t.Description,
                                Percent = t.Percent,
                            };
                            if (t.IsExempt)
                            {
                                tax.PropinaExempt = t.Amount;
                            }
                            else
                            {
                                tax.Propina = t.Amount;
                            }

                            propinas.Add(tax);
                        }
                        else
                        {
                            if (t.IsExempt)
                            {
                                existtax.PropinaExempt += t.Amount;
                            }
                            else
                            {
                                existtax.Propina += t.Amount;
                            }
                        }
                    }
                }


            }

            sales = sales.OrderBy(s => s.Group).ToList();

            foreach (var g in sales)
            {
                foreach (var c in g.Categories)
                {
                    foreach (var p in c.Products)
                    {
                        g.Qty += p.Qty;
                        g.Sales += p.Sales;
                        g.Cost += p.Cost;
                    }
                }

                g.CostPercent = g.Cost / g.Sales * 100.0m;
                g.Profit = g.Sales - g.Cost;
            }

            var lstVentasxCategoria = new List<WDItemModel>();
            decimal totalVentasxCategoria = 0;

            foreach (var sale in sales)
            {
                foreach (var c in sale.Categories)
                {
                    totalVentasxCategoria = totalVentasxCategoria + c.Products.Sum(s => s.Sales);
                    lstVentasxCategoria.Add(new WDItemModel() { Descripcion = c.Category, Valor = "" + string.Format("{0:N2}", c.Products.Sum(s => s.Sales)) });
                }
            }
            lstVentasxCategoria.Add(new WDItemModel() { Descripcion = "Total Categoria", Valor = "" + string.Format("{0:N2}", totalVentasxCategoria) });


            // Sacamos los impuestos

            var lstImpuestos = new List<WDItemModel>();
            var lstVentasxGrupo = new List<WDItemModel>();
            decimal totalImpuestos = 0;
            decimal totalVentasxGrupo = 0;

            foreach (var p in lstProductos.GroupBy(s => s.GroupName))
            {
                totalImpuestos += p.Sum(t => t.Tax);
                totalVentasxGrupo += p.Sum(t => t.Sales);
                lstImpuestos.Add(new WDItemModel() { Descripcion = p.Key, Valor = "" + string.Format("{0:N2}", p.Sum(t => t.Tax)) });
                lstVentasxGrupo.Add(new WDItemModel() { Descripcion = p.Key, Valor = "" + string.Format("{0:N2}", p.Sum(t => t.Sales)) });

            }
            lstImpuestos.Add(new WDItemModel() { Descripcion = "Total", Valor = "" + string.Format("{0:N2}", totalImpuestos) });
            lstVentasxGrupo.Add(new WDItemModel() { Descripcion = "Total", Valor = "" + string.Format("{0:N2}", totalVentasxGrupo) });

            //Sacamos la venta

            var voidProducts = _dbContext.CanceledItems.Include(s => s.Product).Include(s => s.Reason).Include(s => s.Item).ThenInclude(s => s.Order).Where(s => s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date && (s.CreatedBy == UserFullName || UserFullName == "")).OrderByDescending(s => s.UpdatedDate).ToList();

            var lstVoidOrder = new List<VoidOrderReportModel>();

            foreach (var p in voidProducts)
            {
                if (p.Item.Order != null && p.Item.Order.Status == OrderStatus.Void)
                {
                    var exist = lstVoidOrder.FirstOrDefault(s => s.OrderId == p.Item.Order.ID);
                    if (exist == null)
                    {
                        lstVoidOrder.Add(new VoidOrderReportModel()
                        {
                            OrderId = p.Item.Order.ID,
                            Reason = p.Reason.Reason,
                            VoidDate = p.UpdatedDate,
                            OrderAmount = p.Item.Order.TotalPrice,
                            Waitername = p.Item.Order.WaiterName,
                            VoidBy = p.UpdatedBy
                        });
                    }
                }
            }

            /*var lstOrders = new List<Order>();

            foreach (var objTransactionTemp in transactions) {

                if (!lstOrders.Contains(objTransactionTemp.Order)) {
                    lstOrders.Add(objTransactionTemp.Order);
                }					
            }*/

            var lstVentas = new List<WDItemModel>();
            lstVentas.Add(new WDItemModel() { Descripcion = "Ventas Brutas", Valor = "" + string.Format("{0:N2}", sales.Sum(s => s.Sales) + lstOrders.Sum(s => s.Tax) + lstOrders.Sum(s => s.Propina) + lstOrders.Sum(s => s.Delivery)) });
            lstVentas.Add(new WDItemModel() { Descripcion = "Ventas Netas", Valor = "" + string.Format("{0:N2}", sales.Sum(s => s.Sales)) });

            lstVentas.Add(new WDItemModel() { Descripcion = "Anulaciones", Valor = "" + string.Format("{0:N2}", lstVoidOrder.Sum(s => s.OrderAmount)) });




            //Sacamos ventas por tipo de orden

            List<KeyValuePair<string, decimal>> lstVentasXTipoOrdenValores = new List<KeyValuePair<string, decimal>>();
            
            foreach (var trans in transactions)
            {
                if (trans.Type != TransactionType.Tip)
                {
                    KeyValuePair<string, decimal> objOrdenExiste;
                    int indexSelected;
                    if (trans.Order.OrderType!= OrderType.Delivery) {
                        objOrdenExiste = (from t in lstVentasXTipoOrdenValores where t.Key == trans.Order.OrderType.ToString() select t).FirstOrDefault();
                        indexSelected = lstVentasXTipoOrdenValores.IndexOf(objOrdenExiste);
                    }
                    else {
                        if (trans.Order.PrepareType != null) {
                            objOrdenExiste = (from t in lstVentasXTipoOrdenValores where t.Key == trans.Order.PrepareType.Name select t).FirstOrDefault();
                            indexSelected = lstVentasXTipoOrdenValores.IndexOf(objOrdenExiste);
                        }
                        else {
                            objOrdenExiste = (from t in lstVentasXTipoOrdenValores where t.Key == "Para Llevar" select t).FirstOrDefault();
                            indexSelected = lstVentasXTipoOrdenValores.IndexOf(objOrdenExiste);
                        }
                        
                    }
                    


                    if (string.IsNullOrEmpty(objOrdenExiste.Key))
                    {
                        if (trans.Order.OrderType != OrderType.Delivery) {
                            lstVentasXTipoOrdenValores.Add(new KeyValuePair<string, decimal>(trans.Order.OrderType.ToString(), trans.Amount));
                        }
                        else {
                            if (trans.Order.PrepareType != null) {
                                lstVentasXTipoOrdenValores.Add(new KeyValuePair<string, decimal>(trans.Order.PrepareType.Name, trans.Amount));
                            }
                            else {
                                lstVentasXTipoOrdenValores.Add(new KeyValuePair<string, decimal>("Para Llevar", trans.Amount));
                            }
                            
                        }
                        
                    }
                    else
                    {
                        if (trans.Order.OrderType != OrderType.Delivery)
                        {
                            lstVentasXTipoOrdenValores[indexSelected] = new KeyValuePair<string, decimal>(trans.Order.OrderType.ToString(), objOrdenExiste.Value + trans.Amount);
                        }
                        else {
                            if (trans.Order.PrepareType != null) {
                                lstVentasXTipoOrdenValores[indexSelected] = new KeyValuePair<string, decimal>(trans.Order.PrepareType.Name, objOrdenExiste.Value + trans.Amount);
                            }
                            else {
                                lstVentasXTipoOrdenValores[indexSelected] = new KeyValuePair<string, decimal>("Para Llevar", objOrdenExiste.Value + trans.Amount);
                            }
                            
                        }
                         
                    }
                }
            }

            var lstVentasXTipoOrden = new List<WDItemModel>();
            foreach (var objTemp in lstVentasXTipoOrdenValores)
            {
                lstVentasXTipoOrden.Add(new WDItemModel() { Descripcion = objTemp.Key, Valor = "" + string.Format("{0:N2}", objTemp.Value) });
            }
            lstVentasXTipoOrden.Add(new WDItemModel() { Descripcion = "Total", Valor = "" + string.Format("{0:N2}", lstVentasXTipoOrdenValores.Sum(s => s.Value)) });

            //Sacamos ventas por hora

            var lstVentasXHora = new List<WDItemModel>();

            foreach (var p in lstProductos.GroupBy(s => s.Hour))
            {
                lstVentasXHora.Add(new WDItemModel() { Descripcion = p.Key.ToString().PadLeft(2, '0') + "00:" + (p.Key + 1).ToString().PadLeft(2, '0') + "00", Valor = "" + string.Format("{0:N2}", p.Sum(t => t.Sales)) });

            }

            return Json(new
            {
                transacciones = lstTransacciones,
                descuentos = lstDescuentos,
                tarjetas = lstTarjetas,
                propinas_legal = lstPropinasLegal,
                propinas_voluntaria = lstPropinasVoluntaria,
                cargos_domicilio = lstDomicilio,
                ventas_contado = lstVentasContado,
                ventas_otras = lstOtrasVentas,
                ventas_categoria = lstVentasxCategoria,
                ventas = lstVentas,
                impuestos = lstImpuestos,
                ventas_tipo = lstVentasXTipoOrden,
                ventas_hora = lstVentasXHora,
                ventas_grupo = lstVentasxGrupo,
                resultado = true,
                ventas_contado_detalle = lstVentasContadoDetalle,
                ventas_tarjeta_detalle = lstVentasTarjetaDetalle,
                ventas_otras_detalle = lstVentasOtrasDetalle
            });
        }

		[HttpPost]
        public JsonResult GetPropinaReport(string from, string to, int sucursal)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var transactions = _dbContext.OrderTransactions.Include(s => s.Order).Include(s => s.Order.Station).Where(s => s.Type != TransactionType.CloseDrawer && s.UpdatedDate.Date >= fromDate.Date && s.UpdatedDate.Date <= toDate.Date && (s.Order.Station.IDSucursal == sucursal || sucursal == 0)).ToList();

            var result = new List<PropinaReportModel>();
            var lstOrder = new List<Order>();

            foreach (var tran in transactions)
            {
                if (!lstOrder.Contains(tran.Order))
                {

                    var user = tran.Order.CreatedBy;
                    var exist = result.FirstOrDefault(s => s.Username == user);
                    if (exist != null)
                    {
                        exist.Sales += tran.Order.TotalPrice;
                        exist.Propina += tran.Order.Propina;
                    }
                    else
                    {
                        result.Add(new PropinaReportModel()
                        {
                            Username = user,
                            Sales = tran.Order.TotalPrice,
                            Propina = tran.Order.Propina,
                        });
                    }

                    lstOrder.Add(tran.Order);
                }

            }

            var total = new PropinaReportModel();
            total.Username = "TOTAL";
            if (result.Count > 0)
            {
                total.Sales = result.Sum(s => s.Sales);
                total.Propina = result.Sum(s => s.Propina);

            }
            else
            {
                total.Sales = total.Propina = 0;
            }
            result.Add(total);

            return Json(new { trans = result });
        }

        [HttpPost]
        public JsonResult GenerateVariancePDFReport(string from, string to, int almacen)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var result = GetVarianceReport(from, to, almacen);

            // write report
            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "Reporte de varianza_" + "_" + DateTime.Now.Ticks + ".pdf";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;
            var paperSize = iText.Kernel.Geom.PageSize.A4;

            using (var writer = new PdfWriter(uploadsFile))
            {
                var pdf = new PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);
                // REPORT HEADER
                {
                    string IMG = Path.Combine(_hostingEnvironment.WebRootPath, "vendor", "img", "logo-03.jpg");
                    var store = _dbContext.Preferences.First();
                    if (store != null)
                    {
                        var headertable = new iText.Layout.Element.Table(new float[] { 4, 1, 4 });
                        headertable.SetWidth(UnitValue.CreatePercentValue(100));
                        headertable.SetFixedLayout();

                        var info = store.Name + "\n" + store.Address1 + "\n" + "RNC: " + store.RNC + "\nTelefono:" + store.Phone;
                        var time = "Fecha: " + DateTime.Today.ToString("dd/MM/yy") + "\nHora: " + DateTime.Now.ToString("hh:mm tt") + "\nUsuario: " + User.Identity.GetName();

                        var cellh1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(info).SetFontSize(11));

                        Cell cellh2 = new Cell().SetBorder(Border.NO_BORDER).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetTextAlignment(TextAlignment.CENTER);
                        Image img = AlfaHelper.GetLogo(store.Logo);
                        if (img != null)
                        {
                            cellh2.Add(img.ScaleToFit(100, 60));

                        }
                        var cellh3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetFontColor(ColorConstants.DARK_GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph(time).SetFontSize(11));

                        headertable.AddCell(cellh1);
                        headertable.AddCell(cellh2);
                        headertable.AddCell(cellh3);

                        doc.Add(headertable);
                    }
                }
                Paragraph header = new Paragraph("Reporte de varianza")
                               .SetTextAlignment(TextAlignment.CENTER)
                               .SetFontSize(25);
                doc.Add(header);
                Paragraph header1 = new Paragraph(fromDate.ToString("dd-MM-yyyy") + " to " + toDate.ToString("dd-MM-yyyy"))
                          .SetTextAlignment(TextAlignment.CENTER)
                          .SetFontColor(ColorConstants.GRAY)
                          .SetFontSize(12);
                doc.Add(header1);
                var table = new iText.Layout.Element.Table(new float[] { 2, 2, 1, 1, 1, 1, 1 });
                table.SetWidth(UnitValue.CreatePercentValue(100));

                var cell1 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph("Fecha").SetFontSize(12));
                var cell2 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph("Nombre").SetFontSize(12));
                var cell3 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Tipo").SetFontSize(12));
                var cell4 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Antes").SetFontSize(12));
                var cell5 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Diferencia").SetFontSize(12));
                var cell6 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Unidad").SetFontSize(12));
                var cell7 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetBackgroundColor(ColorConstants.GRAY).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("Total").SetFontSize(12));
                table.AddCell(cell1).AddCell(cell2).AddCell(cell3).AddCell(cell4).AddCell(cell5).AddCell(cell6).AddCell(cell7);

                foreach (var d in result)
                {
                    var cell21 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).Add(new Paragraph(d.UpdateDate.ToString("dd-MM-yyyy")).SetFontSize(11));
                    var cell22 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.Name).SetFontSize(11));
                    var cell23 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.Type).SetFontSize(11));
                    var cell24 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.BeforeStock).SetFontSize(11));
                    var cell25 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.Difference).SetFontSize(11));
                    var cell26 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.Unit).SetFontSize(11));
                    var cell27 = new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).Add(new Paragraph("" + d.Total).SetFontSize(11));
                    table.AddCell(cell21).AddCell(cell22).AddCell(cell23).AddCell(cell24).AddCell(cell25).AddCell(cell26).AddCell(cell27);
                }

                doc.Add(table);
                doc.Close();
            }

            return Json(new { status = 0, url = uploadsUrl });
        }

		[Authorize(Policy = "Permission.REPORT.VarianceReport")]
		[HttpPost]
        public JsonResult GenerateVarianceExcelReport(string from, string to, int almacen)
        {
            var result = GetVarianceReport(from, to, almacen);

            string tempFolder = Path.Combine(_hostingEnvironment.WebRootPath, "temp");
            var uniqueFileName = "Reporte de varianza_" + "_" + DateTime.Now.Ticks + ".xlsx";
            var uploadsFile = Path.Combine(tempFolder, uniqueFileName);
            var uploadsUrl = "/temp/" + uniqueFileName;

            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Report");

            var row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("Date");
            row.CreateCell(1).SetCellValue("Name");
            row.CreateCell(2).SetCellValue("Type");
            row.CreateCell(3).SetCellValue("Before Stock");
            row.CreateCell(4).SetCellValue("Difference");
            row.CreateCell(5).SetCellValue("Unit");
            row.CreateCell(6).SetCellValue("Total");

            int index = 1;
            foreach (var d in result)
            {
                var row1 = sheet.CreateRow(index);
                row1.CreateCell(0).SetCellValue(d.UpdateDate.ToString("dd-MM-yyyy"));
                row1.CreateCell(1).SetCellValue(d.Name);
                row1.CreateCell(2).SetCellValue(d.Type);
                row1.CreateCell(3).SetCellValue((double)d.BeforeStock);
                row1.CreateCell(4).SetCellValue((double)d.Difference);
                row1.CreateCell(5).SetCellValue(d.Unit);
                row1.CreateCell(6).SetCellValue((double)d.Total);

                index++;
            }

            FileStream sw = System.IO.File.Create(uploadsFile);
            workbook.Write(sw);
            sw.Close();

            return Json(new { status = 0, url = uploadsUrl });
        }

		[Authorize(Policy = "Permission.REPORT.VarianceReport")]
		private List<VarianceReportModel> GetVarianceReport(string from, string to, int almacen)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var result = new List<VarianceReportModel>();

            var stocks = _dbContext.WarehouseStockChangeHistory.Where(s => (almacen == 0 || s.Warehouse.ID == almacen) && s.ReasonType == StockChangeReason.PhysicalCheck && s.CreatedDate.Date >= fromDate && s.CreatedDate.Date <= toDate).OrderByDescending(s=>s.CreatedDate).ToList();

            foreach(var stock in stocks)
            {
                if (stock.ItemType == ItemType.Article)
                {
                    var article = _dbContext.Articles.Include(s=>s.Items).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                    if (article == null) continue;

                    var unit = article.Items.OrderByDescending(s => s.Number).First();

                    var lastqty = AlfaHelper.ConvertBaseQtyTo(stock.Qty, unit.Number, article.Items.ToList());

                    result.Add(new VarianceReportModel()
                    {
                        Name = article.Name,
                        Cost = unit.Cost,
                        Difference = Math.Round(AlfaHelper.ConvertBaseQtyTo(stock.Qty, unit.Number, article.Items.ToList()), 4),
                        BeforeStock = Math.Round(AlfaHelper.ConvertBaseQtyTo(stock.BeforeBalance, unit.Number, article.Items.ToList()), 2),
                        AfterStock = Math.Round(AlfaHelper.ConvertBaseQtyTo(stock.AfterBalance, unit.Number, article.Items.ToList()), 2),
                        UpdateDate = stock.CreatedDate,
                        Total = Math.Round(AlfaHelper.ConvertBaseQtyTo(stock.Qty, unit.Number, article.Items.ToList()) * unit.Cost, 2),
                        Unit = unit.Name,
                        Type = "Article"
                    });

                }
                else if (stock.ItemType == ItemType.SubRecipe)
                {
                    var subrecipe = _dbContext.SubRecipes.Include(s => s.ItemUnits).FirstOrDefault(s => s.ID == stock.ItemId && s.IsActive);
                    if (subrecipe == null) continue;
					var unit = subrecipe.ItemUnits.OrderByDescending(s => s.Number).First();
					result.Add(new VarianceReportModel()
                    {
                        Name = subrecipe.Name,
                        Cost = unit.Cost,
                        Difference = Math.Round(AlfaHelper.ConvertBaseQtyTo(stock.Qty, unit.Number, subrecipe.ItemUnits.ToList()), 4),
						BeforeStock = Math.Round(AlfaHelper.ConvertBaseQtyTo(stock.BeforeBalance, unit.Number, subrecipe.ItemUnits.ToList()), 2),
                        AfterStock = Math.Round(AlfaHelper.ConvertBaseQtyTo(stock.AfterBalance, unit.Number, subrecipe.ItemUnits.ToList()), 2),
                        UpdateDate = stock.CreatedDate,
                        Total = Math.Round(AlfaHelper.ConvertBaseQtyTo(stock.Qty, unit.Number, subrecipe.ItemUnits.ToList()) * unit.Cost, 2),
                        Unit = unit.Name,
                        Type = "Sub Recipe"
                    });

                }
                
            }

            return result;
        }

		[HttpPost]
        public JsonResult GetServerByCoverReport(string from, string to, int sucursal)
        {
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(to))
            {
                try
                {
                    toDate = DateTime.ParseExact(to, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            var fromDate = DateTime.Now;
            if (!string.IsNullOrEmpty(from))
            {
                try
                {
                    fromDate = DateTime.ParseExact(from, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            var orders = _dbContext.Orders.Include(s => s.Items).Include(s => s.Station).Where(s => s.OrderTime >= fromDate && s.OrderTime <= toDate && s.Status == OrderStatus.Paid && s.OrderType == OrderType.DiningRoom && (s.Station.IDSucursal == sucursal || sucursal == 0)).ToList();

            var result = new List<OrdersByUser>();
            foreach (var o in orders)
            {
                var exist = result.FirstOrDefault(s => s.UserName == o.UpdatedBy);
                if (exist != null)
                {
                    exist.Orders.Add(o);
                }
                else
                {
                    result.Add(new OrdersByUser()
                    {
                        UserName = o.UpdatedBy,
                        Orders = new List<Order>()
                        {
                            o
                        }
                    });
                }
            }

            return Json(result);
        }
		[Authorize(Policy = "Permission.REPORT.ParReport")]
		public JsonResult GetParReport(ParReportRequest request)
        {
            var result = new List<ParReportModel>();

            var stocks = _dbContext.WarehouseStocks.Include(s=>s.Warehouse).Where(s=>s.ItemType == ItemType.Article && s.Warehouse.IsActive).ToList();
            var articles = _dbContext.Articles.Include(s=>s.Items).Include(s=>s.Brand).Where(s => s.IsActive && s.PrimarySupplier > 0 && s.MaximumQuantity > 0 && s.MinimumQuantity > 0).ToList();
            var suppliers = _dbContext.Suppliers.Where(s => s.IsActive).ToList();

			foreach (var stock in stocks)
            {
                var article = articles.FirstOrDefault(s => s.ID == stock.ItemId && (request.SupplierID == 0 || request.SupplierID == s.PrimarySupplier));
                if (article != null)
                {
                    var unit = article.Items.FirstOrDefault(s => s.Number == article.MaximumUnit);
                    var supplier = suppliers.FirstOrDefault(s => s.ID == article.PrimarySupplier);

                    var stockQty = AlfaHelper.ConvertBaseQtyTo(stock.Qty, unit.Number, article.Items.ToList());

                    var exist = result.FirstOrDefault(s => s.ArticleID == article.ID);
                    if (exist != null)
                    {
                        exist.StockQty += stockQty;
                    }
                    else
                    {
						var parreport = new ParReportModel()
						{
							ArticleID = article.ID,
							SupplierID = supplier.ID,
							SupplierName = supplier.Name,
							ArticleName = article.Name,
							Unit = unit.Name,
                            UnitNum = unit.Number,
							UnitCost = unit.Cost,
							StockQty = stockQty,
							Par = (decimal)article.MaximumQuantity,
							Brand = article.Brand.Name,
						};

						result.Add(parreport);
					}

					
                }
            }

            return Json(result);
        }

    }

    public class InventoryLevelModel
    {
        public string Category { get; set; }
        public decimal CategoryTotal { get; set; }
        public List<InventoryStockSub> Items { get; set; }
    }

    public class InventoryDepletionModel
    {
        public string Warehouse { get; set; }
        public string Product { get; set; }
        public List<long> ItemId { get; set; } = new List<long>();
        public long ProductId { get; set; }
        public decimal SalesQty { get; set; }

        public List<InventoryStockSub> Items { get; set; }
    }

    public class InventoryDepletionModelExcel : InventoryDepletionModel
    {
        public string ItemName { get; set; }
        public string Unit1 { get; set; }
        public string Unit2 { get; set; }
        public string Unit3 { get; set; }
    }


    public class InventoryStockSub
    {
        public string ItemName { get; set; }
        public long ItemId { get; set; }
        public string Brand { get; set; }
        public decimal Qty1 { get; set; }
        public decimal Qty2 { get; set; }
        public decimal Qty3 { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Cost { get; set; }
        public string Unit1 { get; set; }
        public string Unit2 { get; set; }
        public string Unit3 { get; set; }
        public string Barcode { get; set; }
    }

    public class PurchaseSupplierModel
    {
        public string Supplier { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }
        public decimal Tax { get; set; }
        public decimal Exempt { get; set; }
        public List<PurchaseItemModel> Items { get; set; }
    }

    public class PurchaseItemModel
    {
        public string Supplier { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string NCF { get; set; }
        public long PurchaseId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Exempt { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class SalesReportGroup
    {
        public string Group { get; set; }
        public decimal Qty { get; set; }
        public decimal Sales { get; set; }
        public decimal Cost { get; set; }
        public decimal CostPercent { get; set; }
        public decimal Profit { get; set; }
        public List<SalesReportCategory> Categories { get; set; }


    }

    public class SalesReportCategory
    {
        public string Category { get; set; }
        public decimal CategoryTotal { get; set; }
        public List<SalesReportProductModel> Products { get; set; }
    }

    public class SalesReportProductModel
    {
        public long ProductId { get; set; }
        public string Group { get; set; }
        public string Category { get; set; }
        public string ProductName { get; set; }
        public long ServingSizeID { get; set; }
        public string ServingSizeName { get; set; }
        public string GroupName { get; set; }
        public decimal Qty { get; set; }
        public decimal Sales { get; set; }
        public decimal Tax { get; set; }
        public int Hour { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
    }

    public class SalesReportTaxModel
    {
        public string TaxName { get; set; }
        public decimal Taxable { get; set; }
        public decimal Percent { get; set; }
        public decimal TaxExempt { get; set; }
        public decimal Tax { get; set; }

    }

    public class SalesReportPropinaModel
    {
        public string PropinaName { get; set; }
        public decimal Propina { get; set; }
        public decimal PropinaExempt { get; set; }
        public decimal Percent { get; set; }
    }

    public class SalesDetailReportModel
    {        
        public string ClientName { get; set; }
        public string ClientRNC { get; set; }
        public DateTime PayDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Amount { get; set; }
        public decimal Tax { get; set; }
        public decimal Propina { get; set; }
        public decimal Delivery { get; set; }
        public decimal Discount { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentType { get; set; }
        public string Comprobante { get; set; }
        public string ComprobanteNumber { get; set; }
        public decimal Tip { get; set; }
        public long Order { get; set; }
        public string UsuarioCreacion { get; set; }
        public string FechaCreacion { get; set; }
        public string UsuarioPago { get; set; }
        public string FechaPago { get; set; }
    }

    public class CloseDrawerReportModel
    {
        public DateTime CloseDate { get; set; }
        public string CloseDateStr { get; set; }
        public string WaiterName { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TransTotal { get; set; }
        public decimal TipTotal { get; set; }
        public decimal TransDifference { get; set; }
        public decimal TipDifference { get; set; }

        public List<CloseDrawerDominationModel> Dominations { get; set; }
        public List<CloseDrawerPaymentModel> Payments { get; set; }
    }

    public class SalesReportByMethodModel
    {
        public string PaymentMethods { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public List<SalesByMethodsDetail> Sales { get; set; }
    }

    public class SalesByMethodsDetail
    {
        public string ClientName { get; set; }
        public decimal Amount { get; set; }
        public DateTime PayDate { get; set; }
    }
    public class VoidOrderReportViewModel
    {
        public string Id { get; set; }
        public string OrderedAt { get; set; }
        public string VoidedAt { get; set; }
        public string Order { get; set; }
        public List<productospopup> listaProductos { get; set; }
		public string Product { get; set; }
		public string Qty { get; set; }
        public string Amount { get; set; }
        public string Fecha { get; set; }
        public string FechaCreado { get; set; }
        public string Razon { get; set; }
        public string Usuario { get; set; }
        public string Void_By { get; set; }
    }

    public class productospopup
    {
		public string Product { get; set; }
		public string Productprecio { get; set; }
	}
    public class VoidOrderReportModel
    {
        public long OrderId { get; set; }
        public decimal OrderAmount { get; set; }
        public string Waitername { get; set; }
        public string VoidBy { get; set; }
        public DateTime VoidDate { get; set; }
        public string Reason { get; set; }
    }

    public class VoidProductReportModel
    {
        public long ProductID { get; set; }
        public string ProductName { get; set; }
        public long OrderID { get; set; }
        public string Reason { get; set; }
        public DateTime VoidDate { get; set; }
    }


    public class WeekOfDayReportModel
    {
        public string DayStr { get; set; }
        public int Day { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalTrans { get; set; }
        public int WeekCount { get; set; }
        public decimal AvgSales { get; set; }
        public decimal AvgAvgSales { get; set; }
        public int AvgTrans { get; set; }
        public List<OrderTransaction> Transactions { get; set; }
        public WeekOfDayReportModel()
        {
            Transactions = new List<OrderTransaction>();
        }
    }

    public class OrdersByUser
    {
        public long UserID { get; set; }
        public string UserName { get; set; }
        public decimal AvgAmount { get; set; }
        public List<Order> Orders { get; set; }

        public OrdersByUser()
        {
            Orders = new List<Order>();
        }
    }

    public class OrderItemsGroup
    {
        public string Name { get; set; }
        public string Amount { get; set; }
    }

    public class PropinaReportModel
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public decimal Sales { get; set; }
        public decimal Propina { get; set; }
    }


    public class WDItemModel
    {
        public string Descripcion { get; set; }
        public string Valor { get; set; }
    }

    public class LOrdersModel
    {
        public string Fecha { get; set; }
        public string ClienteName { get; set; }
        public string Numero { get; set; }
        public string OrderNum { get; set; }
        public string Abierta { get; set; }
        public string AbiertaName { get; set; }
        public string CerradaName { get; set; }
        public string Cerrada { get; set; }
        public string SubTotal { get; set; }
        public string Descuento { get; set; }
        public string Itbis { get; set; }
        public string Propina { get; set; }
        public string PropinaVoluntaria { get; set; }
        public string Delivery { get; set; }
        public string Tipo { get; set; }
        public string Total { get; set; }
        public string Costo { get; set; }
        public string CostoPor { get; set; }
        public string Ganancia { get; set; }
        public List<MetodoPagoModelo> ListmetodoPago { get; set; }
        public List<OrdersItemsModel> ListOrderItems { get; set; }
    }


    public class MetodoPagoModelo
    {
        public string Tipo { get; set; }
        public string Cantidad { get; set; }

    }

    public class OrdersItemsModel
    {

        public string Producto { get; set; }
        public string Precio { get; set; }
        public string QTY { get; set; }
        public string SubTotal { get; set; }
        public string Descuento { get; set; }
        public string Itbis { get; set; }
        public string Propina { get; set; }
        public string Total { get; set; }
        public string Costo { get; set; }
        public string CostoPor { get; set; }
        public string Ganancia { get; set; }
        public List<ProductoArticuloModelo> ListRecetasItems { get; set; }

    }

    public class ProductoItemsModel
    {

        public string Producto { get; set; }
        public string Precio { get; set; }
        public string QTY { get; set; }
        public string SubTotal { get; set; }
        public string Descuento { get; set; }
        public string Itbis { get; set; }
        public string Propuna { get; set; }
        public string Total { get; set; }
        public string Costo { get; set; }
        public string CostoPor { get; set; }
        public string Ganancia { get; set; }

    }

    public class ProductoArticuloModelo
    {
        public string articulo { get; set; }
        public string marca { get; set; }
        public string qty { get; set; }
        public string unidad { get; set; }
        public string costo { get; set; }
        public string total { get; set; }
    }

    public class ProductCostPercentModel
    {
        public long ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public decimal Percent { get; set; }
    }

    public class InventoryGroupModel
    {
        public long GroupID { get; set; }
        public string GroupName { get; set; }
        public decimal Initial { get; set; }
        public decimal Final { get; set; }
        public decimal Purchase { get; set; }
        public decimal Sales { get; set; }
        public decimal Percent { get; set; }
        public List<InventoryGroupSubModel> Items { get; set; } = new List<InventoryGroupSubModel>();
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public class InventoryGroupSubModel
    {
        public DateTime ChangeDate { get; set; }
        public decimal Amount { get; set; }
        public StockChangeReason Reason { get; set; }
    }

    public class InventoryByGroupRequest
    {
        public string from { get; set; }
        public string to { get; set; }
        public long WarehouseID { get; set; }
        public List<long> GroupIDs { get; set; }
    }

    public class VarianceReportModel
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public DateTime UpdateDate { get; set; }
        public decimal BeforeStock { get; set; }
        public decimal AfterStock { get; set; }
        public decimal Difference { get; set; }
        public string Unit { get; set; }
        public decimal Cost { get; set; }
        public decimal Total { get; set; }
    }

    public class ParReportRequest
    {
        public long SupplierID { get; set; }
        public long WarehouseID { get; set; }
    }

    public class ParReportModel
    {
        public long ArticleID { get; set; }
        public long SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string ArticleName { get; set; }
        public string Brand { get; set; }
        public decimal StockQty { get; set; }
        public decimal Par { get; set; }
        public decimal Reorder { get; set; }
        public int UnitNum { get; set; }
        public string Unit { get; set; }
        public decimal UnitCost { get; set; }
    }

    public class CxCReportModel : OrderTransaction
    {
        public string ClientName { get; set; }
    }

}
