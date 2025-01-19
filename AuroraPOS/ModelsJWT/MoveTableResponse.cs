
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class MoveTableResponse
    {
        public int status { get; set; }
        public string? Message { get; set; }
        public bool Success { get; set; }
    }
}
