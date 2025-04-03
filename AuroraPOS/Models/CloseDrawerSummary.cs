namespace AuroraPOS.Models;

public class PCDSDenominations
{
    public string name { get; set; }
    public int qty { get; set; }
    public string amt { get; set; }
}

public class PCDSFormaPago
{
    public string formaPago { get; set; }
    public string valor { get; set; }
}
public class CloseDrawerSummary
{
    public string titulo { get; set; }
    public string empresa { get; set; }
    public string cajero { get; set; }
    public decimal total { get; set; }
    public string grandTotal { get; set; }
    public string expectedTotal { get; set; }
    public string discrepancy { get; set; }
    public string expectedTipTotal { get; set; }
    public string tipDiscrepancy { get; set; }
    public List<PCDSDenominations> denominations { get; set; }
    public List<PCDSFormaPago> formaPagos { get; set; }
}