namespace AuroraPOS.Models
{
    public class Promotion : BaseEntity
    {
        public string Name { get; set; }
        public AmountType AmountType { get; set; }
        public decimal Amount { get; set; }
        public bool IsRecurring { get; set; } = false;
        public bool IsAllDay { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime? RecurringStartDate { get; set; }
        public decimal Duration { get; set; }
        public DiscountType DiscountType { get; set; }
        public PromotionApplyType ApplyType { get; set; }
        public int FirstCount { get; set; }
        public int EveryCount { get; set; }
        public int EndOccurrence { get; set; }
        public bool IsRecurNoEnd { get; set; }
        public int WeekNum { get; set; }
        public string? WeekDays { get; set; }
        public List<PromotionTarget>? Targets { get; set; }
    }
	public class PromotionTarget : BaseEntity
	{
        public long Menu { get; set; }
        public string MenuName { get; set; }
		public ProductRangeType ProductRange { get; set; }
		public long TargetId { get; set; }
        public int ServingSizeID { get; set; }
		public string Description { get; set; }
	}
	public enum ProductRangeType
	{
		Group=1,
		Category=2,
		SubCategory=3,
		Product=4
	}
	public enum PromotionApplyType
    {
        FirstCount,
        EveryCount
    }

    public enum DiscountType
    {
        AccountFunded,
        RetailerFunded,
        ManufacturerFunded
    }
}
