namespace Icons
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Crear un NotifyIcon para la bandeja del sistema
            NotifyIcon notifyIcon = new NotifyIcon
            {
                // Usar un icono genérico del sistema
                //Icon = SystemIcons.Information, // Este es un icono genérico que viene con Windows
                Icon = new Icon("Resources/favicon.ico"),
                Visible = true,
                Text = "Servicio de impresión en ejecución"
            };

            // Opcionalmente, agregar un menú contextual al icono
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Salir", null, (sender, e) => Application.Exit());
            notifyIcon.ContextMenuStrip = contextMenu;

            // Mantener la aplicación en ejecución
            Application.Run();
        }
    }
}