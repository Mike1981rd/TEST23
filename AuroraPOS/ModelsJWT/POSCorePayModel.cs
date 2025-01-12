namespace AuroraPOS.ModelsJWT
{
    public class POSCorePayModel
    {
        public int Status {  get; set; }
        public decimal? Difference { get; set; }
        public decimal? Balance { get; set; }
        public bool? Parcial { get; set; }
    }
}
