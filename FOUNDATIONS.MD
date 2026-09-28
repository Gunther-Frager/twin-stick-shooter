# Fundamentos del proyecto

## Propósito

Twin Stick Shooter es un juego de acción cenital desarrollado en C# con MonoGame. La referencia a los twin-stick shooters de exploración inspira el ritmo y la lectura táctica, pero no implica que el proyecto ya implemente todas las mecánicas de esos juegos.

## Estado implementado

- Bucle de juego con paso fijo de 60 ticks por segundo.
- Entrada local mediante teclado, mouse y mandos; soporte para hasta cuatro índices de jugador.
- Movimiento, apuntado, disparo, escudo y cámara dinámica.
- Balas, partículas, enemigos `Swarmer`, `Roamer`, `Turret` y `Spawner`.
- Mapas procedurales, plantillas de sala JSON, spawns tipados y escena de combate de prueba.
- Colisiones de mapa, primitivas redondeadas, deslizamiento contra paredes y límites del mundo.
- Renderizado geométrico con MonoGame, sin Content Pipeline requerido para los recursos actuales.
- Tests xUnit para colisiones, física, geometría, ciclo de vida de mapas y datos de plantilla.

## Arquitectura

`Game/Game1.cs` coordina inicialización, actualización y dibujo. La lógica se distribuye por responsabilidad:

- `Game/Core/`: constantes, estado, niveles, mapas, física, cámara, pooling y spawners.
- `Game/Entities/`: jugadores, enemigos, balas y partículas.
- `Game/Input/`: captura y normalización del estado de entrada.
- `Game/Rendering/`: renderizadores de arena, naves, enemigos, balas y partículas.
- `Game/Content/`: mapas y plantillas JSON.
- `Game.Tests/`: pruebas unitarias y de regresión.

Se mantienen buffers y pools en sistemas de alta frecuencia para reducir asignaciones durante la simulación. Cualquier afirmación de rendimiento debe comprobarse con mediciones; la ausencia absoluta de asignaciones no se considera garantizada por diseño únicamente.

## Fuera del alcance implementado

No están implementados actualmente el multijugador en red, llaves y puertas, drones acompañantes, economía o mejoras persistentes, audio ni postprocesado de bloom/shaders. Son posibilidades futuras, no capacidades que deban asumirse al modificar el juego.

## Criterios para cambios

- Mantener intactos controles y reglas de combate salvo que el cambio los solicite expresamente.
- Proteger colisiones, límites del mundo, generación y ciclo de vida de entidades con pruebas de regresión.
- Conservar la separación entre simulación, entrada y renderizado.
- Usar `dotnet test Game.Tests/Game.Tests.csproj` antes de integrar cambios.

## Próximas áreas posibles

La evolución puede abordar audio, progresión entre niveles, economía de mejoras, efectos de postprocesado o juego en red. Cada sistema deberá diseñarse y probarse antes de presentarse como funcionalidad disponible.
