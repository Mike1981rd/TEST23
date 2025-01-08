using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetOrderItemsInCheckoutResponse
    {
        public OrderItemsInCheckout? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
