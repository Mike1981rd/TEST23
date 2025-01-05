using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class VoucherListResponse
    {
        public List<Voucher>? Valor { get; set; }
        public string? Error { get; set; }
        public bool Success { get; set; }
    }
}
