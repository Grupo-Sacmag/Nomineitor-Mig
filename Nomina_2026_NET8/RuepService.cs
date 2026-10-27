// RuepService.cs — VERSION LIMPIA para .NET 8
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Nomina_2026_NET8
{
    public class RuepService
    {
        private readonly string _ruta;
        private List<EmpleadoCompleto> _empleados;
        private JObject _jsonAlAbrir;       
        private JObject _empresaAlAbrir;
        private Dictionary<int, EmpleadoCompleto> _empleadosAlAbrir;
        public RuepRoot DatosEmpresa { get; private set; }

        public RuepService(string rutaDat)
        {
            _ruta = rutaDat ?? throw new ArgumentNullException(nameof(rutaDat));

            // Cargar los datos V9 sin guardarlos.
            Cargar();

            // Conservar el JSON original completo.
            _jsonAlAbrir = CryptoService.LoadEncrypted<JObject>(_ruta) ?? throw new InvalidDataException("No fue posible respaldar el estado original del RUEP.");

            // Conservar el estado original del modelo de empresa.
            _empresaAlAbrir = JObject.FromObject(DatosEmpresa);

            // Conservar una copia independiente de cada empleado.
            _empleadosAlAbrir = _empleados.ToDictionary(e => e.Id, e => JsonConvert.DeserializeObject<EmpleadoCompleto>(JsonConvert.SerializeObject(e)));
        }

        // ── Empresa ───────────────────────────────────────────────────────
        public RuepRoot ObtenerEmpresa() => DatosEmpresa;

        public void ActualizarClaveEmpresa(string nuevaClave)
        {
            if (string.IsNullOrWhiteSpace(nuevaClave))
                throw new ArgumentException("La clave no puede estar vacía.");

            DatosEmpresa.clave_empresa = nuevaClave.Trim().ToUpperInvariant();
            GuardarCambios();
        }

        // ── Personal ──────────────────────────────────────────────────────

        public List<EmpleadoCompleto> ObtenerTodos() => _empleados;
        public EmpleadoCompleto ObtenerPorId(int id) => _empleados.FirstOrDefault(e => e.Id == id);

        private static RuepEmpleado ClonarRegistro(RuepEmpleado registro)
        {
            return JsonConvert.DeserializeObject<RuepEmpleado>(JsonConvert.SerializeObject(registro));
        }

        private static void AplicarDiferencias(JObject antes, JObject despues, JObject destino)
        {
            foreach (var propiedad in despues.Properties())
            {
                JToken valorAnterior = antes[propiedad.Name];
                JToken valorNuevo = propiedad.Value;

                // No modificar campos cuyo valor no cambió.
                if (JToken.DeepEquals(valorAnterior, valorNuevo))
                    continue;

                // Procesar objetos anidados sin reemplazarlos
                // completamente.
                if (valorAnterior is JObject objetoAnterior && valorNuevo is JObject objetoNuevo && destino[propiedad.Name] is JObject objetoDestino)
                {
                    AplicarDiferencias(objetoAnterior, objetoNuevo, objetoDestino);
                }
                else
                {
                    destino[propiedad.Name] = valorNuevo.DeepClone();
                }
            }
        }

        // ── Persistencia ──────────────────────────────────────────────────
        private static DateTime ValidarFechaLaboralV9(string valor, string nombreCampo)
        {
            if (!DateTime.TryParseExact(valor, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime fecha))
            {
                throw new InvalidOperationException($"Fecha laboral inválida en {nombreCampo}: '{valor}'. Se esperaba dd/MM/yyyy.");
            }

            return fecha.Date;
        }


        private static void AplicarMovimientoLaboralV9(JObject documento, EmpleadoCompleto anterior, EmpleadoCompleto actual)
        {
            if (anterior == null || actual == null || anterior.Id != actual.Id)
            {
                throw new InvalidOperationException("No es posible identificar el expediente laboral.");
            }

            int id = actual.Id;

            // Nunca permitir que una operación laboral
            // cambie la fecha de alta original.
            if (actual.FechaAlta != anterior.FechaAlta)
            {
                throw new InvalidOperationException($"Empleado {id}: la fecha de alta original no puede modificarse mediante este procedimiento.");
            }

            if (documento["personal"] is not JArray personal)
                throw new InvalidDataException("El RUEP no contiene personal.");

            JObject registro = personal.OfType<JObject>().SingleOrDefault(x => (int?)x["idEmpleado"] == id);

            if (registro == null)
                throw new InvalidDataException($"No existe el empleado {id} en RUEP.dat.");

            if (registro["historial_laboral"] is not JArray periodos || periodos.Count == 0 || periodos.Any(x => x is not JObject))
            {
                throw new InvalidDataException(
                    $"Empleado {id}: historial_laboral inválido o vacío.");
            }

            // Verificar que los periodos tienen IDs válidos.
            var ids = periodos.OfType<JObject>().Select(p => (int?)p["idPeriodo"] ?? 0).ToList();

            if (ids.Any(x => x <= 0) || ids.Distinct().Count() != ids.Count)
            {
                throw new InvalidDataException($"Empleado {id}: existen periodos laborales con identificadores inválidos o duplicados.");
            }

            JObject ultimo = (JObject)periodos.Last;

            if ((int?)ultimo["idPeriodo"] != ids.Max())
            {
                throw new InvalidDataException($"Empleado {id}: los periodos no están ordenados.");
            }

            bool estabaActivo = anterior.EsReingreso ? string.IsNullOrWhiteSpace(anterior.FechaReingresoBaja) : string.IsNullOrWhiteSpace(anterior.FechaBaja);
            bool quedaActivo = actual.EsReingreso ? string.IsNullOrWhiteSpace(actual.FechaReingresoBaja) : string.IsNullOrWhiteSpace(actual.FechaBaja);

            // =================================================
            // CASO 1: BAJA LABORAL
            // =================================================
            if (estabaActivo && !quedaActivo)
            {
                if (actual.EsReingreso != anterior.EsReingreso || actual.FechaReingresoAlta != anterior.FechaReingresoAlta || actual.RespetaAntiguedad != anterior.RespetaAntiguedad)
                {
                    throw new InvalidOperationException($"Empleado {id}: se detectaron cambios laborales incompatibles con una baja.");
                }

                string estatusEsperado = actual.EsReingreso ? "REINGRESO_BAJA" : "BAJA";

                if (!string.Equals(actual.EstatusLaboral, estatusEsperado, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Empleado {id}: el estatus no corresponde a una baja laboral.");
                }

                if (!string.IsNullOrWhiteSpace((string)ultimo["fecha_baja"]))
                {
                    throw new InvalidOperationException($"Empleado {id}: el último periodo ya tiene baja.");
                }

                string altaPeriodoEsperada = anterior.EsReingreso ? anterior.FechaReingresoAlta : anterior.FechaAlta;

                if ((string)ultimo["fecha_alta"] != altaPeriodoEsperada)
                {
                    throw new InvalidDataException($"Empleado {id}: la fecha de alta del periodo no coincide con el expediente.");
                }

                if (anterior.EsReingreso && actual.FechaBaja != anterior.FechaBaja)
                {
                    throw new InvalidOperationException($"Empleado {id}: no se puede cambiar la primera baja al cerrar un reingreso.");
                }

                if (!anterior.EsReingreso && actual.FechaReingresoBaja != anterior.FechaReingresoBaja)
                {
                    throw new InvalidOperationException($"Empleado {id}: movimiento de baja inconsistente.");
                }

                string fechaBaja = actual.EsReingreso ? actual.FechaReingresoBaja : actual.FechaBaja;

                DateTime alta = ValidarFechaLaboralV9(altaPeriodoEsperada, "fecha_alta");
                DateTime baja = ValidarFechaLaboralV9(fechaBaja, "fecha_baja");

                if (baja < alta)
                {
                    throw new InvalidOperationException($"Empleado {id}: la baja no puede ser anterior al inicio del periodo laboral.");
                }

                // Cerrar solamente el último periodo.
                ultimo["fecha_baja"] = fechaBaja;
                return;
            }

            // =================================================
            // CASO 2: REINGRESO
            // =================================================
            if (!estabaActivo && quedaActivo)
            {
                if (!actual.EsReingreso || !string.IsNullOrWhiteSpace(actual.FechaReingresoBaja) || actual.FechaBaja != anterior.FechaBaja || string.IsNullOrWhiteSpace(actual.FechaReingresoAlta) ||
                    actual.FechaReingresoAlta == anterior.FechaReingresoAlta || !string.Equals(actual.EstatusLaboral, "REINGRESO_ACTIVO", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Empleado {id}: los datos no corresponden a un reingreso válido.");
                }

                string bajaAnterior = anterior.EsReingreso ? anterior.FechaReingresoBaja : anterior.FechaBaja;

                if (string.IsNullOrWhiteSpace(bajaAnterior) || (string)ultimo["fecha_baja"] != bajaAnterior)
                {
                    throw new InvalidDataException($"Empleado {id}: no coincide la baja del periodo anterior.");
                }

                DateTime baja = ValidarFechaLaboralV9(bajaAnterior, "baja anterior");

                DateTime nuevaAlta = ValidarFechaLaboralV9(actual.FechaReingresoAlta, "fecha de reingreso");

                if (nuevaAlta <= baja)
                {
                    throw new InvalidOperationException($"Empleado {id}: el reingreso debe ser posterior a la baja anterior.");
                }

                // Agregar un periodo nuevo sin sustituir
                // los periodos anteriores.
                var periodoNuevo = new JObject
                {
                    ["idPeriodo"] = ids.Max() + 1,
                    ["tipo_movimiento"] = "REINGRESO",
                    ["fecha_alta"] = actual.FechaReingresoAlta,
                    ["fecha_baja"] = "",
                    ["id_empresa_origen"] = JValue.CreateNull(),
                    ["id_empleado_origen"] = JValue.CreateNull(),
                    ["respeta_antiguedad"] = actual.RespetaAntiguedad
                };

                periodos.Add(periodoNuevo);
                return;
            }

            // =================================================
            // CASO 3: MOVIMIENTO NO SOPORTADO
            // =================================================
            throw new InvalidOperationException(
                $"Empleado {id}: se detectó un cambio laboral que requiere un procedimiento específico. No se modificó historial_laboral.");
        }


        public void GuardarCambios()
        {
            if (DatosEmpresa == null || _empleados == null)
                throw new InvalidOperationException("No existe un expediente RUEP cargado.");

            if (_empleados.Any(e => e == null || e.Id <= 0) || _empleados.GroupBy(e => e.Id).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Hay identificadores de empleados inválidos o duplicados.");

            // Las altas y eliminaciones de expedientes tendrán
            // su propio procedimiento en la siguiente etapa.
            if (_empleados.Count != _empleadosAlAbrir.Count || _empleados.Any(e => !_empleadosAlAbrir.ContainsKey(e.Id)))
                throw new InvalidOperationException("Las altas y eliminaciones de empleados aún requieren el procedimiento específico de RUEP V9.");

            // Comprobar que otro servicio no haya cambiado
            // el archivo desde que abrimos esta sesión.
            JObject discoActual = CryptoService.LoadEncrypted<JObject>(_ruta);

            if (!JToken.DeepEquals(discoActual, _jsonAlAbrir))
                throw new InvalidOperationException("RUEP.dat fue modificado desde otra sesión. Cierra y vuelve a abrir la empresa antes de guardar.");

            // Trabajar sobre una copia completa del JSON original.
            JObject documentoNuevo = (JObject)_jsonAlAbrir.DeepClone();

            // =============================================
            // CAMBIOS DE EMPRESA
            // =============================================

            JObject empresaAnterior = (JObject)_empresaAlAbrir.DeepClone();
            JObject empresaActual = JObject.FromObject(DatosEmpresa);

            // El personal se procesa por identificador.
            // No reemplazar todo el arreglo.
            empresaAnterior.Remove("personal");
            empresaActual.Remove("personal");

            AplicarDiferencias(empresaAnterior, empresaActual, documentoNuevo);

            // =============================================
            // CAMBIOS DE EMPLEADOS EXISTENTES
            // =============================================

            if (documentoNuevo["personal"] is not JArray personalJson)
                throw new InvalidDataException("El documento no contiene el arreglo personal.");

            foreach (EmpleadoCompleto empleado in _empleados)
            {
                EmpleadoCompleto anterior = _empleadosAlAbrir[empleado.Id];

                // No modificar empleados sin cambios.
                if (JToken.DeepEquals(JObject.FromObject(anterior), JObject.FromObject(empleado)))
                    continue;

                // =============================================
                // DETECTAR MOVIMIENTOS LABORALES V9
                // =============================================

                bool hayCambiosLaborales = empleado.FechaAlta != anterior.FechaAlta || empleado.FechaBaja != anterior.FechaBaja || empleado.EsReingreso != anterior.EsReingreso ||
                    empleado.FechaReingresoAlta != anterior.FechaReingresoAlta || empleado.FechaReingresoBaja != anterior.FechaReingresoBaja || empleado.RespetaAntiguedad != anterior.RespetaAntiguedad || empleado.EstatusLaboral != anterior.EstatusLaboral;

                if (hayCambiosLaborales)
                {
                    AplicarMovimientoLaboralV9(documentoNuevo, anterior, empleado);
                }

                // Estos datos no deben perderse silenciosamente.
                if (empleado.RiesgoIMSS != anterior.RiesgoIMSS || empleado.TipoRegimen != anterior.TipoRegimen || !JToken.DeepEquals(JToken.FromObject(empleado.Obras), JToken.FromObject(anterior.Obras)))
                {
                    throw new InvalidOperationException($"El empleado {empleado.Id} contiene cambios en campos que requieren un mapeo adicional.");
                }

                RuepEmpleado registroOriginal = DatosEmpresa.personal.FirstOrDefault(x => x.idEmpleado == empleado.Id);

                if (registroOriginal == null)
                    throw new InvalidDataException($"No existe el expediente del empleado {empleado.Id}.");

                JObject registroJson = personalJson.OfType<JObject>().SingleOrDefault(x => (int?)x["idEmpleado"] == empleado.Id);

                if (registroJson == null)
                    throw new InvalidDataException($"No se encontró el ID {empleado.Id} en el JSON.");

                // Generar dos proyecciones desde el mismo registro:
                // una con los datos anteriores y otra con los nuevos.
                RuepEmpleado antesMapeado = MapearEmpleadoARuep(anterior, ClonarRegistro(registroOriginal));
                RuepEmpleado despuesMapeado = MapearEmpleadoARuep(empleado, ClonarRegistro(registroOriginal));

                AplicarDiferencias(JObject.FromObject(antesMapeado), JObject.FromObject(despuesMapeado), registroJson);
            }

            // No generar un archivo nuevo si no hubo cambios reales.
            if (JToken.DeepEquals(documentoNuevo, _jsonAlAbrir))
                return;

            // =============================================
            // GUARDADO CON RESPALDO
            // =============================================

            string carpeta = Path.GetDirectoryName(Path.GetFullPath(_ruta))!;
            string carpetaRespaldos = Path.Combine(carpeta, "Respaldos_RUEP");

            Directory.CreateDirectory(carpetaRespaldos);

            string identificador = Guid.NewGuid().ToString("N");
            string respaldo = Path.Combine(carpetaRespaldos, $"RUEP_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{identificador}.dat");
            string temporal = Path.Combine(carpeta, $".RUEP_{identificador}.tmp");

            try
            {
                // Primero escribir en un archivo temporal.
                CryptoService.SaveEncrypted(temporal, documentoNuevo, indented: true);

                // Comprobar que se puede descifrar
                // y que conserva los valores.
                JObject verificado = CryptoService.LoadEncrypted<JObject>(temporal);

                if (!JToken.DeepEquals(verificado, documentoNuevo))
                    throw new InvalidDataException("El archivo temporal no pasó la validación.");

                // Sustituir conservando respaldo del archivo anterior.
                File.Replace(temporal, _ruta, respaldo);

                // Actualizar el estado de esta sesión.
                _jsonAlAbrir = (JObject)documentoNuevo.DeepClone();

                DatosEmpresa = CryptoService.LoadEncrypted<RuepRoot>(_ruta);

                _empresaAlAbrir = JObject.FromObject(DatosEmpresa);

                _empleadosAlAbrir = _empleados.ToDictionary(e => e.Id, e => JsonConvert.DeserializeObject<EmpleadoCompleto>(JsonConvert.SerializeObject(e)));
            }
            finally
            {
                if (File.Exists(temporal))
                    File.Delete(temporal);
            }
        }


        // ── Privado ───────────────────────────────────────────────────────

        private static string ResolverEstatusLaboral(RuepEmpleado e)
        {
            string estatus = (e.estatus_laboral ?? "").Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(estatus))
                return estatus;

            // Compatibilidad con RUEP antiguos.
            if (e.es_reingreso)
            {
                return string.IsNullOrWhiteSpace(e.fecha_reingreso_baja) ? "REINGRESO_ACTIVO" : "REINGRESO_BAJA";
            }

            return string.IsNullOrWhiteSpace(e.fecha_baja) ? "ACTIVO" : "BAJA";
        }

        private static bool EsEmpleadoActivoPorEstatus(string estatus)
        {
            estatus = (estatus ?? "").Trim().ToUpperInvariant();

            return estatus == "ACTIVO" || estatus == "REINGRESO_ACTIVO";
        }

        private static string FechaAltaParaCalculo(RuepEmpleado e)
        {
            bool usarFechaReingreso = e.es_reingreso && !e.respeta_antiguedad && !string.IsNullOrWhiteSpace(e.fecha_reingreso_alta);

            return usarFechaReingreso ? e.fecha_reingreso_alta : e.fecha_alta;
        }

        private static string ResolverEstatusParaGuardar(EmpleadoCompleto e)
        {
            string estatus = (e.EstatusLaboral ?? "").Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(estatus))
                return estatus;

            if (e.EsReingreso)
            {
                return string.IsNullOrWhiteSpace(e.FechaReingresoBaja) ? "REINGRESO_ACTIVO" : "REINGRESO_BAJA";
            }

            return string.IsNullOrWhiteSpace(e.FechaBaja) ? "ACTIVO" : "BAJA";
        }

        private static decimal CalcularSalarioIntegrado(decimal sueldo, decimal viaticos, decimal otras, string fechaAltaParaCalculo, int anoFiscal, decimal umaPorDia)
        {
            decimal total = sueldo + viaticos + otras;
            if (total <= 0m || umaPorDia <= 0m)
                return 0m;
            int mesReferencia = DateTime.Now.Month;
            int diaReferencia = DateTime.Now.Day;
            int antiguedad = ConstantesNomina.AntiguedadLegacyVB6(fechaAltaParaCalculo, mesReferencia, diaReferencia, anoFiscal);
            decimal factor = ConstantesNomina.FactorSDIPorAnioBase(antiguedad);

            decimal calculado = Math.Round((sueldo * factor) + viaticos + otras, 4, MidpointRounding.AwayFromZero);
            decimal tope = Math.Round(25m * umaPorDia, 2, MidpointRounding.AwayFromZero);

            return Math.Min(calculado, tope);
        }

        private void Cargar()
        {
            // ==========================================
            // CARGA SEGURA DE RUEP V9
            // ==========================================

            var root = CryptoService.LoadEncrypted<RuepRoot>(_ruta);

            if (root == null || root.ano_fiscal <= 0 || root.personal == null || root.datos_fiscales == null)
            {
                throw new InvalidOperationException("No se pudo interpretar la estructura RUEP V9.");
            }

            // Validar identidad y posiciones originales.
            if (root.personal.Any(e => e == null || e.idEmpleado <= 0))
            {
                throw new InvalidOperationException("El RUEP contiene empleados con ID inválido.");
            }

            if (root.personal.GroupBy(e => e.idEmpleado).Any(g => g.Count() > 1))
            {
                throw new InvalidOperationException("El RUEP contiene ID de empleados duplicados.");
            }

            DatosEmpresa = root;
            _empleados = new List<EmpleadoCompleto>();

            foreach (var e in root.personal)
            {
                var ingresos = e.ingresos ?? new RuepIngresos();

                // ==========================================
                // HISTORIAL LABORAL V9
                // ==========================================

                var reingreso = e.historial_laboral?.Where(p => p != null && (p.tipo_movimiento ?? "").IndexOf("REINGRESO", StringComparison.OrdinalIgnoreCase) >= 0).OrderBy(p => p.idPeriodo).LastOrDefault();
                bool esReingreso = reingreso != null;
                string estatus;

                if (esReingreso)
                {
                    estatus = string.IsNullOrWhiteSpace(reingreso.fecha_baja) ? "REINGRESO_ACTIVO" : "REINGRESO_BAJA";
                }
                else
                {
                    estatus = string.IsNullOrWhiteSpace(e.fecha_baja) ? "ACTIVO" : "BAJA";
                }

                // El integrado de V9 se respeta.
                // Null queda representado como 0 en EmpleadoCompleto, pero no se guarda ni recalcula automáticamente.
                decimal integrado = ingresos.integrado ?? 0m;

                // ==========================================
                // CONSTRUIR EMPLEADO PARA LOS FORMULARIOS
                // ==========================================

                var empleado = new EmpleadoCompleto
                {
                    // Identificación
                    Id = e.idEmpleado,
                    Nombre = e.nombre ?? "",
                    ApellidoP = e.apellido_paterno ?? "",
                    ApellidoM = e.apellido_materno ?? "",
                    RFC = e.rfc ?? "",
                    NSS = e.imss ?? "",
                    CURP = e.cfdi?.curp ?? "",

                    // Fechas laborales
                    FechaAlta = e.fecha_alta ?? "",
                    FechaBaja = e.fecha_baja ?? "",

                    // Historial laboral
                    EsReingreso = esReingreso,
                    FechaReingresoAlta = reingreso?.fecha_alta ?? "",
                    FechaReingresoBaja = reingreso?.fecha_baja ?? "",
                    RespetaAntiguedad = reingreso?.respeta_antiguedad ?? true,
                    EstatusLaboral = estatus,

                    // Ingresos
                    SalarioDiario = ingresos.sueldo,
                    Viaticos = ingresos.viaticos,
                    Otras = ingresos.otras,
                    SalarioIntegrado = integrado,

                    // ISR se calcula durante la nómina
                    ISR = 0m,

                    // Tarjeta bancaria
                    Banamex = e.datos_personales?.clabe_banamex ?? "",

                    // Domicilio
                    Direccion = e.cfdi?.direccion ?? "",
                    TipoVialidad = e.cfdi?.tipo_vialidad ?? "",
                    Colonia = e.cfdi?.colonia ?? "",
                    Ciudad = e.cfdi?.ciudad ?? "",
                    Municipio = e.cfdi?.delegacion ?? "",
                    Estado = e.cfdi?.estado ?? "",
                    CP = e.cfdi?.cp ?? "",
                    Referencias = e.cfdi?.referencias ?? "",
                    NoExterior = e.cfdi?.no_exterior ?? "",
                    NoInterior = e.cfdi?.no_interior ?? "",
                    EstadoDomicilio = e.cfdi?.estado_domicilio ?? "",
                    EstadoContDomicilio = e.cfdi?.estado_cont_domicilio ?? "",

                    // Correos electrónicos
                    EmailEmpresa = e.cfdi?.correo_empresarial ?? "",
                    Email = e.cfdi?.correo_personal ?? "",
                    CorreoEmpresaPredeterminado = e.cfdi?.correo_empresa_predeterminado ?? false,

                    // Contacto
                    TelefonoFijo = e.datos_personales?.telefono_fijo ?? "",
                    TelefonoMovil = e.datos_personales?.telefono_movil ?? "",
                    LADA = e.datos_personales?.lada ?? "",
                    TelefonoEmpresa = e.datos_personales?.telefono_empresa ?? "",
                    Extension = e.datos_personales?.extension ?? "",

                    // Información fiscal
                    ActividadEconomica = e.datos_personales?.actividad_economica ?? "",
                    RegimenFiscal = e.datos_personales?.regimen_fiscal ?? "",
                    LinkSAT = e.cfdi?.link_sat ?? "",

                    // Configuración CFDI de empresa
                    RiesgoIMSS = root.cfdi_nomina?.riesgo_imss ?? "",

                    // Datos complementarios
                    FormaPagoSAT = e.forma_pago_sat ?? "",
                    Departamento = e.departamento ?? "",
                    Puesto = e.puesto ?? "",
                    TipoContrato = e.tipo_contrato ?? "",
                    TipoJornada = e.tipo_jornada ?? "",
                    TipoRegimen = "",

                    // Distribución por obra, cuando exista
                    Obras = (e.obras ?? new List<RuepObra>()).Select(o => new ObraAsignada
                    {
                        NumeroObra = o.obra,
                        Porcentaje = o.porcentaje,
                        Importe = o.importe
                    }).ToList(),

                    // Auditoría
                    FechaUltimaMod = e.fecha_ultima_mod ?? "",
                    ModificadoPor = e.modificado_por ?? ""
                };
                
                _empleados.Add(empleado);
            }

            // IMPORTANTE:
            // No guardar automáticamente al cargar.
            // No recalcular salarios.
            // No modificar historiales laborales.
            // No renumerar empleados.
        }

        private void IntentarResolverClaveEmpresa()
        {
            if (!string.IsNullOrWhiteSpace(DatosEmpresa.clave_empresa))
                return;

            string clave = BDSyncService.ResolverClaveEmpresa(DatosEmpresa.nombre);
            if (string.IsNullOrEmpty(clave))
                return;

            DatosEmpresa.clave_empresa = clave;
            GuardarCambios();
        }

        /// <summary>
        /// Mapea un EmpleadoCompleto (DTO en memoria) → RuepEmpleado (modelo de disco).
        /// Centraliza la conversión para que GuardarCambios no tenga 25 líneas inline.
        /// </summary>
        private static RuepEmpleado MapearEmpleadoARuep(EmpleadoCompleto e, RuepEmpleado existente)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            // Conservar el expediente original.
            // Solo crear uno si se trata de un empleado nuevo.
            RuepEmpleado r = existente ?? new RuepEmpleado();

            // =============================================
            // IDENTIFICACIÓN
            // =============================================
            r.idEmpleado = e.Id;
            r.nombre = e.Nombre ?? "";
            r.apellido_paterno = e.ApellidoP ?? "";
            r.apellido_materno = e.ApellidoM ?? "";
            r.rfc = e.RFC ?? "";
            r.imss = e.NSS ?? "";

            // En RuepRoot.cs, imss debe tener:
            // [JsonProperty("nss_imss")]

            // =============================================
            // FECHAS
            // =============================================
            r.fecha_alta = e.FechaAlta ?? "";
            r.fecha_baja = e.FechaBaja ?? "";

            // =============================================
            // INGRESOS V9
            // =============================================
            r.ingresos ??= new RuepIngresos();

            r.ingresos.sueldo = e.SalarioDiario;
            r.ingresos.viaticos = e.Viaticos;
            r.ingresos.otras = e.Otras;

            // Conservar null si originalmente no existía
            // salario integrado y la vista sigue indicando cero.
            if (r.ingresos.integrado.HasValue ||
                e.SalarioIntegrado != 0m)
            {
                r.ingresos.integrado = e.SalarioIntegrado;
            }

            // El ISR no forma parte de RUEP V9.

            // =============================================
            // DATOS CFDI Y DOMICILIO
            // =============================================
            r.cfdi ??= new RuepCFDI();

            r.cfdi.curp = e.CURP ?? "";
            r.cfdi.direccion = e.Direccion ?? "";
            r.cfdi.tipo_vialidad = e.TipoVialidad ?? "";
            r.cfdi.colonia = e.Colonia ?? "";
            r.cfdi.ciudad = e.Ciudad ?? "";
            r.cfdi.estado = e.Estado ?? "";
            r.cfdi.delegacion = e.Municipio ?? "";
            r.cfdi.cp = e.CP ?? "";

            r.cfdi.referencias = e.Referencias ?? "";
            r.cfdi.no_exterior = e.NoExterior ?? "";
            r.cfdi.no_interior = e.NoInterior ?? "";

            r.cfdi.estado_domicilio = e.EstadoDomicilio ?? "";
            r.cfdi.estado_cont_domicilio = e.EstadoContDomicilio ?? "";

            // =============================================
            // CORREOS
            // =============================================
            r.cfdi.correo_empresarial = e.EmailEmpresa ?? "";
            r.cfdi.correo_personal = e.Email ?? "";
            r.cfdi.correo_empresa_predeterminado = e.CorreoEmpresaPredeterminado;
            r.cfdi.link_sat = e.LinkSAT ?? "";

            // =============================================
            // DATOS PERSONALES
            // =============================================
            r.datos_personales ??= new RuepDatosPersonales();

            // El alias clabe_banamex debe estar asociado
            // mediante JsonProperty a numero_tarjeta_banamex.
            r.datos_personales.clabe_banamex = e.Banamex ?? "";
            r.datos_personales.telefono_fijo = e.TelefonoFijo ?? "";
            r.datos_personales.telefono_movil = e.TelefonoMovil ?? "";
            r.datos_personales.lada = e.LADA ?? "";
            r.datos_personales.telefono_empresa = e.TelefonoEmpresa ?? "";
            r.datos_personales.extension = e.Extension ?? "";
            r.datos_personales.actividad_economica = e.ActividadEconomica ?? "";
            r.datos_personales.regimen_fiscal = e.RegimenFiscal ?? "";

            // =============================================
            // CAMPOS COMPLEMENTARIOS
            // =============================================
            r.forma_pago_sat = e.FormaPagoSAT ?? "";
            r.departamento = e.Departamento ?? "";
            r.puesto = e.Puesto ?? "";
            r.tipo_contrato = e.TipoContrato ?? "";
            r.tipo_jornada = e.TipoJornada ?? "";

            // =============================================
            // DISTRIBUCIÓN POR OBRA
            // =============================================
            // Por ahora conservar r.obras sin reconstruirlo.
            // V9 base no incorpora maestro.dno.

            // =============================================
            // HISTORIAL LABORAL
            // =============================================
            // NO sustituir r.historial_laboral.
            // La creación y actualización de periodos
            // se manejará mediante un método específico.

            // =============================================
            // AUDITORÍA
            // =============================================
            r.fecha_ultima_mod = e.FechaUltimaMod ?? "";
            r.modificado_por = e.ModificadoPor ?? "";

            return r;
        }

        public void GuardarConfiguracionCfdiNomina(long folio, string serie, int consecutivo, string registroPatronal, string riesgoImss)
        {
            DatosEmpresa.cfdi_nomina ??= new ConfiguracionCfdiNomina();
            DatosEmpresa.cfdi_nomina.folio = folio;
            DatosEmpresa.cfdi_nomina.serie = serie ?? "";
            DatosEmpresa.cfdi_nomina.consecutivo = consecutivo;
            DatosEmpresa.cfdi_nomina.registro_patronal = registroPatronal ?? "";
            DatosEmpresa.cfdi_nomina.riesgo_imss = riesgoImss ?? "";

            GuardarCambios();
        }
        public ConfiguracionCfdiNomina ObtenerConfiguracionCfdiNomina()
        {
            DatosEmpresa.cfdi_nomina ??= new ConfiguracionCfdiNomina();
            return DatosEmpresa.cfdi_nomina;
        }        
    }
}