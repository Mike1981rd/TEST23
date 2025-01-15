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
    }
}
