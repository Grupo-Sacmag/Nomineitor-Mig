// CredentialProtector.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Nomina_2026_NET8
{
    /// <summary>
    /// Cifra/descifra secretos usando DPAPI (Windows Data Protection API).
    /// El secreto queda ligado a la máquina donde se ejecuta — un archivo
    /// copiado a otra PC no se puede descifrar ahí.
    /// </summary>
    public static class CredentialProtector
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Nomina2026-SMTP-v1");

        public static void GuardarSecreto(string ruta, string textoPlano)
        {
            byte[] datos = Encoding.UTF8.GetBytes(textoPlano);
            byte[] cifrado = ProtectedData.Protect(datos, Entropy, DataProtectionScope.LocalMachine);
            File.WriteAllBytes(ruta, cifrado);
        }

        public static string LeerSecreto(string ruta)
        {
            if (!File.Exists(ruta)) return null;
            byte[] cifrado = File.ReadAllBytes(ruta);
            byte[] datos = ProtectedData.Unprotect(cifrado, Entropy, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(datos);
        }               
    }    
}