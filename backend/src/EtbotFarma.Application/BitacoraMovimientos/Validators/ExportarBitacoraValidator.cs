using EtbotFarma.Application.BitacoraMovimientos.Queries;
using FluentValidation;

namespace EtbotFarma.Application.BitacoraMovimientos.Validators;

public sealed class ExportarBitacoraValidator : AbstractValidator<ExportarBitacoraQuery>
{
    private static readonly string[] TiposValidos = ["ENTRADA", "SALIDA", "BAJA"];
    private static readonly DateOnly FechaMinima = new(2000, 1, 1);
    private static readonly DateOnly FechaMaxima = new(2100, 12, 31);

    public ExportarBitacoraValidator()
    {
        RuleFor(x => x.Desde)
            .Must(EstaEnRango).When(x => x.Desde.HasValue)
            .WithMessage(ConsultarBitacoraValidator.MensajeRangoNoValido);

        RuleFor(x => x.Hasta)
            .Must(EstaEnRango).When(x => x.Hasta.HasValue)
            .WithMessage(ConsultarBitacoraValidator.MensajeRangoNoValido);

        RuleFor(x => x)
            .Must(x => x.Desde!.Value <= x.Hasta!.Value)
            .When(x => x.Desde.HasValue && x.Hasta.HasValue)
            .WithMessage(ConsultarBitacoraValidator.MensajeRangoNoValido);

        RuleFor(x => x.IdUsuario)
            .GreaterThan(0).When(x => x.IdUsuario.HasValue)
            .WithMessage("El usuario seleccionado no es válido.");

        RuleFor(x => x.IdMedicamento)
            .GreaterThan(0).When(x => x.IdMedicamento.HasValue)
            .WithMessage("El medicamento seleccionado no es válido.");

        RuleFor(x => x.Tipo)
            .Must(EsTipoValido).When(x => !string.IsNullOrWhiteSpace(x.Tipo))
            .WithMessage("El tipo de movimiento debe ser ENTRADA, SALIDA o BAJA.");
    }

    private static bool EstaEnRango(DateOnly? fecha) =>
        fecha is { } valor && valor >= FechaMinima && valor <= FechaMaxima;

    private static bool EsTipoValido(string? tipo) =>
        tipo is not null && TiposValidos.Contains(tipo.Trim().ToUpperInvariant());
}
