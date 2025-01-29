
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class SubmitConduceOrdersResponse
    {
        public int status { get; set; } 
        public long? newOrderId { get; set; }
        public string? Message { get; set; }
        public bool Success { get; set; }
    }
}
