using UnityEngine;

/*
 * PlayerController.cs
 *
 * Controla el movimiento del jugador en un camino de tres carriles:
 *  - Corre hacia adelante sin detenerse (movimiento forzado).
 *  - Se mueve entre carriles hacia la izquierda y derecha con "A" y "D".
 *  - Salta con Espacio (o W) lo suficiente para pasar sobre los vehículos.
 *  - Recoge monedas y muere al chocar con un dinosaurio u obstáculo.
 * Usa un CharacterController para no atravesar el suelo ni los límites del mapa.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public const int LaneCount = 3; // Cantidad de carriles del camino

    [Header("Configuración de carrera")]
    public float initialForwardSpeed = 11f; // Velocidad inicial hacia adelante (m/s)
    public float maxForwardSpeed = 17f;     // Velocidad máxima con la dificultad máxima (m/s)

    [Header("Configuración de carriles")]
    public float laneWidth = 3.5f;        // Distancia entre el centro de dos carriles
    public float laneChangeSpeed = 16f;   // Rapidez con que cambia de carril (m/s)

    [Header("Configuración de salto")]
    public float jumpHeight = 2.6f; // Altura del salto (mayor que la altura de los vehículos)
    public float gravity = -30f;    // Gravedad aplicada al jugador

    [Header("Animación")]
    public Animator modelAnimator;       // Animator del modelo (animación de caminar con rifle)
    public float runAnimationSpeed = 1.4f; // Velocidad de la animación al correr

    private CharacterController characterController; // Componente que mueve al jugador con colisiones
    private int targetLane = 1;                      // Carril objetivo: 0 izquierda, 1 centro, 2 derecha
    private float verticalVelocity = 0f;             // Velocidad vertical (positiva al subir)
    private bool isDead = false;                     // Si el jugador ya murió

    public bool IsDead { get { return isDead; } }
    public int CurrentLane { get { return targetLane; } }
    public bool IsGrounded { get { return characterController.isGrounded; } }

    // Velocidad de avance actual: aumenta junto con la dificultad de la partida
    public float CurrentForwardSpeed
    {
        get { return Mathf.Lerp(initialForwardSpeed, maxForwardSpeed, GameManager.Instance.Difficulty01); }
    }

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Start()
    {
        GameManager.Instance.StateChanged += OnStateChanged;
        OnStateChanged(GameManager.Instance.State);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged -= OnStateChanged;
    }

    /*
     * Update()
     *
     * Lee el teclado y mueve al jugador solo mientras la partida está en curso.
     */
    void Update()
    {
        if (isDead || !GameManager.Instance.IsPlaying)
            return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            MoveLeft();

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            MoveRight();

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            Jump();

        Move();
    }

    // Cambia un carril hacia la izquierda (si no está en el borde)
    public void MoveLeft()
    {
        targetLane = Mathf.Max(0, targetLane - 1);
    }

    // Cambia un carril hacia la derecha (si no está en el borde)
    public void MoveRight()
    {
        targetLane = Mathf.Min(LaneCount - 1, targetLane + 1);
    }

    /*
     * Jump()
     *
     * Si está en el suelo, calcula la velocidad inicial necesaria para
     * alcanzar jumpHeight con la fórmula v = raíz(2 * g * h).
     */
    public void Jump()
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(2f * -gravity * jumpHeight);
        }
    }

    // Devuelve la posición X del centro de un carril (carril 1 = centro, X = 0)
    public float GetLaneX(int lane)
    {
        return (lane - 1) * laneWidth;
    }

    /*
     * Move()
     *
     * Combina los tres movimientos del cuadro: avance hacia adelante,
     * desplazamiento suave hacia el carril objetivo y salto/gravedad.
     */
    void Move()
    {
        // Desplazamiento lateral suave hacia el centro del carril objetivo
        float currentX = transform.position.x;
        float newX = Mathf.MoveTowards(currentX, GetLaneX(targetLane), laneChangeSpeed * Time.deltaTime);

        // Mientras está en el suelo se mantiene "pegado" con una pequeña velocidad hacia abajo
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = new Vector3(newX - currentX, verticalVelocity * Time.deltaTime, CurrentForwardSpeed * Time.deltaTime);
        characterController.Move(motion);
    }

    /*
     * OnTriggerEnter()
     *
     * Detecta con qué choca el jugador: si es una moneda la recoge,
     * si es un dinosaurio u obstáculo (Hazard) el jugador muere.
     */
    void OnTriggerEnter(Collider other)
    {
        if (isDead)
            return;

        Coin coin = other.GetComponentInParent<Coin>();
        if (coin != null)
        {
            coin.Collect();
            return;
        }

        Hazard hazard = other.GetComponentInParent<Hazard>();
        if (hazard != null)
        {
            hazard.OnPlayerCollision(this);
        }
    }

    /*
     * Die()
     *
     * Marca al jugador como muerto, detiene su animación y avisa al
     * GameManager para mostrar el menú de derrota.
     */
    public void Die()
    {
        if (isDead)
            return; // Evita que se ejecute dos veces

        isDead = true;
        Debug.Log("El Jugador ha muerto");

        if (modelAnimator != null)
            modelAnimator.speed = 0f;

        GameManager.Instance.EndGame();
    }

    // La animación de carrera solo se reproduce mientras se juega
    void OnStateChanged(GameState state)
    {
        if (modelAnimator != null && !isDead)
            modelAnimator.speed = state == GameState.Playing ? runAnimationSpeed : 0f;
    }
}
