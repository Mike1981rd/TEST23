using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class MenuCategoryListResponse
    {
        public List<MenuCategory>? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
