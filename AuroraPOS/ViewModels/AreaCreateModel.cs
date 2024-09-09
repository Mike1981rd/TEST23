using AuroraPOS.Models;

namespace AuroraPOS.ViewModels
{
    public class AreaCreateModel : Area
    {
        public int MenuID { get; set; }        
    }

    public class AreaObjectSizeModel
    {
        public long AreaID { get; set; }
        public long AreaObjectID { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
    }
}
