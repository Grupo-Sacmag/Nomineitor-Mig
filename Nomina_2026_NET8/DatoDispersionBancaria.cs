// DatoDispersionBancaria.cs — VERSION LIMPIA para .NET 8
namespace Nomina_2026_NET8
{
    public class DatoDispersionBancaria
    {
        /// <summary>Número de registro / ID interno del empleado.</summary>
        public int NumeroRegistro { get; set; }

        /// <summary>
        /// Cuenta bancaria Banamex.
        /// Tipo "01" (con guión): SUCURSAL-CUENTA  → ej. "0123-1234567"
        /// Tipo "03" (sin guión): número de tarjeta de 16 dígitos
        /// La detección es automática en FormTraspasoNomBanamex.DetectarTipoCuenta.
        /// </summary>
        public string CuentaTarjeta { get; set; }

        public string Nombre { get; set; }
        public string ApellidoP { get; set; }
        public string ApellidoM { get; set; }

        /// <summary>Importe neto a dispersar.</summary>
        public decimal Importe { get; set; }
    }
}