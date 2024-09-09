namespace AuroraPOS.Models
{
    public class ItemUnit : BaseEntity
    {
        public string Name { get; set; }
        public decimal Rate { get; set; }
        public int Number { get; set; }
        public decimal Cost { get; set; }
        public string? CodeBar { get; set; }
        public decimal PayItem { get; set; }
        public decimal Price { get; set; }
        public long UnitID { get; set; }
        public virtual Unit Unit { get; set; }
    }
}
