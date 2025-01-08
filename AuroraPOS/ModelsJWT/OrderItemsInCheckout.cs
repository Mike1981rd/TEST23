using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class OrderItemsInCheckout
    {
        public List<SeatItem>? seats {  get; set; }
        public Order? order { get; set; }
        public List<OrderItem>? orderItems { get; set; }
        public List<OrderTransaction>? transactions { get; set; }
        public Preference? store { get; set; }
        public int status {  get; set; }

    }
}
