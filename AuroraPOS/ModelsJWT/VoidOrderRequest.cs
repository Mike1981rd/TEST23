using AuroraPOS.Controllers;

namespace AuroraPOS.ModelsJWT
{
    public class VoidOrderRequest
    {
        public int stationId { get; set; }
        public VoidOrderModel orderModel { get; set; }
    }
}
