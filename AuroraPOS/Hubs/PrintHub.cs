using Microsoft.AspNetCore.SignalR;

namespace AuroraPOS.Hubs
{
    public class PrintHub : Hub
    {
        public async Task SendPrintNotification(string printerName, string htmlContent)
        {
            // Enviar notificación a todos los clientes conectados que una impresión debe ser procesada
            await Clients.All.SendAsync("ReceivePrintNotification", printerName, htmlContent);
        }
    }
}
