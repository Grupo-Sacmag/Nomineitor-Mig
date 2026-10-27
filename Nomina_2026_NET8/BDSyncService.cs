// BDSyncService.cs — VERSION LIMPIA para .NET 8
using Microsoft.Data.SqlClient;   // 🔧 cambiado de System.Data.SqlClient
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Nomina_2026_NET8
{
    public class SyncResult
    {
        public int Nuevos { get; set; }
        public int Actualizados { get; set; }
        public int SinCambios { get; set; }
        public List<string> Discrepancias { get; set; } = new();
        public bool HuboCambios => Nuevos > 0 || Actualizados > 0;

        public bool TieneDiscrepancias => Discrepancias.Count > 0;
        public string Resumen => $"Nuevos: {Nuevos} | Actualizados: {Actualizados} | Sin cambios: {SinCambios}";
    }

    public static class BDSyncService
    {
        // 🔧 Cadena de conexión leída desde Settings (no hardcodeada)
        // En appsettings o Properties.Settings.Default.ConnectionString
        private static string ConnStr => Properties.Settings.Default.ConnectionString;

        private const string SQL_SELECT = "SELECT idNomina, rfc, curp, nombre, apellidoP, apellidoM, falta, tipoVialidad, nombreDeVialidad, numeroExterior, numeroInterior, " +
            "nombreColonia, nombreLocalidad, entidadFederativa, nombreMunicipio, cp, entreCalle, yCalle, ladaTel, numeroTel, correoElectronico, actividadEconomica, regimen, estadoDomicilio, " +
            "estadoContDomicilio, link FROM datosSat WHERE empresa = @empresa ORDER BY apellidoP, apellidoM, nombre";

        private static readonly Dictionary<string, int> ClavesEmpresa =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "SACMAG",      1 },
                { "CORDINA",     2 },
                { "SUPERVISA",   3 },
                { "CONTROL",     4 },
                { "SUPTER",      5 },
                { "SUPT",        5 },
                { "CONSULTE",    6 },
                { "INGENIAL",    7 },
                { "GEOAMBIENTE", 8 },
                { "EPESA",       9 },
            };

        // ── API pública ───────────────────────────────────────────────────

        public static IReadOnlyList<string> ObtenerTodasLasClaves() => ClavesEmpresa.Keys.OrderBy(k => k).ToList().AsReadOnly();

        public static string ResolverClaveEmpresa(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto)) return null;

            string norm = nombreCompleto.Trim().ToUpperInvariant();

            if (ClavesEmpresa.ContainsKey(norm)) return norm;

            return ClavesEmpresa.Keys.Where(k => norm.Contains(k.ToUpperInvariant())).OrderByDescending(k => k.Length).ThenBy(k => ClavesEmpresa[k]).FirstOrDefault();
        }

        public static bool TestConexion(out string error)
        {
            error = null;
            try
            {
                using var con = new SqlConnection(ConnStr);
                con.Open();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static SyncResult SincronizarRUEP(string rutaRUEP)
        {
            var root = CryptoService.LoadEncrypted<RuepRoot>(rutaRUEP);

            if (string.IsNullOrWhiteSpace(root.clave_empresa))
            {
                string clave = ResolverClaveEmpresa(root.nombre);
                if (string.IsNullOrEmpty(clave))
                    throw new InvalidOperationException($"No se pudo identificar la empresa: \"{root.nombre}\"\n\nUsa 'Configurar clave BD' para asignarla manualmente.");

                root.clave_empresa = clave;
                CryptoService.SaveEncrypted(rutaRUEP, root, true);
            }

            // GEOAMBIENTE: exclusión total — ni lee ni escribe en BD.
            // Los IDs y datos correctos viven únicamente en el RUEP generado desde los archivos .dno. No hay sincronización posible.
            if (string.Equals(root.clave_empresa, "GEOAMBIENTE", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine("[BDSyncService] GEOAMBIENTE excluida de sincronización BD.");
                return new SyncResult();   // ← sale aquí, sin tocar la BD
            }

            // Para todas las demás empresas: flujo normal
            DataTable dt = ConsultarBD(root.clave_empresa);
            if (dt.Rows.Count == 0) return new SyncResult();

            var result = SincronizarPorRFC(root, dt, rutaRUEP);            

            return result;
        }

        public static void ActualizarEmpleadoEnBD(
            string claveEmpresa, int idEmpleado, RuepEmpleado emp)
        {
            const string SQL = "UPDATE datosSat SET rfc=@rfc, curp=@curp, nombre=@nombre, apellidoP=@apellidoP, apellidoM=@apellidoM, falta=@falta, tipoVialidad=@tipoVialidad, " +
                "nombreDeVialidad=@nombreDeVialidad, numeroExterior=@numeroExterior, numeroInterior=@numeroInterior, nombreColonia=@nombreColonia, entidadFederativa=@entidadFederativa, " +
                "nombreMunicipio=@nombreMunicipio, cp=@cp, estadoDomicilio=@estadoDomicilio, ladaTel=@ladaTel, numeroTel=@numeroTel, correoElectronico=@correoElectronico, " +
                "actividadEconomica=@actividadEconomica, regimen=@regimen, link=@link WHERE idNomina=@idNomina AND empresa=@empresa";

            EjecutarComando(SQL, claveEmpresa, idEmpleado, emp);
        }

        public static void InsertarEmpleadoEnBD(
            string claveEmpresa, int idEmpleado, RuepEmpleado emp)
        {
            const string SQL =
                "INSERT INTO datosSat (idNomina, empresa, rfc, curp, nombre, apellidoP, apellidoM, falta, tipoVialidad, nombreDeVialidad, numeroExterior, numeroInterior, nombreColonia, " +
                "entidadFederativa, nombreMunicipio, cp, estadoDomicilio, ladaTel, numeroTel, correoElectronico, actividadEconomica, regimen, link) VALUES (@idNomina, @empresa, @rfc, @curp, " +
                "@nombre, @apellidoP, @apellidoM, @falta, @tipoVialidad, @nombreDeVialidad, @numeroExterior, @numeroInterior, @nombreColonia, @entidadFederativa, @nombreMunicipio, " +
                "@cp, @estadoDomicilio, @ladaTel, @numeroTel, @correoElectronico, @actividadEconomica, @regimen, @link)";

            EjecutarComando(SQL, claveEmpresa, idEmpleado, emp);
        }

        public static List<string> VerificarDuplicadoEnBD(string claveEmpresa, string rfc, string curp)
        {
            var conflictos = new List<string>();
            if (string.IsNullOrWhiteSpace(claveEmpresa)) return conflictos;

            const string SQL = "SELECT nombre, apellidoP, apellidoM, rfc, curp FROM datosSat WHERE empresa=@empresa AND (@rfc <> '' AND rfc=@rfc OR @curp <> '' AND curp=@curp)";

            try
            {
                using var con = new SqlConnection(ConnStr);
                con.Open();
                using var cmd = new SqlCommand(SQL, con);
                cmd.Parameters.AddWithValue("@empresa", claveEmpresa);
                cmd.Parameters.AddWithValue("@rfc", rfc ?? "");
                cmd.Parameters.AddWithValue("@curp", curp ?? "");

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string nombre = $"{reader["apellidoP"]} {reader["apellidoM"]} " + $"{reader["nombre"]}".Trim();
                    string rfcBD = reader["rfc"].ToString().Trim();
                    string curpBD = reader["curp"].ToString().Trim();

                    if (!string.IsNullOrWhiteSpace(rfc) && string.Equals(rfcBD, rfc, StringComparison.OrdinalIgnoreCase))
                        conflictos.Add($"RFC '{rfc}' ya está registrado → {nombre}");

                    if (!string.IsNullOrWhiteSpace(curp) && string.Equals(curpBD, curp, StringComparison.OrdinalIgnoreCase))
                        conflictos.Add($"CURP '{curp}' ya está registrada → {nombre}");
                }
            }
            catch (Exception ex) when (ex is SqlException || ex is TimeoutException)
            {
                // Solo silenciamos errores de red/timeout, no bugs de SQL
            }

            return conflictos;
        }

        // ── Helpers privados ──────────────────────────────────────────────

        // UPDATE e INSERT ahora comparten este método en lugar de duplicar la apertura de conexión
        private static void EjecutarComando(
            string sql, string claveEmpresa, int idEmpleado, RuepEmpleado emp)
        {
            using var con = new SqlConnection(ConnStr);
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            MapearParametros(cmd, claveEmpresa, idEmpleado, emp);
            cmd.ExecuteNonQuery();
        }

        private static void MapearParametros(SqlCommand cmd, string claveEmpresa, int idEmpleado, RuepEmpleado emp)
        {
            var cfdi = emp.cfdi ?? new RuepCFDI();
            var dp = emp.datos_personales ?? new RuepDatosPersonales();

            string correo = !string.IsNullOrWhiteSpace(cfdi.correo_empresarial) ? cfdi.correo_empresarial : cfdi.correo_personal ?? "";

            cmd.Parameters.AddWithValue("@idNomina", idEmpleado);
            cmd.Parameters.AddWithValue("@empresa", claveEmpresa);
            cmd.Parameters.AddWithValue("@rfc", emp.rfc ?? "");
            cmd.Parameters.AddWithValue("@curp", cfdi.curp ?? "");
            cmd.Parameters.AddWithValue("@nombre", emp.nombre ?? "");
            cmd.Parameters.AddWithValue("@apellidoP", emp.apellido_paterno ?? "");
            cmd.Parameters.AddWithValue("@apellidoM", emp.apellido_materno ?? "");
            cmd.Parameters.AddWithValue("@falta", emp.fecha_alta ?? "");
            cmd.Parameters.AddWithValue("@tipoVialidad", "");
            cmd.Parameters.AddWithValue("@nombreDeVialidad", cfdi.direccion ?? "");
            cmd.Parameters.AddWithValue("@numeroExterior", cfdi.no_exterior ?? "");
            cmd.Parameters.AddWithValue("@numeroInterior", cfdi.no_interior ?? "");
            cmd.Parameters.AddWithValue("@nombreColonia", cfdi.colonia ?? "");
            cmd.Parameters.AddWithValue("@entidadFederativa", cfdi.estado ?? "");
            cmd.Parameters.AddWithValue("@nombreMunicipio", cfdi.delegacion ?? "");
            cmd.Parameters.AddWithValue("@cp", cfdi.cp ?? "");
            cmd.Parameters.AddWithValue("@estadoDomicilio", cfdi.estado_domicilio ?? "");
            cmd.Parameters.AddWithValue("@ladaTel", dp.lada ?? "");
            cmd.Parameters.AddWithValue("@numeroTel", dp.telefono_movil ?? "");
            cmd.Parameters.AddWithValue("@correoElectronico", correo);
            cmd.Parameters.AddWithValue("@actividadEconomica", dp.actividad_economica ?? "");
            cmd.Parameters.AddWithValue("@regimen", dp.regimen_fiscal ?? "");
            cmd.Parameters.AddWithValue("@link", cfdi.link_sat ?? "");
        }

        private static DataTable ConsultarBD(string empresa)
        {
            var dt = new DataTable();
            using var con = new SqlConnection(ConnStr);
            con.Open();
            using var cmd = new SqlCommand(SQL_SELECT, con);
            cmd.Parameters.AddWithValue("@empresa", empresa);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        private static bool ActualizarCampos(RuepEmpleado emp, DataRow fila, string ahora, SyncResult result)
        {
            string BD(string col) => fila.Table.Columns.Contains(col) && fila[col] != DBNull.Value ? fila[col].ToString().Trim().Replace("N/A", "").Trim() : "";

            emp.cfdi ??= new RuepCFDI();
            emp.datos_personales ??= new RuepDatosPersonales();

            string nombreEmpleado = $"{emp.apellido_paterno} {emp.apellido_materno} {emp.nombre}";
            string snap = SnapShot(emp);

            // ─────────────────────────────────────────────
            // DATOS DE IDENTIDAD
            // ─────────────────────────────────────────────

            emp.rfc = ResolverCampo(emp.rfc, BD("rfc"), result.Discrepancias, nombreEmpleado, "RFC");
            emp.nombre = ResolverCampo(emp.nombre, BD("nombre"), result.Discrepancias, nombreEmpleado, "Nombre");
            emp.apellido_paterno = ResolverCampo(emp.apellido_paterno, BD("apellidoP"), result.Discrepancias, nombreEmpleado, "Apellido Paterno");
            emp.apellido_materno = ResolverCampo(emp.apellido_materno, BD("apellidoM"), result.Discrepancias, nombreEmpleado, "Apellido Materno");
            string curpBD = BD("curp");

            if (!string.IsNullOrWhiteSpace(curpBD))
            {
                string curpRuep = emp.cfdi.curp ?? "";

                if (curpRuep.Length < curpBD.Length && curpBD.StartsWith(curpRuep, StringComparison.OrdinalIgnoreCase))
                {
                    emp.cfdi.curp = curpBD;
                }
                else
                {
                    emp.cfdi.curp = ResolverCampo(emp.cfdi.curp, curpBD, result.Discrepancias, nombreEmpleado, "CURP");
                }
            }

            // ─────────────────────────────────────────────
            // FECHA ALTA
            // ─────────────────────────────────────────────

            string fechaStr = "";

            if (fila.Table.Columns.Contains("falta") && fila["falta"] != DBNull.Value && DateTime.TryParse(fila["falta"].ToString(), out DateTime fechaBD))
            {
                fechaStr = fechaBD.ToString("dd/MM/yyyy");
            }

            emp.fecha_alta = ResolverCampo(emp.fecha_alta, fechaStr, result.Discrepancias, nombreEmpleado, "Fecha Alta");

            // ─────────────────────────────────────────────
            // DOMICILIO
            // ─────────────────────────────────────────────

            string tipoBD = BD("tipoVialidad");
            string nvialBD = BD("nombreDeVialidad");
            string dirBD = string.Join(" ", new[] { tipoBD, nvialBD }.Where(s => !string.IsNullOrWhiteSpace(s)));

            emp.cfdi.direccion = ResolverCampo(emp.cfdi.direccion, dirBD, result.Discrepancias, nombreEmpleado, "Dirección");
            emp.cfdi.no_exterior = ResolverCampo(emp.cfdi.no_exterior, BD("numeroExterior"), result.Discrepancias, nombreEmpleado, "Número Exterior");
            emp.cfdi.no_interior = ResolverCampo(emp.cfdi.no_interior, BD("numeroInterior"), result.Discrepancias, nombreEmpleado, "Número Interior");
            emp.cfdi.colonia = ResolverCampo(emp.cfdi.colonia, BD("nombreColonia"), result.Discrepancias, nombreEmpleado, "Colonia");
            emp.cfdi.estado = ResolverCampo(emp.cfdi.estado, BD("entidadFederativa"), result.Discrepancias, nombreEmpleado, "Estado");
            emp.cfdi.delegacion = ResolverCampo(emp.cfdi.delegacion, BD("nombreMunicipio"), result.Discrepancias, nombreEmpleado, "Municipio");
            emp.cfdi.cp = ResolverCampo(emp.cfdi.cp, BD("cp"), result.Discrepancias, nombreEmpleado, "Código Postal");

            string entre = BD("entreCalle");
            string yc = BD("yCalle");
            string referenciasBD = string.Join(" / ", new[] { entre, yc }.Where(s => !string.IsNullOrWhiteSpace(s)));

            if (!string.IsNullOrWhiteSpace(referenciasBD))
            {
                emp.cfdi.referencias = ResolverCampo(emp.cfdi.referencias, referenciasBD, result.Discrepancias, nombreEmpleado, "Referencias");
            }
            emp.cfdi.estado_domicilio = ResolverCampo(emp.cfdi.estado_domicilio, BD("estadoDomicilio"), result.Discrepancias, nombreEmpleado, "Estado Domicilio");
            emp.cfdi.estado_cont_domicilio = ResolverCampo(emp.cfdi.estado_cont_domicilio, BD("estadoContDomicilio"), result.Discrepancias, nombreEmpleado, "Estado Contribuyente");

            // ─────────────────────────────────────────────
            // CONTACTO
            // ─────────────────────────────────────────────

            string correo = BD("correoElectronico");

            if (!string.IsNullOrWhiteSpace(correo))
            {
                if (correo.Contains("@grupo-sacmag.com.mx", StringComparison.OrdinalIgnoreCase))
                {
                    emp.cfdi.correo_empresarial = ResolverCampo(emp.cfdi.correo_empresarial, correo, result.Discrepancias, nombreEmpleado, "Correo Empresarial");
                }
                else
                {
                    emp.cfdi.correo_personal = ResolverCampo(emp.cfdi.correo_personal, correo, result.Discrepancias, nombreEmpleado, "Correo Personal");
                }
            }

            emp.datos_personales.lada = ResolverCampo(emp.datos_personales.lada, BD("ladaTel"), result.Discrepancias, nombreEmpleado, "LADA");
            emp.datos_personales.telefono_movil = ResolverCampo(emp.datos_personales.telefono_movil, BD("numeroTel"), result.Discrepancias, nombreEmpleado, "Teléfono");

            // ─────────────────────────────────────────────
            // SAT / FISCALES
            // ─────────────────────────────────────────────

            emp.datos_personales.actividad_economica = ResolverCampo(emp.datos_personales.actividad_economica, BD("actividadEconomica"), result.Discrepancias, nombreEmpleado, "Actividad Económica");
            emp.datos_personales.regimen_fiscal = ResolverCampo(emp.datos_personales.regimen_fiscal, BD("regimen"), result.Discrepancias, nombreEmpleado, "Régimen Fiscal");
            emp.cfdi.link_sat = ResolverCampo(emp.cfdi.link_sat, BD("link"), result.Discrepancias, nombreEmpleado, "Link SAT");

            // ─────────────────────────────────────────────
            // DETECCIÓN DE CAMBIOS
            // ─────────────────────────────────────────────

            bool cambio = SnapShot(emp) != snap;

            if (cambio)
            {
                emp.fecha_ultima_mod = ahora;
                emp.modificado_por = "Sincronización BD";
            }

            return cambio;
        }

        private static RuepEmpleado CrearDesdeDB(int id, DataRow fila, string ahora)
        {
            string BD(string col) => fila.Table.Columns.Contains(col) && fila[col] != DBNull.Value ? fila[col].ToString().Trim().Replace("N/A", "").Trim() : "";

            string correo = BD("correoElectronico");
            string tipoBD = BD("tipoVialidad");
            string nvialBD = BD("nombreDeVialidad");
            string dirBD = string.Join(" ", new[] { tipoBD, nvialBD }.Where(s => !string.IsNullOrWhiteSpace(s)));

            string fechaAltaBD = "";
            if (fila.Table.Columns.Contains("falta") && fila["falta"] != DBNull.Value && DateTime.TryParse(fila["falta"].ToString(), out DateTime fechaBD))
                fechaAltaBD = fechaBD.ToString("dd/MM/yyyy");

            bool esEmpresarial = correo.Contains("@grupo-sacmag.com.mx", StringComparison.OrdinalIgnoreCase);

            return new RuepEmpleado
            {
                idEmpleado = id,
                nombre = BD("nombre"),
                apellido_paterno = BD("apellidoP"),
                apellido_materno = BD("apellidoM"),
                rfc = BD("rfc"),
                imss = "",
                fecha_alta = fechaAltaBD,
                fecha_baja = "",
                ingresos = new RuepIngresos(),
                cfdi = new RuepCFDI
                {
                    curp = BD("curp"),
                    direccion = dirBD,
                    no_exterior = BD("numeroExterior"),
                    no_interior = BD("numeroInterior"),
                    colonia = BD("nombreColonia"),
                    estado = BD("entidadFederativa"),
                    delegacion = BD("nombreMunicipio"),
                    cp = BD("cp"),
                    correo_empresarial = esEmpresarial ? correo : "",
                    correo_personal = !esEmpresarial ? correo : "",
                    estado_domicilio = BD("estadoDomicilio"),
                    estado_cont_domicilio = BD("estadoContDomicilio"),
                    referencias = string.Join(" / ", new[] { BD("entreCalle"), BD("yCalle") }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    link_sat = BD("link"),
                    correo_empresa_predeterminado = true,
                },
                datos_personales = new RuepDatosPersonales
                {
                    lada = BD("ladaTel"),
                    telefono_movil = BD("numeroTel"),
                    actividad_economica = BD("actividadEconomica"),
                    regimen_fiscal = BD("regimen"),
                },
                fecha_ultima_mod = ahora,
                modificado_por = "Sincronización BD (Alta automática)",
            };
        }

        private static string CrearKey(string rfc, string curp)
        {
            return
                $"{rfc?.Trim().ToUpperInvariant()}|" + $"{curp?.Trim().ToUpperInvariant()}";
        }

        private static SyncResult SincronizarPorRFC(RuepRoot root, DataTable dt, string rutaRUEP)
        {            
            var result = new SyncResult();
            string ahora = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            var dictBD = dt.AsEnumerable().GroupBy(r => CrearKey(r["rfc"]?.ToString(), r["curp"]?.ToString())).ToDictionary(g => g.Key, g => g.First());

            foreach (var emp in root.personal)
            {
                DataRow fila = dt.AsEnumerable().FirstOrDefault(r => string.Equals((r["rfc"]?.ToString() ?? "").Trim(), (emp.rfc ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

                if (fila == null)
                {
                    result.SinCambios++;
                    continue;
                }

                if (ActualizarCampos(emp, fila, ahora, result))
                    result.Actualizados++;
                else 
                    result.SinCambios++;
            }

            var keysRuep = new HashSet<string>(root.personal.Select(e => CrearKey(e.rfc, e.cfdi?.curp)));

            foreach (var item in dictBD)
            {
                if (keysRuep.Contains(item.Key))
                    continue;

                int idNomina = Convert.ToInt32(item.Value["idNomina"]);
                root.personal.Add(CrearDesdeDB(idNomina, item.Value, ahora));
                result.Nuevos++;
            }

            if (result.HuboCambios)
            {
                root.personal = root.personal.OrderBy(p => p.idEmpleado).ToList();

                CryptoService.SaveEncrypted(rutaRUEP, root, true);
            }

            if (result.Nuevos > 0 || result.Actualizados > 0)
            {
                root.personal = root.personal.OrderBy(p => p.idEmpleado).ToList();
                CryptoService.SaveEncrypted(rutaRUEP, root, true);
            }

            return result;
        }

        private static string ResolverCampo(string ruepVal, string bdVal, List<string> discrepancias, string empleado, string campo)
        {
            ruepVal = (ruepVal ?? "").Trim();
            bdVal = (bdVal ?? "").Trim();

            if (string.IsNullOrWhiteSpace(ruepVal))
                return bdVal;

            if (string.IsNullOrWhiteSpace(bdVal))
                return ruepVal;

            if (!string.Equals(ruepVal, bdVal, StringComparison.OrdinalIgnoreCase))
            {
                discrepancias.Add($"{empleado} | {campo}" + Environment.NewLine + $"RUEP: {ruepVal}" + Environment.NewLine + $"BD: {bdVal}");
            }

            return ruepVal;
        }


        // ── Utilidades ────────────────────────────────────────────────────
        private static string SnapShot(RuepEmpleado e) => string.Concat(e.rfc, e.nombre, e.apellido_paterno, e.apellido_materno, e.fecha_alta, e.cfdi?.curp,
            e.cfdi?.direccion, e.cfdi?.no_exterior, e.cfdi?.colonia, e.cfdi?.estado, e.cfdi?.delegacion, e.cfdi?.cp, e.cfdi?.estado_domicilio, e.cfdi?.estado_cont_domicilio,
            e.cfdi?.referencias, e.cfdi?.correo_empresarial, e.cfdi?.correo_personal, e.cfdi?.link_sat, e.datos_personales?.lada, e.datos_personales?.telefono_movil,
            e.datos_personales?.actividad_economica, e.datos_personales?.regimen_fiscal);
    }
}