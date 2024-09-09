namespace AuroraPOS.Models
{
	public class Preference : BaseEntity
	{
		public string? Name { get; set; } = string.Empty;
		public string? Company { get; set; } = string.Empty;
        public string? Logo { get; set; } = string.Empty;
        public string? RNC { get; set; } = string.Empty;
        public string? Phone { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? Address1 { get; set; } = string.Empty;
        public string? Address2 { get; set; } = string.Empty;
        public string? City { get; set; } = string.Empty;
        public string? State { get;set; } = string.Empty;
        public string? PostalCode { get; set; } = string.Empty;
        public string? Country { get; set; } = string.Empty;
        public string? Currency { get; set; } = string.Empty;
        public string? SecondCurrency { get; set; } = string.Empty;
        public decimal Tasa { get; set; } = 0;
        public int StationLimit { get; set; } = 0;
    }
}
