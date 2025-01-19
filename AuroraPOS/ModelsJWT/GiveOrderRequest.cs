
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GiveOrderRequest
    {
        public long orderId { get; set; }
        public long userId { get; set; }
        public int stationId { get; set; }
    }
}
