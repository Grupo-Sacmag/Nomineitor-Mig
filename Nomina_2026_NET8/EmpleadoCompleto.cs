// EmpleadoCompleto.cs — VERSION LIMPIA para .NET 8
using System.Collections.Generic;

namespace Nomina_2026_NET8
{
    /// <summary>
    /// Una asignación de distribución por obra (equivalente a un slot O_n/por_n/im_n
    /// de maestro.dno en VB6). Solo informativo — no participa en el cálculo de
    /// sueldo/ISR/IMSS, se conserva para reportes de distribución por obra.
    /// </summary>
    public class ObraAsignada
    {
        /// <summary>Clave/número de obra (O_n en VB6).</summary>
        public int NumeroObra { get; set; }

        /// <summary>Porcentaje asociado (por_n en VB6).</summary>
        public decimal Porcentaje { get; set; }

        /// <summary>Importe asociado (im_n en VB6).</summary>
        public decimal Importe { get; set; }
    }

    public class EmpleadoCompleto
    {
        public int Id { get; set; }

        // ── Datos generales ───────────────────────────────────────────
        public string Nombre { get; set; } = "";
        public string ApellidoP { get; set; } = "";
        public string ApellidoM { get; set; } = "";
        public string RFC { get; set; } = "";
        public string CURP { get; set; } = "";
        public string NSS { get; set; } = "";
        public string FechaAlta { get; set; } = "";
        public string FechaBaja { get; set; } = "";

        // ── Es Reingreso ──────────────────────────────────────────────
        public bool EsReingreso { get; set; }
        public string FechaReingresoAlta { get; set; } = "";
        public string FechaReingresoBaja { get; set; } = "";
        public bool RespetaAntiguedad { get; set; } = true;
        public string EstatusLaboral { get; set; } = "";

        // ── Ingresos ──────────────────────────────────────────────────
        public decimal SalarioDiario { get; set; }
        public decimal Viaticos { get; set; }
        public decimal Otras { get; set; }

        /// <summary>
        /// Calculado por RuepService y persistido en RUEP.dat (salario_integrado).
        /// Fórmula: (SalarioDiario + Viaticos + Otras) × factor de antigüedad, tope 25 × UMA.
        /// Se recalcula automáticamente cuando cambian los ingresos base o la fecha de
        /// alta (ver FormEDICIONCENTRALPERSONAL.AplicarCambiosAlEmpleado); en cualquier
        /// otro caso se reutiliza el valor ya guardado, para no recalcular en cada carga.
        /// </summary>
        public decimal SalarioIntegrado { get; set; }

        /// <summary>ISR mensual estimado. Calculado en runtime. NO se persiste.</summary>
        public decimal ISR { get; set; }

        // ── Banco ─────────────────────────────────────────────────────
        public string Banamex { get; set; } = "";

        // ── Domicilio ─────────────────────────────────────────────────
        /// <summary>
        /// Dirección completa de la vialidad (tipoVialidad + nombreDeVialidad en BD).
        /// Equivale a cfdi.direccion en RUEP.dat.
        /// </summary>
        public string Direccion { get; set; } = "";
        // 🗑️ Calle → ELIMINADO (alias de Direccion, nadie lo leía)

        public string Colonia { get; set; } = "";

        /// <summary>Localidad. Viene de cfdi.ciudad en RUEP.dat.</summary>
        public string Ciudad { get; set; } = "";

        /// <summary>Municipio/Alcaldía. Viene de cfdi.delegacion en RUEP.dat.</summary>
        public string Municipio { get; set; } = "";

        public string Estado { get; set; } = "";
        public string CP { get; set; } = "";

        // ── Domicilio extendido ───────────────────────────────────────
        // Nota: estos campos NO existen en los .dno originales (Perscfdi.dno no los
        // trae) — nacen vacíos en la migración y se llenan manualmente después.
        public string Referencias { get; set; } = "";
        public string NoExterior { get; set; } = "";
        public string NoInterior { get; set; } = "";
        public string EstadoDomicilio { get; set; } = "";
        public string EstadoContDomicilio { get; set; } = "";

        // ── Correos ───────────────────────────────────────────────────
        public string Email { get; set; } = "";  // correo_personal
        public string EmailEmpresa { get; set; } = "";  // correo_empresarial

        /// <summary>true = usar EmailEmpresa al timbrar CFDI (default).</summary>
        public bool CorreoEmpresaPredeterminado { get; set; } = true;

        // ── Teléfonos ─────────────────────────────────────────────────
        public string TelefonoFijo { get; set; } = "";
        public string TelefonoMovil { get; set; } = "";
        public string LADA { get; set; } = "";
        public string TelefonoEmpresa { get; set; } = "";
        public string Extension { get; set; } = "";

        // ── Fiscales ──────────────────────────────────────────────────
        public string ActividadEconomica { get; set; } = "";
        public string RegimenFiscal { get; set; } = "";
        public string LinkSAT { get; set; } = "";

        // ── Datos SAT / IMSS ──────────────────────────────────────────
        /// <summary>Clase de riesgo de trabajo (I-V). Informativo/CFDI — NO afecta
        /// el cálculo de IMSS obrero en CalculadoraIMSS (eso es cuota patronal).</summary>
        public string RiesgoIMSS { get; set; } = "";
        public string FormaPagoSAT { get; set; } = "";
        public string Departamento { get; set; } = "";
        public string Puesto { get; set; } = "";

        // 🔧 Unificados: eliminados los duplicados con sufijo SAT
        // TipoContrato    reemplaza a TipoContrato + TipoContratoSAT
        // TipoJornada     reemplaza a TipoJornada  + TipoJornadaSAT
        // TipoRegimen     reemplaza a TipoRegimenSAT
        public string TipoContrato { get; set; } = "";
        public string TipoJornada { get; set; } = "";
        public string TipoRegimen { get; set; } = "";

        // ── Distribución por obra (maestro.dno) ─────────────────────────
        /// <summary>
        /// Distribución por obra migrada desde maestro.dno. Puramente informativo
        /// para reportes — no participa en sueldo/ISR/IMSS.
        /// </summary>
        public List<ObraAsignada> Obras { get; set; } = new();

        // ── Auditoría ─────────────────────────────────────────────────
        public string FechaUltimaMod { get; set; } = "";
        public string ModificadoPor { get; set; } = "";
    }
}