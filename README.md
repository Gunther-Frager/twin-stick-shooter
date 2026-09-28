# Twin Stick Shooter

Shooter twin-stick modular desarrollado en C# con .NET 8 y MonoGame DesktopGL.
El juego combina hasta cuatro jugadores, combate con enemigos de varios tipos,
mapas procedurales con plantillas JSON, colisiones y renderizado geométrico.
No requiere Node.js ni Content Pipeline para los recursos actuales.

## Requisitos

- .NET 8 SDK.
- Acceso a NuGet para restaurar MonoGame y las dependencias de pruebas.

## Estructura

```text
Game/
  Program.cs                 Entrada de la aplicación.
  Game1.cs                   Ciclo principal y coordinación de sistemas.
  Core/                      Configuración, mapas, física, cámara, pooling y spawners.
  Entities/                  Jugadores, enemigos, balas, partículas y sus managers.
  Input/                     Estado y lectura de teclado, mouse y mandos.
  Rendering/                 Renderizadores de arena, naves, enemigos y efectos.
  Content/
    Maps/                    Mapas JSON para carga directa.
    RoomTemplates/           Plantillas JSON para generación procedural.
Game.Tests/                  Pruebas unitarias y regresiones de geometría/física.
```

Módulos principales:

- `Core/GameConstants.cs`: valores de simulación, controles, límites y generación.
- `Core/LevelManager.cs`, `MapGenerator.cs` y `MapLoader.cs`: ciclo de vida y creación de mapas.
- `Core/RoomTemplateData.cs` y `RoomTemplateLoader.cs`: lectura de grillas y spawns tipados.
- `Core/PhysicsHelper.cs`, `MapPrimitive.cs` y `RoundedContour.cs`: consultas de colisión y geometría.
- `Core/ObjectPool.cs`: reutilización de objetos para entidades de alta frecuencia.
- `Entities/`: lógica de jugador, enemigos, proyectiles, partículas y administración de sus ciclos de vida.
- `Input/InputManager.cs`: snapshot de input para hasta cuatro jugadores y deadzone radial.
- `Rendering/`: conversión del estado del juego en geometría dibujada por MonoGame.

## Ejecutar

Desde la raíz del repositorio, restaura y ejecuta el proyecto:

```powershell
dotnet restore Game/TwinStickShooter.csproj
dotnet run --project Game/TwinStickShooter.csproj
```

El jugador 1 puede moverse con `WASD` o las flechas, apuntar con el mouse o
`IJKL`/teclado numérico, disparar con espacio o clic izquierdo y activar el
escudo con `Shift`. Un mando usa los sticks izquierdo y derecho, gatillo derecho
para disparar y botón `A` para el escudo. Los mandos adicionales controlan a los
jugadores 2 a 4. `F1` y `F2` seleccionan los modos de juego; `F3` alterna la escena
normal y la escena de combate de prueba cuando los hotkeys de depuración están
habilitados.

## Pruebas

Ejecuta la suite completa con:

```powershell
dotnet test Game.Tests/Game.Tests.csproj
```

Las pruebas cubren regresiones de colisión, descomposición y ciclo de vida de
primitivas de mapa, contornos redondeados, deslizamiento físico y acceso a celdas
de plantillas. El proyecto usa xUnit y Microsoft.NET.Test.Sdk.

## Plantillas y escena de combate

Las plantillas de `Game/Content/RoomTemplates/` definen una grilla y una lista
opcional `enemySpawns`. Cada spawn puede especificar `Swarmer`, `Roamer`,
`Turret` o `Spawner`; los valores ausentes o desconocidos usan `Swarmer` como
tipo por defecto. `F3` permite inspeccionar durante la ejecución la escena de
combate de prueba y volver al mapa procedural.
