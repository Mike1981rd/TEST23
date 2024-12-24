using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;

public class TicketPrinter
{
    private List<PrintElement> elements = new List<PrintElement>();
    private Font defaultFont = new Font("Arial", 10);
    private float leftMargin = 10;
    private float topMargin = 10;
    private float lineSpacing = 5; // Espaciado entre líneas
    private float pageWidth = 300; // Ancho del papel o del ticket

    // Estructura para almacenar cada elemento de impresión
    public class PrintElement
    {
        public Action<Graphics, float> DrawAction { get; set; }
        public float Height { get; set; }
    }

    // Método auxiliar para calcular la altura de una línea de texto
    private float GetFontHeight(Font font)
    {
        return font.Size * 1.2f; // Factor de escala típico para la altura de la fuente
    }

    // Agregar una línea de texto con alineación
    public void AddLine(string text, Font font = null, TextAlign alignment = TextAlign.Left)
    {
        Font drawFont = font ?? defaultFont;
        elements.Add(new PrintElement
        {
            DrawAction = (g, yPos) =>
            {
                if (g == null) throw new ArgumentNullException(nameof(g), "El objeto Graphics no puede ser nulo.");
                SizeF textSize = g.MeasureString(text, drawFont);

                // Alineación del texto
                float xPos = leftMargin;
                if (alignment == TextAlign.Center)
                {
                    xPos = (pageWidth - textSize.Width) / 2;
                }
                else if (alignment == TextAlign.Right)
                {
                    xPos = pageWidth - textSize.Width - leftMargin;
                }

                g.DrawString(text, drawFont, Brushes.Black, xPos, yPos);
            },
            Height = GetFontHeight(drawFont) + lineSpacing
        });
    }

    // Agregar una imagen
    public void AddImage(Image image)
    {
        elements.Add(new PrintElement
        {
            DrawAction = (g, yPos) =>
            {
                if (g == null) throw new ArgumentNullException(nameof(g), "El objeto Graphics no puede ser nulo.");
                if (image == null) throw new ArgumentNullException(nameof(image), "La imagen no puede ser nula.");

                // Calcular el nuevo ancho de la imagen como el 50% del ancho de la página
                float newWidth = pageWidth * 0.5f;

                // Calcular la altura proporcional para mantener las proporciones originales
                float newHeight = (image.Height / image.Width) * newWidth;

                // Calcular la posición X para centrar la imagen
                float xPos = (pageWidth - newWidth) / 2;

                // Dibujar la imagen centrada y redimensionada en la posición X y Y proporcionada
                g.DrawImage(image, xPos, yPos, newWidth, newHeight);

                // Actualizar la posición Y después de imprimir la imagen
                yPos += newHeight + lineSpacing;
            },
            Height = (pageWidth * 0.5f * image.Height / image.Width) + lineSpacing  // La altura proporcional de la imagen más el espaciado
        });
    }




    public void AddColumns(string[] columns, float[] widths, Font font = null, TextAlign[] alignments = null)
    {
        Font drawFont = font ?? defaultFont;
        elements.Add(new PrintElement
        {
            DrawAction = (g, yPos) =>
            {
                if (g == null) throw new ArgumentNullException(nameof(g), "El objeto Graphics no puede ser nulo.");
                float xPos = leftMargin;

                for (int i = 0; i < columns.Length; i++)
                {
                    TextAlign alignment = alignments != null && alignments.Length > i ? alignments[i] : TextAlign.Left;
                    string column = columns[i];
                    SizeF textSize = g.MeasureString(column, drawFont);

                    // Truncar el texto si excede el ancho de la columna y agregar tres puntos
                    if (textSize.Width > widths[i])
                    {
                        // Calcular el número de caracteres que caben en el ancho de la columna
                        int maxChars = (int)(widths[i] / (textSize.Width / column.Length));
                        column = column.Substring(0, maxChars - 3) + "..."; // Truncar y agregar los tres puntos
                    }

                    // Volver a medir el tamaño del texto truncado
                    textSize = g.MeasureString(column, drawFont);

                    // Ajustar posición X según la alineación de cada columna
                    float columnX = xPos;
                    if (alignment == TextAlign.Center)
                    {
                        columnX += (widths[i] - textSize.Width) / 2;
                    }
                    else if (alignment == TextAlign.Right)
                    {
                        columnX += (widths[i] - textSize.Width); // Alineación a la derecha
                    }

                    // Dibujar el texto de la columna
                    g.DrawString(column, drawFont, Brushes.Black, columnX, yPos);

                    // Mover xPos al inicio de la siguiente columna
                    xPos += widths[i];
                }
            },
            Height = GetFontHeight(drawFont) + lineSpacing
        });
    }



    public void AddMultilineText(string text, Font font = null)
    {
        Font drawFont = font ?? defaultFont;
        float yPos = topMargin; // Empezamos en el margen superior
        int lineCount = 0; // Contador de líneas

        // Creamos un objeto Graphics para medir el texto
        using (Bitmap bmp = new Bitmap(1, 1))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            // Lista de líneas de texto
            var words = text.Split(' ');
            string currentLine = "";
            List<string> lines = new List<string>();

            // Dividir el texto en líneas
            foreach (var word in words)
            {
                SizeF wordSize = g.MeasureString(currentLine + " " + word, drawFont);
                if (wordSize.Width < pageWidth - leftMargin)
                {
                    currentLine += " " + word;
                }
                else
                {
                    lines.Add(currentLine);  // Añadir línea completa a la lista
                    currentLine = word;      // Empezar nueva línea
                }
            }

            // Añadir la última línea
            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            // Calcular la altura total del texto
            float totalHeight = lines.Count * (GetFontHeight(drawFont) + lineSpacing);

            // Agregar el PrintElement con el cálculo de la altura total
            elements.Add(new PrintElement
            {
                DrawAction = (g2, yPos2) =>
                {
                    if (g2 == null) throw new ArgumentNullException(nameof(g2), "El objeto Graphics no puede ser nulo.");
                    float xPos = leftMargin;

                    // Dibujar cada línea
                    foreach (var line in lines)
                    {
                        g2.DrawString(line, drawFont, Brushes.Black, xPos, yPos2);
                        yPos2 += GetFontHeight(drawFont) + lineSpacing;  // Avanzar a la siguiente línea
                    }
                },
                Height = totalHeight  // Establecer la altura total
            });
        }
    }





    // Agregar una línea vacía (espacio en blanco)
    public void AddEmptyLine()
    {
        elements.Add(new PrintElement
        {
            DrawAction = (g, yPos) => { /* No dibuja nada, solo agrega espacio */ },
            Height = lineSpacing
        });
    }

    // Imprimir el ticket
    public void Print(string printerName)
    {
        PrintDocument printDocument = new PrintDocument();
        printDocument.PrinterSettings.PrinterName = printerName;
        printDocument.PrintPage += new PrintPageEventHandler(PrintPage);
        printDocument.Print();
    }

    private void PrintPage(object sender, PrintPageEventArgs e)
    {
        Graphics g = e.Graphics;
        if (g == null)
        {
            throw new InvalidOperationException("El objeto Graphics es nulo en el evento PrintPage.");
        }

        float yPos = topMargin;

        foreach (var element in elements)
        {
            element.DrawAction(g, yPos);
            yPos += element.Height;
        }
    }
}

public enum TextAlign
{
    Left,
    Center,
    Right
}
