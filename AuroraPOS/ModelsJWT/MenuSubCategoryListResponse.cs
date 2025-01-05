using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class MenuSubCategoryListResponse
    {
        public List<MenuSubCategory>? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
