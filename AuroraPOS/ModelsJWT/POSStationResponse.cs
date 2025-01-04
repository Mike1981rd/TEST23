using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class POSStationResponse
    {
        public string redirectTo { get; set; }
        public string status { get; set; }
        public Order order { get; set; }
        public string error { get; set; }
    }
}
