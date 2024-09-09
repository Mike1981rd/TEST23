namespace AuroraPOS.ViewModels
{
    public class PurchaseHistoryViewModel
    {
        public DateTime PurchaseDate { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public decimal Cost { get; set; }
        public decimal Total {  get; set; }
        public string Supplier { get; set; }

    }
}
