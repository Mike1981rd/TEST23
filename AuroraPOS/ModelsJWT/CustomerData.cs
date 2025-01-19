using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class CustomerData
    {
        public long? ID { get; set; }
        public string? ReservationDate { get; set; }
        public string? ReservationTime { get; set; }
        public ReservationStatus? Status { get; set; }
        public decimal? Duration { get; set; }
        public string? GuestName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Comments { get; set; }
        public string? TableName { get; set; }

    }
}
