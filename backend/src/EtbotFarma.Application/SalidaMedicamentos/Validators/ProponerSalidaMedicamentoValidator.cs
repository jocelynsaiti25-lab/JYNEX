using System.Globalization;
using EtbotFarma.Application.SalidaMedicamentos.Queries;
using FluentValidation;

namespace EtbotFarma.Application.SalidaMedicamentos.Validators;

public sealed class ProponerSalidaMedicamentoValidator : AbstractValidator<ProponerSalidaMedicamentoQuery>
{
    public ProponerSalidaMedicamentoValidator()
    {
        RuleFor(x => x.IdMedicamento)
            .GreaterThan(0).WithMessage("Este campo es obligatorio");

        RuleFor(x => x.Cantidad)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Este campo es obligatorio")
            .Must(EsEnteroMayorQueCero).WithMessage("La cantidad debe ser un número entero mayor que 0");
    }

    private static bool EsEnteroMayorQueCero(string? valor) =>
        int.TryParse(valor?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var numero) && numero > 0;
}
