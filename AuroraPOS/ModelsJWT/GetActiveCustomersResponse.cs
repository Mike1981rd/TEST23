
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetActiveCustomersResponse
    {
        public List<Customer>? result { get; set; } 
        public string? Error { get; set; }
        public bool Success { get; set; }
        public string? message { get; set; }
    }
}
