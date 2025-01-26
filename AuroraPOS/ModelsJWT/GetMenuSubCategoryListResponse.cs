using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT;

public class GetMenuSubCategoryListResponse
{
    public List<MenuSubCategory>? Valor { get; set; }
    public string? Error { get; set; }
    public bool Success { get; set; }
}