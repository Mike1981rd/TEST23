using AuroraPOS.ViewModels;

namespace AuroraPOS.ModelsJWT
{
    public class MenuResponse
    {
        public List<CategoryViewModel>? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
