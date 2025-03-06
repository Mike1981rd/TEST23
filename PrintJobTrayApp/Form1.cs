using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ServiceProcess;
using Microsoft.Win32;


namespace PrintJobTrayApp
{
    public partial class Form1 : Form
    {


        public Form1()
        {
            InitializeComponent();

            // Ocultar el formulario y solo mostrar el icono en la bandeja
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            notifyIcon1.Visible = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetStartup();
            UpdateServiceStatus();
        }


        private void SetStartup()
        {
            string appName = "PrintJobTrayApp";
            string exePath = Application.ExecutablePath;

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            key.SetValue(appName, exePath);
        }

        private void UpdateServiceStatus()
        {
            try
            {
                ServiceController service = new ServiceController("PrintJobService");

                if (service.Status == ServiceControllerStatus.Running)
                {
                    notifyIcon1.Text = "Print Job Service - Ejecutándose";
                }
                else
                {
                    notifyIcon1.Text = "Print Job Service - Detenido";
                }
            }
            catch
            {
                notifyIcon1.Text = "Print Job Service - No Instalado";
            }
        }

      

        private void iniciarServicioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ServiceController service = new ServiceController("PrintJobService");
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                    notifyIcon1.ShowBalloonTip(3000, "Servicio Iniciado", "Print Job Service está en ejecución.", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al iniciar el servicio: " + ex.Message);
            }
            UpdateServiceStatus();
        }

        private void salirToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void detenerServicioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ServiceController service = new ServiceController("PrintJobService");
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                    notifyIcon1.ShowBalloonTip(3000, "Servicio Detenido", "Print Job Service ha sido detenido.", ToolTipIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al detener el servicio: " + ex.Message);
            }
            UpdateServiceStatus();

        }
    }
}
