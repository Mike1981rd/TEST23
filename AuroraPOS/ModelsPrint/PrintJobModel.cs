namespace AuroraPOS;

public class PrintJobModel
{
    public long Id { get; set; }   
    public long ObjectId { get; set; }   
    public int Type { get; set; }   
    public string PhysicalName { get; set; }   
    public long StationId { get; set; }   
    public long SucursalId { get; set; }
    public PrintOrderModel printJobOrder { get; set; }
}