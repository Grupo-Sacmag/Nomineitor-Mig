// RuepGenerator.cs — VERSION LIMPIA para .NET 8
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
                throw new DirectoryNotFoundException("La ruta de trabajo no existe.");

            // ── Verificar archivos requeridos ─────────────────────────
            var archivos = new Dictionary<string, string>
            {
                ["empresa"] = Path.Combine(rutaTrabajo, "empresa.dno"),
                ["empcomp"] = Path.Combine(rutaTrabajo, "Empcomp.dno"),
                ["personal"] = Path.Combine(rutaTrabajo, "personal.dno"),
                ["perotros"] = Path.Combine(rutaTrabajo, "PerOtre.dno"),
                ["perscfdi"] = Path.Combine(rutaTrabajo, "Perscfdi.dno"),
                ["banamex"] = Path.Combine(rutaTrabajo, "Bnxcla.dno"),
            };

            foreach (var k in new[] { "empresa", "empcomp", "personal", "perotros", "perscfdi", "banamex" })
                if (!File.Exists(archivos[k]))
                    throw new FileNotFoundException("Archivo requerido no encontrado: " + archivos[k]);

            // maestro.dno es OPCIONAL: según la guía no afecta sueldo/ISR/IMSS, así que
            // su ausencia no debe bloquear la generación del RUEP — simplemente los
            // empleados quedan sin distribución por obra.
            string rutaMaestro = Path.Combine(rutaTrabajo, "maestro.dno");

            // ── Empresa ───────────────────────────────────────────────
            var empresaRaw = ReadLastRecord(archivos["empresa"], EmpresaSize) ?? throw new Exception("Error leyendo empresa.dno");

            string nombreEmpresa = DecodeField(empresaRaw, 0, 60);
            short anoFiscal = BitConverter.ToInt16(empresaRaw, 60);
            long umaPorDiaRaw = BitConverter.ToInt64(empresaRaw, 62);
            long salarioMinimoRaw = BitConverter.ToInt64(empresaRaw, 70);
            string fechaActualizacion = DecodeField(empresaRaw, 78, 14);

            // 🔧 ResolverIdEmpresa eliminado — derivamos desde BDSyncService
            string claveEmpresa = BDSyncService.ResolverClaveEmpresa(nombreEmpresa);
            int idEmpresa = ResolverIdDesdeClaveEmpresa(claveEmpresa);

            // ── Empresa complemento ───────────────────────────────────
            var empcompRaw = ReadLastRecord(archivos["empcomp"], EmpcompSize);
            object datosFiscales = null;

            if (empcompRaw != null)
            {
                datosFiscales = new Dictionary<string, object>
                {
                    ["rfc_empresa"] = DecodeField(empcompRaw, 0, 25),
                    ["registro_patronal"] = "",     // 🗑️ duplicado eliminado
                    ["lugar_expedicion"] = "",
                    ["regimen_fiscal"] = "",
                    ["direccion"] = DecodeField(empcompRaw, 25, 70),
                    ["representante_legal"] = new Dictionary<string, object>
                    {
                        ["apellido_paterno"] = DecodeField(empcompRaw, 95, 20),
                        ["apellido_materno"] = DecodeField(empcompRaw, 115, 20),
                        ["nombre"] = DecodeField(empcompRaw, 135, 20),
                        ["rfc_representante"] = DecodeField(empcompRaw, 155, 25),
                        ["curp"] = DecodeField(empcompRaw, 180, 25)
                    },
                    ["banamex"] = new Dictionary<string, object>
                    {
                        ["sucursal"] = DecodeField(empcompRaw, 205, 4),
                        ["cuenta"] = DecodeField(empcompRaw, 209, 12),
                        ["cliente"] = DecodeField(empcompRaw, 223, 12)
                    }
                };
            }

            // ── Leer listas de registros ──────────────────────────────
            var personalList = ReadAllRecords(archivos["personal"], PersonalSize);
            var otrosList = ReadAllRecords(archivos["perotros"], OtrosSize);
            var cfdiList = ReadAllRecords(archivos["perscfdi"], CfdiSize);
            var bnxList = ReadAllRecords(archivos["banamex"], BanamexSize);
            var maestroList = File.Exists(rutaMaestro) ? ReadAllRecords(rutaMaestro, MaestroSize) : new List<byte[]>();

            // ── Construir empleados ───────────────────────────────────
            var empleados = new List<Dictionary<string, object>>();

            for (int i = 0; i < personalList.Count; i++)
            {
                var per = personalList[i];

                string nombre = DecodeField(per, 0, 20);
                string ap_p = DecodeField(per, 20, 20);
                string ap_m = DecodeField(per, 40, 20);
                string rfc = DecodeField(per, 60, 18);
                string imss = DecodeField(per, 78, 18);
                string fechaAlta = DecodeField(per, 96, 12);
                string fechaBaja = DecodeField(per, 108, 12);

                if (string.IsNullOrWhiteSpace(nombre) &&
                    string.IsNullOrWhiteSpace(ap_p)) continue;

                long sueldoRaw = BitConverter.ToInt64(per, 120);
                long viaticosRaw = BitConverter.ToInt64(per, 128);
                long otrasRaw = BitConverter.ToInt64(per, 136);

                decimal sueldo = CurrencyVB6FromRaw(sueldoRaw);
                decimal viaticos = CurrencyVB6FromRaw(viaticosRaw);
                decimal otras = CurrencyVB6FromRaw(otrasRaw);

                string banamex = i < bnxList.Count ? DecodeField(bnxList[i], 0, 16) : "";

                string curp = i < otrosList.Count ? DecodeField(otrosList[i], 0, 30) : "";
                string cfdiDir = i < cfdiList.Count ? DecodeField(cfdiList[i], 0, 100) : "";
                string cfdiCol = i < cfdiList.Count ? DecodeField(cfdiList[i], 100, 100) : "";
                string cfdiCiu = i < cfdiList.Count ? DecodeField(cfdiList[i], 200, 100) : "";
                string cfdiEst = i < cfdiList.Count ? DecodeField(cfdiList[i], 300, 64) : "";
                string cfdiDel = i < cfdiList.Count ? DecodeField(cfdiList[i], 364, 64) : "";
                string cfdiCP = i < cfdiList.Count ? DecodeField(cfdiList[i], 428, 6) : "";
                string correo = i < cfdiList.Count ? DecodeField(cfdiList[i], 434, 64) : "";

                var obras = i < maestroList.Count ? ReadMaestroObras(maestroList[i]) : new List<Dictionary<string, object>>();

                empleados.Add(new Dictionary<string, object>
                {
                    ["idEmpleado"] = i + 1,
                    ["nombre"] = nombre,
                    ["apellido_paterno"] = ap_p,
                    ["apellido_materno"] = ap_m,
                    ["rfc"] = rfc,
                    ["imss"] = imss,
                    ["fecha_alta"] = fechaAlta,
                    ["fecha_baja"] = fechaBaja,
                    ["es_reingreso"] = false,
                    ["fecha_reingreso_alta"] = "",
                    ["fecha_reingreso_baja"] = "",
                    ["respeta_antiguedad"] = true,
                    ["estatus_laboral"] = string.IsNullOrWhiteSpace(fechaBaja) ? "ACTIVO" : "BAJA",
                    ["riesgo_imss"] = (object)null,
                    ["forma_pago_sat"] = (object)null,
                    ["departamento"] = (object)null,
                    ["puesto"] = (object)null,
                    ["tipo_contrato"] = (object)null,
                    ["tipo_jornada"] = (object)null,
                    // salario_integrado se deja SIN establecer (null): RuepService lo
                    // calcula la primera vez que se carga el RUEP y lo persiste de inmediato.
                    ["ingresos"] = new Dictionary<string, object>
                    {
                        ["sueldo"] = sueldo,
                        ["viaticos"] = viaticos,
                        ["otras"] = otras,
                    },
                    ["cfdi"] = new Dictionary<string, object>
                    {
                        ["curp"] = curp,
                        ["direccion"] = cfdiDir,
                        ["colonia"] = cfdiCol,
                        ["ciudad"] = cfdiCiu,
                        ["estado"] = cfdiEst,
                        ["delegacion"] = cfdiDel,
                        ["cp"] = cfdiCP,
                        ["referencias"] = "",
                        ["no_exterior"] = "",
                        ["no_interior"] = "",
                        ["correo_empresarial"] = correo,
                        ["correo_personal"] = "",
                        ["correo_empresa_predeterminado"] = true,
                        ["link_sat"] = ""
                    },
                    ["datos_personales"] = new Dictionary<string, object>
                    {
                        ["clabe_banamex"] = banamex,
                        ["telefono_fijo"] = "",
                        ["telefono_movil"] = "",
                        ["lada"] = "",
                        ["telefono_empresa"] = "",
                        ["extension"] = "",
                        ["actividad_economica"] = "",
                        ["regimen_fiscal"] = ""
                    },
                    ["obras"] = obras,
                    ["fecha_ultima_mod"] = "",
                    ["modificado_por"] = ""
                });
            }

            // ── Construir modelo completo ─────────────────────────────
            var root = new Dictionary<string, object>
            {
                ["idEmpresa"] = idEmpresa,
                ["nombre"] = nombreEmpresa,
                ["clave_empresa"] = claveEmpresa ?? "",
                ["ano_fiscal"] = anoFiscal,
                ["salario_minimo"] = Math.Round(salarioMinimoRaw / 10000m, 2, MidpointRounding.AwayFromZero),
                ["uma_por_dia"] = Math.Round(umaPorDiaRaw / 10000m, 2, MidpointRounding.AwayFromZero),
                ["fecha_actualizacion"] = fechaActualizacion,
                ["modificado_por"] = "",
                ["datos_fiscales"] = datosFiscales,
                ["personal"] = empleados
            };

            // 🔧 Cifrado delegado a CryptoService — elimina 50 líneas de AES
            //    manual y la constante AES_KEY_BASE64 duplicada
            string rutaRUEP = Path.Combine(rutaTrabajo, OUTPUT_FILE);
            CryptoService.SaveEncrypted(rutaRUEP, root, indented: true);
        }

        // ── Helpers privados ──────────────────────────────────────────

        /// <summary>
        /// Deriva el ID numérico de empresa a partir de la clave resuelta
        /// por BDSyncService. Elimina la lista _empresaIds duplicada.
        /// </summary>
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