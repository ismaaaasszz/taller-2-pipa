using UnityEngine;

/*
 * ScoreManager.cs
 *
 * Calcula el puntaje de la partida según la distancia que avanza el jugador
 * y administra el récord (mejor puntaje), que se guarda entre partidas con PlayerPrefs.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance; // Instancia única accesible desde cualquier script

    [Header("Referencias")]
    public Transform player; // Jugador, para medir cuánto ha avanzado

    [Header("Configuración")]
    public float pointsPerMeter = 1f; // Puntos que se ganan por cada metro recorrido

    private const string HighScoreKey = "DinoCityRun_BestScore"; // Clave del récord en PlayerPrefs
    private float startPositionZ;                                 // Posición Z donde empezó el jugador
    private int score = 0;                                        // Puntaje actual
    private int highScore = 0;                                    // Mejor puntaje guardado
    private bool isNewHighScore = false;                          // Si la partida terminó con nuevo récord

    void Awake()
    {
        Instance = this;
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    void Start()
    {
        startPositionZ = player.position.z;
    }

    /*
     * Update()
     *
     * Mientras se juega, el puntaje es la distancia avanzada (en metros)
     * multiplicada por los puntos por metro.
     */
    void Update()
    {
        if (!GameManager.Instance.IsPlaying)
            return;

        float distance = Mathf.Max(0f, player.position.z - startPositionZ);
        score = Mathf.FloorToInt(distance * pointsPerMeter);
    }

    /*
     * SaveHighScore()
     *
     * Se llama al perder: si el puntaje supera al récord, lo guarda.
     */
    public void SaveHighScore()
    {
        if (score > highScore)
        {
            highScore = score;
            isNewHighScore = true;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }
    }

    public int GetScore()        { return score; }
    public int GetHighScore()    { return Mathf.Max(highScore, score); }
    public bool IsNewHighScore() { return isNewHighScore; }
}
