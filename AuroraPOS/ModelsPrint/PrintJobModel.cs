namespace AuroraPOS;

public class PrintJobModel
{
    public long Id { get; set; }   
    public long ObjectId { get; set; }   
    public int Type { get; set; }   
    public string PhysicalName { get; set; }   
    public int StationId { get; set; }   
    public int SucursalId { get; set; }   
    public string Json { get; set; }
}