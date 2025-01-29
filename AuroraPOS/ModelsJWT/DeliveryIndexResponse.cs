
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class DeliveryIndexResponse
    {
        public List<CancelReason>? cancelReasons { get; set; } 
        public List<t_sucursal>? Branchs { get; set; }
        public int? filterBranch { get; set; }
        public int? SucursalActual { get; set; }
    }
}
