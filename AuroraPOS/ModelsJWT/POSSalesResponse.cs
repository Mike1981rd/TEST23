using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class POSSalesResponse
    {
        public string? redirectTo { get; set; }
        public int? status { get; set; }
        public Order? order { get; set; }
        public string? error { get; set; }
        public bool success { get; set; }
    }
}
