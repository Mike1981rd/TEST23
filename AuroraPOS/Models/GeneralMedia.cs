namespace AuroraPOS.Models
{
    public class GeneralMedia : BaseEntity
    {
        public string? URL { get; set; }
        public string? Src { get; set; }
        public bool IsURL { get; set; }
        public long ItemID { get; set; }
        public MediaType Type { get; set; }
        public MediaDestination DestEntity { get; set; }
    }


    public enum MediaType
    {
        None,
        Image,
        Video
    }

    public enum MediaDestination
    {
        None,
        DamagedArticle,
        Product,
        Area,
        AreaObject
    }
}
