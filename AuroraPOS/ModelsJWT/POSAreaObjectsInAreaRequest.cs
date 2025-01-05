using AuroraPOS.ViewModels;

namespace AuroraPOS.ModelsJWT
{
    public class POSAreaObjectsInAreaRequest
    {
        public int stationId { get; set; }
        public string db { get; set; }
        public long areaID {  get; set; }
    }
}
