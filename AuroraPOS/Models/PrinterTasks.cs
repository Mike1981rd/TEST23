namespace AuroraPOS.Models;

public class PrinterTasks : BaseEntity
{
    public long? ObjectID { get; set; }
    public int? Type { get; set; }
    public int? Status { get; set; }
    public long? StationId { get; set; }
    public long? SucursalId { get; set; }
    public int? DivideNum { get; set; }
    public int? SeatNum { get; set; }
    public string? PhysicalName { get; set; }
    public string? Items { get; set; }
}

public enum PrinterTasksStatus
{
    Pendiente = 0, 
    Impreso = 1
}

public enum PrinterTasksType
{
    TicketOrden = 0,
    TicketCocina = 1,
    TicketPaymentSummary = 2,
    TicketPaymentSummaryCopy = 4,
    TicketCloseDrawerSummary = 5,
}