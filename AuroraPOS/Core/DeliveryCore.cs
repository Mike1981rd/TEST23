using Microsoft.AspNetCore.Mvc;
using AuroraPOS.Data;
using AuroraPOS.Services;
using AuroraPOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuroraPOS.ModelsJWT;
using Org.BouncyCastle.Utilities;
using NPOI.SS.Formula.Functions;
using AuroraPOS.Controllers;
using NPOI.SS.Formula.PTG;
using AuroraPOS.ModelsJWT;
using Newtonsoft.Json;
using System.Diagnostics;
using AuroraPOS.ViewModels;
using System.Globalization;

namespace AuroraPOS.Core
{
    public class DeliveryCore
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _context;

        public DeliveryCore(IUserService userService, AppDbContext dbContext, IHttpContextAccessor context)
        {
            _userService = userService;
            _dbContext = dbContext;
            _context = context;
        }

        public List<DeliveryAuxiliar> GetDeliveryList(string fecha, int? status, int branch = 0)
        {
            var reasons = _dbContext.CancelReasons.ToList();

            //var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
            // Skiping number of Rows count  
            //var start = Request.Form["start"].FirstOrDefault();
            // Paging Length 10,20  
            //var length = Request.Form["length"].FirstOrDefault();
            // Sort Column Name  
            //var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            // Sort Column Direction ( asc ,desc)  
            //var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            // Search Value from (Search box)  
            //var searchValue = Request.Form["search[value]"].FirstOrDefault();

            //Paging Size (10,20,50,100)  
            //int pageSize = length != null ? Convert.ToInt32(length) : 0;
            //int skip = start != null ? Convert.ToInt32(start) : 0;
            //int recordsTotal = 0;
            //if (branch > 0)
            //{
            //    HttpContext.Session.SetInt32("FilterBranch", branch);
            //}
            // Getting all Customer data

            DateTime dtFecha = DateTime.ParseExact(fecha, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            var deliveryData = _dbContext.Deliverys.Where(d => d.CreatedDate >= new DateTime(dtFecha.Year, dtFecha.Month, dtFecha.Day) && d.CreatedDate < (new DateTime(dtFecha.Year, dtFecha.Month, dtFecha.Day)).AddDays(1)).Include(s => s.Order).ThenInclude(s => s.Station).Include(s => s.Order).Include(s => s.Order.PrepareType).Include(s => s.Order.Items.Where(s => !s.IsDeleted)).Include(s => s.Carrier).Include(s => s.Zone).Include(s => s.Customer).ToList();


            //deliveryData = (from d in deliveryData where d.CreatedDate >= new DateTime(dtFecha.Year, dtFecha.Month, dtFecha.Day) && d.CreatedDate < (new DateTime(dtFecha.Year, dtFecha.Month, dtFecha.Day)).AddDays(1) select d).ToList();

            if (status != null)
            {
                deliveryData = (from d in deliveryData where d.Status == (StatusEnum)status select d).ToList();
            }

            var lstResultado = new List<DeliveryAuxiliar>();

            foreach (var objDelivery in deliveryData)
            {
                var objDeliveryAuxiliar = new DeliveryAuxiliar();

                if (branch > 0 && objDelivery.Order.Station.IDSucursal != branch) continue;

                objDeliveryAuxiliar.DeliveryId = (long)objDelivery.ID;
                objDeliveryAuxiliar.Creacion = (objDelivery.UpdatedDate > objDelivery.CreatedDate ? objDelivery.UpdatedDate : objDelivery.CreatedDate);
                objDeliveryAuxiliar.Entrega = objDelivery.DeliveryTime;
                objDeliveryAuxiliar.Orden = objDelivery.Order;
                objDeliveryAuxiliar.Direccion = string.IsNullOrEmpty(objDelivery.Address1) && objDelivery.Zone == null ? "Para llevar" : objDelivery.Address1;

                if (objDelivery.Order.PrepareType != null && objDelivery.Order.PrepareType.SinChofer)
                {
                    objDeliveryAuxiliar.Zona = (objDelivery.Zone != null ? objDelivery.Zone.Name : "");
                }
                else
                {
                    objDeliveryAuxiliar.Zona = (objDelivery.Zone != null ? objDelivery.Zone.Name + "($" + objDelivery.Zone.Cost.ToString() + ")" : "");
                }


                objDeliveryAuxiliar.Repartidor = (objDelivery.Carrier != null ? objDelivery.Carrier.Name : "");
                objDeliveryAuxiliar.Status = objDelivery.Status;
                if (objDelivery.Order.ConduceOrderId > 0 || objDelivery.Order.IsConduce)
                {
                    objDeliveryAuxiliar.Status = StatusEnum.Cerrado;
                }
                objDeliveryAuxiliar.Actualizacion = objDelivery.UpdatedDate;
                objDeliveryAuxiliar.DeliveryType = objDelivery.Order.PrepareType != null ? objDelivery.Order.PrepareType.Name : "";


                objDeliveryAuxiliar.Total = objDelivery.Order.TotalPrice /*+ (objDelivery.Zone!=null ? objDelivery.Zone.Cost : 0)*/;

                foreach (var objOrderItem in objDeliveryAuxiliar.Orden.Items)
                {

                    if (!string.IsNullOrEmpty(objDeliveryAuxiliar.Descripcion))
                    {
                        objDeliveryAuxiliar.Descripcion = objDeliveryAuxiliar.Descripcion + ", ";
                    }

                    objDeliveryAuxiliar.Descripcion = objDeliveryAuxiliar.Descripcion + objOrderItem.Name;
                }

                if (objDeliveryAuxiliar.Orden != null && objDeliveryAuxiliar.Orden.Items.Any())
                {
                    lstResultado.Add(objDeliveryAuxiliar);
                }


            }

            /*Sorting
            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            {
                try
                {
                    customerData = customerData.OrderBy(sortColumn + " " + sortColumnDirection);
                }
                catch { }
            }*/
            ////Search  
            /*if (!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.Trim().ToLower();
                customerData = customerData.Where(m => m.Name.ToLower().Contains(searchValue) || m.Model.ToLower().Contains(searchValue) || m.PhysicalName.ToLower().Contains(searchValue));
            }*/

            //total number of rows count   
            //recordsTotal = customerData.Count();
            //Paging   
            /*var data = customerData.Skip(skip).ToList();
            if (pageSize != -1)
            {
                data = data.Take(pageSize).ToList();
            }*/
            //Returning Json Data  
            return lstResultado;
        }
    }
}
