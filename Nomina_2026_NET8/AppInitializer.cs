// AppInitializer.cs — VERSION LIMPIA para .NET 8
using System.IO;

namespace Nomina_2026_NET8
{
    public static class AppInitializer
    {
        /// <summary>
        /// Garantiza que todas las carpetas de la aplicación existen.
        /// Llamar una sola vez desde Program.cs al arrancar.
        /// Directory.CreateDirectory no falla si la carpeta ya existe.
        /// </summary>
        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(AppPaths.BackupNominas);
            Directory.CreateDirectory(AppPaths.Tarifas);
            Directory.CreateDirectory(AppPaths.AcumuladosBackUp); // 🔧 agregado
        }
    }
}