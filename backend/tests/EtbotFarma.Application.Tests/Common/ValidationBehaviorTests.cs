using EtbotFarma.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Application.Tests.Common;

/// <summary>
/// Comprueba que el pipeline CQRS ejecuta los validadores de FluentValidation
/// ANTES del handler, usando un comando de prueba.
/// </summary>
public class ValidationBehaviorTests
{
    // ----- Comando de prueba (solo existe en este proyecto de pruebas) -----
    public sealed record SaludarCommand(string Nombre) : IRequest<string>;

    public sealed class SaludarValidator : AbstractValidator<SaludarCommand>
    {
        public SaludarValidator()
        {
            RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.");
        }
    }

    public sealed class SaludarHandler : IRequestHandler<SaludarCommand, string>
    {
        public Task<string> Handle(SaludarCommand request, CancellationToken cancellationToken)
            => Task.FromResult($"Hola {request.Nombre}");
    }

    private static ISender CrearSender()
    {
        var assembly = typeof(ValidationBehaviorTests).Assembly;
        var services = new ServiceCollection();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);

        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    [Fact]
    public async Task Ejecuta_el_handler_si_los_datos_son_validos()
    {
        var sender = CrearSender();

        var resultado = await sender.Send(new SaludarCommand("Ana"));

        Assert.Equal("Hola Ana", resultado);
    }

    [Fact]
    public async Task Lanza_ValidationException_si_los_datos_no_son_validos()
    {
        var sender = CrearSender();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sender.Send(new SaludarCommand("")));

        Assert.Contains(ex.Errors, e => e.ErrorMessage == "El nombre es obligatorio.");
    }
}
