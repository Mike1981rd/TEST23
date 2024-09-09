namespace AuroraPOS.Models
{
    public class Propina : BaseEntity
    {
        public string PropinaName { get; set; } = string.Empty;
        public double PropinaValue { get; set; } // percent
        public virtual ICollection<Category> Categories { get; set; }
        public virtual ICollection<Product> Products { get; set; }
        public bool IsInPurchaseOrder { get; set; }
        public bool IsInArticle { get; set; }
        public bool IsShipping { get; set; }
        public bool IsToGoExclude { get; set; }
        public bool IsBarcodeExclude { get; set; }
        public bool IsKioskExclude { get; set; }
    }
}
