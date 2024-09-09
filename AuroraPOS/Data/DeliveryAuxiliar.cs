using System;
using AuroraPOS.Models;

namespace AuroraPOS.Data
{
	public class DeliveryAuxiliar
	{        
        public long DeliveryId { get; set; }
        public DateTime Creacion { get; set; }
        public DateTime Entrega { get; set; }
        public DateTime Actualizacion { get; set; }
        public string Descripcion { get; set; }
        public string Direccion { get; set; }
        public string Repartidor { get; set; }
        public string Zona { get; set; }
        public StatusEnum Status { get; set; }
        public decimal Total { get; set; }
        public string DeliveryType { get; set; }
        public Order Orden { get; set; }        

        public DeliveryAuxiliar()
		{
		}
	}
}

