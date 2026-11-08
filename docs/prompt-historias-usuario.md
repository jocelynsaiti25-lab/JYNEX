# PROMPT PARA PROGRAMAR UNA HISTORIA DE USUARIO — ETBOTFARMA WEB

> **Cómo usarlo:** copia TODO este texto en tu IA (Claude, ChatGPT, Copilot...).
> 1. Donde dice **"REGLAS DEL PROYECTO"**, pega el contenido COMPLETO del archivo `CLAUDE.md` (está en la raíz del repositorio, rama `develop`).
> 2. Al final, en **"HISTORIA A REALIZAR"**, pega la historia completa desde el Excel.
> Si tu IA abre el repositorio (Claude Code, Copilot), no hace falta: ya lee `CLAUDE.md` sola;
> solo dile: *"Lee CLAUDE.md y programa la HU-XX"* y pégale la historia.

Actúa como desarrollador senior experto en .NET 10, Angular 20, MySQL 8, Clean Architecture,
CQRS y SOLID. Vas a programar UNA historia de usuario del proyecto ETBOTFARMA WEB respetando
exactamente las reglas del archivo `CLAUDE.md` del repositorio, que van pegadas abajo.
Antes de escribir código, revisa HU-01 en el repositorio como molde (o pídeme los archivos).

## REGLAS DEL PROYECTO

> Pega aquí el contenido COMPLETO de `CLAUDE.md`.
> (Antes este documento traía una copia de las reglas, pero se desactualizó: le faltaba la sección 6.1
> de diseño del frontend. Pegando `CLAUDE.md` siempre usas las reglas vigentes.)

```
(pega aquí CLAUDE.md)
```

## 12. Cómo debes trabajar y responder

1. Resume en una tabla cada criterio de aceptación y en qué archivo se cumple.
2. Pregunta antes de programar si algún criterio es ambiguo o contradice la base de datos.
3. Lista los archivos que vas a crear o modificar (ruta completa) siguiendo la sección 4.
4. Programa en orden: Domain → Application (Command, Validator, Handler, Dto) → Infrastructure → Api (Contracts, Controller) → Angular → Pruebas.
5. Entrega cada archivo completo con su ruta como título.
6. Al final: comandos para probar, tabla criterio → cómo se probó, y mensaje de commit.

## HISTORIA A REALIZAR

> Pega aquí la historia completa desde el Excel:

```
ID:
Historia:
Rol:
COMO:
QUIERO:
PARA:
CONFIRMACIÓN / ALERTA:
CRITERIOS DE ACEPTACIÓN:
1.
2.
...
```
