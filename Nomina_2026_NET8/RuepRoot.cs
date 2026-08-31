using Newtonsoft.Json;
using System.Collections.Generic;

namespace Nomina_2026_NET8
{
    // ── EMPRESA ───────────────────────────────────────────────────────
    public class RuepRoot
    {
        public int idEmpresa { get; set; }
        public string nombre { get; set; }
        public string clave_empresa { get; set; }
        public int ano_fiscal { get; set; }
        public decimal salario_minimo { get; set; }
        public decimal uma_por_dia { get; set; }
        public string fecha_actualizacion { get; set; }
        public string modificado_por { get; set; }

        public DatosFiscales datos_fiscales { get; set; }

        // Equivalente moderno de EMP_CFDI.DNO
        public ConfiguracionCfdiNomina cfdi_nomina { get; set; } = new();

        public List<RuepEmpleado> personal { get; set; }
    }

    public class DatosFiscales
    {
        [JsonProperty("rfc_empresa")]
        public string rfc { get; set; }
        public string registro_patronal { get; set; }
        public string direccion { get; set; }
        public string lugar_expedicion { get; set; }
        public string regimen_fiscal { get; set; }
        public RepresentanteLegal representante_legal { get; set; }
        public BanamexEmpresa banamex { get; set; }
    }

    public class RepresentanteLegal
    {
        public string apellido_paterno { get; set; }
        public string apellido_materno { get; set; }
        public string nombre { get; set; }
        [JsonProperty("rfc_representante")]
        public string rfc { get; set; }
        public string curp { get; set; }
    }

    public class BanamexEmpresa
    {
        public string sucursal { get; set; }
        public string cuenta { get; set; }
        public string cliente { get; set; }
    }

    // ── DISTRIBUCIÓN POR OBRA (maestro.dno) ──────────────────────────
    /// <summary>
    /// Un slot de distribución por obra (equivalente a O_n/por_n/im_n en el
    /// Type "ob" de maestro.dno). Solo informativo para reportes — no participa
    /// en el cálculo de sueldo/ISR/IMSS.
    /// </summary>
    public class RuepObra
    {
        public int obra { get; set; }
        public decimal porcentaje { get; set; }
        public decimal importe { get; set; }
    }

    // ── EMPLEADO ──────────────────────────────────────────────────────
    public class RuepEmpleado
    {
        public int idEmpleado { get; set; }
        public string nombre { get; set; }
        public string apellido_paterno { get; set; }
        public string apellido_materno { get; set; }
        public string rfc { get; set; }
        public string imss { get; set; }

        // ── Primer ciclo laboral ─────────────────────────────────────────
        // Estos campos se conservan siempre, aunque exista reingreso.
        public string fecha_alta { get; set; }
        public string fecha_baja { get; set; }

        // ── Reingreso ────────────────────────────────────────────────────
        public bool es_reingreso { get; set; }
        public string fecha_reingreso_alta { get; set; }
        public string fecha_reingreso_baja { get; set; }
        public bool respeta_antiguedad { get; set; } = true;
        public string estatus_laboral { get; set; }

        public string riesgo_imss { get; set; }
        public string forma_pago_sat { get; set; }
        public string departamento { get; set; }
        public string puesto { get; set; }
        public string tipo_contrato { get; set; }
        public string tipo_jornada { get; set; }
        public RuepIngresos ingresos { get; set; }
        public RuepCFDI cfdi { get; set; }
        public RuepDatosPersonales datos_personales { get; set; }

        /// <summary>
        /// Salario diario integrado, PERSISTIDO (a diferencia del comportamiento previo
        /// que lo recalculaba en cada carga). Nullable para retrocompatibilidad: un
        /// RUEP.dat generado antes de este cambio no trae el campo y deserializa a null;
        /// RuepService lo calcula una sola vez en ese caso y lo guarda de inmediato.
        /// </summary>
        public decimal? salario_integrado { get; set; }

        /// <summary>Distribución por obra migrada desde maestro.dno. Solo informativo.</summary>
        public List<RuepObra> obras { get; set; } = new();

        public string fecha_ultima_mod { get; set; }
        public string modificado_por { get; set; }
    }

    /// Ingresos base diarios del empleado.
    /// ISR es valor runtime — NO se persiste. SalarioIntegrado SÍ se persiste
    /// (ver RuepEmpleado.salario_integrado).
    public class RuepIngresos
    {
        public decimal sueldo { get; set; }
        public decimal viaticos { get; set; }
        public decimal otras { get; set; }
    }

    public class RuepCFDI
    {
        public string curp { get; set; }

        // ── Dirección ─────────────────────────────────────────────────
        // "direccion" = campo legado (todo en uno) — usado por RuepService
        // "tipo_vialidad" + "nombre_vialidad" = campos BD remota (BDSyncService)
        // BDSyncService combina ambos → direccion al sincronizar
        public string direccion { get; set; }
        public string tipo_vialidad { get; set; }
        public string nombre_vialidad { get; set; }

        public string no_exterior { get; set; }
        public string no_interior { get; set; }
        public string colonia { get; set; }
        public string ciudad { get; set; }
        public string estado { get; set; }
        public string delegacion { get; set; }  // = municipio/alcaldía
        public string cp { get; set; }
        public string referencias { get; set; }
        public string estado_domicilio { get; set; }
        public string estado_cont_domicilio { get; set; }
        public string correo_empresarial { get; set; }
        public string correo_personal { get; set; }
        public bool correo_empresa_predeterminado { get; set; }
        public string link_sat { get; set; }
    }

    public class RuepDatosPersonales
    {
        public string clabe_banamex { get; set; }
        public string telefono_fijo { get; set; }
        public string telefono_movil { get; set; }
        public string lada { get; set; }
        public string telefono_empresa { get; set; }
        public string extension { get; set; }
        public string actividad_economica { get; set; }
        public string regimen_fiscal { get; set; }
    }

    public class ConfiguracionCfdiNomina
    {
        // EmpCFDI.Folio
        public long folio { get; set; }

        // EmpCFDI.serie
        public string serie { get; set; } = "";

        // EmpCFDI.Consecutivo
        public int consecutivo { get; set; }

        // EmpCFDI.RegPatr
        public string registro_patronal { get; set; } = "";

        // EmpCFDI.RiesgoImss
        public string riesgo_imss { get; set; } = "";
    }
}