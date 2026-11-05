namespace EtbotFarma.Application.Features.Ejemplo;

/// <summary>
/// EJEMPLO DE APRENDIZAJE: validador de Producto.
///
/// ¿Qué hace?
///   Revisa que los datos de un Producto sean correctos ANTES de usarlos.
///
/// ¿Cómo funciona?
///   1. Crea una lista de errores vacía.
///   2. Revisa cada regla con un "if". Si no se cumple, agrega un mensaje a la lista.
///   3. Devuelve la lista. Si está vacía, los datos son correctos.
/// </summary>
public class ProductoValidator
{
    public List<string> Validar(Producto producto)
    {
        // 1. Lista de errores vacía
        var errores = new List<string>();

        // 2. Revisar cada regla

        // Regla 1: el nombre es obligatorio
        if (string.IsNullOrWhiteSpace(producto.Nombre))
        {
            errores.Add("El nombre es obligatorio.");
        }
        // Regla 2: el nombre no puede tener más de 100 caracteres
        else if (producto.Nombre.Length > 100)
        {
            errores.Add("El nombre no puede tener más de 100 caracteres.");
        }

        // Regla 3: el precio debe ser mayor que cero
        if (producto.Precio <= 0)
        {
            errores.Add("El precio debe ser mayor que 0.");
        }

        // Regla 4: la cantidad no puede ser negativa
        if (producto.Cantidad < 0)
        {
            errores.Add("La cantidad no puede ser negativa.");
        }

        // 3. Devolver la lista (vacía = todo correcto)
        return errores;
    }
}
