namespace AuroraPOS.Models
{
    public class Discount : BaseEntity
    {
        public string Name { get; set; }
        public AmountType DiscountAmountType { get; set; }
        public decimal Amount { get; set; }
    }

    public enum AmountType
    {
        Percent,
        Amount
    }
}
