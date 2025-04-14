namespace AuroraPOS.ModelsJWT
{
    public class PayRequest
    {
        public int Amount { get; set; }
        public int DividerId { get; set; }
        public int Method {  get; set; }
        public int OrderId { get; set; }
        public int SeatNum { get; set; }
        public int StationId { get; set; }
        
    }
}
