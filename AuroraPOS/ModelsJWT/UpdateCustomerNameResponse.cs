
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class UpdateCustomerNameResponse
    {
        public int status { get; set; } 
        public string? Message { get; set; }
        public bool Success { get; set; }
    }
}
