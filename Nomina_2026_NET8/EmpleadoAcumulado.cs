// EmpleadoAcumulado.cs — clase extraída de FormAcumulados
namespace Nomina_2026_NET8
{
    // Representa un empleado leído desde los archivos binarios VB6 (personal.dno + Bnxcla.dno) para el módulo de acumulados.
    // Distinto de EmpleadoCompleto que viene del RUEP.dat.
    public class EmpleadoAcumulado
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string RFCE { get; set; }
        public string IMSSE { get; set; }
        public string CURPE { get; set; }
        public string FechaAltaE { get; set; }
        public string FechaBajaE { get; set; }
        public decimal SalarioDiarioE { get; set; }
        public decimal ViaticosE { get; set; }
        public decimal OtrasE { get; set; }
        public decimal IntegradoE { get; set; }
        public string BanamexE { get; set; }
    }
}