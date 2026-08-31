// BackupService.cs — VERSION LIMPIA para .NET 8
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Nomina_2026_NET8
{
    public static class BackupService
    {
        public static string CarpetaNominas => AppPaths.BackupNominas;
        //public static string CarpetaAcumulados => Path.Combine(AppPaths.Raiz, "AcumuladosBackUp");

        public static string CarpetaAcumulados => AppPaths.AcumuladosBackUp;

        /// <summary>
        /// Copia un archivo al directorio indicado.
        /// Retorna la ruta destino, o null si falló.
        /// Silencioso — sin MessageBox.
        /// </summary>
        public static string Respaldar(string rutaOrigen, string carpetaDestino)
        {
            try
            {
                Directory.CreateDirectory(carpetaDestino);
                string destino = Path.Combine(carpetaDestino, Path.GetFileName(rutaOrigen));
                File.Copy(rutaOrigen, destino, overwrite: true);
                return destino;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupService.Respaldar] {ex.Message}");
                return null;
            }
        }

        /// <summary>Respaldo discreto de nómina (.json). Sin aviso al usuario.</summary>
        public static void RespaldarNomina(string rutaJson) => Respaldar(rutaJson, CarpetaNominas);

        /// <summary>
        /// Guarda contenido HTML en la carpeta de acumulados.
        /// Retorna la ruta guardada, o null si falló.
        /// </summary>
        public static string GuardarAcumuladoHTML(string contenidoHtml, string nombreSugerido)
        {
            try
            {
                Directory.CreateDirectory(CarpetaAcumulados);
                string ruta = Path.Combine(CarpetaAcumulados, nombreSugerido);
                File.WriteAllText(ruta, contenidoHtml, Encoding.UTF8);
                return ruta;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupService.GuardarAcumuladoHTML] {ex.Message}");
                return null;
            }
        }
    }
}