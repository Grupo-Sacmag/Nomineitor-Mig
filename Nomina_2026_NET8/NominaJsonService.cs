// NominaJsonService.cs — lógica extraída del Form
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nomina_2026_NET8
{
    // Servicio para leer archivos de nómina cifrados (.json).
    // Extraído de FormAcumulados para que FormCaptura también pueda usarlo sin depender del Form.
    public static class NominaJsonService
    {
        // Lee y descifra un archivo .json de nómina. Lanza excepción si el HMAC es inválido o el formato es incorrecto.
        public static List<NominaJSON> Cargar(string rutaArchivo)
        {
            string raw = File.ReadAllText(rutaArchivo, Encoding.UTF8).Trim().Trim('\uFEFF', '\u200B');

            var paquete = JsonConvert.DeserializeObject<EncryptedJsonPackage>(raw) ?? throw new InvalidDataException("Paquete inválido.");

            if (string.IsNullOrWhiteSpace(paquete.Data))
                throw new InvalidDataException("Data vacío.");
            if (string.IsNullOrWhiteSpace(paquete.Hmac))
                throw new InvalidDataException("Hmac vacío.");

            byte[] ivPlusCipher = Convert.FromBase64String(paquete.Data);
            byte[] hmacBytes = Convert.FromBase64String(paquete.Hmac);

            if (!CryptoService.ValidateHmac(ivPlusCipher, hmacBytes))
                throw new InvalidDataException("HMAC inválido: archivo posiblemente alterado.");

            string jsonPlano = CryptoService.Decrypt(ivPlusCipher);

            return JsonConvert.DeserializeObject<List<NominaJSON>>(jsonPlano) ?? throw new InvalidDataException("No se pudo interpretar el JSON plano.");
        }
    }
}