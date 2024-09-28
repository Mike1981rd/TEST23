using DinkToPdf.Contracts;
using DinkToPdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using DinkToPdf.EventDefinitions;
using Printer.Contracts;

namespace Printer.Converters
{
    public class PdfConverter : DinkToPdf.Contracts.IConverter
    {
        private readonly DinkToPdf.Contracts.IConverter _converter;

        public PdfConverter()
        {
            _converter = new SynchronizedConverter(new PdfTools());
        }

        // Implementación del método Convert que acepta IDocument
        public byte[] Convert(IDocument document)
        {
            // Aquí puedes agregar lógica para invocar eventos de progreso, etc.
            return _converter.Convert(document);
        }

        // Implementación de eventos
        public event EventHandler<PhaseChangedArgs> PhaseChanged;
        public event EventHandler<ProgressChangedArgs> ProgressChanged;
        public event EventHandler<FinishedArgs> Finished;
        public event EventHandler<ErrorArgs> Error;

        // Implementación de la propiedad Warning
        public event EventHandler<WarningArgs> Warning;

        // Métodos para invocar eventos
        protected void OnPhaseChanged(PhaseChangedArgs e)
        {
            PhaseChanged?.Invoke(this, e);
        }

        protected void OnProgressChanged(ProgressChangedArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }

        protected void OnFinished(FinishedArgs e)
        {
            Finished?.Invoke(this, e);
        }

        protected void OnError(ErrorArgs e)
        {
            Error?.Invoke(this, e);
        }
    }
}
