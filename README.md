# Dino City Run 

**Taller N°2 – Proyecto Integrador de Programación Avanzada (II-2026)**
Universidad Católica del Norte – Departamento de Ingeniería de Sistemas y Computación

## Integrantes

| Nombre | RUT |
|---|---|
| Marcos Astudillo | 21.071.892-3 |

## Descripción

Videojuego 3D estilo *Infinite Runner Shooter*. El jugador corre sin detenerse por una ciudad
devastada e invadida por dinosaurios, en un camino de tres carriles: debe disparar a los
dinosaurios, saltar los vehículos abandonados y recoger monedas para llegar lo más lejos posible.

- **Motor:** Unity 2022.3.62f3 (plantilla 3D, Built-in Render Pipeline)
- **Lenguaje:** C#
- **Resolución:** 1920 x 1080
- **Escena principal:** `Assets/MainGame/DinoCityRun.unity`

## Controles

| Tecla | Acción |
|---|---|
| A / D (o flechas) | Moverse al carril izquierdo / derecho |
| Espacio (o W) | Saltar |
| F | Disparar (máximo 1 bala cada 0.5 s) |
| Esc | Pausar / continuar |

## Diseños de dinosaurio elegidos

| Diseño | Velocidad | Tamaño | Color | Animación |
|---|---|---|---|---|
| Velociraptor | 6.5 m/s (rápido) | x1.00 | Café | Run |
| Triceratops | 3.5 m/s (lento) | x1.45 | Gris verdoso | Walk |
| T-Rex (extra) | 4.5 m/s | x1.60 | Verde oscuro | Run |

Todos comparten la clase `Dinosaur`; la variación se define con un `ScriptableObject`
(`DinosaurDesign`, en `Assets/Data/`) más el modelo y material de cada prefab.

## Estructura del proyecto

```
Assets/
├── AssetsDinoCityRun/   Modelos entregados (personajes, armas, autos, ciudad, animaciones)
├── Animations/          Animator Controllers del jugador y los dinosaurios
├── Audio/Music/         Música de menú, juego y derrota
├── Audio/SFX/           Efectos de sonido
├── Data/                Diseños de dinosaurio (ScriptableObjects)
├── Editor/              Constructor automático del proyecto y sintetizador de audio
├── MainGame/            Escena del juego
├── Materials/           Materiales
├── Prefabs/             Jugador, Plataforma, Bala, Moneda, Dinosaurios, Vehículos, Decoración, Efectos
├── Scripts/             Código del juego
└── TextMesh Pro/        Recursos de TextMeshPro
```

## Cómo abrir

1. Abrir la carpeta del proyecto con **Unity 2022.3.62f3** desde Unity Hub.
2. Abrir la escena `Assets/MainGame/DinoCityRun.unity` y presionar Play
   (ajustar la vista *Game* a 1920x1080).

Si se necesita regenerar la escena, prefabs, materiales y audio, usar el menú
**Dino City Run → 2. Construir proyecto completo**.

## Créditos de assets

- Modelos entregados por la asignatura (personajes, armas, dinosaurios y animaciones).
- **KayKit – City Builder Bits** por Kay Lousberg (CC0) – edificios, autos y objetos de ciudad.
- **LowPoly Apocalyptic Buildings** por Majadroid (CC0) – texturas de concreto y rocas.
