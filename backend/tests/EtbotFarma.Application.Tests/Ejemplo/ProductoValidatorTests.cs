using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Features.Ejemplo;

namespace EtbotFarma.Application.Tests.Ejemplo;

/// <summary>
/// Pruebas de ProductoValidator y ProductoHandler.
/// Forma de usar el validador:
///   1. Crear el validador
///   2. Llamar a Validar(datos)
///   3. Revisar la lista de errores (vacía = correcto)
/// </summary>
public class ProductoValidatorTests
{
    private readonly ProductoValidator _validator = new();

    [Fact]
    public void Producto_con_datos_correctos_no_tiene_errores()
    {
        var producto = new Producto("Paracetamol 500 mg", 35.50m, 10);

        var errores = _validator.Validar(producto);

        Assert.Empty(errores);
    }

    [Fact]
    public void Producto_sin_nombre_tiene_error()
    {
        var producto = new Producto("", 35.50m, 10);

        var errores = _validator.Validar(producto);

        Assert.Contains("El nombre es obligatorio.", errores);
    }

    [Fact]
    public void Producto_con_precio_cero_tiene_error()
    {
        var producto = new Producto("Paracetamol 500 mg", 0m, 10);

        var errores = _validator.Validar(producto);

        Assert.Contains("El precio debe ser mayor que 0.", errores);
    }

    [Fact]
    public void Producto_con_cantidad_negativa_tiene_error()
    {
        var producto = new Producto("Paracetamol 500 mg", 35.50m, -5);

        var errores = _validator.Validar(producto);

        Assert.Contains("La cantidad no puede ser negativa.", errores);
    }

    [Fact]
    public void Producto_con_todo_mal_tiene_tres_errores()
    {
        var producto = new Producto("", 0m, -5);

        var errores = _validator.Validar(producto);

        Assert.Equal(3, errores.Count);
    }

    [Fact]
    public async Task El_handler_lanza_ValidacionException_si_los_datos_son_incorrectos()
    {
        var handler = new ProductoHandler();

        var ex = await Assert.ThrowsAsync<ValidacionException>(
            () => handler.Handle(new Producto("", 0m, -5), CancellationToken.None));

        Assert.Equal(3, ex.Errores.Count);
    }

    [Fact]
    public async Task El_handler_devuelve_mensaje_si_los_datos_son_correctos()
    {
        var handler = new ProductoHandler();

        var mensaje = await handler.Handle(new Producto("Paracetamol 500 mg", 35.50m, 10), CancellationToken.None);

        Assert.Contains("Paracetamol 500 mg", mensaje);
    }
}
