using System.Collections.Generic;
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

        public List<PaymentMethod> PaymentMethods { get; set; }
        public List<Denomination> Denominations { get; set; }
        
        public bool Refund { get; set; } 
        
        public int StationId { get; set; }
        public List<OrderTransaction> SelectedItems { get; set; }
        
        public bool HasSecondCurrency { get; set; }
        
        
    }
}
