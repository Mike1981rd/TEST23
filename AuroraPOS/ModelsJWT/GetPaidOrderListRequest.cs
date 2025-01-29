namespace AuroraPOS.ModelsJWT
{
    public class GetPaidOrderListRequest
    {
        public long? AreaId { get; set; } = 0;
        public string? From { get; set; }
        public string? To { get; set; }
        public long? Cliente { get; set; } = 0;
        public long? Orden { get; set; } = 0;
        public decimal? Monto { get; set; } = 0;
        public int? Branch { get; set; } = 0;
        public int? Factura { get; set; } = 0;
        public int StationId { get; set; }
        //public int? Start { get; set; } = 0;
        //public int? Length { get; set; } = 10;
    }
}
