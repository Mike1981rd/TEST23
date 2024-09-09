namespace AuroraPOS.Models
{
    public class SubRecipeProduction : BaseEntity
    {
        public virtual Warehouse Warehouse { get; set; }
        public DateTime ProductionDate { get; set; }
        public SubRecipe SubRecipe { get; set; }
        public int UnitNum { get; set; }
        public decimal Qty { get; set; }
        public decimal EndQty { get; set; }
        public ProductionStatus Status { get; set; }
        public string? Description { get; set; }
    }

    public enum ProductionStatus
    {
        None = 0, 
        Pending = 1,
        Cancelled =2,
        Completed = 3
    }
}
