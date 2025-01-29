using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT;

public class GetMenuProductListResponse
{
    public List<MenuProduct>? Valor { get; set; }
    public string? Error { get; set; }
    public bool Success { get; set; }
}