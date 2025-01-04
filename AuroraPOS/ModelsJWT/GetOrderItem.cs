using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class GetOrderItem
    {
        public Order? Order { get; set; }
        public List<SeatItem>? Seat { get; set; }
        public int status { get; set; }
        public List<OrderItem>? Items { get; set; }
        public string? clientName { get; set; }

    }
}
