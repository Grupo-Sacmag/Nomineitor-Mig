// DatosCFDIEmpleado.cs — VERSION LIMPIA para .NET 8
namespace Nomina_2026_NET8
{
    /// <summary>
    /// DTO que transporta datos calculados de un empleado
    /// desde FormCaptura hacia FormNOMCFDIPlantillas.
    ///
    /// Equivalencias VB6:
    ///   sue3=Salario, t_per=TotalIngresos, t_ded=TotalDeducciones,
    ///   T_neto=Neto, isr12=ISR, sub13=Subsidio, subc13=SubsidioCausado,
    ///   ims14=IMSS, pre15=Prestamos, fon16=FONACOT, pea17=PensionAlimenticia,
    ///   ifv18=INFONAVIT, pva8=PrimaVacacional, pee10=PrimaVacacionalExenta,
    ///   otr9=Otras, via7=OF, t_grav=Gravado, t_ext=PercExenta
    /// </summary>
    public class DatosCFDIEmpleado
    {
        // ── Identificación ────────────────────────────────────────────
        public int NumeroEmpleado { get; set; }
        public string Nombre { get; set; }
        public int DiasTrabajados { get; set; }

        // ── Percepciones ──────────────────────────────────────────────
        public decimal Salario { get; set; }
        public decimal OF { get; set; }
        public decimal PrimaVacacional { get; set; }
        public decimal Otras { get; set; }
        public decimal PercExenta { get; set; }

        // ── Nóminas especiales ────────────────────────────────────────
        public decimal AguinaldoGravado { get; set; }
        public decimal AguinaldoExento { get; set; }
        public decimal PTUGravado { get; set; }
        public decimal PTUExento { get; set; }
        public decimal Bono { get; set; }

        // ── Totales calculados ────────────────────────────────────────
        public decimal TotalIngresos { get; set; }
        public decimal ISR { get; set; }
        public decimal Subsidio { get; set; }
        public decimal SubsidioCausado { get; set; }
        public decimal IMSS { get; set; }
        public decimal Prestamos { get; set; }
        public decimal FONACOT { get; set; }
        public decimal PensionAlimenticia { get; set; }
        public decimal INFONAVIT { get; set; }
        public decimal TotalDeducciones { get; set; }
        public decimal Neto { get; set; }

        /// <summary>Pensión alimenticia (D007 en CFDI). Antes mal documentado como "generalmente 0".</summary>
        public decimal OtrasDeducciones { get; set; }

        public string BANAMEX { get; set; }

        // ── Del EmpleadoCompleto (RUEP) ───────────────────────────────
        public string RFC { get; set; }
        public string CURP { get; set; }
        public string NSS { get; set; }
        public string FechaAlta { get; set; }
        public decimal SalarioDiario { get; set; }
        public decimal SalarioDiarioInteg { get; set; }
        public string RiesgoIMSS { get; set; }
        public string RFCLabora { get; set; }
        public string RegistroPatronal { get; set; }
        public string MetodoPago { get; set; }
        public string FormaPagoSAT { get; set; }

        // 🔧 Sufijos SAT eliminados — consistente con EmpleadoCompleto
        public string TipoRegimen { get; set; }
        public string TipoContrato { get; set; }
        public string TipoJornada { get; set; }
        public string PeriodicidadPago { get; set; }

        public string Departamento { get; set; }
        public string Puesto { get; set; }

        // ── Domicilio ─────────────────────────────────────────────────
        public string Direccion { get; set; }
        public string Colonia { get; set; }
        public string Ciudad { get; set; }
        public string Estado { get; set; }
        public string Delegacion { get; set; }
        public string CodigoPostal { get; set; }
        public string Correo { get; set; }


        // ── Valores exactos equivalentes a reng() VB6 ────────────────
        public decimal Gravado { get; set; }
        public decimal Exento { get; set; }

        public decimal ExentoOriginal { get; set; }

        public decimal Aguinaldo { get; set; }

        public decimal PTU1 { get; set; }
        public decimal PTU2 { get; set; }
        public decimal PTU3 { get; set; }

        public decimal OtrosIngresosTotal { get; set; }

        public bool TieneISROriginal { get; set; }
        public bool TieneAguinaldoOriginal { get; set; }
        public bool TienePTUOriginal { get; set; }
        public bool TieneExentoOriginal { get; set; }
    }
}