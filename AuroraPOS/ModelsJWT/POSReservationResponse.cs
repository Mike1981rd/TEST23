namespace AuroraPOS.ModelsJWT
{
    public class POSReservationResponse
    {
        public int status { get; set; }
        public object reservation { get; set; }
        public string time { get; set; }
    }
}
