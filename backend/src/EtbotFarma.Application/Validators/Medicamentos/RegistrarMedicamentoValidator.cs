using System.Globalization;
using EtbotFarma.Application.Commands.Medicamentos;
using FluentValidation;

namespace EtbotFarma.Application.Validators.Medicamentos;

/// <summary>
/// HU-06 criterios 8, 9, 12 y 13: valida el formato y obligatoriedad de los datos de entrada
/// antes de que se ejecute RegistrarMedicamentoHandler.
/// </summary>
public sealed class RegistrarMedicamentoValidator : AbstractValidator<RegistrarMedicamentoCommand>
{
    public RegistrarMedicamentoValidator()
    {
        // HU-06 escenario 8: campos obligatorios vacíos
        RuleFor(x => x.NombreComercial)
            .NotEmpty().WithMessage("Este campo es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre comercial no puede exceder 100 caracteres.");

        RuleFor(x => x.NombreSustancia)
            .NotEmpty().WithMessage("Este campo es obligatorio.")
            .MaximumLength(100).WithMessage("La sustancia activa no puede exceder 100 caracteres.");

        RuleFor(x => x.Presentacion)
            .NotEmpty().WithMessage("Este campo es obligatorio.")
            .MaximumLength(50).WithMessage("La presentación no puede exceder 50 caracteres.");

        RuleFor(x => x.Concentracion)
            .NotEmpty().WithMessage("Este campo es obligatorio.")
            .MaximumLength(50).WithMessage("La concentración no puede exceder 50 caracteres.");

        // HU-06 escenarios 8 y 13: número de lote obligatorio y solo letras, números y guiones
        RuleFor(x => x.NumeroLote)
            .NotEmpty().WithMessage("Este campo es obligatorio.")
            .MaximumLength(50).WithMessage("El número de lote solo puede tener letras, números y guiones.")
            .Matches("^[a-zA-Z0-9-]+$").WithMessage("El número de lote solo puede tener letras, números y guiones.");

        // HU-06 escenarios 8 y 9: fecha obligatoria y válida en formato yyyy-MM-dd
        RuleFor(x => x.FechaCaducidad)
            .NotEmpty().WithMessage("Este campo es obligatorio.")
            .Must(EsFechaValida).WithMessage("Fecha de caducidad no válida.");

        // HU-06 escenarios 8 y 12: cantidad obligatoria y número entero >= 0
        RuleFor(x => x.CantidadInicial)
            .NotEmpty().WithMessage("Este campo es obligatorio.")
            .Must(EsEnteroNoNegativo).WithMessage("La cantidad debe ser un número entero de 0 o más.");
    }

    private static bool EsFechaValida(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return false;
        return DateOnly.TryParseExact(
            valor.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    private static bool EsEnteroNoNegativo(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return false;
        return int.TryParse(valor.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var numero)
               && numero >= 0;
    }
}