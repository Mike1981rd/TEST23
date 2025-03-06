using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;


namespace PrintJobService
{
    public enum TextAlign
    {
        Left,
        Center,
        Right
    }

    public class TicketPrinter
    {
        private List<string> _lines = new List<string>();
        private Font _defaultFont = new Font("Arial", 9);
        private Image _logo;
        private string _printerName;

        public void SetPrinter(string printerName)
        {
            _printerName = printerName;
        }

        public void AddImage(Image logo)
        {
            _logo = logo;
        }

        public void AddLine(string text, Font font = null, TextAlign align = TextAlign.Left)
        {
            if (font == null)
            {
                font = _defaultFont;
            }
            _lines.Add(FormatText(text, align));
        }

        public void AddColumns(string[] texts, float[] widths, Font font, TextAlign[] aligns)
        {
            string formattedLine = "";
            for (int i = 0; i < texts.Length; i++)
            {
                string text = texts[i].PadRight((int)widths[i]);
                if (aligns[i] == TextAlign.Right)
                    text = text.PadLeft((int)widths[i]);
                else if (aligns[i] == TextAlign.Center)
                    text = text.PadLeft((int)(widths[i] / 2) + text.Length / 2);
                formattedLine += text;
            }
            _lines.Add(formattedLine);
        }

        public void AddEmptyLine()
        {
            _lines.Add("");
        }

        public void Print(string printerName)
        {
            _printerName = printerName;
            PrintDocument printDoc = new PrintDocument();
            printDoc.PrinterSettings.PrinterName = _printerName;
            printDoc.PrintPage += new PrintPageEventHandler(PrintPage);
            printDoc.Print();
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            float yPos = 10;
            if (_logo != null)
            {
                e.Graphics.DrawImage(_logo, new Rectangle(50, (int)yPos, 100, 50));
                yPos += 60;
            }
            foreach (var line in _lines)
            {
                e.Graphics.DrawString(line, _defaultFont, Brushes.Black, 10, yPos);
                yPos += 20;
            }
        }

        private string FormatText(string text, TextAlign align)
        {
            if (align == TextAlign.Center)
                return text.PadLeft((40 + text.Length) / 2);
            if (align == TextAlign.Right)
                return text.PadLeft(40);
            return text;
        }
    }
}
