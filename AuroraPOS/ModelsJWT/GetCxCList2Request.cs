
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetCxCList2Request
    {
        public string from { get; set; }
        public string to { get; set; }
        public long cliente { get; set; } 
        public long orden { get; set; }
        public decimal monto { get; set; }
    }
}
