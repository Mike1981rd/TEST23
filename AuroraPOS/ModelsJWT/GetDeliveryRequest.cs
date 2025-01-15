namespace AuroraPOS.ModelsJWT
{
    public class GetDeliveryRequest
    {
        public string Fecha {  get; set; } = string.Empty;
        public int? Status {  get; set; }
        public int Branch { get; set; } = 0;
    }
}
