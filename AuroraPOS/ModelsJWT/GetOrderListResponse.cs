using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetOrderListResponse
    {
        public object result { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
