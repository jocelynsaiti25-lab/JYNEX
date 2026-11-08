using EtbotFarma.Infrastructure.Seguridad;

namespace EtbotFarma.Api.IntegrationTests;

/// <summary>
/// Pruebas del PasswordHasher REAL (PBKDF2). NO necesitan MySQL, por eso no llevan
/// la categoría "Integracion" y también corren en el job "Backend" de GitHub Actions.
/// </summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void La_contrasenia_correcta_se_verifica()
    {
        var hash = _hasher.GenerarHash("Secreta123");

        Assert.NotEqual("Secreta123", hash);   // nunca se guarda en texto normal
        Assert.True(_hasher.Verificar(hash, "Secreta123"));
    }

    [Fact]
    public void Una_contrasenia_incorrecta_no_se_verifica()
    {
        var hash = _hasher.GenerarHash("Secreta123");

        Assert.False(_hasher.Verificar(hash, "Equivocada"));
    }

    /// <summary>
    /// HU-01 criterio 4: si el usuario no existe, el handler manda un hash vacío.
    /// Debe responder false (nunca dejar entrar) y no lanzar error.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Con_hash_vacio_responde_false(string hashVacio)
    {
        Assert.False(_hasher.Verificar(hashVacio, "CualquierCosa"));
        Assert.False(_hasher.Verificar(hashVacio, string.Empty));
    }
}
