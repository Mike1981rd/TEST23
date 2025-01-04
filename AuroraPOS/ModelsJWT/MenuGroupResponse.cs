using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class MenuGroupResponse
    {
        public List<MenuGroup>? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
