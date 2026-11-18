using UnityEngine;
using TMPro;

/*
 * UIManager.cs
 *
 * Controla toda la interfaz del juego:
 *  - HUD: contador de puntaje y de monedas anclados arriba (siempre visibles al jugar).
 *  - Menú principal, menú de pausa y menú de derrota.
 * Muestra u oculta cada panel según el estado de la partida.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    public GameObject hudPanel;    // Panel con los contadores superiores
    public TMP_Text scoreText;     // "PUNTUACIÓN: 00000000"
    public TMP_Text coinText;      // "MONEDAS: 00"
    public TMP_Text highScoreText; // "RÉCORD: 00000000"

    [Header("Paneles de menú")]
    public GameObject mainMenuPanel; // Menú principal
    public GameObject pausePanel;    // Menú de pausa
    public GameObject gameOverPanel; // Menú de derrota

    [Header("Textos de menús")]
    public TMP_Text menuHighScoreText;   // Récord mostrado en el menú principal
    public TMP_Text finalSummaryText;    // Resumen de la partida en el menú de derrota
    public GameObject newHighScoreLabel; // Cartel "¡NUEVO RÉCORD!"

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

    // Actualiza los contadores del HUD cada cuadro
    void Update()
    {
        scoreText.text = "PUNTUACIÓN: " + ScoreManager.Instance.GetScore().ToString("00000000");
        coinText.text = "MONEDAS: " + CoinManager.Instance.GetCoins().ToString("00");
        highScoreText.text = "RÉCORD: " + ScoreManager.Instance.GetHighScore().ToString("00000000");
    }

    /*
     * OnStateChanged()
     *
     * Activa solo los paneles que corresponden al estado actual.
     * Al perder, escribe el resumen de la partida.
     */
    void OnStateChanged(GameState state)
    {
        hudPanel.SetActive(state != GameState.MainMenu);
        mainMenuPanel.SetActive(state == GameState.MainMenu);
        pausePanel.SetActive(state == GameState.Paused);
        gameOverPanel.SetActive(state == GameState.GameOver);

        if (state == GameState.MainMenu)
        {
            menuHighScoreText.text = "RÉCORD: " + ScoreManager.Instance.GetHighScore().ToString("00000000");
        }

        if (state == GameState.GameOver)
        {
            finalSummaryText.text =
                "PUNTUACIÓN: " + ScoreManager.Instance.GetScore().ToString("00000000") + "\n" +
                "MONEDAS: " + CoinManager.Instance.GetCoins() + "\n" +
                "DINOSAURIOS ELIMINADOS: " + GameManager.Instance.DinosaursKilled + "\n" +
                "RÉCORD: " + ScoreManager.Instance.GetHighScore().ToString("00000000");

            newHighScoreLabel.SetActive(ScoreManager.Instance.IsNewHighScore());
        }
    }
}
