using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class CustomerCreateRequest : BaseEntity
    {
        public string Name { get; set; }
        public string? RNC { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; } = string.Empty;
        public string? Address1 { get; set; } = string.Empty;
        public string? Address2 { get; set; } = string.Empty;
        public string? City { get; set; } = string.Empty;
        public string? State { get; set; } = string.Empty;
        public string? PostalCode { get; set; } = string.Empty;
        public string? Country { get; set; } = string.Empty;
        public string? Avatar { get; set; } = string.Empty;
        public string? Company { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal CreditLimit { get; set; }
        public int CreditDays { get; set; }
        public Voucher? Voucher { get; set; }
		public long VoucherId { get; set; }
        public long? DeliveryZoneID { get; set; }
    }
}
