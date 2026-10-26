using JuegoAviones.Core.Entidades;
using JuegoAviones.Core.Interfaces;
using JuegoAviones.Data.Conexion;
using Microsoft.EntityFrameworkCore;

namespace JuegoAviones.Data.Repositorios;

public sealed class UsuarioRepository(JuegoDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorNombreAsync(string nombre, CancellationToken cancellationToken) =>
        db.Usuarios.SingleOrDefaultAsync(usuario => usuario.Nombre == nombre, cancellationToken);

    public async Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(cancellationToken);
    }
}
