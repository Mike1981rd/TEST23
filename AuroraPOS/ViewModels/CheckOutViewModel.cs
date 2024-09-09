using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
    public class CheckOutViewModel
    {
        public long OrderId { get; set; }
        public Order Order { get; set; }
        public int PaymentType { get; set; }
        public int SeatNum { get; set; }
        public int DividerId { get; set; }
        public decimal BalanceToPay { get; set; }
        public string ClientName { get; set; }
        public string ComprebanteName { get; set; }
    }
}
