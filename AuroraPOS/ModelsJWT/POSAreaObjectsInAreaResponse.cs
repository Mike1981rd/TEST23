using AuroraPOS.Controllers;
using AuroraPOS.Models;
using AuroraPOS.ViewModels;

namespace AuroraPOS.ModelsJWT
{
    public class POSAreaObjectsInAreaResponse
    {
        public AreaObjects? result { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
