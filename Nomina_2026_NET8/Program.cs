// Program.cs
using NOMINA_2025;
using System;
using System.Text;
using System.Windows.Forms;

namespace Nomina_2026_NET8
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Necesario para leer ISO-8859-1 en RuepGenerator y archivos .dno
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ApplicationConfiguration.Initialize();

            // Garantizar carpetas antes de que cualquier Form escriba
            AppInitializer.EnsureDirectories();

            Application.Run(new FormPrincipal());
        }
    }
}