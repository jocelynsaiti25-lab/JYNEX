using EtbotFarma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtbotFarma.Infrastructure.Persistence.Configuraciones;

/// <summary>Clase Rol → tabla rol.</summary>
public sealed class RolConfiguracion : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("rol");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id_rol");
        builder.Property(r => r.Nombre).HasColumnName("nombre_rol");
    }
}
