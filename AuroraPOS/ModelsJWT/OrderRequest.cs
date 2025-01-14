namespace AuroraPOS.ModelsJWT
{
    public class OrderRequest
    {
        public long OrderId { get; set; }
        public int StationId { get; set; }
        public string db {  get; set; }
        public DateTime? SaveDate { get; set; } = null;
        public int DivideNum { get; set; } = 0;
    }
}
