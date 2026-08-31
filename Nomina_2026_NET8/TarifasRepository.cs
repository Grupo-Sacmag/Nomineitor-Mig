// TarifasRepository.cs — VERSION LIMPIA para .NET 8
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nomina_2026_NET8
{
    /// <summary>
    /// Repositorio para leer y escribir TablasTarifa.dat (cifrado AES-256-CBC).
    /// Ruta: C:\ProgramData\NominaEmpresarial2026\Tarifas\TARIFA##.dat
    /// </summary>
    public static class TarifasRepository
    {
        // Propiedad delegada — recalcula el año cada vez que se llama
        public static string Ruta => AppPaths.RutaTablasTarifa;

        // ── Obtener ──────────────────────────────────────────────────────
        public static TablasTarifaRoot Obtener()
        {
            AsegurarDirectorio();

            if (!File.Exists(Ruta))
                throw new FileNotFoundException($"No se encontró TablasTarifa.dat.\nRuta esperada: {Ruta}\n\nContacta con el equipo de desarrollo para asistencia.");

            byte[] raw = File.ReadAllBytes(Ruta);
            string json = CryptoService.Decrypt(raw);

            if (string.IsNullOrEmpty(json))
                throw new InvalidDataException("No se pudo descifrar TablasTarifa.dat.");

            var modelo = JsonConvert.DeserializeObject<TablasTarifaRoot>(json);
            ValidarTablas(modelo);
            return modelo;
        }

        // ── Guardar ──────────────────────────────────────────────────────
        public static void Guardar(TablasTarifaRoot modelo)
        {
            AsegurarDirectorio();

            string json = JsonConvert.SerializeObject(modelo, Formatting.Indented);
            byte[] cipher = CryptoService.Encrypt(json, out byte[] iv);

            // Construir IV[16] + cipher usando spans (.NET 8)
            byte[] ivPlusCipher = new byte[iv.Length + cipher.Length];
            iv.CopyTo(ivPlusCipher, 0);
            cipher.CopyTo(ivPlusCipher, iv.Length);

            File.WriteAllBytes(Ruta, ivPlusCipher);
        }

        // 🗑️ ExportarJsonPlano → ELIMINADO (nadie lo llama)
        // 🗑️ ImportarJsonPlano → ELIMINADO (nadie lo llama)

        // ── Validación ───────────────────────────────────────────────────
        private static void ValidarTablas(TablasTarifaRoot m)
        {
            if (m?.tablas == null)
                throw new InvalidDataException(
                    "TablasTarifa.dat: estructura inválida.");

            var faltantes = new List<string>();

            if (m.tablas.ISR_113 == null || m.tablas.ISR_113.Count == 0) faltantes.Add("ISR_113");
            if (m.tablas.SUBSIDIO_114 == null || m.tablas.SUBSIDIO_114.Count == 0) faltantes.Add("SUBSIDIO_114");
            if (m.tablas.ISR_MENSUAL == null || m.tablas.ISR_MENSUAL.Count == 0) faltantes.Add("ISR_MENSUAL");
            if (m.tablas.SUBSIDIO_MENSUAL == null || m.tablas.SUBSIDIO_MENSUAL.Count == 0) faltantes.Add("SUBSIDIO_MENSUAL"); // ← agregado
            if (m.tablas.ISR_ANUAL_117 == null || m.tablas.ISR_ANUAL_117.Count == 0) faltantes.Add("ISR_ANUAL_117");

            if (faltantes.Count > 0)
                throw new InvalidDataException(
                    $"TablasTarifa.dat: tablas vacías o faltantes: " +
                    $"{string.Join(", ", faltantes)}\n" +
                    "Regenera el archivo con TablasGenerator.");
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private static void AsegurarDirectorio()
        {
            // CreateDirectory no falla si ya existe — no necesitamos el if
            Directory.CreateDirectory(Path.GetDirectoryName(Ruta)!);
        }
    }
}

public class TablasTarifaRoot
{
    public int anio { get; set; }
    public TablasInternas tablas { get; set; }
}

public class TablasInternas
{
    /// <summary>Tab08Kin.ISR  — ISR quincenal (Art.113). ISPT primera quincena.</summary>
    public List<RangoTarifa> ISR_113 { get; set; }

    /// <summary>Tab08kin.SUB — Subsidio quincenal. Primera quincena.</summary>
    public List<RangoTarifa> SUBSIDIO_114 { get; set; }

    /// <summary>
    /// Tab08Mes.ISR — Segunda quincena.
    /// Se busca el ingreso QUINCENAL aquí y el resultado ES el ISPT directo.
    /// </summary>
    public List<RangoTarifa> ISR_MENSUAL { get; set; }

    /// <summary>
    /// Tab08Mes.SUB — Subsidio al empleo segunda quincena.
    /// Valores = exactamente 2× los de SUBSIDIO_114 (quincenal).
    /// limSup=$11,492.66  cuotaFija=$536.22
    /// </summary>
    public List<RangoTarifa> SUBSIDIO_MENSUAL { get; set; }

    /// <summary>ISR177.03 — ISR anual (Art.117). Ajuste diciembre.</summary>
    public List<RangoTarifa> ISR_ANUAL_117 { get; set; }
}

public class RangoTarifa
{
    public decimal limInf { get; set; }
    public decimal limSup { get; set; }
    public decimal cuotaFija { get; set; }
    public decimal porcentaje { get; set; }
}
