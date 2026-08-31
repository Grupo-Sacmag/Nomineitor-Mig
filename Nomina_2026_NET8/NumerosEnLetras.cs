// NumerosEnLetras.cs — extraído de FormNOMCFDIPlantillas
namespace Nomina_2026_NET8
{
    // Convierte importes decimales a texto en español para documentos fiscales.
    // Ejemplo: 1250.75 → "MIL DOSCIENTOS CINCUENTA PESOS 75/100 M.N."
    public static class NumerosEnLetras
    {
        public static string Convertir(decimal numero)
        {
            if (numero < 0) return "MENOS " + Convertir(-numero);

            long entero = (long)Math.Floor(numero);
            int centavos = (int)Math.Round((numero - entero) * 100);
            string letra = string.IsNullOrEmpty(ConvertirEntero(entero)) ? "CERO" : ConvertirEntero(entero).Trim().ToUpper();

            return $"{letra} PESOS {centavos:00}/100 M.N.";
        }

        private static string ConvertirEntero(long n)
        {
            if (n == 0) return "";
            if (n < 0) return "MENOS " + ConvertirEntero(-n);

            string[] unidades = { "", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE", "DIEZ", "ONCE", 
                "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE" };

            string[] decenas = { "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };

            // 🔧 Formas compuestas del 21-29 (correcto en español fiscal)
            string[] veintiX =
            {
                "", "VEINTIÚN", "VEINTIDÓS", "VEINTITRÉS", "VEINTICUATRO",
                "VEINTICINCO", "VEINTISÉIS", "VEINTISIETE", "VEINTIOCHO",
                "VEINTINUEVE"
            };

            string[] centenas = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", 
                "OCHOCIENTOS", "NOVECIENTOS" };

            if (n < 20) return unidades[n];

            // Formas compuestas 21-29
            if (n >= 21 && n <= 29) return veintiX[n - 20];

            if (n == 100) return "CIEN";

            if (n < 100)
            {
                string dec = decenas[n / 10];
                string uni = n % 10 > 0 ? " Y " + unidades[n % 10] : "";
                return dec + uni;
            }

            if (n < 1_000)
                return centenas[n / 100] + (n % 100 > 0 ? " " + ConvertirEntero(n % 100) : "");

            if (n < 2_000)
                return "MIL" + (n % 1000 > 0 ? " " + ConvertirEntero(n % 1000) : "");

            if (n < 1_000_000)
            {
                string miles = ConvertirEntero(n / 1000) + " MIL";
                return miles + (n % 1000 > 0 ? " " + ConvertirEntero(n % 1000) : "");
            }

            if (n == 1_000_000) return "UN MILLÓN";

            if (n < 2_000_000)
                return "UN MILLÓN" + (n % 1_000_000 > 0 ? " " + ConvertirEntero(n % 1_000_000) : "");

            string mill = ConvertirEntero(n / 1_000_000) + " MILLONES";
            return mill + (n % 1_000_000 > 0 ? " " + ConvertirEntero(n % 1_000_000) : "");
        }
    }
}