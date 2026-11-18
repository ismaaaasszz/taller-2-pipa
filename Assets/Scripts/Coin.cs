using UnityEngine;

/*
 * Coin.cs
 *
 * Moneda recolectable. Gira y flota sobre la plataforma; cuando el jugador
 * la toca suma al contador de monedas (CoinManager) y desaparece.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public class Coin : MonoBehaviour
{
    [Header("Configuración de la moneda")]
    public int value = 1;                  // Monedas que otorga al recogerla
    public float spinSpeed = 180f;         // Velocidad de giro (grados por segundo)
    public float bobAmplitude = 0.15f;     // Altura de la oscilación vertical
    public GameObject collectEffectPrefab; // Brillo que aparece al recogerla

    private Vector3 startLocalPosition; // Posición inicial (base de la oscilación)
    private float bobOffset;            // Desfase para que no todas floten iguales
    private bool collected = false;     // Evita recogerla dos veces

    void Start()
    {
        startLocalPosition = transform.localPosition;
        bobOffset = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        float bob = Mathf.Sin(Time.time * 3f + bobOffset) * bobAmplitude;
        transform.localPosition = startLocalPosition + Vector3.up * bob;
    }

    /*
     * Collect()
     *
     * Suma la moneda al contador, reproduce el sonido y la destruye.
     */
    public void Collect()
    {
        if (collected)
            return;

        collected = true;
        CoinManager.Instance.AddCoin(value);
        AudioManager.Instance.PlayCoin();

        if (collectEffectPrefab != null)
            Instantiate(collectEffectPrefab, transform.position + Vector3.up, Quaternion.identity);

        Destroy(gameObject);
    }
}
