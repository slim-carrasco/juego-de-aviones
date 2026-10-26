using JuegoAviones.Core.Entidades;
using JuegoAviones.Core.Interfaces;

namespace JuegoAviones.Core.CasosDeUso;

public sealed class UsuarioService(IUsuarioRepository usuarios)
{
    public async Task<Usuario> RegistrarAsync(string nombre, string clave, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length is < 3 or > 30)
            throw new ArgumentException("El usuario debe tener entre 3 y 30 caracteres.", nameof(nombre));
        if (clave is null || clave.Length is < 6 or > 128)
            throw new ArgumentException("La contraseña debe tener entre 6 y 128 caracteres.", nameof(clave));
        nombre = nombre.Trim();
        if (await usuarios.ObtenerPorNombreAsync(nombre, cancellationToken) is not null)
            throw new InvalidOperationException("El nombre de usuario ya existe.");
        var usuario = Usuario.Crear(nombre, ClaveHasher.Crear(clave));
        await usuarios.AgregarAsync(usuario, cancellationToken);
        return usuario;
    }

    public async Task<Usuario?> IngresarAsync(string nombre, string clave, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nombre) || clave is null || clave.Length is < 6 or > 128)
            return null;
        var usuario = await usuarios.ObtenerPorNombreAsync(nombre.Trim(), cancellationToken);
        return usuario is not null && ClaveHasher.Verificar(clave, usuario.ClaveHash) ? usuario : null;
    }
}
