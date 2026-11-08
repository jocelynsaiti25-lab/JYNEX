## Historia

<!-- Ejemplo: HU-02 Cierre seguro de sesión (carpeta CierreSesion). Si no es una historia, explica qué cambia y por qué. -->

## Criterios de aceptación

<!-- Un renglón por criterio: cómo se cumple y qué prueba lo comprueba. -->

| Criterio | Dónde se cumple | Prueba |
|---|---|---|
| 1. | | |

## Lista de revisión

- [ ] La rama sale de `develop` y este PR va **hacia `develop`**.
- [ ] Los archivos nuevos están en las carpetas de mi historia (`CLAUDE.md` §4) y con los nombres de §5.
- [ ] En los archivos compartidos solo **agregué** líneas, con comentario `// HU-XX` (`docs/trabajo-en-equipo.md` §3).
- [ ] Controller sin lógica; handler solo coordina; reglas de negocio en `Domain`; SQL solo en `Infrastructure` y con parámetros.
- [ ] Validator con FluentValidation (no se crea con `new` en el handler).
- [ ] Pruebas: reglas del Domain, handler (con falsos), validator y, si hay endpoint, integración.
- [ ] `dotnet test backend/EtbotFarma.slnx --filter "Category!=Integracion"` en verde en mi computadora.
- [ ] `npm run build` (en `frontend`) en verde.
- [ ] Actualicé la fila de mi historia en `docs/historias-usuario.md`.
- [ ] Sin contraseñas, `appsettings.Local.json` ni librerías nuevas sin aprobación.
- [ ] Actualicé mi rama con `develop` antes de abrir el PR.

## Capturas (si hay pantalla)
