using AuroraPOS.Controllers;
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetDatosDGIIResponse
    {
        public DatosDGIIResponse? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
