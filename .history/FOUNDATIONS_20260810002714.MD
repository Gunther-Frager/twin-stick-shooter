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

Todos los proyectos
twin-stick shooter


Escribe / para habilidades
Se quedó sin mensajes gratuitos hasta 5:10 a.m.
Recientes
Mejoras de juego: modos, gráficos y mecánicas
hace 3 minutos
Plan de desarrollo fase 1 del programa
hace 6 horas
Instrucciones

Agregar instrucciones para personalizar las respuestas de Claude
Archivos
3% de la capacidad del proyecto utilizada

Inferno+: Influencia del proyecto
Inferno+ es un aclamado twin-stick shooter desarrollado por Radiangames que combina la acción frenética de los juegos de disparos de doble palanca con una estructura de exploración basada en niveles, muy inspirada en el clásico Gauntlet. A continuación, te presento un desglose detallado de sus características:  
1. Mecánicas de Juego y Estructura
 
A diferencia de muchos shooters de pantalla fija donde solo debes sobrevivir a oleadas, Inferno+ es un juego basado en niveles que requieren navegación y exploración.
 
    Exploración y Mazmorras: El jugador controla una nave a través de entornos laberínticos llenos de muros, pasillos estrechos y rincones. Debes navegar estos mapas para encontrar la salida.  
 
    Sistema estilo "Gauntlet": Encontrarás "generadores" (spawners) de enemigos que lanzan hordas de rivales constantemente. Para detener el flujo, debes destruir el generador central.  
 
    Progreso: El juego requiere recolectar llaves para abrir puertas bloqueadas por rayos o barreras, permitiendo avanzar a través del nivel.  
 
    Drones y Potenciadores: A medida que avanzas, puedes recoger drones que te acompañan y aumentan tu potencia de fuego, disparando junto a ti.  
 
    Sistema de Mejoras: Al recolectar oro en los niveles, puedes comprar mejoras permanentes como mayor velocidad, salud extra, escudos, fuego rápido, disparo extendido o proyectiles teledirigidos.  
 
2. Controles  
 
El juego sigue el estándar de los twin-stick shooters de alta precisión:  
 
    Palanca Izquierda: Movimiento de la nave.
 
    Palanca Derecha: Apuntado y disparo de 360 grados.
 
    Acciones adicionales: Incluye un botón para activar escudos defensivos (crucial para sobrevivir al daño). Los controles son descritos como extremadamente responsivos y precisos.  
 
3. Estilo Visual y Niveles
 
    Estética: Utiliza un estilo visual minimalista, con gráficos vectoriales y colores neón vibrantes que contrastan con fondos oscuros. Es una estética "retro" muy característica de los títulos de Radiangames.  
 
    Variedad: El diseño de niveles cambia periódicamente (por ejemplo, cada 10 niveles), introduciendo nuevos esquemas de colores, layouts y enemigos.  
 
    Perspective: Presenta una vista cenital (top-down) bastante cerrada, lo que obliga al jugador a estar atento a lo que tiene justo alrededor de su nave.
 
4. Puntuación y Progresión
 
    Estructura: El juego cuenta con decenas de niveles (40 en la versión Inferno+), ofreciendo una curva de dificultad creciente.  
 
    Desafío: Tras completar la campaña principal, se desbloquea un modo New Game+ para aquellos que buscan un reto superior.  
 
    Jefes: El progreso se siente marcado por la aparición periódica de jefes, aunque algunos críticos consideraron los combates finales como un punto que podría haber tenido mayor profundidad.  
 
5. Música y Sonido
 
    El apartado sonoro es un pilar fundamental del estilo del juego, complementando la acción rápida con ritmos que cambian junto con los niveles. La atmósfera sonora está diseñada para mantener la inmersión durante las intensas secuencias de combate en los laberintos.
 
Resumen de la experiencia
 
Inferno+ destaca por ser un juego que logra transformar la mecánica de los "disparos de doble palanca" en una experiencia estratégica. No se trata solo de disparar, sino de gestionar tu posición, priorizar la destrucción de generadores y administrar tus recursos (llaves y mejoras) para sobrevivir en sus laberintos dinámicos. Es ideal si buscas un juego de acción pura, con una estética limpia y una curva de aprendizaje bien diseñada.
 
Si nos adentramos en las entrañas técnicas de Inferno+ y el desarrollo de Radiangames, nos encontramos con un enfoque minimalista pero sumamente eficiente. A diferencia de los motores masivos actuales (como Unity o Unreal), el estudio (liderado por Luke Schneider) se caracterizó por exprimir tecnologías ligeras orientadas al código.A continuación, se detalla el análisis técnico de su motor, renderización y efectos visuales:
1. Base Tecnológica y Motor
-Framework (Microsoft XNA / MonoGame): Inferno+ fue desarrollado utilizando el ecosistema Microsoft XNA (escrito en C#), una plataforma basada en código que otorga control directo sobre el pipeline gráfico sin la sobrecarga de un motor gráfico pesado de propósito general. Posteriormente, sus títulos se adaptaron de manera fluida a múltiples plataformas gracias a la portabilidad de esta arquitectura.  
-Filosofía "Custom-Made": Radiangames no utilizaba una interfaz visual de arrastrar y soltar (como un editor de niveles tradicional pesado). Todo el sistema de juego, la gestión de entidades, las físicas simplificadas y el pipeline de renderizado se programaron "desde cero" utilizando librerías propias optimizadas específicamente para juegos arcade de alta velocidad.
 
2. Pipeline de Renderizado y Gráficos
El apartado visual es engañosamente simple: parece arte vectorial puro, pero detrás hay técnicas muy cuidadas de renderizado 2D y pseudo-3D:
--Renderizado basado en Sprites y Geometría Vectorial:
-La mayor parte del juego utiliza una combinación de primitivas geométricas renderizadas por hardware y texturas de alta resolución basadas en vectores (gráficos escalables limpios).
-Los elementos del escenario (muros, laberintos) y las naves se dibujan con líneas muy definidas y contornos brillantes, lo que permite que la imagen mantenga una nitidez absoluta sin importar la resolución de la pantalla (ya sea en monitores de PC o pantallas móviles de alta densidad).
 
--Uso intensivo de Shaders (HLSL / Pixel Shaders):
-Para lograr esa estética característica de "neón brillante" (glow), el motor implementa shaders personalizados por pasada (post-processing).
-Se aplica un efecto de Bloom dinámico muy preciso: los elementos luminosos (láseres, explosiones, núcleos de energía) desprenden un resplandor que satura ligeramente los píxeles circundantes sobre los fondos oscuros, simulando tubos de luz real o pantallas de fósforo antiguo.
--Capas y Profundidad visual:Aunque el juego se percibe cenital (top-down plano), utiliza un sistema de capas (layers) bien estructurado. El fondo del laberinto suele tener patrones estáticos o sutilmente animados de baja luminosidad, sobre los cuales flota la acción principal, y por encima de todo se dibujan las partículas y los efectos de interfaz (HUD).
 
3. Sistema de Partículas y Efectos Visuales (VFX)
El caos visual controlado de Inferno+ recae en un motor de partículas hiperoptimizado:
-Partículas en CPU/GPU ligeras: Cada explosión, destrucción de generadores o estela de los proyectiles genera decenas de pequeñas partículas geométricas (cuadrados o líneas brillantes) que se desvanecen rápidamente mediante interpolación lineal de color y opacidad (alpha blending).
-Efectos de distorsión e iluminación dinámica: Cuando ocurren explosiones masivas o se usan armas especiales (como el vórtice o los misiles), el motor aplica micro-sacudidas de cámara (screen shake) basadas en vectores de desplazamiento aleatorio y variaciones de contraste en el framebuffer.
 
4. Rendimiento y Optimización (Performance)
-Tasa de cuadros por segundo (Framerate) ultra estable: Al ser un twin-stick shooter frenético, la prioridad absoluta del motor es mantener una tasa de refresco bloqueada (usualmente a 60 FPS estables o más). La caída de un solo cuadro puede arruinar la precisión de los controles.
-Gestión de Memoria (Garbage Collection Minimization): En C# y XNA, el recolector de basura (Garbage Collector) puede causar tirones de rendimiento (stutters) si se crean objetos constantemente. El motor de Radiangames utiliza patrones de reutilización de objetos (Object Pooling) para enemigos, balas y partículas, asegurando que la memoria permanezca estática durante las partidas intensas y evitando pausas inesperadas.
