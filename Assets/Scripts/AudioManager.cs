using UnityEngine;

/*
 * AudioManager.cs
 *
 * Centraliza todos los efectos de sonido y la música del juego.
 * Otros scripts acceden a él mediante AudioManager.Instance.
 * Cambia la música automáticamente según el estado de la partida:
 * menú, juego y derrota. Los clips no asignados se ignoran sin errores.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance; // Instancia única accesible desde cualquier script

    [Header("Fuentes de audio")]
    public AudioSource sfxSource;   // Reproduce los efectos cortos
    public AudioSource musicSource; // Reproduce la música en bucle

    [Header("Música")]
    public AudioClip menuMusic;     // Música del menú principal
    public AudioClip gameplayMusic; // Música de fondo mientras se juega
    public AudioClip gameOverMusic; // Música del menú de derrota

    [Header("Clips de sonido")]
    public AudioClip shootSound;         // Disparo del jugador
    public AudioClip dinosaurDeathSound; // Rugido del dinosaurio al morir
    public AudioClip explosionSound;     // Vehículo destruido por una bala
    public AudioClip coinSound;          // Moneda recogida
    public AudioClip playerDeathSound;   // Jugador atrapado
    public AudioClip buttonSound;        // Clic en un botón

    void Awake()
    {
        Instance = this;
    }

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

    /*
     * OnStateChanged()
     *
     * Elige la música según el estado del juego. En pausa la música se detiene
     * momentáneamente y al perder suena el golpe del jugador y la música de derrota.
     */
    void OnStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                PlayMusic(menuMusic, true);
                break;
            case GameState.Playing:
                PlayMusic(gameplayMusic, true);
                musicSource.UnPause();
                break;
            case GameState.Paused:
                musicSource.Pause();
                break;
            case GameState.GameOver:
                PlaySafe(playerDeathSound, 1f);
                PlayMusic(gameOverMusic, false);
                break;
        }
    }

    public void PlayShoot()         { PlaySafe(shootSound, 0.6f); }
    public void PlayDinosaurDeath() { PlaySafe(dinosaurDeathSound, 1f); }
    public void PlayExplosion()     { PlaySafe(explosionSound, 0.8f); }
    public void PlayCoin()          { PlaySafe(coinSound, 0.7f); }
    public void PlayButton()        { PlaySafe(buttonSound, 0.8f); }

    // Cambia la pista de música (si no es la que ya está sonando)
    void PlayMusic(AudioClip clip, bool loop)
    {
        if (clip == null || (musicSource.clip == clip && musicSource.isPlaying))
            return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    // Reproduce un clip solo si existe, evitando errores por campos vacíos
    void PlaySafe(AudioClip clip, float volume)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
