namespace AuroraPOS.ModelsJWT;

public class GetProductListRequest
{
    public string draw { get; set; } 
    public int start { get; set; }
    public int length { get; set; } 
    public string sortColumn { get; set; } 
    public string sortColumnDirection { get; set; } 
    public string searchValue { get; set; } 
    public string all  { get; set; } 
    public string category { get; set; } 
    public string barcode { get; set; } 
    public string status  { get; set; } 
    public string db { get; set; } 
}