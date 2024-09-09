namespace AuroraPOS.Models
{
    public class Reservation : BaseEntity
    {
        public DateTime ReservationTime { get; set; }
        public int GuestCount {  get; set; }
        public decimal Duration {  get; set; }
        public decimal Cost {  get; set; }
        public string GuestName {  get; set; }
        public string ClientName { get; set; }
        public string PhoneNumber { get; set; }
        public string Comments { get; set; }
        public int TableID { get; set; }
        public int StationID { get; set; }
        public int AreaID { get; set; }
        public ReservationStatus Status { get; set; }
        public string TableName { get; set; }
    }

    public enum ReservationStatus
    {
        Open,
        Done,
        Canceled,
        Arrived
    }
}
