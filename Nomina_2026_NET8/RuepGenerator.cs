using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nomina_2026_NET8
{
    public class RuepGenerator
    {
        private const string OUTPUT_FILE = "RUEP.dat";

        // Encoding registrado una sola vez — necesario en .NET 8
        //private static readonly Encoding LATIN1;
        private static readonly Encoding LATIN1 = Encoding.Latin1;

        // Tamaños de registro VB6
        private const int EmpresaSize = 92;   // 60 + 2 + 8 + 8 + 14
        private const int EmpcompSize = 235;  // 25+70+20+20+20+25+25+4+12+2+12
        private const int PersonalSize = 152;  // 20+20+20+18+18+12+12+(8×4)
        private const int OtrosSize = 120;  // 30 × 4 campos
        private const int CfdiSize = 498;  // 100+100+100+64+64+6+64
        private const int BanamexSize = 16;
        private const int MaestroSize = 246;  // Type ob — ver ReadMaestroObras
        //private const int NomSize = 192;  // 8 × 24 Currency

        public (string Nombre, int AnoFiscal, decimal SalarioMinimo, decimal Uma) LeerEncabezadoEmpresa(string rutaTrabajo)
        {
            string rutaEmpresa = Path.Combine(rutaTrabajo, "empresa.dno");
            if (!File.Exists(rutaEmpresa))
                throw new FileNotFoundException("Archivo requerido no encontrado: " + rutaEmpresa);

            var empresaRaw = ReadLastRecord(rutaEmpresa, EmpresaSize) ?? throw new Exception("Error leyendo empresa.dno");

            string nombreEmpresa = DecodeField(empresaRaw, 0, 60);
            short anoFiscal = BitConverter.ToInt16(empresaRaw, 60);
            long umaPorDiaRaw = BitConverter.ToInt64(empresaRaw, 62);
            long salarioMinimoRaw = BitConverter.ToInt64(empresaRaw, 70);

            return (nombreEmpresa, anoFiscal, Math.Round(salarioMinimoRaw / 10000.0m, 2), Math.Round(umaPorDiaRaw / 10000.0m, 2)
            );
        }


        public void Generar(string rutaTrabajo)
        {
            if (!Directory.Exists(rutaTrabajo))
                throw new DirectoryNotFoundException(rutaTrabajo);

            string destino = Path.Combine(rutaTrabajo, "RUEP.dat");

            // No sobrescribir un expediente existente.
            if (File.Exists(destino))
                throw new IOException("Ya existe RUEP.dat. No se sobrescribirá.");

            // Los String * N de VB6 utilizan ANSI.
            Encoding ansi = Encoding.GetEncoding(1252);

            // ===========================================
            // LECTURA DE REGISTROS BINARIOS
            // ===========================================

            List<byte[]> Leer(string nombre, int tamano, bool obligatorio = true)
            {
                string ruta = Path.Combine(rutaTrabajo, nombre);

                if (!File.Exists(ruta))
                {
                    if (obligatorio)
                        throw new FileNotFoundException("Falta archivo obligatorio: " + nombre);

                    return new List<byte[]>();
                }

                byte[] datos = File.ReadAllBytes(ruta);

                if (datos.Length % tamano != 0)
                    throw new InvalidDataException($"{nombre}: tamaño de archivo incorrecto. No se continuará para evitar cruzar empleados.");

                var registros = new List<byte[]>();

                for (int i = 0; i < datos.Length; i += tamano)
                {
                    byte[] registro = new byte[tamano];
                    Buffer.BlockCopy(datos, i, registro, 0, tamano);
                    registros.Add(registro);
                }

                return registros;
            }

            byte[] Posicion(List<byte[]> lista, int indice)
            {
                return indice >= 0 && indice < lista.Count ? lista[indice] : null;
            }

            string Texto(byte[] registro, int inicio, int longitud)
            {
                if (registro == null)
                    return "";

                return ansi.GetString(registro, inicio, longitud).Trim(' ', '\0');
            }

            decimal Moneda(byte[] registro, int inicio)
            {
                if (registro == null)
                    return 0m;

                return BitConverter.ToInt64(registro, inicio) / 10000m;
            }

            string FechaPersonal(string fecha)
            {
                if (string.IsNullOrWhiteSpace(fecha) || fecha == "0" || fecha == "00000000" || fecha == "00/00/0000")
                    return "";

                return fecha;
            }

            string FechaEmpresa(string valor)
            {
                if (string.IsNullOrWhiteSpace(valor) || valor.Trim('0').Length == 0)
                    return "";

                if (DateTime.TryParseExact(valor, "ddMMyyyyHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
                {
                    return fecha.ToString("dd/MM/yyyy");
                }

                return valor;
            }

            int ObtenerIdEmpresa(string nombre)
            {
                string n = nombre.ToUpperInvariant();

                if (n.Contains("SACMAG")) return 1;
                if (n.Contains("COORDINADORA") || n.Contains("CORDINA")) return 2;
                if (n.Contains("SUPERVISA")) return 3;
                if (n.Contains("CONTROL")) return 4;
                if (n.Contains("SUPTER") || n.Contains("SUPT")) return 5;
                if (n.Contains("CONSULTE")) return 6;
                if (n.Contains("INGENIAL")) return 7;
                if (n.Contains("GEOAMBIENTE")) return 8;
                if (n.Contains("EPESA")) return 9;

                throw new InvalidDataException("Empresa no reconocida: " + nombre);
            }

            // ===========================================
            // ARCHIVOS DE EMPRESA
            // ===========================================

            var empresas = Leer("empresa.dno", 92);
            var complementos = Leer("Empcomp.dno", 235);
            var cfdiEmpresa = Leer("EMP_CFDI.DNO", 102);

            if (empresas.Count == 0 || complementos.Count == 0 || cfdiEmpresa.Count == 0)
            {
                throw new InvalidDataException("Faltan registros de configuración empresarial.");
            }

            // VB6 obtiene la última posición de empresa.dno
            // y la primera de los archivos complementarios.
            byte[] emp = empresas[empresas.Count - 1];
            byte[] comp = complementos[0];
            byte[] timbre = cfdiEmpresa[0];

            // ===========================================
            // ARCHIVOS DE EMPLEADOS
            // ===========================================

            var personal = Leer("personal.dno", 152);
            var otros = Leer("PerOtre.dno", 120);
            var domicilios = Leer("Perscfdi.dno", 498);
            var tarjetas = Leer("Bnxcla.dno", 16, false);

            if (personal.Count == 0)
                throw new InvalidDataException("personal.dno no contiene empleados.");

            // ===========================================
            // ESTRUCTURA GENERAL RUEP V9
            // ===========================================

            string nombreEmpresa = Texto(emp, 0, 60);

            JObject ruep = new JObject
            {
                ["idEmpresa"] = ObtenerIdEmpresa(nombreEmpresa),
                ["nombre"] = nombreEmpresa,
                ["anio_fiscal"] = BitConverter.ToInt16(emp, 60),
                ["uma_por_dia"] = Moneda(emp, 62),
                ["salario_minimo"] = Moneda(emp, 70),
                ["fecha_actualizacion"] = FechaEmpresa(Texto(emp, 78, 14)),
                ["modificado_por"] = "",
                ["datos_fiscales"] = new JObject
                {
                    ["rfc_empresa"] = Texto(comp, 0, 25),
                    ["direccion_empresa"] = Texto(comp, 25, 70),
                    ["registro_patronal_empresa"] = Texto(timbre, 16, 32),
                    ["regimen_fiscal"] = "",

                    ["representante_legal_empresa"] = new JObject
                    {
                        ["apellido_paterno"] = Texto(comp, 95, 20),
                        ["apellido_materno"] = Texto(comp, 115, 20),
                        ["nombre"] = Texto(comp, 135, 20),
                        ["rfc_representante_legal"] = Texto(comp, 155, 25),
                        ["curp_representante_legal"] = Texto(comp, 180, 25)
                    },

                    ["banamex"] = new JObject
                    {
                        ["sucursal"] = Texto(comp, 205, 4),
                        ["cuenta"] = Texto(comp, 209, 12),
                        ["cliente"] = Texto(comp, 223, 12)
                    }
                },

                ["cfdi_nomina"] = new JObject
                {
                    ["folio"] = BitConverter.ToInt32(timbre, 0),
                    ["serie"] = Texto(timbre, 4, 10),
                    ["consecutivo"] = BitConverter.ToInt16(timbre, 14),
                    ["riesgo_imss"] = Texto(timbre, 48, 20)
                },

                ["personal"] = new JArray()
            };

            JArray empleados = (JArray)ruep["personal"];

            // ===========================================
            // CONSTRUIR EMPLEADOS SEGÚN POSICIÓN FÍSICA
            // ===========================================

            int totalPosiciones = Math.Max(Math.Max(personal.Count, otros.Count), Math.Max(domicilios.Count, tarjetas.Count));

            for (int i = 0; i < totalPosiciones; i++)
            {
                byte[] p = Posicion(personal, i);
                byte[] o = Posicion(otros, i);
                byte[] d = Posicion(domicilios, i);
                byte[] b = Posicion(tarjetas, i);

                string nombre = Texto(p, 0, 20);
                string apellidoP = Texto(p, 20, 20);
                string apellidoM = Texto(p, 40, 20);
                string rfc = Texto(p, 60, 18);
                string nss = Texto(p, 78, 18);
                string curp = Texto(o, 0, 30);

                bool tieneIdentidad = false;

                foreach (string valor in new[] { nombre, apellidoP, apellidoM, rfc, nss })
                {
                    if (!string.IsNullOrWhiteSpace(valor) && valor.Trim('0').Length > 0)
                    {
                        tieneIdentidad = true;
                        break;
                    }
                }

                bool curpPlausible = Regex.IsMatch(curp, @"^[A-ZÑ&]{4}\d{6}[HM][A-Z]{5}[A-Z0-9]\d$", RegexOptions.IgnoreCase);

                if (!tieneIdentidad && !curpPlausible)
                    continue;

                string fechaAlta = FechaPersonal(Texto(p, 96, 12));
                string fechaBaja = FechaPersonal(Texto(p, 108, 12));
                string tarjeta = Texto(b, 0, 16);

                if (tarjeta.Trim('0').Length == 0)
                    tarjeta = "";

                string correo = Texto(d, 434, 64);
                bool correoEmpresarial = correo.IndexOf('@') >= 0 && correo.IndexOf('@') == correo.LastIndexOf('@') && correo.EndsWith("@grupo-sacmag.com.mx", StringComparison.OrdinalIgnoreCase);

                // Cada posición conserva su ID original.
                // No usar empleados.Count + 1.
                int idEmpleado = i + 1;

                JObject registro = new JObject
                {
                    ["idEmpleado"] = idEmpleado,
                    ["nombre"] = nombre,
                    ["apellido_paterno"] = apellidoP,
                    ["apellido_materno"] = apellidoM,
                    ["rfc"] = rfc,
                    ["nss_imss"] = nss,
                    ["fecha_alta"] = fechaAlta,
                    ["fecha_baja"] = fechaBaja,

                    ["ingresos"] = new JObject
                    {
                        ["sueldo"] = Moneda(p, 120),
                        ["viaticos"] = Moneda(p, 128),
                        ["otras"] = Moneda(p, 136),
                        ["integrado"] = Moneda(p, 144)
                    },

                    ["cfdi"] = new JObject
                    {
                        ["curp"] = curp,
                        ["direccion"] = Texto(d, 0, 100),
                        ["colonia"] = Texto(d, 100, 100),
                        ["ciudad"] = Texto(d, 200, 100),
                        ["estado"] = Texto(d, 300, 64),
                        ["delegacion"] = Texto(d, 364, 64),
                        ["cp"] = Texto(d, 428, 6),

                        ["referencias"] = "",
                        ["no_exterior"] = "",
                        ["no_interior"] = "",
                        ["tipo_vialidad"] = "",

                        ["correo_empresarial"] = correoEmpresarial ? correo : "",
                        ["correo_personal"] = correoEmpresarial ? "" : correo,
                        ["correo_empresa_predeterminado"] = correoEmpresarial
                    },

                    ["datos_personales"] = new JObject
                    {
                        ["numero_tarjeta_banamex"] = tarjeta,
                        ["telefono_fijo"] = "",
                        ["telefono_movil"] = "",
                        ["lada"] = "",
                        ["telefono_empresa"] = "",
                        ["extension"] = "",
                        ["actividad_economica"] = "",
                        ["regimen_fiscal"] = ""
                    },

                    ["historial_laboral"] = new JArray
                    {
                        new JObject
                        {
                            ["idPeriodo"] = 1,
                            ["tipo_movimiento"] = "MIGRACION_VB6",
                            ["fecha_alta"] = fechaAlta,
                            ["fecha_baja"] = fechaBaja,
                            ["id_empresa_origen"] = JValue.CreateNull(),
                            ["id_empleado_origen"] = JValue.CreateNull(),
                            ["respeta_antiguedad"] = JValue.CreateNull()
                        }
                    },

                    ["fecha_ultima_mod"] = "",
                    ["modificado_por"] = ""
                };

                empleados.Add(registro);
            }

            if (empleados.Count == 0)
                throw new InvalidDataException("No se encontraron empleados identificables.");

            // ===========================================
            // CIFRAR, VERIFICAR Y PUBLICAR RUEP
            // ===========================================

            string temporal = Path.Combine(rutaTrabajo, ".ruep_v9_" + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                CryptoService.SaveEncrypted(temporal, ruep, indented: true);

                // Comprobar que lo escrito se descifra bien.
                JObject verificado = CryptoService.LoadEncrypted<JObject>(temporal);
                JObject esperado = JObject.Parse(ruep.ToString(Newtonsoft.Json.Formatting.None));

                if (verificado == null || !JToken.DeepEquals(verificado, esperado))
                {
                    throw new InvalidDataException("La verificación del RUEP V9 falló.");
                }

                // File.Move sin overwrite impide sustituir accidentalmente un expediente existente.
                File.Move(temporal, destino);
            }
            finally
            {
                if (File.Exists(temporal))
                    File.Delete(temporal);
            }
        }

        // ── Helpers privados ──────────────────────────────────────────
        // Deriva el ID numérico de empresa a partir de la clave resuelta por BDSyncService. Elimina la lista _empresaIds duplicada.
        private static int ResolverIdDesdeClaveEmpresa(string clave)
        {
            return clave switch
            {
                "SACMAG" => 1,
                "CORDINA" => 2,
                "SUPERVISA" => 3,
                "CONTROL" => 4,
                "SUPTER" => 5,
                "SUPT" => 5,
                "CONSULTE" => 6,
                "INGENIAL" => 7,
                "GEOAMBIENTE" => 8,
                "EPESA" => 9,
                _ => 0
            };
        }

        private static List<byte[]> ReadAllRecords(string path, int recordSize)
        {
            var list = new List<byte[]>();
            var data = File.ReadAllBytes(path);
            for (int i = 0; i + recordSize <= data.Length; i += recordSize)
            {
                var rec = new byte[recordSize];
                Array.Copy(data, i, rec, 0, recordSize);
                list.Add(rec);
            }
            return list;
        }

        private static byte[] ReadLastRecord(string path, int recordSize)
        {
            var data = File.ReadAllBytes(path);
            if (data.Length < recordSize) return null;
            var rec = new byte[recordSize];
            Array.Copy(data, data.Length - recordSize, rec, 0, recordSize);
            return rec;
        }

        private static string DecodeField(byte[] source, int offset, int len)
        {
            if (offset + len > source.Length)
                len = Math.Max(0, source.Length - offset);
            var slice = new byte[len];
            Array.Copy(source, offset, slice, 0, len);
            string s = LATIN1.GetString(slice);
            int nul = s.IndexOf('\0');
            if (nul >= 0) s = s[..nul];
            return s.Trim();
        }

        private static decimal CurrencyVB6FromRaw(long raw)
        {
            return Math.Round(raw / 10000m, 4, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Decodifica un registro de maestro.dno (Type "ob", 246 bytes, hasta 20 slots
        /// O_n/por_n/im_n). Layout por slot: O_n(Integer,2) + por_n(Integer,2) + im_n(Currency,8)
        /// = 12 bytes — EXCEPTO el slot 9, donde O_9 es Currency (8 bytes) en vez de Integer,
        /// según la anomalía documentada en la guía de archivos .DNO:
        ///
        ///   slots 1-8:  offset 0..95   (8 × 12 bytes)
        ///   slot 9:     offset 96..113 (O_9 Currency=8, por_9 Integer=2, im_9 Currency=8 = 18 bytes)
        ///   slots 10-20: offset 114..245 (11 × 12 bytes)
        ///
        /// Total: 96 + 18 + 132 = 246 bytes ✔ coincide con el tamaño documentado.
        /// Se omiten los slots vacíos (obra=0 y porcentaje=0 e importe=0).
        /// </summary>
        private static List<Dictionary<string, object>> ReadMaestroObras(byte[] record)
        {
            var lista = new List<Dictionary<string, object>>();
            if (record == null || record.Length < MaestroSize)
                return lista;

            int offset = 0;

            for (int slot = 1; slot <= 20; slot++)
            {
                int obra;
                decimal porcentaje;
                decimal importe;

                if (slot == 9)
                {
                    // O_9 es Currency (8 bytes), no Integer — anomalía confirmada en la guía.
                    obra = (int)CurrencyVB6FromRaw(BitConverter.ToInt64(record, offset));
                    porcentaje = BitConverter.ToInt16(record, offset + 8);
                    importe = CurrencyVB6FromRaw(BitConverter.ToInt64(record, offset + 10));
                    offset += 18;
                }
                else
                {
                    obra = BitConverter.ToInt16(record, offset);
                    porcentaje = BitConverter.ToInt16(record, offset + 2);
                    importe = CurrencyVB6FromRaw(BitConverter.ToInt64(record, offset + 4));
                    offset += 12;
                }

                if (obra == 0 && porcentaje == 0m && importe == 0m)
                    continue;

                lista.Add(new Dictionary<string, object>
                {
                    ["obra"] = obra,
                    ["porcentaje"] = porcentaje,
                    ["importe"] = importe
                });
            }

            return lista;
        }
    }
}