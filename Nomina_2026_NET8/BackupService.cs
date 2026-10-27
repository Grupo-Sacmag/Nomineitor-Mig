using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Nomina_2026_NET8
{
    public static class BackupService
    {
        private const string MarcaNormales = "---- NÓMINAS NORMALES ----";
        private const string MarcaEspeciales = "---- NÓMINAS ESPECIALES ----";
        private static readonly System.Globalization.CultureInfo CulturaMx = new("es-MX");
        public static string CarpetaNominas(string nombreCarpetaEmpresa, int anio) => AppPaths.BackupNominas(nombreCarpetaEmpresa, anio);
        public static string CarpetaAcumulados(string nombreCarpetaEmpresa, int anio) => AppPaths.AcumuladosBackUp(nombreCarpetaEmpresa, anio);

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

        public static void RespaldarNomina(string rutaJson, string nombreCarpetaEmpresa, int anio) => Respaldar(rutaJson, CarpetaNominas(nombreCarpetaEmpresa, anio));
        public static string GuardarAcumuladoHTML(string contenidoHtml, string nombreSugerido, string nombreCarpetaEmpresa, int anio)
        {
            try
            {
                string carpeta = CarpetaAcumulados(nombreCarpetaEmpresa, anio);
                Directory.CreateDirectory(carpeta);
                string ruta = Path.Combine(carpeta, nombreSugerido);
                File.WriteAllText(ruta, contenidoHtml, Encoding.UTF8);
                return ruta;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupService.GuardarAcumuladoHTML] {ex.Message}");
                return null;
            }
        }

        public static void RegistrarNomina(string tipoNomina, string nombreArchivo, decimal totalPagado,
            string nombreCarpetaEmpresa, int anio, string motivo = null)
        {
            try
            {
                string carpetaAnio = AppPaths.CarpetaEmpresaAnio(nombreCarpetaEmpresa, anio);
                Directory.CreateDirectory(carpetaAnio);
                string rutaBitacora = AppPaths.Bitacora(nombreCarpetaEmpresa, anio);

                var normales = new List<string>();
                var especiales = new List<string>();

                if (File.Exists(rutaBitacora))
                {
                    bool enNormales = false, enEspeciales = false;
                    foreach (var linea in File.ReadAllLines(rutaBitacora, Encoding.UTF8))
                    {
                        string t = linea.Trim();
                        if (t == MarcaNormales) { enNormales = true; enEspeciales = false; continue; }
                        if (t == MarcaEspeciales) { enNormales = false; enEspeciales = true; continue; }
                        if (t.StartsWith("====") || string.IsNullOrWhiteSpace(t)) continue;

                        if (enNormales) normales.Add(linea);
                        else if (enEspeciales) especiales.Add(linea);
                    }
                }

                string fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string equipo = Environment.MachineName;
                string montoFmt = totalPagado.ToString("C2", CulturaMx);

                string nuevaLinea = tipoNomina.Equals("Normal", StringComparison.OrdinalIgnoreCase) ? $"{fecha} | Equipo: {equipo} | Archivo: {nombreArchivo} | Total pagado: {montoFmt}"
                    : $"{fecha} | Equipo: {equipo} | Archivo: {nombreArchivo} | Motivo: {motivo} | Total pagado: {montoFmt}";

                if (tipoNomina.Equals("Normal", StringComparison.OrdinalIgnoreCase))
                    normales.Add(nuevaLinea);
                else
                    especiales.Add(nuevaLinea);

                var sb = new StringBuilder();
                sb.AppendLine("==================== BITÁCORA DE NÓMINAS ====================");
                sb.AppendLine();
                sb.AppendLine(MarcaNormales);
                foreach (var l in normales) sb.AppendLine(l);
                sb.AppendLine();
                sb.AppendLine(MarcaEspeciales);
                foreach (var l in especiales) sb.AppendLine(l);

                File.WriteAllText(rutaBitacora, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupService.RegistrarNomina] {ex.Message}");
            }
        }
    }
}