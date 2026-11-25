using System.Globalization;
using System.Text.RegularExpressions;
using EtbotFarma.Application.EntradaLote.Commands;
using FluentValidation;

namespace EtbotFarma.Application.EntradaLote.Validators;

/// <summary>
/// HU-07 escenarios 2, 3, 4 y 5: valida datos obligatorios y formatos antes de llegar al handler.
/// </summary>
public sealed class RegistrarEntradaLoteValidator : AbstractValidator<RegistrarEntradaLoteCommand>
{
    private static readonly Regex PatronLote = new("^[a-zA-Z0-9-]+$", RegexOptions.Compiled);

    public RegistrarEntradaLoteValidator()
    {
        RuleFor(x => x.IdMedicamento)
            .GreaterThan(0)
            .WithMessage("Debe seleccionar un medicamento válido y activo del catálogo.");

        RuleFor(x => x.NumeroLote)
            .NotEmpty().WithMessage("El número de lote es obligatorio.")
            .MaximumLength(50).WithMessage("El número de lote no puede exceder 50 caracteres.")
            .Matches(PatronLote).WithMessage("El número de lote solo puede contener letras, números y guiones.");

        RuleFor(x => x.FechaCaducidad)
            .NotEmpty().WithMessage("La fecha de caducidad es obligatoria.")
            .Must(EsFechaValida).WithMessage("La fecha de caducidad no tiene un formato válido (aaaa-MM-dd).");

        RuleFor(x => x.Cantidad)
            .NotEmpty().WithMessage("La cantidad es obligatoria.")
            .Must(EsEnteroMayorQueCero).WithMessage("La cantidad debe ser un número entero mayor que cero.");
    }

    private static bool EsFechaValida(string? fecha) =>
        !string.IsNullOrWhiteSpace(fecha)
        && DateOnly.TryParseExact(fecha.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static bool EsEnteroMayorQueCero(string? valor) =>
        !string.IsNullOrWhiteSpace(valor)
        && int.TryParse(valor.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var numero)
        && numero > 0;
}