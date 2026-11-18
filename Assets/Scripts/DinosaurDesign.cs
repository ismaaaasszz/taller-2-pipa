using UnityEngine;

/*
 * DinosaurDesign.cs
 *
 * ScriptableObject con los datos de un diseño de dinosaurio.
 * Todos los diseños comparten el mismo comportamiento (clase Dinosaur);
 * entre diseños solo cambia lo que permite el enunciado:
 * el modelo y su material/color (en el prefab), el tamaño y la velocidad.
 * Se crean desde el menú: Assets > Create > Dino City Run > Diseño de Dinosaurio.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
[CreateAssetMenu(fileName = "DinosaurDesign", menuName = "Dino City Run/Diseño de Dinosaurio")]
public class DinosaurDesign : ScriptableObject
{
    public string designName = "Dinosaurio"; // Nombre del diseño (ej: Velociraptor)
    public float moveSpeed = 4f;             // Velocidad con la que avanza hacia el jugador (m/s)
    public float sizeMultiplier = 1f;        // Multiplicador de tamaño del modelo
    public Color color = Color.white;        // Color del material del modelo
    public float animationSpeed = 1f;        // Velocidad de la animación de movimiento
}
