using AuroraPOS.Controllers;

namespace AuroraPOS.Models
{
    public class AreaObjects
    {
        public ICollection<AreaObject>? objects { get; set; }
        public List<StationOrderModel>? orders { get; set; }
        public List<OrderHoldModel>? holditems { get; set; }
    }
}
