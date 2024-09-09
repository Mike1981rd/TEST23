using System.Security.Cryptography.Pkcs;

namespace AuroraPOS.Models
{
    public class CloseDrawer : BaseEntity
    {
        public string Username { get; set; }
        public long StationID { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TransTotal { get; set; }
        public decimal TipTotal { get; set; }
        public decimal TransDifference { get; set; }
        public decimal TipDifference { get; set; }
        public string Denominations { get; set; }
        public string PaymentMethods { get; set; }
    }
}
