using AuroraPOS.Data;

namespace AuroraPOS.ModelsJWT
{
    public class DeliveryListResponse
    {
        public List<DeliveryAuxiliar>? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
