using AuroraPOS.ViewModels;

namespace AuroraPOS.ModelsJWT
{
    public class CheckoutResponse
    {
        public CheckOutViewModel? Valor { get; set; }
        public string? redirectTo { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
