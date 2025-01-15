namespace AuroraPOS.ModelsJWT
{
    public class CheckoutRequest
    {
        public long OrderId { get; set; }
        public int StationId { get; set; }
        public string db {  get; set; }
        public int Seat {  get; set; } = 0;
        public int DividerId { get; set; } = 0;
        public string SelectedItems { get; set; } = string.Empty;
        public bool Refund { get; set; } = false;
    }
}
