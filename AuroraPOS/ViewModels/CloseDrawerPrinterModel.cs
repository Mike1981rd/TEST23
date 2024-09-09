namespace AuroraPOS.ViewModels
{
    public class CloseDrawerPrinterModel
    {
        public string UserName { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TransTotal { get; set; }
        public decimal TipTotal { get; set; }
        public decimal TransDifference { get; set; }
        public decimal TipDifference { get; set; }
        public List<CloseDrawerDominationModel> Denominations { get; set; }
        public List<CloseDrawerPaymentModel> PMethods { get; set; }
        public CloseDrawerPrinterModel()
        {
            Denominations = new List<CloseDrawerDominationModel>();
            PMethods = new List<CloseDrawerPaymentModel>();
        }
    }

    public class CloseDrawerDominationModel
    {
        public string Name { get; set; }
        public int Qty { get; set; }
        public decimal Amount { get; set; }
    }
    public class CloseDrawerPaymentModel
    {
        public string Name { get; set; }
        public decimal Amount { get; set; }
    }
}
