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
                // Usar un icono gen�rico del sistema
                //Icon = SystemIcons.Information, // Este es un icono gen�rico que viene con Windows
                Icon = new Icon("Resources/favicon.ico"),
                Visible = true,
                Text = "Servicio de impresi�n en ejecuci�n"
            };

            // Opcionalmente, agregar un men� contextual al icono
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Salir", null, (sender, e) => Application.Exit());
            notifyIcon.ContextMenuStrip = contextMenu;

            // Mantener la aplicaci�n en ejecuci�n
            Application.Run();
        }
    }
}