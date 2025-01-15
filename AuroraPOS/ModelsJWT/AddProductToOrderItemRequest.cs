namespace AuroraPOS.ModelsJWT
{
    public class AddProductToOrderItemRequest
    {
        public long OrderId { get; set; }
        public long ProductId { get; set; }
        public long MenuProductId { get; set; }
        public decimal Qty { get; set; }
        public int SeatNum { get; set; }
        public int DividerNum { get; set; }
        public int StationId { get; set; }
        public string db {  get; set; }
    }
}
