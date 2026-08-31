// CryptoService.cs — VERSION LIMPIA para .NET 8
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Nomina_2026_NET8
{
    public static class CryptoService
    {
        // ── Clave AES ─────────────────────────────────────────────────────
        // Lazy: se carga solo cuando se necesita, no al arrancar la app.
        // Si falla, lanza excepción con mensaje claro en el lugar correcto.
        private static readonly Lazy<byte[]> _key = new(() =>
        {
            string b64 = Properties.Settings.Default.AesKey;
            if (string.IsNullOrWhiteSpace(b64))
                throw new InvalidOperationException("La clave AES no está configurada en Settings (AesKey).");
            return Convert.FromBase64String(b64);
        });

        private static byte[] Key => _key.Value;

        // ── Cifrado / Descifrado base ─────────────────────────────────────

        /// <summary>Cifra texto plano. Devuelve IV[16] + cipher concatenados.</summary>
        public static byte[] Encrypt(string plainText, out byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            iv = aes.IV;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }

        /// <summary>Descifra un bloque IV(16 bytes) + cipher.</summary>
        public static string Decrypt(byte[] ivPlusCipher)
        {
            if (ivPlusCipher == null || ivPlusCipher.Length <= 16)
                return string.Empty;

            byte[] iv = ivPlusCipher[..16];          // rango .NET 8
            byte[] cipher = ivPlusCipher[16..];

            return DecryptInternal(cipher, iv);
        }

        /// <summary>Descifra pasando IV y cipher por separado.</summary>
        public static string Decrypt(byte[] cipherText, byte[] iv)
            => DecryptInternal(cipherText, iv);

        // ── Archivos cifrados ─────────────────────────────────────────────

        /// <summary>Lee un archivo cifrado (IV[16] + cipher) y deserializa.</summary>
        public static T LoadEncrypted<T>(string path)
        {
            byte[] fileBytes = File.ReadAllBytes(path);
            string json = Decrypt(fileBytes);
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Serializa y guarda un objeto en archivo cifrado (IV[16] + cipher).
        /// Ahora reutiliza Encrypt() en lugar de duplicar la lógica AES.
        /// </summary>
        public static void SaveEncrypted<T>(string path, T data, bool indented = false)
        {
            string json = JsonConvert.SerializeObject(
                data, indented ? Formatting.Indented : Formatting.None);

            // Reutiliza Encrypt() — sin duplicar código AES
            byte[] cipher = Encrypt(json, out byte[] iv);

            byte[] finalBytes = new byte[iv.Length + cipher.Length];
            iv.CopyTo(finalBytes, 0);
            cipher.CopyTo(finalBytes, iv.Length);

            File.WriteAllBytes(path, finalBytes);
        }

        // ── HMAC ──────────────────────────────────────────────────────────

        public static byte[] ComputeHmac(byte[] data)
        {
            using var hmac = new HMACSHA256(Key);
            return hmac.ComputeHash(data);
        }

        public static bool ValidateHmac(byte[] data, byte[] expectedHmac)
            => ComputeHmac(data).SequenceEqual(expectedHmac);

        // ── Privado ───────────────────────────────────────────────────────

        /// <summary>
        /// Implementación real del descifrado.
        /// Lanza CryptographicException si la clave o formato es incorrecto —
        /// el CALLER decide si muestra UI o loguea.
        /// </summary>
        private static string DecryptInternal(byte[] cipherText, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(
                cipherText, 0, cipherText.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}