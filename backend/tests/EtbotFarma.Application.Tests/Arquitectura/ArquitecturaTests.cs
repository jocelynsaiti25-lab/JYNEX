using System.Reflection;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Domain.Common;
using FluentValidation;
using MediatR;

namespace EtbotFarma.Application.Tests.Arquitectura;

/// <summary>
/// Pruebas que vigilan que todo el equipo respete la arquitectura.
/// Si alguien rompe una regla en una historia nueva, estas pruebas fallan en GitHub.
/// </summary>
public class ArquitecturaTests
{
    private static readonly Assembly Application = typeof(DependencyInjection).Assembly;
    private static readonly Assembly Domain = typeof(DomainException).Assembly;

    private static IEnumerable<Type> ClasesDe(Assembly assembly) =>
        assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false } && !t.IsNested);

    [Fact]
    public void Domain_no_depende_de_ninguna_otra_capa()
    {
        var referencias = Domain.GetReferencedAssemblies().Select(a => a.Name!).ToList();

        Assert.DoesNotContain(referencias, nombre => nombre.StartsWith("EtbotFarma."));
    }

    [Fact]
    public void Application_solo_depende_de_Domain()
    {
        var referencias = Application.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(nombre => nombre.StartsWith("EtbotFarma."))
            .ToList();

        Assert.Equal(new[] { "EtbotFarma.Domain" }, referencias);
    }

    [Fact]
    public void Cada_command_y_query_tiene_exactamente_un_handler()
    {
        var pedidos = ClasesDe(Application).Where(t => t.GetInterfaces().Any(EsPedidoCqrs));

        foreach (var pedido in pedidos)
        {
            var handlers = ClasesDe(Application).Where(clase => clase.GetInterfaces().Any(i =>
                i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                && i.GetGenericArguments()[0] == pedido));

            Assert.True(handlers.Count() == 1, $"{pedido.Name} debe tener exactamente un handler.");
        }
    }

    [Fact]
    public void Los_handlers_usan_ICommandHandler_o_IQueryHandler_y_estan_en_la_carpeta_Handlers()
    {
        var handlers = ClasesDe(Application).Where(t => t.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));

        foreach (var handler in handlers)
        {
            var usaAbstraccion = handler.GetInterfaces().Any(i => i.IsGenericType
                && (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)
                    || i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));

            Assert.True(usaAbstraccion, $"{handler.Name} debe implementar ICommandHandler o IQueryHandler.");
            Assert.EndsWith("Handler", handler.Name);
            Assert.Contains(".Handlers.", handler.Namespace);
        }
    }

    [Fact]
    public void Los_validators_heredan_solo_de_AbstractValidator_y_estan_en_la_carpeta_Validators()
    {
        var validators = ClasesDe(Application).Where(t => typeof(IValidator).IsAssignableFrom(t));

        foreach (var validator in validators)
        {
            Assert.True(validator.BaseType is { IsGenericType: true }
                && validator.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>),
                $"{validator.Name} debe heredar directamente de AbstractValidator<T>.");
            Assert.EndsWith("Validator", validator.Name);
            Assert.Contains(".Validators.", validator.Namespace);
        }
    }

    [Fact]
    public void Commands_y_queries_no_heredan_de_otras_clases_y_estan_en_su_carpeta()
    {
        var pedidos = ClasesDe(Application).Where(t => t.GetInterfaces().Any(EsPedidoCqrs));

        foreach (var pedido in pedidos)
        {
            Assert.Equal(typeof(object), pedido.BaseType);
            var esCommand = pedido.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
            Assert.EndsWith(esCommand ? "Command" : "Query", pedido.Name);
            Assert.Contains(esCommand ? ".Commands." : ".Queries.", pedido.Namespace);
        }
    }

    [Fact]
    public void Los_handlers_no_heredan_de_otras_clases()
    {
        var handlers = ClasesDe(Application).Where(t => t.Name.EndsWith("Handler"));

        foreach (var handler in handlers)
        {
            Assert.Equal(typeof(object), handler.BaseType);
        }
    }

    private static bool EsPedidoCqrs(Type interfaz) =>
        interfaz.IsGenericType
        && (interfaz.GetGenericTypeDefinition() == typeof(ICommand<>)
            || interfaz.GetGenericTypeDefinition() == typeof(IQuery<>));
}
