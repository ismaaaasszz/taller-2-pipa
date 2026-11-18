using UnityEngine;

/*
 * PlayerShooter.cs
 *
 * Permite al jugador disparar balas hacia adelante al presionar la tecla "F".
 * Entre un disparo y otro debe pasar como mínimo 0.5 segundos, para que
 * no se puedan lanzar proyectiles en grandes cantidades.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
[RequireComponent(typeof(PlayerController))]
public class PlayerShooter : MonoBehaviour
{
    [Header("Configuración de disparo")]
    public Projectile projectilePrefab; // Prefab de la bala
    public Transform muzzlePoint;       // Punto desde donde salen las balas (boca del arma)
    public float fireCooldown = 0.5f;   // Tiempo mínimo entre disparos (segundos)

    [Header("Efectos")]
    public GameObject muzzleFlashPrefab; // Fogonazo que aparece al disparar

    private PlayerController player;    // Para saber si el jugador está vivo y su velocidad
    private float nextShotTime = 0f;    // Momento (Time.time) desde el cual se puede volver a disparar

    void Awake()
    {
        player = GetComponent<PlayerController>();
        fireCooldown = Mathf.Max(0.5f, fireCooldown); // Nunca menos de 0.5 s (requisito del enunciado)
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying || player.IsDead)
            return;

        // Mantener F presionada dispara cada 0.5 s
        if (Input.GetKey(KeyCode.F))
        {
            TryShoot();
        }
    }

    /*
     * TryShoot()
     *
     * Dispara una bala si ya pasó el tiempo mínimo desde el disparo anterior.
     * La bala hereda la velocidad del jugador para no quedar atrás.
     * Devuelve verdadero si se disparó.
     */
    public bool TryShoot()
    {
        if (Time.time < nextShotTime)
            return false;

        nextShotTime = Time.time + fireCooldown;

        Projectile bullet = Instantiate(projectilePrefab, muzzlePoint.position, Quaternion.identity);
        bullet.Launch(Vector3.forward, player.CurrentForwardSpeed);

        if (muzzleFlashPrefab != null)
            Instantiate(muzzleFlashPrefab, muzzlePoint.position, Quaternion.identity, muzzlePoint);

        AudioManager.Instance.PlayShoot();
        return true;
    }
}
