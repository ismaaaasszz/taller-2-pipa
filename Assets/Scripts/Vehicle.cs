using UnityEngine;

/*
 * Vehicle.cs
 *
 * Obstáculo estático: vehículo abandonado (hereda de Hazard).
 * Permanece quieto y fijo a la plataforma donde apareció, es lo bastante
 * bajo para saltarlo y explota al recibir una bala.
 * Al aparecer se gira un poco al azar para que se vea mal estacionado.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public class Vehicle : Hazard
{
    [Header("Vehículo")]
    public string vehicleType = "Auto"; // Tipo de vehículo (Taxi, Policía, Sedán, etc.)
    public Transform modelRoot;         // Modelo visual del vehículo
    public float maxRandomYaw = 12f;    // Giro aleatorio máximo (grados) para el aspecto "abandonado"

    void Start()
    {
        modelRoot.localRotation *= Quaternion.Euler(0f, Random.Range(-maxRandomYaw, maxRandomYaw), 0f);
    }

    // Al ser destruido por una bala suena una explosión
    protected override void OnDestroyedByProjectile()
    {
        AudioManager.Instance.PlayExplosion();
    }
}
