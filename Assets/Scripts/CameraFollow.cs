using UnityEngine;

/*
 * CameraFollow.cs
 *
 * Cámara que sigue al jugador posicionándose arriba y detrás de él.
 * Solo avanza en el eje Z junto con el jugador: no se mueve hacia
 * los lados ni sigue los saltos. Al perder, se sacude brevemente.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public class CameraFollow : MonoBehaviour
{
    [Header("Seguimiento")]
    public Transform target;                            // Jugador a seguir
    public Vector3 offset = new Vector3(0f, 5.5f, -8.5f); // Posición relativa (X e Y quedan fijas)

    [Header("Sacudida al perder")]
    public float shakeDuration = 0.4f;   // Duración de la sacudida (segundos)
    public float shakeMagnitude = 0.35f; // Intensidad de la sacudida

    private float shakeTimeLeft = 0f; // Tiempo que le queda a la sacudida

    void Start()
    {
        GameManager.Instance.StateChanged += OnStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged -= OnStateChanged;
    }

    /*
     * LateUpdate()
     *
     * Se ejecuta después de que el jugador se movió. Solo copia la Z del
     * jugador; la X y la Y son siempre las del offset.
     */
    void LateUpdate()
    {
        Vector3 position = new Vector3(offset.x, offset.y, target.position.z + offset.z);

        if (shakeTimeLeft > 0f)
        {
            shakeTimeLeft -= Time.unscaledDeltaTime;
            position += Random.insideUnitSphere * shakeMagnitude * (shakeTimeLeft / shakeDuration);
        }

        transform.position = position;
    }

    // Inicia la sacudida cuando el jugador pierde
    void OnStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
            shakeTimeLeft = shakeDuration;
    }
}
