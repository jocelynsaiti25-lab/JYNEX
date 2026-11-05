using EtbotFarma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EtbotFarma.Infrastructure.Persistence.Configuraciones;

/// <summary>Clase Usuario → tabla usuario.</summary>
public sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id_usuario");
        builder.Property(u => u.Nombre).HasColumnName("nombre");
        builder.Property(u => u.Apellidos).HasColumnName("apellidos");
        builder.Property(u => u.NombreUsuario).HasColumnName("nombre_usuario");
        builder.Property(u => u.ContraseniaHash).HasColumnName("contrasenia_hash");
        builder.Property(u => u.Telefono).HasColumnName("telefono");
        builder.Property(u => u.IdRol).HasColumnName("id_rol");
        builder.Property(u => u.Activo).HasColumnName("activo");
        builder.Property(u => u.FechaRegistro).HasColumnName("fecha_registro");

        // NombreCompleto se calcula en C#, no es una columna
        builder.Ignore(u => u.NombreCompleto);

        // Relación: muchos usuarios tienen un rol
        builder.HasOne(u => u.Rol)
               .WithMany()
               .HasForeignKey(u => u.IdRol);
    }
}
