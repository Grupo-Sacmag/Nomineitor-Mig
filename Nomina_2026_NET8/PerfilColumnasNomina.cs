using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System;

namespace NOMINA_2025
{
    internal enum TipoNominaCaptura { Ordinaria, Liquidacion, Finiquito, Aguinaldo, PTU, Bono, Otra }

    internal static class PerfilColumnasNomina
    {
        // VB6: MARCA_SEPARACION_2026 (se guarda en .hstri). Currency = Int64 / 10,000.
        public const long MarcaSeparacion2026 = 20261010L;
        public const long MarcaSeparacion2026Raw = MarcaSeparacion2026 * 10000L;

        private static readonly Regex _reNormal = new Regex(@"^(ENE|FEB|MAR|ABR|MAY|JUN|JUL|AGO|SEP|OCT|NOV|DIC)[12]\d{4}$");

        public static string Normalizar(string texto)
        {
            string d = (texto ?? "").Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in d)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        }

        // PROVISIONAL: equivale a ClasificarTipoNomina de VB6 (aún no la he visto).
        public static TipoNominaCaptura Clasificar(string texto)
        {
            string n = Normalizar(texto);
            if (n.Length == 0 || _reNormal.IsMatch(n)) return TipoNominaCaptura.Ordinaria;
            if (n.Contains("LIQ")) return TipoNominaCaptura.Liquidacion;
            if (n.Contains("FINI")) return TipoNominaCaptura.Finiquito;
            if (n.Contains("AGUI")) return TipoNominaCaptura.Aguinaldo;
            if (n.Contains("PTU")) return TipoNominaCaptura.PTU;
            if (n.Contains("BONO") || n.Contains("PREMIO")) return TipoNominaCaptura.Bono;
            return TipoNominaCaptura.Otra;
        }

        public static bool EsSeparacion(TipoNominaCaptura t) => t == TipoNominaCaptura.Liquidacion || t == TipoNominaCaptura.Finiquito;

        // Encabezados por columna del grid para Liquidación / Finiquito.
        public static Dictionary<string, string> EncabezadosSeparacion(TipoNominaCaptura tipo, bool legacy)
        {
            if (legacy)
                return new Dictionary<string, string>
                {
                    ["hsNorm"] = "Compensaciones",
                    ["hsDobles"] = "P. Antigüedad",
                    ["hsTriples"] = "Indemnizaciones",
                    ["OF"] = "Sueldo Vacaciones"
                };

            bool fini = tipo == TipoNominaCaptura.Finiquito;
            return new Dictionary<string, string>
            {
                ["hsNorm"] = fini ? "Sdo. Vacaciones" : "Compensaciones",
                ["hsDobles"] = "Aguinaldo",
                ["hsTriples"] = "P. Antigüedad",
                ["OF"] = fini ? "Bono" : "Indemnización",
                ["PVacac"] = "P. Vacacional"
            };
        }

        // Nombre de archivo sin acentos ni espacios (evita "LIQUIDACIÓN.json").
        private static readonly string[] _mesesAbrev = { "ENE", "FEB", "MAR", "ABR", "MAY", "JUN", "JUL", "AGO", "SEP", "OCT", "NOV", "DIC" };

        // Columnas de captura visibles por tipo. Si quieres mostrar u ocultar alguna, se ajusta SOLO aquí.
        public static HashSet<string> ColumnasConcepto(TipoNominaCaptura tipo)
        {
            switch (tipo)
            {
                case TipoNominaCaptura.Aguinaldo:
                case TipoNominaCaptura.PTU:
                case TipoNominaCaptura.Otra:
                    return new HashSet<string> { "Otras", "PercExenta" };
                case TipoNominaCaptura.Bono:
                    return new HashSet<string> { "OF", "PercExenta" };
                default: // Ordinaria, Liquidación, Finiquito
                    return new HashSet<string> { "DiasTrabajados", "Salario", "hsNorm", "hsDobles", "hsTriples", "OF", "PVacac", "Otras", "PercExenta" };
            }
        }

        // Concepto estándar para el nombre de archivo (solo letras; BONO coincide con la palabra clave de VB6).
        public static string NombreArchivoMotivo(string motivo)
        {
            switch (Clasificar(motivo))
            {
                case TipoNominaCaptura.Liquidacion: return "LIQUIDACION";
                case TipoNominaCaptura.Finiquito: return "FINIQUITO";
                case TipoNominaCaptura.Aguinaldo: return "AGUINALDO";
                case TipoNominaCaptura.PTU: return "PTU";
                case TipoNominaCaptura.Bono: return "BONO";
                default:
                    string n = Normalizar(motivo);
                    if (n.Contains("VACAC")) return "PRIMAVACACIONAL";
                    return Regex.Replace(n, "[^A-Z]+", "");
            }
        }

        // NN + CONCEPTO + MES + AAAA  →  01FINIQUITOOCT2026 (NN = siguiente consecutivo libre)
        public static string SiguienteNombreEspecial(string rutaTrabajo, string concepto, int mes, int anio)
        {
            string sufijo = $"{concepto}{_mesesAbrev[Math.Clamp(mes, 1, 12) - 1]}{anio}";
            int max = 0;

            if (!string.IsNullOrWhiteSpace(rutaTrabajo) && Directory.Exists(rutaTrabajo))
            {
                var re = new Regex(@"^(\d{2,})" + Regex.Escape(sufijo) + "$", RegexOptions.IgnoreCase);
                foreach (string f in Directory.EnumerateFiles(rutaTrabajo, "*" + sufijo + ".json"))
                {
                    var m = re.Match(Path.GetFileNameWithoutExtension(f));
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int n) && n > max) max = n;
                }
            }
            return $"{max + 1:00}{sufijo}";
        }
    }
}