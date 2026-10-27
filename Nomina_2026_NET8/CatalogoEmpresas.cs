using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nomina_2026_NET8
{
    public static class CatalogoEmpresas
    {
        private static readonly string RutaCatalogo = Path.Combine(AppPaths.Raiz, "_catalogo_empresas.txt");

        private static readonly string[] CatalogoInicial = { "SACMAG", "SUPTER", "SUPERVISA", "CORDINA", "EPESA", "GEOAMBIENTE", "CONSULTE", "CONTROL", "INGENIAL" };

        public static List<string> ObtenerNombresBase()
        {
            Directory.CreateDirectory(AppPaths.Raiz);

            if (!File.Exists(RutaCatalogo))
            {
                File.WriteAllLines(RutaCatalogo, CatalogoInicial, Encoding.UTF8);
                return CatalogoInicial.ToList();
            }

            return File.ReadAllLines(RutaCatalogo, Encoding.UTF8).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList();
        }

        public static void AgregarNombreBaseSiNoExiste(string nombreBase)
        {
            var actuales = ObtenerNombresBase();
            if (actuales.Contains(nombreBase, StringComparer.OrdinalIgnoreCase)) return;

            File.AppendAllLines(RutaCatalogo, new[] { nombreBase.Trim().ToUpperInvariant() }, Encoding.UTF8);
        }

        /// <summary>Aplica siempre la convención "BASE" o "BASE - SUCURSAL".</summary>
        public static string ConstruirNombreCarpeta(string nombreBase, string sucursal)
        {
            nombreBase = nombreBase.Trim().ToUpperInvariant();
            return string.IsNullOrWhiteSpace(sucursal) ? nombreBase : $"{nombreBase} - {sucursal.Trim().ToUpperInvariant()}";
        }
    }
}