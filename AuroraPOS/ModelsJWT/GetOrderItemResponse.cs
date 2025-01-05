using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetOrderItemResponse
    {
        public GetOrderItem? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
