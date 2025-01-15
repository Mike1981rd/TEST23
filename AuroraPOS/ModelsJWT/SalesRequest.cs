using AuroraPOS.Models;

namespace AuroraPOS.ModelsJWT
{
    public class SalesRequest
    {
        public OrderType OrderType {  get; set; }
        public int StationId { get; set; }
        public string? UserName { get; set; }
        public OrderMode Mode { get; set; } = OrderMode.Standard;
        public long OrderId { get; set; } = 0;
        public long AreaObject { get; set; } = 0;
        public int Person { get; set; } = 0;
        public string SelectedItems { get; set; } = string.Empty;

    }
}
