using UnityEngine;

/*
 * Projectile.cs
 *
 * Bala disparada por el jugador. Viaja en línea recta sin verse afectada
 * por la gravedad (Rigidbody cinemático y sin gravedad). Al chocar con un
 * dinosaurio u obstáculo lo destruye y se destruye a sí misma.
 * Si no choca con nada, desaparece después de unos segundos.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Configuración de la bala")]
    public float speed = 40f;   // Velocidad propia de la bala (m/s)
    public float lifetime = 2f; // Segundos antes de autodestruirse

    private Vector3 direction = Vector3.forward; // Dirección de vuelo
    private float totalSpeed;                    // Velocidad de la bala + velocidad del jugador
    private bool hasHit = false;                 // Evita impactar dos cosas a la vez

    void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Las balas no son afectadas por la gravedad
        rb.isKinematic = true;
        totalSpeed = speed;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    // Configura la dirección y suma la velocidad de quien disparó
    public void Launch(Vector3 flightDirection, float shooterSpeed)
    {
        direction = flightDirection.normalized;
        totalSpeed = speed + shooterSpeed;
    }

    void Update()
    {
        transform.position += direction * totalSpeed * Time.deltaTime;
    }

    /*
     * OnTriggerEnter()
     *
     * Si la bala toca un dinosaurio u obstáculo (Hazard), lo destruye
     * y luego se destruye ella. Ignora monedas, suelo y límites.
     */
    void OnTriggerEnter(Collider other)
    {
        if (hasHit)
            return;

        Hazard hazard = other.GetComponentInParent<Hazard>();
        if (hazard == null)
            return;

        hasHit = true;
        hazard.OnHitByProjectile();
        Destroy(gameObject);
    }
}
