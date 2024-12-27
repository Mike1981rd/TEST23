using System;

using System.IO;
using System.Net;

namespace Printer.Services
{
    public class LogoService
    {
        private static readonly string LogoUrl = ""; // Cambia esto a la URL real del logo
        private static readonly string LocalLogoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");

        public static bool DownloadLogo()
        {
            try
            {
                // Verificar si el archivo ya existe y tiene menos de un día
                if (File.Exists(LocalLogoPath))
                {
                    var fileInfo = new FileInfo(LocalLogoPath);
                    if (fileInfo.LastWriteTime.Date == DateTime.Now.Date)
                    {
                        Console.WriteLine("El logo ya está actualizado.");
                        return true;
                    }
                }

                // Descargar el logo usando WebClient
                using (var webClient = new WebClient())
                {
                    Console.WriteLine("Descargando el logo...");
                    webClient.DownloadFile(LogoUrl, LocalLogoPath); // Guardar directamente el archivo
                    Console.WriteLine($"Logo descargado y guardado en {LocalLogoPath}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al descargar el logo: {ex.Message}");
                return false;
            }
        }
    }
}
