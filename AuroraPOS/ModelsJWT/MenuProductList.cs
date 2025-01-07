namespace AuroraPOS.ModelsJWT
{
    public class MenuProductList
    {
        public string? draw {  get; set; }
        public int recordsFiltered { get; set; } = 0;
        public int recordsTotal { get; set; } = 0;
        public List<ProductFiltered>? productFiltereds { get; set; }
    }

    public class ProductFiltered
    {

        public long ID { get; set; }
        public string Name { get; set; }
        public string Printer { get; set; }
        public string Photo { get; set; }
        public string CategoryName { get; set; }
        public long CategoryId { get; set; }
        public string Barcode { get; set; }
        public bool IsActive { get; set; }
    }
}
