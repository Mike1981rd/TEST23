using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class POSProductToOrderItem
    {
        public int Status {  get; set; }
        public long? ItemId { get; set; }
        public bool? HasQuestion { get; set; }
        public List<Question>? Questions { get; set; }
        public bool? HasServingSize { get; set; }
        public List<ServingSize>? ServingSizes { get; set; }
        public string? ProductName { get; set; }
        public Product? Product { get; set; }
    }
}
