namespace AuroraPOS.Models
{
    public class PrintJob
    {
        public int Id { get; set; }  // ID único del trabajo de impresión
        public string JobId { get; set; }  // Identificador del trabajo de impresión (como un UUID)
        public string PrinterName { get; set; }  // Nombre de la impresora asignada
        public string HtmlContent { get; set; }  // Contenido HTML para imprimir
        public string Status { get; set; }  // Estado del trabajo de impresión: "pendiente" o "impreso"
        public DateTime CreatedAt { get; set; }  // Fecha y hora de creación del trabajo
        public DateTime? PrintedAt { get; set; }  // Fecha y hora de impresión (puede ser nula si no ha sido impreso)
    }
}
