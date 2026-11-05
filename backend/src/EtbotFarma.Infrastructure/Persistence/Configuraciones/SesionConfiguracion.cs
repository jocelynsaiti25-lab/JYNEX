using EtbotFarma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtbotFarma.Infrastructure.Persistence.Configuraciones;

/// <summary>Clase Sesion → tabla sesion.</summary>
public sealed class SesionConfiguracion : IEntityTypeConfiguration<Sesion>
{
    public void Configure(EntityTypeBuilder<Sesion> builder)
    {
        builder.ToTable("sesion");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id_sesion");
        builder.Property(s => s.IdUsuario).HasColumnName("id_usuario");
        builder.Property(s => s.Token).HasColumnName("token");
        builder.Property(s => s.FechaInicio).HasColumnName("fecha_inicio");
        builder.Property(s => s.UltimaActividad).HasColumnName("ultima_actividad");
        builder.Property(s => s.FechaCierre).HasColumnName("fecha_cierre");
        builder.Property(s => s.Cerrada).HasColumnName("cerrada");

        // Relación: un usuario puede tener muchas sesiones
        builder.HasOne(s => s.Usuario)
               .WithMany()
               .HasForeignKey(s => s.IdUsuario);
    }
}
