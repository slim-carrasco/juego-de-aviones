using JuegoAviones.Core.Entidades;

namespace JuegoAviones.Core.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorNombreAsync(string nombre, CancellationToken cancellationToken);
    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken);
}
