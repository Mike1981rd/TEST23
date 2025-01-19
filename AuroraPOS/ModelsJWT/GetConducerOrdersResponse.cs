using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetConduceOrdersResponse
    {
        public List<Order>? result { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
