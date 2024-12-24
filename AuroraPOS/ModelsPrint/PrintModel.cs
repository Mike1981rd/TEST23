namespace AuroraPOS;

public class PrintModel
{
    public List<PrintJobModel>? Lista { get; set; }
    public int Cantidad { get; set; }

    public PrintModel()
    {
        Lista = null;
        Cantidad = 0;
    }
}


