// RuepService.cs — VERSION LIMPIA para .NET 8
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nomina_2026_NET8
{
    /// <summary>
    /// Servicio unificado para leer y escribir RUEP.dat.
    /// Es el único punto de entrada para datos de empresa y personal.
    /// </summary>
    public class RuepService
    {
        private readonly string _ruta;
        private List<EmpleadoCompleto> _empleados;

        /// <summary>Datos raíz de la empresa. Disponible tras construir el servicio.</summary>
        public RuepRoot DatosEmpresa { get; private set; }

        public RuepService(string rutaDat)
        {
            _ruta = rutaDat;
            Cargar();   // renombrado de Recargar → Cargar (ahora privado)
        }

        // ── Empresa ───────────────────────────────────────────────────────

        /// <summary>Devuelve los datos raíz de la empresa.</summary>
        public RuepRoot ObtenerEmpresa() => DatosEmpresa;

        /// <summary>
        /// Actualiza la clave BD de la empresa y persiste de inmediato.
        /// </summary>
        public void ActualizarClaveEmpresa(string nuevaClave)
        {
            if (string.IsNullOrWhiteSpace(nuevaClave))
                throw new ArgumentException("La clave no puede estar vacía.");

            DatosEmpresa.clave_empresa = nuevaClave.Trim().ToUpperInvariant();
            GuardarCambios();
        }

        // ── Personal ──────────────────────────────────────────────────────

        public List<EmpleadoCompleto> ObtenerTodos() => _empleados;

        /// <summary>Busca un empleado por ID. Retorna null si no existe.</summary>
        public EmpleadoCompleto ObtenerPorId(int id) => _empleados.FirstOrDefault(e => e.Id == id);

        // 🗑️ RecalcularIngresosVacios → ELIMINADO (nadie lo llamaba)

        // ── Persistencia ──────────────────────────────────────────────────

        /// <summary>
        /// Persiste el estado actual en RUEP.dat.
        /// ISR es runtime — NO se escribe. SalarioIntegrado SÍ se escribe
        /// (ver MapearEmpleadoARuep / RuepEmpleado.salario_integrado).
        /// </summary>
        public void GuardarCambios()
        {
            // En lugar de reconstruir RuepRoot desde cero, actualizamos solo la lista personal dentro del root existente.
            // Así no hay riesgo de perder propiedades nuevas de RuepRoot.
            DatosEmpresa.personal = _empleados.Select(MapearEmpleadoARuep).ToList();

            CryptoService.SaveEncrypted(_ruta, DatosEmpresa, indented: true);
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

        /// Calcula el salario diario integrado con la misma fórmula usada en el resto
        /// del sistema (FormEDICIONCENTRALPERSONAL). Extraído aquí para no duplicar la
        /// lógica entre la carga inicial (migración de RUEP viejos sin el campo) y
        /// cualquier otro punto que necesite recalcular.
        private static decimal CalcularSalarioIntegrado(decimal sueldo, decimal viaticos, decimal otras, string fechaAltaParaCalculo, int anoFiscal, decimal umaPorDia)
        {
            decimal total = sueldo + viaticos + otras;
            if (total <= 0m || umaPorDia <= 0m)
                return 0m;

            int anioBaseImss = ConstantesNomina.AntiguedadBaseImssVB6(fechaAltaParaCalculo, anoFiscal);
            decimal factor = ConstantesNomina.FactorSDIPorAnioBase(anioBaseImss);
            decimal calculado = Math.Round(total * factor, 4, MidpointRounding.AwayFromZero);
            decimal tope = Math.Round(25m * umaPorDia, 2, MidpointRounding.AwayFromZero);

            return Math.Min(calculado, tope);
        }

        /// Carga y deserializa RUEP.dat.
        private void Cargar()
        {
            var root = CryptoService.LoadEncrypted<RuepRoot>(_ruta);
            DatosEmpresa = root;
            DatosEmpresa.cfdi_nomina ??= new ConfiguracionCfdiNomina();
            _empleados = new List<EmpleadoCompleto>();

            decimal uma = root.uma_por_dia;
            bool huboQueCalcularIntegrado = false;

            foreach (var e in root.personal)
            {
                decimal sueldo = e.ingresos?.sueldo ?? 0m;
                decimal viaticos = e.ingresos?.viaticos ?? 0m;
                decimal otras = e.ingresos?.otras ?? 0m;

                string fechaAltaParaCalculo = FechaAltaParaCalculo(e);

                // SalarioIntegrado: si ya viene persistido en el RUEP, se reutiliza tal
                // cual (no se recalcula en cada carga). Si es un RUEP generado antes de
                // este cambio (o recién migrado desde .dno), el campo llega en null —
                // se calcula UNA vez aquí y se marca para persistirlo de inmediato.
                decimal sdi;
                if (e.salario_integrado.HasValue)
                {
                    sdi = e.salario_integrado.Value;
                }
                else
                {
                    sdi = CalcularSalarioIntegrado(sueldo, viaticos, otras, fechaAltaParaCalculo, root.ano_fiscal, uma);
                    e.salario_integrado = sdi;
                    huboQueCalcularIntegrado = true;
                }

                string estatusLaboral = ResolverEstatusLaboral(e);

                _empleados.Add(new EmpleadoCompleto
                {
                    Id = e.idEmpleado,
                    Nombre = e.nombre ?? "",
                    ApellidoP = e.apellido_paterno ?? "",
                    ApellidoM = e.apellido_materno ?? "",
                    RFC = e.rfc ?? "",
                    NSS = e.imss ?? "",
                    CURP = e.cfdi?.curp ?? "",
                    FechaAlta = e.fecha_alta ?? "",
                    FechaBaja = e.fecha_baja ?? "",
                    EsReingreso = e.es_reingreso,
                    FechaReingresoAlta = e.fecha_reingreso_alta ?? "",
                    FechaReingresoBaja = e.fecha_reingreso_baja ?? "",
                    RespetaAntiguedad = e.respeta_antiguedad,
                    EstatusLaboral = estatusLaboral,
                    SalarioDiario = sueldo,
                    Viaticos = viaticos,
                    Otras = otras,
                    SalarioIntegrado = sdi,
                    ISR = 0m,   // se calcula en FormCaptura
                    Banamex = e.datos_personales?.clabe_banamex ?? "",
                    Direccion = e.cfdi?.direccion ?? "",
                    // 🗑️ Calle eliminado — era alias de Direccion
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
                    EmailEmpresa = e.cfdi?.correo_empresarial ?? "",
                    Email = e.cfdi?.correo_personal ?? "",
                    CorreoEmpresaPredeterminado =
                        e.cfdi?.correo_empresa_predeterminado ?? true,
                    TelefonoFijo = e.datos_personales?.telefono_fijo ?? "",
                    TelefonoMovil = e.datos_personales?.telefono_movil ?? "",
                    LADA = e.datos_personales?.lada ?? "",
                    TelefonoEmpresa = e.datos_personales?.telefono_empresa ?? "",
                    Extension = e.datos_personales?.extension ?? "",
                    ActividadEconomica = e.datos_personales?.actividad_economica ?? "",
                    RegimenFiscal = e.datos_personales?.regimen_fiscal ?? "",
                    LinkSAT = e.cfdi?.link_sat ?? "",
                    FechaUltimaMod = e.fecha_ultima_mod ?? "",
                    ModificadoPor = e.modificado_por ?? "",
                    RiesgoIMSS = e.riesgo_imss ?? "",
                    FormaPagoSAT = e.forma_pago_sat ?? "",
                    Departamento = e.departamento ?? "",
                    Puesto = e.puesto ?? "",
                    // 🔧 nombres unificados sin sufijo SAT
                    TipoContrato = e.tipo_contrato ?? "",
                    TipoJornada = e.tipo_jornada ?? "",
                    TipoRegimen = "",   // no viene del RUEP aún — se captura en el Form
                    Obras = (e.obras ?? new List<RuepObra>()).Select(o => new ObraAsignada
                    {
                        NumeroObra = o.obra,
                        Porcentaje = o.porcentaje,
                        Importe = o.importe
                    }).ToList(),
                });
            }

            // 🔧 Auto-resolución de clave_empresa separada del proceso de carga
            //    Solo guarda si realmente falta y se pudo resolver
            IntentarResolverClaveEmpresa();

            // Si tuvimos que calcular salario_integrado por primera vez para alguno
            // (RUEP viejo o recién migrado), persistimos de inmediato para que la
            // próxima carga ya lo reutilice sin recalcular.
            if (huboQueCalcularIntegrado)
                GuardarCambios();
        }

        /// <summary>
        /// Intenta completar clave_empresa si está vacía.
        /// Solo persiste si la clave se resolvió exitosamente.
        /// </summary>
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
        private static RuepEmpleado MapearEmpleadoARuep(EmpleadoCompleto e) =>
            new()
            {
                idEmpleado = e.Id,
                nombre = e.Nombre,
                apellido_paterno = e.ApellidoP,
                apellido_materno = e.ApellidoM,
                rfc = e.RFC,
                imss = e.NSS,
                fecha_alta = e.FechaAlta,
                fecha_baja = e.FechaBaja,
                es_reingreso = e.EsReingreso,
                fecha_reingreso_alta = e.FechaReingresoAlta,
                fecha_reingreso_baja = e.FechaReingresoBaja,
                respeta_antiguedad = e.RespetaAntiguedad,
                estatus_laboral = ResolverEstatusParaGuardar(e),
                riesgo_imss = e.RiesgoIMSS,
                forma_pago_sat = e.FormaPagoSAT,
                departamento = e.Departamento,
                puesto = e.Puesto,
                tipo_contrato = e.TipoContrato,   // 🔧 sin sufijo SAT
                tipo_jornada = e.TipoJornada,    // 🔧 sin sufijo SAT
                salario_integrado = e.SalarioIntegrado,
                ingresos = new RuepIngresos
                {
                    sueldo = e.SalarioDiario,
                    viaticos = e.Viaticos,
                    otras = e.Otras,
                    // ISR: runtime, NO se persiste
                },
                cfdi = new RuepCFDI
                {
                    curp = e.CURP,
                    direccion = e.Direccion,
                    colonia = e.Colonia,
                    ciudad = e.Ciudad,
                    estado = e.Estado,
                    delegacion = e.Municipio,
                    cp = e.CP,
                    referencias = e.Referencias,
                    no_exterior = e.NoExterior,
                    no_interior = e.NoInterior,
                    estado_domicilio = e.EstadoDomicilio,
                    estado_cont_domicilio = e.EstadoContDomicilio,
                    correo_empresarial = e.EmailEmpresa,
                    correo_personal = e.Email,
                    correo_empresa_predeterminado = e.CorreoEmpresaPredeterminado,
                    link_sat = e.LinkSAT,
                },
                datos_personales = new RuepDatosPersonales
                {
                    clabe_banamex = e.Banamex,
                    telefono_fijo = e.TelefonoFijo,
                    telefono_movil = e.TelefonoMovil,
                    lada = e.LADA,
                    telefono_empresa = e.TelefonoEmpresa,
                    extension = e.Extension,
                    actividad_economica = e.ActividadEconomica,
                    regimen_fiscal = e.RegimenFiscal,
                },
                obras = (e.Obras ?? new List<ObraAsignada>()).Select(o => new RuepObra
                {
                    obra = o.NumeroObra,
                    porcentaje = o.Porcentaje,
                    importe = o.Importe
                }).ToList(),
                fecha_ultima_mod = e.FechaUltimaMod,
                modificado_por = e.ModificadoPor,
            };

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