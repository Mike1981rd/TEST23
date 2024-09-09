namespace AuroraPOS.Models
{
    public class AccountType : BaseEntity
    {
        public string Name { get; set; }
    }
    public class AccountDescription : BaseEntity
    {
        public string Name { get; set; }
    }

    public class Account : BaseEntity
    {
        public AccountType AccountType { get; set; }   
        public AccountDescription AccountDescription { get; set; }
        public string Number { get; set; }
        public int CreditOrDebit { get; set; }        // 0 credit  1  debit
        public List<int>? Defaults { get; set; }
    }

    public class AccountItem : BaseEntity
    {
        public int Order { get; set; }
        public Account Account { get; set; }
        public long ItemID { get; set; }
        public AccountingTarget Target { get; set; }
    }

    public enum AccountingTarget
    {
        None,
        Category,
        Product,
        Article,
        Supplier,
        Client,
        Tax,
        Propina,
        PaymentMethod,
        Waste,
        Tip,
        Discount,
        VoidReason,
        Group,
		DeliveryZones
	}
}
