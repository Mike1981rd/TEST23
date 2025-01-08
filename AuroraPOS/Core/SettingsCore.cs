using AuroraPOS.Data;
using AuroraPOS.Models;
using AuroraPOS.ModelsJWT;
using AuroraPOS.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;

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

        public ActiveCustomerList GetActiveCustomerList(string draw, int start, int length,
            string sortColumn, string sortColumnDirection, string searchValue, long clienteid = 0)
        {
            var activeCustomerList = new ActiveCustomerList();

            //Paging Size (10,20,50,100)  
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            int recordsTotal = 0;

            // Getting all Customer data  
            var customerData = (from s in _dbContext.Customers
                                where s.IsActive
                                select s);

            if (clienteid > 0)
            {
                customerData = (from s in _dbContext.Customers
                                where s.IsActive && s.ID == clienteid
                                select s);


            }

            //Sorting
            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            {
                try
                {
                    customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                }
                catch { }
            }
            ////Search  
            if (!string.IsNullOrEmpty(searchValue))
            {
                customerData = customerData.Where(m => m.Name.ToLower().Contains(searchValue.ToLower()) || m.Phone.ToLower().Contains(searchValue.ToLower()) || m.Address1.ToLower().Contains(searchValue.ToLower()));
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
            activeCustomerList.draw = draw;
            activeCustomerList.recordsFiltered = recordsTotal;
            activeCustomerList.recordsTotal = recordsTotal;
            activeCustomerList.activeCustomers = data;

            return activeCustomerList;
        }

        public PrepareTypesList GetPrepareTypes(string draw, int start, int length, string sortColumn, 
            string sortColumnDirection, string searchValue,bool justData = false)
        {
            var prepareTypes = new PrepareTypesList();

            if (!justData)
            {
                //Paging Size (10,20,50,100)  
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                // Getting all Customer data  
                var pepareTypesData = (from s in _dbContext.PrepareTypes
                                       where !s.IsDeleted
                                       select s);

                //Sorting
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    try
                    {
                        pepareTypesData = pepareTypesData.OrderBy(sortColumn + " " + sortColumnDirection);
                    }
                    catch { }
                }
                ////Search  
                if (!string.IsNullOrEmpty(searchValue))
                {
                    pepareTypesData = pepareTypesData.Where(m => m.Name.Contains(searchValue));
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
                prepareTypes.draw = draw;
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
    }
}
