namespace AuroraPOS.Models
{
    public class Brand : BaseEntity
    {
        public string Name { get; set; }
        public bool IsSystemDefault { get; set; } = false;
    }
}
