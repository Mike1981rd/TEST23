
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetCxCListResponse
    {
        public List<OrderTransaction>? result { get; set; } 
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
