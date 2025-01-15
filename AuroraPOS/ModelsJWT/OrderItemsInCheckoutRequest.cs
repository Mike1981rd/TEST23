namespace AuroraPOS.ModelsJWT
{
    public class OrderItemsInCheckoutRequest
    {
        public long OrderId { get; set; }
        public int SeatNum {  get; set; }
        public int DividerId { get; set; }
    }
}
