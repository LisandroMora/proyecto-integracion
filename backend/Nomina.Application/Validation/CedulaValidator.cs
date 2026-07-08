namespace Nomina.Application.Validation;

public static class CedulaValidator
{
    // Deja solo dígitos (quita guiones y espacios).
    public static string Normalize(string? cedula) =>
        (cedula ?? string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).Trim();

    // Algoritmo oficial JCE: 10 dígitos con pesos alternos 1-2 (Luhn simplificado),
    // suma con "casting out nines", y verificador = (10 - suma % 10) % 10.
    public static bool EsValida(string? cedula)
    {
        var normalizada = Normalize(cedula);

        if (normalizada.Length != 11 || !normalizada.All(char.IsDigit))
            return false;

        int[] pesos = { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 };
        int suma = 0;

        for (int i = 0; i < 10; i++)
        {
            int producto = (normalizada[i] - '0') * pesos[i];
            suma += producto < 10 ? producto : producto - 9;
        }

        int verificadorEsperado = (10 - (suma % 10)) % 10;
        int verificadorReal = normalizada[10] - '0';

        return verificadorEsperado == verificadorReal;
    }
}
