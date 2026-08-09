# FOUNDATIONS: Proyecto Twin-Stick Shooter (Inspirado en Inferno+)

Este documento establece los pilares fundamentales, la esencia del juego y la arquitectura técnica. Es el faro que guía todas las decisiones de diseño e implementación.

---

## 1. Esencia del Juego (Inspiración: Inferno+)

El proyecto busca capturar la experiencia de **Inferno+**, un twin-stick shooter que combina acción frenética con exploración de laberintos y progresión estratégica.

### Mecánicas Principales
*   **Exploración de Mazmorras:** Navegación a través de entornos laberínticos llenos de pasillos estrechos y secretos.
*   **Sistema Estilo "Gauntlet":** Presencia de generadores (spawners) que lanzan hordas constantes. La prioridad táctica es destruir el generador central.
*   **Progreso por Llaves:** Recolección de llaves de colores para abrir barreras de rayos y avanzar en el nivel.
*   **Drones y Potenciadores:** Recolección de drones acompañantes que aumentan la potencia de fuego.
*   **Sistema de Mejoras:** Recolección de oro para comprar mejoras permanentes (velocidad, salud, escudos, fuego rápido, proyectiles teledirigidos).
*   **Controles de Precisión:** 
    *   Stick Izquierdo: Movimiento.
    *   Stick Derecho: Apuntado y disparo 360°.
    *   Botón de Escudo: Crucial para la supervivencia.

### Estética y Atmósfera
*   **Estilo Neón Minimalista:** Gráficos vectoriales vibrantes sobre fondos oscuros.
*   **Vista Cenital Cerrada:** Perspectiva top-down que enfatiza el peligro inmediato.
*   **Atmósfera Sonora Inmersiva:** Ritmos electrónicos que evolucionan con la intensidad de la acción.

---

## 2. Pilares Técnicos (Core Pillars)

1.  **Control Directo (Custom-Made):** Desarrollo basado en código puro (MonoGame/C#) para un control total sobre el pipeline gráfico y la gestión de memoria.
2.  **Rendimiento Extremo (Zero-Allocation):** Política estricta de asignación cero en el Game Loop. Uso de `structs`, Object Pooling y algoritmos $O(1)$ para garantizar 60+ FPS constantes.
3.  **Multijugador Integrado:** Soporte para hasta 4 jugadores (local y online) diseñado desde los cimientos.

---

## 3. Base Tecnológica

| Criterio | Especificación |
| :--- | :--- |
| **Framework** | **MonoGame (C#)** - Sucesor de XNA, control total del pipeline. |
| **Pipeline Gráfico** | **HLSL Custom Shaders** - Efectos de Bloom y Glow dinámicos. |
| **Gestión de Memoria** | **Zero-Allocation** - Minimización del Garbage Collector mediante pooling estricto. |
| **Red** | **LiteNetLib / Steamworks.NET** - Sincronización basada en eventos deterministas. |

---

## 4. Arquitectura Técnica

### A. Estructura de Sistemas
El juego utiliza un modelo de managers desacoplados:
*   **ScreenManager:** Gestión de estados (Menú, Juego, Tienda).
*   **LevelManager:** Carga de mapas (JSON/Grid), gestión de Spawners y colisiones $O(1)$.
*   **EntityManager:** Gestión optimizada de Jugadores, Enemigos, Balas y Partículas.
*   **UpgradeSystem:** Sistema data-driven para estadísticas y mejoras.

### B. Pipeline de Renderizado Neón
1.  **Sprites Vectoriales:** Texturas blancas con degradados alfa.
2.  **Bloom de Dos Pasadas:** Extracción de brillo (umbral) + Desenfoque Gaussiano + Composición aditiva.

### C. Multijugador (Hasta 4 Jugadores)
*   **Local:** Cámara dinámica con centro de masa y zoom automático.
*   **Online:** Modelo Host-Cliente con predicción local y sincronización de proyectiles por eventos (ID, Frame, Ángulo).

---

## 5. Hoja de Ruta (Roadmap)

1.  **Fase 1: Arquitectura Base:** Game Loop, Input Twin-Stick (4 mandos), renderizado básico.
2.  **Fase 2: Sistemas de Pooling:** `ObjectPool` y `ParticleSystem` (Zero-Allocation).
3.  **Fase 3: Multijugador Local:** Cámara dinámica y límites de pantalla.
4.  **Fase 4: Laberinto y Colisiones:** Carga de mapas, colisiones por grilla.
5.  **Fase 5: IA y Combate:** Enemigos de horda, torretas y drones.
6.  **Fase 6: Sincronización Online:** Integración de red y predicción de movimiento.
7.  **Fase 7: Pipeline de Shaders:** Implementación de Bloom/Glow.
8.  **Fase 8: Economía y NG+:** Tienda de mejoras y curva de dificultad.

---

## 6. Estrategia de Desarrollo con IA

*   **Algoritmos Puros:** Solicitar lógica matemática aislada.
*   **Código MonoGame:** Generar clases completas aprovechando la arquitectura "code-only".
*   **Monitoreo:** Verificación constante de asignaciones en el Heap.
