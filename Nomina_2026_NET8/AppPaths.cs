// AppPaths.cs — VERSION LIMPIA
using System;
using System.IO;

namespace Nomina_2026_NET8
{
    public static class AppPaths
    {
        // ── Rutas fijas ───────────────────────────────────────────────
        public static readonly string Raiz = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NominaEmpresarial2026");

        public static readonly string BackupNominas = Path.Combine(Raiz, "BackUp Nóminas");
        public static readonly string Tarifas = Path.Combine(Raiz, "Tarifas");
        public static readonly string AcumuladosBackUp = Path.Combine(Raiz, "AcumuladosBackUp");

        // ── Año fiscal activo ─────────────────────────────────────────
        public static int AnoFiscalActivo
        {
            get
            {
                DateTime hoy = DateTime.Today;
                return (hoy.Month == 1 && hoy.Day <= 15) ? hoy.Year - 1 : hoy.Year;
            }
        }

        // ── Tarifas dinámicas ─────────────────────────────────────────
        public static string RutaTablasTarifa
        {
            get
            {
                string sufijo = (AnoFiscalActivo % 100).ToString("D2");
                return Path.Combine(Tarifas, $"TARIFA{sufijo}.dat");
            }
        }

        // Usada por TablasGenerator para leer binarios VB6
        public static string CarpetaVB6ParaAnio(int anio)
        {
            string sufijo = (anio % 100).ToString("D2");
            return $@"C:\TARIFA{sufijo}";
        }

        // 🗑️ RutaTarifaParaAnio → ELIMINADA (nadie la llama)
    }
}