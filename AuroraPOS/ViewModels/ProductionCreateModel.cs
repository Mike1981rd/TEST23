using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
    public class ProductionCreateModel
    {
        public long ID { get; set; }
        public long WarehouseID { get; set; }
        public long SubRecipeID { get; set; }
        public string ProductionDate { get; set; }
        public decimal Qty { get; set; }
        public decimal EndQty { get; set; }
        public int UnitNum { get; set; }
        public string Description { get; set; } = "";
        public ProductionStatus Status { get; set; }
    }

    public class StatusUpdateModel
    {
        public long ID { get; set; }
        public int Status { get; set; }

    }

    public class ProductionViewModel
    {
        public long ID { get; set; }

		public string WarehouseName {get;set; }  
        public long WarehouseId { get; set; }
        public string SubRecipeName { get; set; }
        public SubRecipe SubRecipe { get; set; }
        public int UnitNumber { get; set; }
        public string UnitName { get; set; }
        public decimal Qty { get; set; }
        public ProductionStatus Status { get; set; }			
    }
}
