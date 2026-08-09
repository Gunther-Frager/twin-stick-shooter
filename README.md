# Fase 1 — Arquitectura Base

Esqueleto funcional de la Fase 1: Game Loop a 60 FPS fijos, input twin-stick
para hasta 4 mandos, y renderizado vectorial básico (naves triangulares sin
texturas ni Content Pipeline).

## Estructura

```
TwinStickShooter/
├── Program.cs                  # Entry point
├── Game1.cs                    # Game loop (Initialize/Update/Draw)
├── Core/
│   └── GameConstants.cs        # Config centralizada (deadzones, velocidad, colores)
├── Input/
│   ├── PlayerInputState.cs     # struct de input por jugador (zero-alloc)
│   └── InputManager.cs         # Polling de 4 GamePads + deadzone radial
├── Entities/
│   └── Player.cs               # Posición, ángulo de apuntado, escudo
└── Rendering/
    └── ShipRenderer.cs         # Naves triangulares vía DrawUserPrimitives
```

## Requisitos

- .NET 8 SDK
- Plantillas de MonoGame:
  ```bash
  dotnet new install MonoGame.Templates.CSharp
  ```
  (No es estrictamente necesario si ya tenés el `.csproj`, pero instala las
  plantillas y confirma que el paquete `MonoGame.Framework.DesktopGL` esté
  disponible en tu feed de NuGet.)

## Compilar y correr

```bash
cd TwinStickShooter
dotnet restore
dotnet run
```

## Qué deberías ver

- Una ventana de 1280x720 con fondo oscuro.
- 4 naves triangulares (una por color: cian, magenta, amarillo, violeta) en
  el centro de la pantalla, una por cada mando conectado.
- Cada nave se mueve con el stick izquierdo y rota apuntando según el stick
  derecho (mantiene el último ángulo si soltás el stick).
- Mantener el botón A/Cross dibuja un anillo blanco alrededor de la nave
  (escudo).
- El título de la ventana muestra FPS y cantidad de mandos conectados.

## Notas de diseño (por qué está así)

- **Sin Content Pipeline todavía:** las naves son geometría generada en
  código (`ShipRenderer`), no sprites. Esto evita la complejidad de MGCB en
  esta fase; el pipeline de shaders/bloom llega recién en la Fase 7.
- **Zero-allocation en el loop:** los arrays de `InputManager` y
  `ShipRenderer` se crean una sola vez; `Update`/`Draw` solo escriben sobre
  buffers existentes. `PlayerInputState` es un `struct` a propósito.
- **Deadzone radial (no por eje):** más natural para apuntado 360° en un
  twin-stick shooter que la deadzone cuadrada por defecto.
- **HUD en el título de ventana:** evita traer `SpriteFont` (y por lo tanto
  el Content Pipeline) solo para mostrar FPS en Fase 1.

## Próximos pasos sugeridos (Fase 2)

- `ObjectPool<T>` genérico para balas/partículas.
- `ParticleSystem` con buffers pre-alocados.
- Mover el spawn de jugadores fuera de `Game1.Initialize` hacia un futuro
  `LevelManager` (Fase 4).
