# Twin Stick Shooter

Shooter cenital modular desarrollado en C# con **.NET 8** y **MonoGame DesktopGL**.

El juego combina soporte para hasta cuatro jugadores locales, combate táctico contra 7 arquetipos de enemigos, generación procedural de mazmorras con pasillos y arenas orgánicas mediante crawlers y plantillas JSON, simulación física con deslizamiento en paredes y renderizado geométrico vectorial con primitivas 2D. No requiere herramientas externas como Node.js ni el Content Pipeline (MGCB) para los recursos actuales.

---

## Documentación Técnica

- 📘 [**FOUNDATIONS.MD**](FOUNDATIONS.MD): Propósito general, alcance actual del proyecto y criterios mandatorios para la introducción de cambios.
- 📐 [**docs/ARCHITECTURE.md**](docs/ARCHITECTURE.md): Diagramas de flujo y arquitectura Mermaid, desglose completo del árbol de código y principios de diseño (Zero-Allocation, IA de visión, física orgánica).

---

## Requisitos

- **.NET 8 SDK** (versión 8.0 o superior).
- Conexión a NuGet para restaurar paquetes de MonoGame DesktopGL y el framework de pruebas xUnit.

---

## Ejecutar

Desde la raíz del repositorio, restaura los paquetes y ejecuta el proyecto:

```powershell
dotnet restore Game/TwinStickShooter.csproj
dotnet run --project Game/TwinStickShooter.csproj
```

### Controles

| Periférico | Movimiento | Apuntado | Disparo | Escudo |
| :--- | :--- | :--- | :--- | :--- |
| **Teclado / Mouse** | `W`, `A`, `S`, `D` o Flechas | Cursor del Mouse o `I`, `J`, `K`, `L` | `Espacio` o Clic Izquierdo | `Shift Izquierdo` |
| **Gamepad (Xbox/Genérico)** | Stick Izquierdo | Stick Derecho | Gatillo Derecho (`RT`) | Botón `A` o `RB` |

- **Jugadores 2 a 4:** Se conectan automáticamente conectando mandos adicionales.
- **Teclas de Función / Debug:**
  - `F1`: Cambiar a modo un jugador.
  - `F2`: Cambiar a modo multijugador local.
  - `F3`: Alternar entre el mapa procedural normal y la escena de combate de prueba.
  - `F4`: Abrir / Cerrar el panel de desarrollo en pantalla.
  - `F5`: Regenerar el mapa usando la misma semilla (Seed).
  - `F6`: Generar un nuevo mapa con una semilla aleatoria.

---

## Pruebas

Ejecuta la suite completa de pruebas unitarias y de integración:

```powershell
dotnet test Game.Tests/Game.Tests.csproj
```

La suite cubre 86 pruebas que garantizan:
- Algoritmos de colisión contra grillas y primitivas geométricas (círculos, cápsulas, polígonos redondeados).
- Físicas de deslizamiento contra paredes y resolución de penetración.
- Generador procedural por crawlers, conectividad mediante MST y cálculo de caminos mínimos (BFS).
- Planificación determinista de encuentros por presupuestos (`EncounterDirector`).
- Comportamiento y ciclo de vida de los 7 arquetipos de enemigos y conteo regional $O(1)$.
- Deserialización y validación de plantillas de salas en JSON.

---

## Arquetipos de Enemigos y Plantillas

El motor soporta 7 tipos de enemigos completamente integrados en el combate y el sistema de generación:

1. **`Swarmer`**: Enemigo veloz y ligero que se desplaza en línea directa hacia el jugador.
2. **`Roamer`**: Unidad errática que patrulla y rebota contra obstáculos y paredes.
3. **`Turret`**: Torreta estática con campo de tiro periódico orientada al jugador.
4. **`Spawner`**: Generador anclado al suelo que despliega Swarmers periódicamente.
5. **`Rusher`**: Unidad de asalto veloz que persigue activamente al jugador más cercano.
6. **`StaticShooter`**: Tirador de largo alcance con detección de línea de visión optimizada.
7. **`MobileGenerator`**: Unidad acorazada móvil que patrulla la zona y produce Rushers.

Las plantillas de sala ubicadas en `Game/Content/RoomTemplates/` permiten definir estructuras pre-diseñadas y ubicar marcadores `enemySpawns` con cualquiera de estos tipos.
