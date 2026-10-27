// LogService.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Nomina_2026_NET8
{
    public enum ResultadoPagoEspecial
    {
        Correspondiente,  // se le liberaron días y sí se le pagó dentro del rango calculado
        Excedido,          // se le pagaron más días de los que le correspondían → dispara correo
        NoRecibido         // el contador confirmó que se le pagaría, pero al final no se le dieron los días extra
    }

    public static class LogService
    {
        private static readonly object _lock = new object();
        public static string CarpetaLogs(string nombreCarpetaEmpresa, int anio) => AppPaths.CarpetaEmpresaAnio(nombreCarpetaEmpresa, anio);

        public static void RegistrarEventoNomina(string nombreEmpleado, ResultadoPagoEspecial resultado, string nombreCarpetaEmpresa, int anio)
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(CarpetaLogs(nombreCarpetaEmpresa, anio));
                    string rutaLog = Path.Combine(CarpetaLogs(nombreCarpetaEmpresa, anio), "Logs.txt");
                    string rutaHash = Path.Combine(CarpetaLogs(nombreCarpetaEmpresa, anio), "Logs.hash");

                    string encabezado = $"------ {DateTime.Now:dd/MM/yyyy} ------";
                    string detalle = resultado switch
                    {
                        ResultadoPagoEspecial.Excedido => $"- \"{nombreEmpleado}\" recibió el pago superando los días que le correspondían.",
                        ResultadoPagoEspecial.Correspondiente => $"- \"{nombreEmpleado}\" recibió el pago correspondiente a la quincena que entró más la actual.",
                        ResultadoPagoEspecial.NoRecibido => $"- \"{nombreEmpleado}\" no recibió el pago de días anteriores.",
                        _ => $"- \"{nombreEmpleado}\" evento no clasificado."
                    };
                    string autoria = $"    (usuario: {Environment.UserName}, equipo: {Environment.MachineName})";

                    var lineas = new List<string>();
                    if (ObtenerUltimoEncabezado(rutaLog) != encabezado) lineas.Add(encabezado);
                    lineas.Add(detalle);
                    lineas.Add(autoria);

                    File.AppendAllLines(rutaLog, lineas, Encoding.UTF8);
                    ActualizarSelloDeIntegridad(rutaLog, rutaHash);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LogService.RegistrarEventoNomina] {ex.Message}");
                }
            }
        }

        private static string ObtenerUltimoEncabezado(string rutaLog)
        {
            if (!File.Exists(rutaLog)) return null;
            return File.ReadAllLines(rutaLog).LastOrDefault(l => l.StartsWith("------"));
        }

        private static void ActualizarSelloDeIntegridad(string rutaLog, string rutaHash)
        {
            byte[] contenido = File.ReadAllBytes(rutaLog);
            byte[] hmac = CryptoService.ComputeHmac(contenido);
            File.WriteAllText(rutaHash, Convert.ToBase64String(hmac));
        }

        public static bool ArchivoFueAlterado(string nombreCarpetaEmpresa, int anio)
        {
            string rutaLog = Path.Combine(CarpetaLogs(nombreCarpetaEmpresa, anio), "Logs.txt");
            string rutaHash = Path.Combine(CarpetaLogs(nombreCarpetaEmpresa, anio), "Logs.hash");
            if (!File.Exists(rutaLog) || !File.Exists(rutaHash)) return false;

            byte[] hmacActual = CryptoService.ComputeHmac(File.ReadAllBytes(rutaLog));
            string hmacGuardado = File.ReadAllText(rutaHash).Trim();
            return Convert.ToBase64String(hmacActual) != hmacGuardado;
        }
    }
}