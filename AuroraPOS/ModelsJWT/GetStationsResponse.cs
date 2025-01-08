
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetStationsResponse
    {
        public List<Station>? result { get; set; } 
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
