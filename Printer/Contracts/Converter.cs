using DinkToPdf.EventDefinitions;
using DinkToPdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Printer.Contracts
{
    public interface IConverter
    {
        byte[] Convert(HtmlToPdfDocument doc);  // Método para convertir HTML a PDF
        event EventHandler<WarningArgs> Warning; // Definición del evento

        event EventHandler<PhaseChangedArgs> PhaseChanged;  // Evento de cambio de fase
        event EventHandler<ProgressChangedArgs> ProgressChanged;  // Evento de progreso
        event EventHandler<FinishedArgs> Finished;  // Evento de finalización
        event EventHandler<ErrorArgs> Error;  // Evento de error

      
    }
}
