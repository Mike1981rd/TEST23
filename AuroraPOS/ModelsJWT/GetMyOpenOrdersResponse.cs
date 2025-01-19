
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetMyOpenOrdersResponse
    {
        public List<Order>? result { get; set; } 
        public string? Error { get; set; }
        public bool Success { get; set; }
        public string? message { get; set; }
    }
}
