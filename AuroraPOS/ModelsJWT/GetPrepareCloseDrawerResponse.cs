
using AuroraPOS.Controllers;
using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetPrepareCloseDrawerResponse
    {
        public List<Order>? resultOrders { get; set; }
        public List<PaymentMethodSummary>? resultExpectedPayments { get; set; }
        public int status { get; set; }
        public string? name { get; set; }
        public decimal? expected { get; set; }
        public long? sequance { get; set; }
        public decimal? expectedtip { get; set; }
        public string? message { get; set; }
    }
}
