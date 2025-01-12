namespace AuroraPOS.ModelsJWT
{
    public class POSPayResponse
    {
        public POSCorePayModel? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
