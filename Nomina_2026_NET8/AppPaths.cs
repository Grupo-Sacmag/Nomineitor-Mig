using System;
using System.IO;

namespace Nomina_2026_NET8
{
    public static class AppPaths
    {
        // ── Raíz ──────────────────────────────────────────────────────
        public static readonly string Raiz = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NominaEmpresarial_ENHACED");

        // ── Tarifas — compartida para todo el programa, NO por empresa ──
        public static readonly string Tarifas = Path.Combine(Raiz, "TARIFAS");

        // ── Año fiscal activo ─────────────────────────────────────────
        public static int AnoFiscalActivo
        {
            get
            {
                DateTime hoy = DateTime.Today;
                return (hoy.Month == 1 && hoy.Day <= 15) ? hoy.Year - 1 : hoy.Year;
            }
        }

        // ── Tabla de tarifa vigente (compartida, sin depender de empresa) ─
        public static string RutaTablasTarifa
        {
            get
            {
                string sufijo = (AnoFiscalActivo % 100).ToString("D2");
                return Path.Combine(Tarifas, $"TARIFA{sufijo}.dat");
            }
        }

        // ── Carpetas por empresa + año ───────────────────────────────────
        public static string CarpetaEmpresa(string nombreCarpetaEmpresa) => Path.Combine(Raiz, nombreCarpetaEmpresa);
        public static string CarpetaEmpresaAnio(string nombreCarpetaEmpresa, int anio) => Path.Combine(CarpetaEmpresa(nombreCarpetaEmpresa), anio.ToString());
        public static string BackupNominas(string nombreCarpetaEmpresa, int anio) => Path.Combine(CarpetaEmpresaAnio(nombreCarpetaEmpresa, anio), "BackUp Nóminas");
        public static string AcumuladosBackUp(string nombreCarpetaEmpresa, int anio) => Path.Combine(CarpetaEmpresaAnio(nombreCarpetaEmpresa, anio), "AcumuladosBackup");
        public static string Bitacora(string nombreCarpetaEmpresa, int anio) => Path.Combine(CarpetaEmpresaAnio(nombreCarpetaEmpresa, anio), "Bitacora.txt");

        // Usada por TablasGenerator para leer binarios VB6 (sin cambios)
        public static string CarpetaVB6ParaAnio(int anio)
        {
            string sufijo = (anio % 100).ToString("D2");
            return $@"C:\TARIFA{sufijo}";
        }
    }
}