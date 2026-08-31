// TablasGenerator.cs — VERSION LIMPIA para .NET 8
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Nomina_2026_NET8
{
    public class TablasGenerator
    {
        // 4 campos Currency VB6 × 8 bytes cada uno = 32 bytes por registro
        private const int RECORD_SIZE = 32;

        private static readonly (string Archivo, string Clave)[] Mapeo =
        {
            ("Tab08Kin.ISR", "ISR_113"),
            ("Tab08kin.SUB", "SUBSIDIO_114"),
            ("Tab08Mes.ISR", "ISR_MENSUAL"),
            ("Tab08Mes.SUB", "SUBSIDIO_MENSUAL"),
            ("ISR177.03",    "ISR_ANUAL_117"),
        };

        public string Generar(int anio = 0)
        {
            // 🔧 Corregido: resolver anio ANTES de calcular sufijo
            if (anio == 0)
                anio = AppPaths.AnoFiscalActivo;

            string sufijo = (anio % 100).ToString("D2");
            string carpetaVB6 = AppPaths.CarpetaVB6ParaAnio(anio);

            TablasInternas tablas;
            string mensaje;

            if (Directory.Exists(carpetaVB6))
            {
                tablas = LeerTablasDesdeVB6(carpetaVB6);
                mensaje = $"Tablas leídas desde {carpetaVB6} y " + $"guardadas en TARIFA{sufijo}.dat.";
            }
            else
            {
                tablas = ObtenerTablasEmbebidas2026();
                mensaje = $"Se crearon las tablas embebidas 2026 y " + $"guardadas en TARIFA{sufijo}.dat.";
            }

            TarifasRepository.Guardar(
                new TablasTarifaRoot { anio = anio, tablas = tablas });

            return mensaje;
        }

        // ── Leer binarios VB6 ─────────────────────────────────────────────
        private TablasInternas LeerTablasDesdeVB6(string carpeta)
        {
            var tablas = new TablasInternas();

            foreach (var (archivo, clave) in Mapeo)
            {
                var registros = LeerTablaVB6(Path.Combine(carpeta, archivo), clave);

                switch (clave)
                {
                    case "ISR_113": tablas.ISR_113 = registros; break;
                    case "SUBSIDIO_114": tablas.SUBSIDIO_114 = registros; break;
                    case "ISR_MENSUAL": tablas.ISR_MENSUAL = registros; break;
                    case "SUBSIDIO_MENSUAL": tablas.SUBSIDIO_MENSUAL = registros; break;
                    case "ISR_ANUAL_117": tablas.ISR_ANUAL_117 = registros; break;
                }
            }

            return tablas;
        }

        // 🔧 Agrega nombre de tabla al parámetro para poder reportar qué falló
        private List<RangoTarifa> LeerTablaVB6(string rutaArchivo, string nombreTabla)
        {
            var lista = new List<RangoTarifa>();

            if (!File.Exists(rutaArchivo))
            {
                Debug.WriteLine($"[TablasGenerator] Archivo no encontrado: {rutaArchivo} " + $"(tabla: {nombreTabla}) — se usará tabla vacía.");
                return lista;
            }

            byte[] raw = File.ReadAllBytes(rutaArchivo);
            int total = raw.Length / RECORD_SIZE;

            for (int i = 0; i < total; i++)
            {
                int b = i * RECORD_SIZE;
                lista.Add(new RangoTarifa
                {
                    limInf = BitConverter.ToInt64(raw, b) / 10000m,
                    limSup = BitConverter.ToInt64(raw, b + 8) / 10000m,
                    cuotaFija = BitConverter.ToInt64(raw, b + 16) / 10000m,
                    porcentaje = BitConverter.ToInt64(raw, b + 24) / 10000m,
                });
            }

            return lista;
        }

        // ── Tablas embebidas 2026 ─────────────────────────────────────────
        private TablasInternas ObtenerTablasEmbebidas2026() => new()
        {
            ISR_113 = new List<RangoTarifa>
            {
                new() { limInf =      0.01m, limSup =     416.70m, cuotaFija =      0.00m, porcentaje =  1.92m },
                new() { limInf =    416.71m, limSup =    3537.15m, cuotaFija =      7.95m, porcentaje =  6.40m },
                new() { limInf =   3537.16m, limSup =    6216.15m, cuotaFija =    207.75m, porcentaje = 10.88m },
                new() { limInf =   6216.16m, limSup =    7225.95m, cuotaFija =    499.20m, porcentaje = 16.00m },
                new() { limInf =   7225.96m, limSup =    8651.40m, cuotaFija =    660.75m, porcentaje = 17.92m },
                new() { limInf =   8651.41m, limSup =   17448.75m, cuotaFija =    916.20m, porcentaje = 21.36m },
                new() { limInf =  17448.76m, limSup =   27501.60m, cuotaFija =   2795.25m, porcentaje = 23.52m },
                new() { limInf =  27501.61m, limSup =   52505.25m, cuotaFija =   5159.70m, porcentaje = 30.00m },
                new() { limInf =  52505.26m, limSup =   70006.95m, cuotaFija =  12660.75m, porcentaje = 32.00m },
                new() { limInf =  70006.96m, limSup =  210020.70m, cuotaFija =  18261.30m, porcentaje = 34.00m },
                new() { limInf = 210020.71m, limSup = 9999999.00m, cuotaFija =  65866.02m, porcentaje = 35.00m },
            },
            SUBSIDIO_114 = new List<RangoTarifa>
            {
                new() { limInf = 0.01m, limSup = 5746.33m, cuotaFija = 268.11m, porcentaje = 0m },
            },
            ISR_MENSUAL = new List<RangoTarifa>
            {
                new() { limInf =      0.01m, limSup =      844.59m, cuotaFija =       0.00m, porcentaje =  1.92m },
                new() { limInf =    844.60m, limSup =     7168.51m, cuotaFija =      16.22m, porcentaje =  6.40m },
                new() { limInf =   7168.52m, limSup =    12598.02m, cuotaFija =     420.95m, porcentaje = 10.88m },
                new() { limInf =  12598.03m, limSup =    14644.64m, cuotaFija =    1011.68m, porcentaje = 16.00m },
                new() { limInf =  14644.65m, limSup =    17533.64m, cuotaFija =    1339.14m, porcentaje = 17.92m },
                new() { limInf =  17533.65m, limSup =    35362.83m, cuotaFija =    1856.84m, porcentaje = 21.36m },
                new() { limInf =  35362.84m, limSup =    55736.68m, cuotaFija =    5665.16m, porcentaje = 23.52m },
                new() { limInf =  55736.69m, limSup =   106410.50m, cuotaFija =   10457.09m, porcentaje = 30.00m },
                new() { limInf = 106410.51m, limSup =   141880.66m, cuotaFija =   25659.23m, porcentaje = 32.00m },
                new() { limInf = 141880.67m, limSup =   425641.99m, cuotaFija =   37009.69m, porcentaje = 34.00m },
                new() { limInf = 425642.00m, limSup =  6000000.00m, cuotaFija =  133488.54m, porcentaje = 35.00m },
            },
            SUBSIDIO_MENSUAL = new List<RangoTarifa>
            {
                new() { limInf = 0.01m, limSup = 11492.66m, cuotaFija = 536.22m, porcentaje = 0m },
            },
            ISR_ANUAL_117 = new List<RangoTarifa>
            {
                new() { limInf =       0.01m, limSup =      10135.11m, cuotaFija =        0.00m, porcentaje =  1.92m },
                new() { limInf =   10135.12m, limSup =      86022.11m, cuotaFija =      194.59m, porcentaje =  6.40m },
                new() { limInf =   86022.12m, limSup =     151176.19m, cuotaFija =     5051.37m, porcentaje = 10.88m },
                new() { limInf =  151176.20m, limSup =     175735.66m, cuotaFija =    12140.13m, porcentaje = 16.00m },
                new() { limInf =  175735.67m, limSup =     210403.69m, cuotaFija =    16069.64m, porcentaje = 17.92m },
                new() { limInf =  210403.70m, limSup =     424353.97m, cuotaFija =    22282.14m, porcentaje = 21.36m },
                new() { limInf =  424353.98m, limSup =     668840.14m, cuotaFija =    67981.92m, porcentaje = 23.52m },
                new() { limInf =  668840.15m, limSup =    1276925.98m, cuotaFija =   125485.07m, porcentaje = 30.00m },
                new() { limInf = 1276925.99m, limSup =    1702567.97m, cuotaFija =   307910.81m, porcentaje = 32.00m },
                new() { limInf = 1702567.98m, limSup =    5107703.92m, cuotaFija =   444116.23m, porcentaje = 34.00m },
                new() { limInf = 5107703.93m, limSup =  999999999.00m, cuotaFija =  1601862.46m, porcentaje = 35.00m },
            },
        };
    }
}