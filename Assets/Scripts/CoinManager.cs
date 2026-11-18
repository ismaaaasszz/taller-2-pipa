using UnityEngine;

/*
 * CoinManager.cs
 *
 * Lleva la cuenta de monedas recolectadas en la partida,
 * de forma independiente al puntaje por distancia.
 * Se reinicia sola al recargar la escena.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance; // Instancia única accesible desde cualquier script

    private int coins = 0; // Monedas recolectadas en la partida actual

    void Awake()
    {
        Instance = this;
    }

    // Suma monedas al contador (solo mientras se juega)
    public void AddCoin(int amount)
    {
        if (!GameManager.Instance.IsPlaying)
            return;

        coins += amount;
    }

    public int GetCoins() { return coins; }
}
