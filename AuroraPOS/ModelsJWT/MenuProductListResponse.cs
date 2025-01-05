using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class MenuProductListResponse
    {
        public List<MenuProduct>? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
