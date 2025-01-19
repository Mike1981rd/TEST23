
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetCxCListRequest
    {
        public string? customerName { get; set; }
        public long customerId { get; set; }
    }
}
