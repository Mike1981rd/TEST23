using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.ModelsJWT;
using AuroraPOS.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using OpenQA.Selenium.Chrome;
using AuroraPOS.Controllers;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using AuroraPOS.ViewModels;


namespace AuroraPOS.Core
{
    public class SettingsCore
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;
        public SettingsCore(IUserService userService, AppDbContext dbContext, IHttpContextAccessor context)
        {
            _userService = userService;
            _dbContext = dbContext;
            _context = context;
        }

        public DateTime? GetDia(int stationId)
        {
            var objStation = _dbContext.Stations.Where(d => d.ID == stationId).FirstOrDefault();

            var objDay = _dbContext.WorkDay.Where(d => d.IsActive == true && d.IDSucursal == objStation.IDSucursal).FirstOrDefault();

            if (objDay == null)
            {
                return null;
            }

            DateTime dtNow = new DateTime(objDay.Day.Year, objDay.Day.Month, objDay.Day.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

            return dtNow;
        }

        public List<Voucher>? GetActiveVoucherList() 
        {
            return _dbContext.Vouchers.Where(s => s.IsActive).ToList();
        }

        public List<DeliveryZone>? GetActiveDeliveryZoneList()
        {
            return _dbContext.DeliveryZones.Where(s => s.IsActive && !s.IsDeleted).ToList();
        }

        public ActiveCustomerList GetActiveCustomerList(ActiveCustomerListRequest request)
        {
            var activeCustomerList = new ActiveCustomerList();

            //Paging Size (10,20,50,100)  
            int pageSize = request.Length != null ? Convert.ToInt32(request.Length) : 0;
            int skip = request.Start != null ? Convert.ToInt32(request.Start) : 0;
            int recordsTotal = 0;

            // Getting all Customer data  
            var customerData = (from s in _dbContext.Customers
                                where s.IsActive
                                select s);

            if (request.ClienteId > 0)
            {
                customerData = (from s in _dbContext.Customers
                                where s.IsActive && s.ID == request.ClienteId
                                select s);


            }

            //Sorting
            if (!string.IsNullOrEmpty(request.SortColumn) && !string.IsNullOrEmpty(request.SortColumnDirection))
            {
                try
                {
                    customerData = customerData.OrderBy(request.SortColumn + " " + request.SortColumnDirection);
                }
                catch { }
            }
            ////Search  
            if (!string.IsNullOrEmpty(request.SearchValue))
            {
                customerData = customerData.Where(m => m.Name.ToLower().Contains(request.SearchValue.ToLower()) || m.Phone.ToLower().Contains(request.SearchValue.ToLower()) || m.Address1.ToLower().Contains(request.SearchValue.ToLower()));
            }

            //total number of rows count   
            recordsTotal = customerData.Count();
            //Paging   
            var data = customerData.Skip(skip).ToList();
            if (pageSize != -1)
            {
                data = data.Take(pageSize).ToList();
            }
            //Returning Json Data
            activeCustomerList.recordsFiltered = recordsTotal;
            activeCustomerList.recordsTotal = recordsTotal;
            activeCustomerList.activeCustomers = data;

            return activeCustomerList;
        }

        public PrepareTypesList GetPrepareTypes(PrepareTypesRequest request)
        {
            var prepareTypes = new PrepareTypesList();

            if (!request.JustData)
            {
                //Paging Size (10,20,50,100)  
                int pageSize = request.Length != null ? Convert.ToInt32(request.Length) : 0;
                int skip = request.Start != null ? Convert.ToInt32(request.Start) : 0;
                int recordsTotal = 0;

                // Getting all Customer data  
                var pepareTypesData = (from s in _dbContext.PrepareTypes
                                       where !s.IsDeleted
                                       select s);

                //Sorting
                if (!string.IsNullOrEmpty(request.SortColumn) && !string.IsNullOrEmpty(request.SortColumnDirection))
                {
                    try
                    {
                        pepareTypesData = pepareTypesData.OrderBy(request.SortColumn + " " + request.SortColumnDirection);
                    }
                    catch { }
                }
                ////Search  
                if (!string.IsNullOrEmpty(request.SearchValue))
                {
                    pepareTypesData = pepareTypesData.Where(m => m.Name.Contains(request.SearchValue));
                }

                //total number of rows count   
                recordsTotal = pepareTypesData.Count();
                //Paging   
                var data = pepareTypesData.Skip(skip).ToList();
                if (pageSize != -1)
                {
                    data = data.Take(pageSize).ToList();
                }
                //Returning Json Data  
                prepareTypes.recordsFiltered = recordsTotal;
                prepareTypes.recordsTotal = recordsTotal;
                prepareTypes.prepareTypes = data;
            }
            else
            {
                // Getting all Customer data  
                prepareTypes.prepareTypes = (from s in _dbContext.PrepareTypes
                                       where !s.IsDeleted
                                       select s).ToList();
            }
            return prepareTypes;
        }

        public List<DeliveryCarrier>? GetActiveDeliveryCarrierList()
        {
            return _dbContext.DeliveryCarriers.Where(s => s.IsActive && !s.IsDeleted).ToList();
        }

        public List<Customer>? GetActiveCustomers()
        {
            var customers = _dbContext.Customers.Where(s => s.IsActive).ToList();
            return customers;
        }

        public List<OrderTransaction> GetCxCList(string customerName, long customerId = 0)
        {
            List<OrderTransaction> cxc = null;
            if (customerId == 0)
            {
                string customerNameLower = customerName?.ToLower();

                cxc = _dbContext.OrderTransactions
                    .Include(ot => ot.Order)
                    .Where(ot => ot.Order != null &&
                                 ot.Order.ClientName != null &&
                                 ot.Order.ClientName.ToLower() == customerNameLower &&
                                 ot.Method != null &&
                                 ot.PaymentType != null &&
                                 ot.PaymentType.ToUpper() == "C X C")
                    .Select(ot => new OrderTransaction
                    {
                        ID = ot.ID,
                        Amount = ot.Amount,
                        Method = ot.Method,
                        PaymentDate = ot.PaymentDate,
                        PaymentType = ot.PaymentType,
                    })
                    .ToList();
            }
            else
            {
                string customerNameLower = customerName?.ToLower();

                cxc = _dbContext.OrderTransactions
                    .Include(ot => ot.Order)
                    .Where(ot => ot.Order != null &&
                                 ot.Order.CustomerId == customerId &&
                                 ot.Method != null &&
                                 ot.PaymentType != null &&
                                 ot.PaymentType.ToUpper() == "C X C")
                    .Select(ot => new OrderTransaction
                    {
                        ID = ot.ID,
                        Amount = ot.Amount,
                        Method = ot.Method,
                        PaymentDate = ot.PaymentDate,
                        PaymentType = ot.PaymentType,
                    })
                    .ToList();
            }


            foreach (var order in cxc)
            {
                // Obtener órdenes asociadas al ReferenceId
                var associatedOrders = _dbContext.OrderTransactions
                    .Where(ot => ot.ReferenceId == order.ID)
                    .ToList();

                // Calcular la diferencia y almacenarla en la propiedad Difference
                order.TemporaryDifference = order.Amount - associatedOrders.Sum(ao => ao.Amount);
            }

            return cxc.Where(s => s.TemporaryDifference > 0).ToList();
        }

        public List<OrderTransaction> GetCxCList2(string from, string to, long cliente, long orden, decimal monto)
        {
            // Verificar si los parámetros recibidos están vacíos o nulos
            DateTime fromDate = DateTime.MinValue;
            DateTime toDate = DateTime.MaxValue;

            if (!string.IsNullOrEmpty(from))
            {
                fromDate = DateTime.ParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(to))
            {
                toDate = DateTime.ParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                toDate = toDate.AddDays(1).AddMinutes(-1);
            }

            // Filtrar la lista basada en los parámetros recibidos
            var cxcList = _dbContext.OrderTransactions
                .Include(ot => ot.Order)
                .Where(ot => (orden == 0 || ot.ReferenceId == orden) &&
                             (cliente == 0 || ot.Order.CustomerId == cliente) &&
                             (fromDate == DateTime.MinValue || ot.Order.OrderTime >= fromDate) &&
                             (toDate == DateTime.MaxValue || ot.Order.OrderTime <= toDate) &&
                             (monto == 0 || ot.Amount >= monto))
                .ToList();

            if (cxcList != null && cxcList.Any())
            {
                foreach (var objCxc in cxcList)
                {
                    objCxc.Amount = Math.Round(objCxc.Amount, 2, MidpointRounding.AwayFromZero);
                }
            }

            return cxcList;
        }

        public void DeactivateWorkDay(int stationID)
        {
            //var stationID = int.Parse(GetCookieValue("StationID"));
            var objStation = _dbContext.Stations.Where(d => d.ID == stationID).FirstOrDefault();

            var lstDays = _dbContext.WorkDay.Where(d => d.IsActive == true && d.IDSucursal == objStation.IDSucursal).ToList();

            foreach (var objDay in lstDays)
            {
                objDay.IsActive = false;

            }

            _dbContext.SaveChanges();
        }

        public DatosDGIIResponse ObtenerDatosDGII(string RNC)
        {
            var options = new ChromeOptions();
            options.AddArguments("headless");
            //options.AddArguments("start-maximized");
            DatosDGIIResponse responseDatos = new DatosDGIIResponse();
            responseDatos.isValid = false;

            using (IWebDriver driver = new ChromeDriver(options))
            {
                driver.Navigate().GoToUrl("https://www.dgii.gov.do/app/WebApps/ConsultasWeb/consultas/rnc.aspx");

                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(4));
                wait.Until(driver => driver.FindElement(By.TagName("body")));

                IWebElement inputRNC = driver.FindElement(By.Id("ctl00_cphMain_txtRNCCedula"));
                IWebElement buscarRNCBtn = driver.FindElement(By.Id("ctl00_cphMain_btnBuscarPorRNC"));

                inputRNC.SendKeys(RNC);

                buscarRNCBtn.Click();

                try
                {
                    var div = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("ctl00_cphMain_divBusqueda")));
                    
                    var compania = driver.FindElement(By.XPath("//table[@id='ctl00_cphMain_dvDatosContribuyentes']/tbody/tr[3]/td[2]")).Text;
                    responseDatos.compania = compania;
                    var nombre = driver.FindElement(By.XPath("//table[@id='ctl00_cphMain_dvDatosContribuyentes']/tbody/tr[2]/td[2]")).Text;
                    responseDatos.nombre = nombre;

                    responseDatos.isValid = true;

                    //Response.ContentType = "application/json";
                    return responseDatos;

                    // Procesar el elemento encontrado
                }
                catch (NoSuchElementException)
                {
                    // Manejo del caso cuando el elemento no se encuentra
                    responseDatos.isValid = false;
                    return responseDatos;

                }
            }
        }

        public Tuple<int, long> EditCustomer(CustomerCreateViewModel request)
        {
            var existing = _dbContext.Customers.FirstOrDefault(x => x.ID == request.ID);
            if (existing != null)
            {
                existing.Name = request.Name;
                existing.Phone = request.Phone;
                existing.Email = request.Email;
                existing.RNC = request.RNC;
                existing.Address1 = request.Address1;
                existing.City = request.City;
                existing.CreditLimit = request.CreditLimit;
                existing.CreditDays = request.CreditDays;
                existing.Balance = request.Balance;
                existing.Avatar = request.Avatar;
                existing.IsActive = request.IsActive;
                existing.DeliveryZoneID = request.DeliveryZoneID;
                existing.Company = request.Company;

                if (request.VoucherId > 0)
                {
                    var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == request.VoucherId);
                    existing.Voucher = voucher;
                }


                _dbContext.SaveChanges();
                return new Tuple<int, long>(0, existing.ID);
            }
            else
            {

                existing = new Customer();
                existing.Name = request.Name;
                existing.Phone = request.Phone;
                existing.Email = request.Email;
                existing.RNC = request.RNC;
                existing.Address1 = request.Address1;
                existing.City = request.City;
                existing.CreditLimit = request.CreditLimit;
                existing.CreditDays = request.CreditDays;
                existing.Balance = request.Balance;
                existing.Avatar = request.Avatar;
                existing.IsActive = request.IsActive;
                existing.DeliveryZoneID = request.DeliveryZoneID;
                existing.Company = request.Company;
                if (request.VoucherId > 0)
                {
                    var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == request.VoucherId);
                    existing.Voucher = voucher;
                }

                _dbContext.Customers.Add(existing);
                _dbContext.SaveChanges();
                return new Tuple<int, long>(0, existing.ID);
            }
        }

        public Tuple<int, long> EditCustomerSimple(CustomerSimpleCreateViewModel request)
        {
            //Debug.WriteLine(request.VoucherId);
            var existing = _dbContext.Customers.FirstOrDefault(x => x.ID == request.ID);
            //Debug.WriteLine(existing);
            if (existing != null)
            {
                //Debug.WriteLine("existe");
                existing.Name = request.Name;
                existing.RNC = request.RNC;
                existing.Phone = request.Phone;
                existing.Email = request.Email;
                existing.Address1 = request.Address1;
                existing.Address2 = request.Address2;
                existing.DeliveryZoneID = request.ZoneId;
                existing.Company = request.Company;
                /*if (request.ZoneId > 0)
                {
                    var zone = _dbContext.DeliveryZones.FirstOrDefault(s => s.ID == request.ZoneId);
                    existing.Zone = zone;
                }*/

                if (request.VoucherId > 0)
                {
                    var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == request.VoucherId);
                    //Debug.WriteLine("ola");
                    //Debug.WriteLine(voucher);
                    existing.Voucher = voucher;
                }


                _dbContext.SaveChanges();
                return new Tuple<int, long>(0, existing.ID);
            }
            else
            {
                //Debug.WriteLine("no existe");
                existing = new Customer();
                existing.Name = request.Name;
                existing.RNC = request.RNC;
                existing.Phone = request.Phone;
                existing.Email = request.Email;
                existing.Address1 = request.Address1;
                existing.Address2 = request.Address2;
                existing.DeliveryZoneID = request.ZoneId;
                existing.Company = request.Company;

                /*if (request.ZoneId > 0)
                {
                    var zone = _dbContext.DeliveryZones.FirstOrDefault(s => s.ID == request.ZoneId);
                    existing.Zone = zone;
                }*/

                if (request.VoucherId > 0)
                {
                    //Debug.WriteLine("ola2");

                    var voucher = _dbContext.Vouchers.FirstOrDefault(s => s.ID == request.VoucherId);
                    //Debug.WriteLine(voucher);
                    existing.Voucher = voucher;
                }

                _dbContext.Customers.Add(existing);
                _dbContext.SaveChanges();
                return new Tuple<int, long>(0, existing.ID);
            }
        }
    }

}
