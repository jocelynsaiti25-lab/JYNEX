namespace EtbotFarma.Api.Historias.CreacionUsuarios;

/// <summary>
/// CONTRATO de entrada (JSON) para POST api/usuarios.
/// Ejemplo:
/// {
///   "nombre": "María Fernanda",
///   "apellidos": "López García",
///   "nombreUsuario": "mflopez",
///   "correo": "maria.lopez@gmail.com",
///   "telefono": "5512345678",
///   "contrasenia": "Farmacia2026",
///   "idRol": 2
/// }
/// idRol: 1 = Administrador, 2 = Empleado.
/// </summary>
public sealed record CrearUsuarioRequest(
    string? Nombre,
    string? Apellidos,
    string? NombreUsuario,
    string? Correo,
    string? Telefono,
    string? Contrasenia,
    int? IdRol);
