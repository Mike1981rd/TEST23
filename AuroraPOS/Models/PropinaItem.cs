namespace AuroraPOS.Models
{
    public class PropinaItem : BaseEntity
    {
        public long PropinaId { get; set; }
        public string Description { get; set; }
        public decimal Percent { get; set; }
        public decimal Amount { get; set; }
        public bool IsExempt { get; set; } = false;
        public bool ToGoExclude { get; set; }
        public bool BarcodeExclude { get; set; }
    }
}
