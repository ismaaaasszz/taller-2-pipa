using UnityEngine;

/*
 * Dinosaur.cs
 *
 * Enemigo dinosaurio (hereda de Hazard). Aparece en uno de los tres carriles
 * y avanza en línea recta sin detenerse, en dirección contraria al jugador
 * ("hacia la pantalla"), sin cambiarse de carril.
 * Su collider es más alto que el salto del jugador, así que no se puede saltar.
 * El tamaño, la velocidad y la animación se toman de su DinosaurDesign.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
[RequireComponent(typeof(Rigidbody))]
public class Dinosaur : Hazard
{
    [Header("Dinosaurio")]
    public DinosaurDesign design;   // Diseño con tamaño, velocidad y color
    public Transform modelRoot;     // Objeto que contiene el modelo 3D y su collider (se escala)
    public Animator modelAnimator;  // Animator con la animación de movimiento en bucle

    private float moveSpeed; // Velocidad final de este dinosaurio

    /*
     * Awake()
     *
     * Aplica los valores del diseño: tamaño del modelo, velocidad
     * y un desfase aleatorio de animación para que no se muevan sincronizados.
     */
    void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // Se mueve por código, no por físicas
        rb.useGravity = false;

        moveSpeed = design.moveSpeed;
        modelRoot.localScale *= design.sizeMultiplier;

        if (modelAnimator != null)
            modelAnimator.Play(0, 0, Random.value);
    }

    /*
     * Update()
     *
     * Avanza en línea recta hacia la cámara mientras se juega.
     * En pausa y en la derrota la animación se congela.
     */
    void Update()
    {
        GameState state = GameManager.Instance.State;

        if (modelAnimator != null)
        {
            bool animate = state == GameState.Playing || state == GameState.MainMenu;
            modelAnimator.speed = animate ? design.animationSpeed : 0f;
        }

        if (state != GameState.Playing)
            return;

        transform.position += Vector3.back * moveSpeed * Time.deltaTime;
    }

    // Al morir por una bala: ruge y suma al contador de dinosaurios eliminados
    protected override void OnDestroyedByProjectile()
    {
        AudioManager.Instance.PlayDinosaurDeath();
        GameManager.Instance.RegisterDinosaurKill();
    }
}
