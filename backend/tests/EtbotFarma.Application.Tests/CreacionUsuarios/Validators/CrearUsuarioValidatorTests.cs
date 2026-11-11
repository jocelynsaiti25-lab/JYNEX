using EtbotFarma.Application.CreacionUsuarios.Commands;
using EtbotFarma.Application.CreacionUsuarios.Validators;
using FluentValidation.TestHelper;

namespace EtbotFarma.Application.Tests.CreacionUsuarios.Validators;

/// <summary>Pruebas del validator de HU-03 (criterios 2 y 3).</summary>
public class CrearUsuarioValidatorTests
{
    private readonly CrearUsuarioValidator _validator = new();

    /// <summary>Datos correctos; cada prueba cambia solo lo que quiere revisar con "with".</summary>
    private static readonly CrearUsuarioCommand Valido = new(
        "María Fernanda", "López García", "mflopez", "maria.lopez@gmail.com", "5512345678", "Farmacia2026", 2);

    [Fact]
    public void Con_datos_correctos_no_hay_errores()
    {
        _validator.TestValidate(Valido).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Sin_datos_marca_cada_campo_obligatorio()
    {
        var resultado = _validator.TestValidate(new CrearUsuarioCommand("", "", "", "", "", "", 0));

        resultado.ShouldHaveValidationErrorFor(c => c.Nombre).WithErrorMessage("El nombre es obligatorio.");
        resultado.ShouldHaveValidationErrorFor(c => c.Apellidos).WithErrorMessage("Los apellidos son obligatorios.");
        resultado.ShouldHaveValidationErrorFor(c => c.NombreUsuario).WithErrorMessage("El nombre de usuario es obligatorio.");
        resultado.ShouldHaveValidationErrorFor(c => c.Correo).WithErrorMessage("El correo electrónico es obligatorio.");
        resultado.ShouldHaveValidationErrorFor(c => c.Telefono).WithErrorMessage("El teléfono es obligatorio.");
        resultado.ShouldHaveValidationErrorFor(c => c.Contrasenia).WithErrorMessage("La contraseña es obligatoria.");
        resultado.ShouldHaveValidationErrorFor(c => c.IdRol);
    }

    [Theory]
    [InlineData("maria.gmail")]          // sin @
    [InlineData("maria@gmail")]          // sin dominio completo
    [InlineData("maria lopez@gmail.com")] // con espacios
    public void Correo_con_formato_invalido_marca_el_error(string correo)
    {
        var resultado = _validator.TestValidate(Valido with { Correo = correo });

        resultado.ShouldHaveValidationErrorFor(c => c.Correo).WithErrorMessage("Formato de correo inválido.");
    }

    [Theory]
    [InlineData("55123")]          // pocos dígitos
    [InlineData("55-1234-5678")]   // con guiones
    [InlineData("55abc45678")]     // con letras
    public void Telefono_con_formato_invalido_marca_el_error(string telefono)
    {
        var resultado = _validator.TestValidate(Valido with { Telefono = telefono });

        resultado.ShouldHaveValidationErrorFor(c => c.Telefono);
    }

    [Theory]
    [InlineData("Corta1", "La contraseña debe tener al menos 8 caracteres.")]
    [InlineData("farmacia2026", "La contraseña debe tener al menos una letra mayúscula.")]
    [InlineData("FARMACIA2026", "La contraseña debe tener al menos una letra minúscula.")]
    [InlineData("FarmaciaEtbot", "La contraseña debe tener al menos un número.")]
    public void Contrasenia_que_no_cumple_la_politica_dice_que_falta(string contrasenia, string mensaje)
    {
        var resultado = _validator.TestValidate(Valido with { Contrasenia = contrasenia });

        resultado.ShouldHaveValidationErrorFor(c => c.Contrasenia).WithErrorMessage(mensaje);
    }

    [Fact]
    public void Nombre_de_usuario_con_espacios_marca_el_error()
    {
        var resultado = _validator.TestValidate(Valido with { NombreUsuario = "maria lopez" });

        resultado.ShouldHaveValidationErrorFor(c => c.NombreUsuario);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Rol_que_no_es_Administrador_ni_Empleado_marca_el_error(int idRol)
    {
        var resultado = _validator.TestValidate(Valido with { IdRol = idRol });

        resultado.ShouldHaveValidationErrorFor(c => c.IdRol)
            .WithErrorMessage("Selecciona un rol válido (Administrador o Empleado).");
    }
}
