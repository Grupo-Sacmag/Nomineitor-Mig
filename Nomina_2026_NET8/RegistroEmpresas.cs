using Nomina_2026_NET8;
using System.Text;

public static class RegistroEmpresas
{
    private static readonly string RutaIndice = Path.Combine(AppPaths.Raiz, "_indice_empresas.txt");
    // Formato por línea: RutaTrabajo|NombreCarpetaEmpresa

    public static string ObtenerNombreCarpeta(string rutaTrabajo)
    {
        if (!File.Exists(RutaIndice)) return null;

        return File.ReadAllLines(RutaIndice, Encoding.UTF8).Select(l => l.Split('|')).Where(p => p.Length == 2 && string.Equals(p[0], rutaTrabajo, StringComparison.OrdinalIgnoreCase))
            .Select(p => p[1]).FirstOrDefault();
    }

    public static void Registrar(string rutaTrabajo, string nombreCarpeta)
    {
        Directory.CreateDirectory(AppPaths.Raiz);

        var lineas = File.Exists(RutaIndice) ? File.ReadAllLines(RutaIndice, Encoding.UTF8).ToList() : new List<string>();

        lineas.RemoveAll(l => l.StartsWith(rutaTrabajo + "|", StringComparison.OrdinalIgnoreCase));
        lineas.Add($"{rutaTrabajo}|{nombreCarpeta}");

        File.WriteAllLines(RutaIndice, lineas, Encoding.UTF8);
    }
}