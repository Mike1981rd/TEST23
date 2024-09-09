using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
    public class ProductCreateModel
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public long CategoryId { get; set; }
        public long SubCategoryId { get; set; }
        public int CourseID { get; set; }
        public string PrinterName { get; set; }
        public List<long> Taxes { get; set; }
        public List<long> Propinas { get; set; }
        public List<long> PrinterChannels { get; set; }
        public string Photo { get; set; }
        public bool HasServingSize { get; set; }
        public bool IsActive { get; set; }
        public string BackColor { get; set; }
        public string TextColor { get; set; }
        public string Barcode { get; set; }
        public decimal Price1 { get; set; }
        public decimal Price2 { get; set; }
        public decimal Price3 { get; set; }
        public decimal Price4 { get; set; }
        public decimal Price5 { get; set; }
        public decimal Price6 { get; set; }
        public decimal Price7 { get; set; }
        public decimal Price8 { get; set; }
        public decimal ProductCost { get; set; }
        public List<ProductServingSizeModel> ProductServingSizeItems { get; set; }
        public List<ProductRecipeItemModel> Recipes { get; set; }
        public List<ProductQuestionModel> Questions { get; set; }

        public string ImageUpload { get; set; }
    }

    public class ProductRecipeItemModel
    {
        public int Type { get; set; }
        public long ItemId { get; set; }
        public decimal Qty { get; set; }
        public int UnitNum { get; set; }
        public int ServingSizeID { get; set; }
    }

    public class ProductQuestionModel
    {
        public long ID { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class ProductServingSizeViewModel : ProductServingSize
    {
        public List<ProductRecipeItemViewModel> items { get; set; }
        public ProductServingSizeViewModel(ProductServingSize item)
        {
            ID = item.ID;
            ServingSizeID = item.ServingSizeID;
            Cost = item.Cost;
            Price = item.Price;
            ServingSizeName = item.ServingSizeName;
            Order = item.Order;
            IsDefault = item.IsDefault;
            items = new List<ProductRecipeItemViewModel>();
        }
    }

    public class ProductRecipeItemViewModel : ProductRecipeItem
    {
        public InventoryItem Article { get; set; }
        public SubRecipe SubRecipe { get; set; }
        public Product Product { get; set; }
        public ProductRecipeItemViewModel(ProductRecipeItem item)
        {
            this.ID = item.ID;
            this.Type= item.Type;
            this.UnitNum = item.UnitNum;
            this.Qty = item.Qty;
            this.ItemID = item.ItemID;
        }
    }

    public class ProductServingSizeModel
    {
        public int ServingSizeID { get; set; }
        public bool IsDefault { get; set; }
        public int Order { get; set; }
        public decimal Price1 { get; set; }
        public decimal Price2 { get; set; }
        public decimal Price3 { get; set; }
        public decimal Price4 { get; set; }
        public decimal Price5 { get; set; }
        public decimal Price6 { get; set; }
        public decimal Price7 { get; set; }
        public decimal Price8 { get; set; }
        public decimal ProductCost { get; set; }
    }
}
