using System.ComponentModel.DataAnnotations.Schema;

namespace AuroraPOS.Models
{
    public class Area : BaseEntity
    {
        public string Name { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public string? BackColor { get; set; }
        public string? BackImage { get; set;}
        public ICollection<Station> Stations { get; set; }
        public ICollection<AreaObject>? AreaObjects { get; set; }
        public ICollection<Order>? Orders { get; set; }

        [NotMapped]
        public string ImageUpload { get; set; } = string.Empty;
    }
    public class AreaObject : BaseEntity
    {
        public string Name { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
        public decimal Radius { get; set; }
        public int SeatCount { get; set; }
        public string? BackColor { get; set; }
        public string? TextColor { get; set; }
        public string? BorderColor { get; set; }
        public string? BackImage { get; set; }
        public Area Area { get; set; }
        public AreaObjectShape Shape { get; set; }
        public AreaObjectType ObjectType { get; set; }
        public virtual ICollection<Order> Orders { get; set; }

        [NotMapped]
        public string ImageUpload { get; set; } = string.Empty;
    }

    public enum AreaObjectShape
    {
        Rectangle,
        Eclipse,
        RoundedRectangle
    }

    public enum AreaObjectType
    {
        Static,
        Table,
        Chair
    }
}
