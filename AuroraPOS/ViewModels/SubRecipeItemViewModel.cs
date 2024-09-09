using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
    public class SubRecipeItemViewModel : SubRecipeItem
    {
        public InventoryItem Article { get; set; }
        public SubRecipe SubRecipe { get; set; }
        public SubRecipeItemViewModel(SubRecipeItem item)
        {
            this.ID = item.ID;
            this.UnitNum = item.UnitNum;
            this.Qty = item.Qty;
            this.ItemCost = item.ItemCost;
            this.UnitCost = item.UnitCost;
            this.IsArticle = item.IsArticle;
            this.ItemID = item.ItemID;
            this.FirstQty= item.FirstQty;
        }
    }
}
