using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Nomina_2026_NET8
{
    public static class EmailService
    {
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string CuentaEnvia = "sacmag.proveedores@gmail.com";   // no es secreto, puede ir en código
        private const string CuentaRecibe = "desarrollo.conta@grupo-sacmag.com.mx";

        private static string RutaSecreto => Path.Combine(AppPaths.Raiz, "smtp.secure");

        public static async Task NotificarPagoExcedidoAsync(string nombreEquipo, string nombreEmpresa, string nombreEmpleado)
        {
            string claveApp = CredentialProtector.LeerSecreto(RutaSecreto);
            if (string.IsNullOrEmpty(claveApp))
            {
                EncolarParaReintento(nombreEquipo, nombreEmpresa, nombreEmpleado);
                return; // sin credencial configurada, no truena, solo encola
            }

            string cuerpo = $"Equipo: {nombreEquipo}\nEmpresa: {nombreEmpresa}\nEmpleado: {nombreEmpleado}\n" + $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}\n\n" +
                "Se detectó que este empleado recibió un pago superando los días hábiles que le correspondían por su fecha de alta, durante la captura de nómina.";

            try
            {
                using var mensaje = new MailMessage(CuentaEnvia, CuentaRecibe, "Aviso: pago de nómina superando días correspondientes", cuerpo);
                using var cliente = new SmtpClient(SmtpHost, SmtpPort)
                {
                    Credentials = new NetworkCredential(CuentaEnvia, claveApp),
                    EnableSsl = true,
                    Timeout = 8000
                };
                await cliente.SendMailAsync(mensaje);
            }
            catch (Exception)
            {
                EncolarParaReintento(nombreEquipo, nombreEmpresa, nombreEmpleado);
            }
        }

        private static void EncolarParaReintento(string nombreEquipo, string nombreEmpresa, string nombreEmpleado)
        {
            string rutaCola = Path.Combine(AppPaths.Raiz, "CorreosPendientes.txt");
            string linea = $"{DateTime.Now:O}|{nombreEquipo}|{nombreEmpresa}|{nombreEmpleado}";
            File.AppendAllLines(rutaCola, new[] { linea }, Encoding.UTF8);
        }

        public static async Task ReintentarPendientesAsync()
        {
            string rutaCola = Path.Combine(AppPaths.Raiz, "CorreosPendientes.txt");
            if (!File.Exists(rutaCola)) return;

            var restantes = new List<string>();
            foreach (var linea in File.ReadAllLines(rutaCola))
            {
                var partes = linea.Split('|');
                if (partes.Length != 4) continue;
                try { await NotificarPagoExcedidoAsync(partes[1], partes[2], partes[3]); }
                catch { restantes.Add(linea); }
            }
            File.WriteAllLines(rutaCola, restantes, Encoding.UTF8);
        }
    }
}