using UnityEngine;

/*
 * Hazard.cs
 *
 * Clase base ABSTRACTA para todo lo que mata al jugador:
 * dinosaurios (enemigos) y vehículos abandonados (obstáculos).
 * Define el comportamiento común que exige el enunciado:
 *  - Matar al jugador al colisionar con él.
 *  - Destruirse al recibir un proyectil (bala) del jugador.
 * Cada clase hija (Dinosaur, Vehicle) define qué pasa además al ser destruida.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public abstract class Hazard : MonoBehaviour
{
    [Header("Peligro")]
    public GameObject destructionEffectPrefab; // Partículas que aparecen al ser destruido

    protected bool isDestroyed = false; // Evita procesar dos impactos seguidos

    /*
     * OnPlayerCollision()
     *
     * Se llama cuando el jugador choca con este peligro: el jugador muere.
     */
    public virtual void OnPlayerCollision(PlayerController player)
    {
        if (isDestroyed)
            return;

        player.Die();
    }

    /*
     * OnHitByProjectile()
     *
     * Se llama cuando una bala impacta este peligro: aparece el efecto,
     * se ejecuta la reacción propia de cada tipo y el objeto se destruye.
     */
    public void OnHitByProjectile()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;

        if (destructionEffectPrefab != null)
            Instantiate(destructionEffectPrefab, transform.position + Vector3.up, Quaternion.identity);

        OnDestroyedByProjectile();
        Destroy(gameObject);
    }

    // Reacción específica de cada tipo de peligro (sonido, contador, etc.)
    protected abstract void OnDestroyedByProjectile();
}
