using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EtbotFarma.Infrastructure.Persistence;

/// <summary>
/// Conexión con la base de datos MySQL "etbotfarma".
/// Las tablas ya existen (se crean con database/etbotfarma_mysql.sql);
/// aquí solo se indica cómo se relaciona cada clase con su tabla.
/// También implementa IUnitOfWork: guardar todos los cambios juntos.
/// </summary>
public sealed class EtbotFarmaDbContext(DbContextOptions<EtbotFarmaDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Carga todas las clases de la carpeta Configuraciones
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EtbotFarmaDbContext).Assembly);
    }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken)
        => SaveChangesAsync(cancellationToken);
}
