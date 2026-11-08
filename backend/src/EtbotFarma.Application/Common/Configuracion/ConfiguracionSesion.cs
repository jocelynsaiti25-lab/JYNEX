namespace EtbotFarma.Application.Common.Configuracion;

/// <summary>
/// Valores configurables de la sesión. Se leen de appsettings.json, sección "Sesion".
/// </summary>
public sealed class ConfiguracionSesion
{
    /// <summary>Minutos sin actividad después de los cuales la sesión expira (HU-01 criterio 6).</summary>
    public int MinutosInactividad { get; init; } = 20;

    public TimeSpan InactividadMaxima => TimeSpan.FromMinutes(MinutosInactividad);
}
