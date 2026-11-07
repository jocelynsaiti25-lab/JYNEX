using EtbotFarma.Application.Common.Interfaces;
using MySqlConnector;

namespace EtbotFarma.Infrastructure.Persistence;

/// <summary>
/// Conexión y transacción con MySQL de UNA petición HTTP (se crea una por petición).
///
///   1. El primer repositorio que la usa abre la conexión e inicia la transacción (BEGIN).
///   2. Todos los repositorios de la petición escriben dentro de ESA transacción.
///   3. El handler llama a GuardarCambiosAsync → COMMIT (se guarda todo junto).
///   4. Si hubo un error y no se llegó al COMMIT → ROLLBACK al terminar la petición (no se guarda nada).
///
/// Implementa IUnitOfWork, que es lo único que conoce la capa Application.
/// </summary>
public sealed class ConexionMySql(string cadenaConexion) : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private MySqlConnection? _conexion;
    private MySqlTransaction? _transaccion;

    /// <summary>
    /// Crea un comando SQL listo para usarse dentro de la transacción de la petición.
    /// Los valores SIEMPRE se mandan como parámetros (@nombre), nunca pegados al texto:
    /// así se evita la inyección SQL.
    /// </summary>
    public async Task<MySqlCommand> CrearComandoAsync(string sql, CancellationToken cancellationToken)
    {
        if (_conexion is null)
        {
            _conexion = new MySqlConnection(cadenaConexion);
            await _conexion.OpenAsync(cancellationToken);
        }

        _transaccion ??= await _conexion.BeginTransactionAsync(cancellationToken);

        return new MySqlCommand(sql, _conexion, _transaccion);
    }

    /// <summary>COMMIT: confirma todo lo que se escribió en esta petición.</summary>
    public async Task GuardarCambiosAsync(CancellationToken cancellationToken)
    {
        if (_transaccion is null)
        {
            return;   // no se escribió nada
        }

        await _transaccion.CommitAsync(cancellationToken);
        await _transaccion.DisposeAsync();
        _transaccion = null;   // si se vuelve a escribir, empieza otra transacción
    }

    /// <summary>Al terminar la petición: ROLLBACK de lo no confirmado y cerrar la conexión.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_transaccion is not null)
        {
            await _transaccion.DisposeAsync();   // sin COMMIT, MySqlConnector hace ROLLBACK
            _transaccion = null;
        }

        if (_conexion is not null)
        {
            await _conexion.DisposeAsync();
            _conexion = null;
        }
    }

    public void Dispose()
    {
        _transaccion?.Dispose();
        _conexion?.Dispose();
        _transaccion = null;
        _conexion = null;
    }
}
