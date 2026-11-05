using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Controllers;

/// <summary>
/// Base de todos los controladores. Expone "Sender" para enviar commands y queries.
/// Los controladores NO tienen lógica de negocio: solo reciben la petición y la envían.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender =>
        _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
