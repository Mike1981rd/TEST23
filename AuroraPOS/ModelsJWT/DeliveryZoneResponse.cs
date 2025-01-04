using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class DeliveryZoneResponse
    {
        public List<DeliveryZone>? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
