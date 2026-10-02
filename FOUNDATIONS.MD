# Fundamentos del proyecto

## Propósito

Twin Stick Shooter es un juego de acción cenital modular desarrollado en C# con MonoGame DesktopGL (.NET 8). La referencia a los twin-stick shooters de exploración inspira el ritmo y la lectura táctica, pero no implica que el proyecto ya implemente todas las mecánicas de esos juegos.

## Estado implementado

- **Bucle de juego:** Paso fijo de 60 ticks por segundo (`IsFixedTimeStep = true`).
- **Entrada local:** Detección de teclado, mouse y hasta cuatro mandos con deadzone radial y asignación independiente.
- **Jugador:** Movimiento con inercia y colisión suave, apuntado 360°, disparo con cooldown, escudo temporal, invulnerabilidad y penalización de velocidad por contacto físico.
- **Cámara dinámica:** Seguimiento suave del centro de masa de todos los jugadores activos con zoom automático interpolado.
- **Enemigos y combate:** 7 arquetipos de enemigos implementados con comportamientos diferenciados:
  - `Swarmer`: Hostil básico, rápido y frágil en enjambre.
  - `Roamer`: Movimiento errático con rebote en paredes.
  - `Turret`: Estacionario con disparo periódico hacia jugadores a la vista.
  - `Spawner`: Generador estático anclado que despliega periódicamente Swarmers.
  - `Rusher`: Hostil agresivo de persecución directa hacia el jugador más cercano.
  - `StaticShooter`: Tirador de largo alcance con detección de línea de visión optimizada.
  - `MobileGenerator`: Unidad móvil acorazada que patrulla y despliega Rushers.
- **Generación procedural:** Algoritmo orgánico basado en crawlers, colocación de arenas y pockets con ownership regional, obstáculos destructibles y cálculo de rutas transitables (BFS / MST).
- **Encuentros y balance:** `EncounterDirector` para planificación determinista de oleadas y grupos según presupuestos de dificultad por sala.
- **Física y colisiones:** Primitivas geométricas (círculos, cápsulas y polígonos redondeados), detección continua de colisiones y deslizamiento suave en paredes.
- **Optimización de memoria:** Política de asignación cero en caliente mediante `ObjectPool<T>` (balas y enemigos), partículas struct-based y conteo regional $O(1)$.
- **Renderizado geométrico:** Dibujo vectorial con `BasicEffect` y `VertexPositionColor`, sin necesidad de Content Pipeline (MGCB) para los recursos actuales.
- **Pruebas automatizadas:** Suite de 86 pruebas xUnit que validan física, colisiones, generación, IA de enemigos y ciclo de vida.

## Arquitectura

El archivo `Game/Game1.cs` coordina el ciclo de vida, la actualización física y el renderizado. La solución técnica detallada, diagramas de flujo y el mapa de archivos completo se documentan en:

👉 [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

Distribución modular por responsabilidad:
- `Game/Core/`: Configuración, niveles, mapas procedurales, física, cámara, pooling y spawners.
- `Game/Entities/`: Jugadores, enemigos, balas, partículas y managers de ciclo de vida.
- `Game/Input/`: Normalización de entrada para teclado, mouse y múltiples mandos.
- `Game/Rendering/`: Mallas de vértices, dibujo de naves, arena, enemigos y efectos visuales.
- `Game/Content/`: Mapas y plantillas de salas en formato JSON.
- `Game.Tests/`: Pruebas unitarias y de integración del motor.

## Fuera del alcance implementado

Actualmente no están implementados:
- Multijugador en red / online.
- Drones acompañantes, economía o mejoras persistentes entre partidas.
- Sistema de audio o efectos de sonido.
- Shaders de postprocesado (bloom, distorsión CRT).
- Pathfinding complejo de IA (A* por mallas de navegación).

Estas son posibilidades futuras, no capacidades que deban asumirse al modificar el juego.

## Criterios para cambios

- **Reglas del juego:** Mantener intactos controles y reglas de combate salvo que el cambio los solicite expresamente.
- **Validación con tests:** Ejecutar `dotnet test Game.Tests/Game.Tests.csproj` antes y después de integrar cualquier cambio para garantizar que todas las pruebas pasen.
- **Separación de responsabilidades:** Conservar la división estricta entre simulación, entrada y renderizado. Los renderizadores nunca deben mutar el estado del mundo.
- **Ciclo de vida MonoGame:** Mantener la carga de archivos de disco y generación procedural dentro de `LoadContent()`, nunca en `Initialize()`.
- **Política de memoria:** No realizar asignaciones (`new` de objetos o colecciones dinámicas) dentro del game loop (`Update`/`Draw`). Usar los pools preexistentes.
- **Mantenimiento de Arquitectura (Obligatorio):** Si agregas, renombras, reestructuras o eliminas un sistema, archivo o arquetipo de enemigo, **debes actualizar obligatoriamente [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)** y actualizar la línea `Última revisión: fecha / fase` en su cabecera. El agente de implementación de fases debe verificar y reportar este mantenimiento en su resumen de cierre.

## Próximas áreas posibles

La evolución del proyecto puede abordar audio procedural, progresión entre niveles, sistema de puntuación, efectos de postprocesado o juego cooperativo en red. Cada nuevo sistema deberá diseñarse, documentarse y probarse con pruebas de regresión.
