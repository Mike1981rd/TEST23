using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class ActiveCustomerListResponse
    {
        public ActiveCustomerList? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
