using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * GameManager.cs
 *
 * Controla el flujo general de la partida de Dino City Run: menú principal,
 * estado "jugando", pausa y menú de derrota. También calcula la dificultad
 * (que aumenta con el tiempo jugado) y reinicia la partida recargando la escena,
 * con lo que todos los puntajes vuelven a cero.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */

// Estados posibles de la partida
public enum GameState
{
    MainMenu,   // Se muestra el menú principal, el jugador está quieto
    Playing,    // La partida está en curso
    Paused,     // La partida está en pausa (menú de pausa visible)
    GameOver    // El jugador perdió y se muestra el menú de derrota
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Instancia única accesible desde cualquier script

    // Si es verdadero, al recargar la escena se empieza a jugar sin pasar por el menú (botón "Reiniciar")
    private static bool startImmediatelyOnLoad = false;

    [Header("Dificultad")]
    public float secondsToMaxDifficulty = 120f; // Segundos de juego necesarios para llegar a la dificultad máxima

    public GameState State { get; private set; } = GameState.MainMenu; // Estado actual de la partida
    public float ElapsedPlayTime { get; private set; }                  // Segundos jugados en la partida actual
    public int DinosaursKilled { get; private set; }                    // Dinosaurios eliminados en la partida

    // Evento que avisa a otros scripts (UI, audio, cámara, jugador) cuando cambia el estado
    public event System.Action<GameState> StateChanged;

    // Atajo para saber si la partida está en curso
    public bool IsPlaying { get { return State == GameState.Playing; } }

    // Valor entre 0 y 1 que indica qué tan difícil es la partida en este momento
    public float Difficulty01 { get { return Mathf.Clamp01(ElapsedPlayTime / secondsToMaxDifficulty); } }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // Por seguridad, si la escena se recargó estando en pausa
    }

    /*
     * Start()
     *
     * Decide si la partida comienza en el menú principal o directamente
     * jugando (cuando se presionó "Reiniciar" en el menú de derrota).
     */
    void Start()
    {
        if (startImmediatelyOnLoad)
        {
            startImmediatelyOnLoad = false;
            StartGame();
        }
        else
        {
            ChangeState(GameState.MainMenu);
        }
    }

    /*
     * Update()
     *
     * Escucha la tecla ESC para pausar/reanudar y acumula el tiempo jugado,
     * que se usa para aumentar la dificultad.
     */
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (State == GameState.Playing)
                PauseGame();
            else if (State == GameState.Paused)
                ResumeGame();
        }

        if (IsPlaying)
        {
            ElapsedPlayTime += Time.deltaTime;
        }
    }

    // Comienza la partida (botón "Jugar")
    public void StartGame()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.Playing);
    }

    // Pausa la partida congelando el tiempo del juego (tecla ESC)
    public void PauseGame()
    {
        if (State != GameState.Playing)
            return;

        Time.timeScale = 0f;
        ChangeState(GameState.Paused);
    }

    // Reanuda una partida en pausa (botón "Continuar" o tecla ESC)
    public void ResumeGame()
    {
        if (State != GameState.Paused)
            return;

        Time.timeScale = 1f;
        ChangeState(GameState.Playing);
    }

    /*
     * EndGame()
     *
     * Se llama cuando el jugador choca con un dinosaurio u obstáculo.
     * Guarda el récord (si corresponde) y muestra el menú de derrota.
     */
    public void EndGame()
    {
        if (State != GameState.Playing)
            return; // Evita terminar la partida dos veces

        ScoreManager.Instance.SaveHighScore();
        ChangeState(GameState.GameOver);
    }

    // Suma un dinosaurio eliminado al contador de la partida
    public void RegisterDinosaurKill()
    {
        DinosaursKilled++;
    }

    /*
     * RestartGame()
     *
     * Reinicia la partida recargando la escena: plataformas, puntaje y monedas
     * vuelven a su estado inicial. Se salta el menú principal.
     */
    public void RestartGame()
    {
        startImmediatelyOnLoad = true;
        ReloadScene();
    }

    // Vuelve al menú principal recargando la escena
    public void GoToMainMenu()
    {
        startImmediatelyOnLoad = false;
        ReloadScene();
    }

    // Cierra el juego (en el editor detiene el modo Play)
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Cambia el estado actual y avisa a todos los scripts suscritos
    void ChangeState(GameState newState)
    {
        State = newState;
        if (StateChanged != null)
            StateChanged(newState);
    }

    // Recarga la escena actual. Importante: restaurar timeScale antes, o la escena nueva queda congelada
    void ReloadScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
