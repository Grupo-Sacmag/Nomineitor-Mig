using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomina_2026_NET8
{
    /// Formato en archivo: { Data, Hmac, FechaGeneracion, TipoNomina, UsuarioModificacion, Quincena }
    public class EncryptedJsonPackage
    {
        public string Data { get; set; }
        public string Hmac { get; set; }
        public string FechaGeneracion { get; set; }
        public string TipoNomina { get; set; }
        public string UsuarioModificacion { get; set; }
        public string Quincena { get; set; }
    }

    /// Registro individual de nómina dentro del payload descifrado.
    public class NominaJSON
    {
        public string idNomina { get; set; }
        public string Nombre { get; set; }
        public decimal DiasT { get; set; }
        public decimal Aguinaldo { get; set; }
        public decimal PTU { get; set; }
        public decimal Bono { get; set; }
        public decimal Compensaciones { get; set; }
        public decimal PrimaAntiguedad { get; set; }
        public decimal Indemnizacion { get; set; }
        public decimal SueldoVacaciones { get; set; }
        public decimal AguinaldoFiniquito { get; set; }
        public decimal PrimaVacacionalFiniquito { get; set; }
        public decimal ISRCalculado { get; set; }     // ImpTot
        public decimal SubsidioAplicado { get; set; } // subapl
        public decimal SubsidioNoAplicado { get; set; } // subNap
        public decimal CreditoAplicado { get; set; } // CreTot
        public decimal CreditoPagado { get; set; } // CredNe
        public decimal Sueldo { get; set; }
        public decimal hsNorm { get; set; }
        public decimal hsDobles { get; set; }
        public decimal hsTriples { get; set; }
        public decimal OF { get; set; }
        public decimal Pvacacional { get; set; }
        public decimal Otras { get; set; }
        public decimal PercExenta { get; set; }
        public decimal TotalIngr { get; set; }
        public decimal ISTP { get; set; }
        public decimal SubPEmpl { get; set; }
        public decimal IMSS { get; set; }
        public decimal Prestamos { get; set; }
        public decimal Fonacot { get; set; }
        public decimal PensionAlimenticia { get; set; }
        public decimal Infonavit { get; set; }
        public decimal TotalDeduc { get; set; }
        public decimal Neto { get; set; }
        public string Banamex { get; set; }
    }
}
