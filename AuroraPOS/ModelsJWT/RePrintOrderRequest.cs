
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class RePrintOrderRequest
    {
        public long orderId { get; set; }
        public int stationId { get; set; }
        //public string db { get; set; }
    }
}
