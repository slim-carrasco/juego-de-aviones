using System.Security.Cryptography;

namespace JuegoAviones.Core.CasosDeUso;

public static class ClaveHasher
{
    private const int Iteraciones = 150_000;

    public static string Crear(string clave)
    {
        if (clave.Length is < 6 or > 128)
            throw new ArgumentException("La contraseña debe tener entre 6 y 128 caracteres.", nameof(clave));
        byte[] sal = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2_sha256${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string clave, string guardado)
    {
        string[] partes = guardado.Split('$');
        if (partes.Length != 4 || partes[0] != "pbkdf2_sha256" ||
            !int.TryParse(partes[1], out int iteraciones) || iteraciones is < 100_000 or > 1_000_000)
            return false;
        try
        {
            byte[] sal = Convert.FromBase64String(partes[2]);
            byte[] esperado = Convert.FromBase64String(partes[3]);
            if (sal.Length != 16 || esperado.Length != 32) return false;
            byte[] obtenido = Rfc2898DeriveBytes.Pbkdf2(clave, sal, iteraciones, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(obtenido, esperado);
        }
        catch (FormatException) { return false; }
    }
}
