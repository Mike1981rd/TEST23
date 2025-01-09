namespace AuroraPOS.ModelsJWT
{
    public class ProductToOrderItemResponse
    {
        public POSProductToOrderItem? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
