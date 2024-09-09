namespace AuroraPOS.Models
{
    public class DamageArticle : BaseEntity
    {
        public InventoryItem? Article { get; set; }
        public SubRecipe? SubRecipe { get; set; }
        public ItemType ItemType { get; set; }
        public int UnitNum { get; set; }
        public string? UnitName { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public Warehouse Warehouse { get; set;  }
        public string? Description { get; set; }
        public DateTime DamageDate { get; set; }
        public DamageArticleStatus Status { get; set; }
        public ICollection<GeneralMedia> Photos { get; set; }
    }

    public enum DamageArticleStatus
    {
        None = 0,
        Pending =1,
        Cancelled = 2,
        Confirmed = 3
    }
}
