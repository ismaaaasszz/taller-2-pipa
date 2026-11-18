using System;
using System.Collections;
using System.IO;
using UnityEngine;

/*
 * AutoTestBot.cs
 *
 * Herramienta de prueba automática (contenido extra).
 * Solo se activa si el juego compilado se abre con el argumento "-autotest";
 * en una partida normal se desactiva sola y no hace nada.
 * Juega sola por más de 30 segundos (dispara a los dinosaurios, salta los
 * vehículos y esquiva), toma capturas de pantalla y escribe un resumen en el log.
 * Sirve para comprobar que el juego es "jugable" al menos 30 segundos.
 *
 * Ejemplo: DinoCityRun.exe -autotest -autotestdir C:\capturas
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public class AutoTestBot : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerController player; // Jugador que controla el bot
    public PlayerShooter shooter;   // Disparo del jugador

    [Header("Configuración")]
    public float playSeconds = 35f; // Segundos que juega antes de dejarse perder

    private string outputFolder;    // Carpeta donde se guardan las capturas
    private bool isDriving = false; // Si el bot está controlando al jugador
    private int jumps = 0;          // Saltos realizados
    private int shots = 0;          // Disparos realizados
    private int dodges = 0;         // Cambios de carril para esquivar

    void Awake()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (Array.IndexOf(args, "-autotest") < 0)
        {
            enabled = false; // Partida normal: el bot no hace nada
            return;
        }

        int dirIndex = Array.IndexOf(args, "-autotestdir");
        outputFolder = dirIndex >= 0 && dirIndex + 1 < args.Length
            ? args[dirIndex + 1]
            : Path.Combine(Application.persistentDataPath, "AutoTest");
        Directory.CreateDirectory(outputFolder);
    }

    void Start()
    {
        if (enabled)
            StartCoroutine(RunTest());
    }

    /*
     * RunTest()
     *
     * Secuencia completa: captura del menú, inicio de partida, capturas
     * durante el juego, derrota, captura del menú de derrota y cierre.
     */
    IEnumerator RunTest()
    {
        Log("Inicio de prueba automática");
        yield return new WaitForSecondsRealtime(2f);
        Capture("01_menu_principal");
        yield return new WaitForSecondsRealtime(0.5f); // Espera a que la captura se guarde

        GameManager.Instance.StartGame();
        isDriving = true;
        float startTime = Time.realtimeSinceStartup;

        yield return new WaitForSeconds(1.5f);
        Capture("02_inicio_partida");
        yield return new WaitForSeconds(6f);
        Capture("03_partida_8s");
        yield return new WaitForSeconds(8f);
        Capture("04_partida_16s");
        yield return new WaitForSeconds(8f);
        Capture("05_partida_24s");

        while (GameManager.Instance.State != GameState.GameOver && Time.realtimeSinceStartup - startTime < playSeconds)
            yield return null;

        Log("Sobrevivió " + (Time.realtimeSinceStartup - startTime).ToString("F1") + " s. Estado=" + GameManager.Instance.State +
            " Puntaje=" + ScoreManager.Instance.GetScore() + " Monedas=" + CoinManager.Instance.GetCoins() +
            " Dinos=" + GameManager.Instance.DinosaursKilled + " Saltos=" + jumps + " Disparos=" + shots + " Esquivas=" + dodges);

        // Deja de controlar al jugador para que choque y aparezca el menú de derrota
        isDriving = false;
        float waitStart = Time.realtimeSinceStartup;
        while (GameManager.Instance.State != GameState.GameOver && Time.realtimeSinceStartup - waitStart < 30f)
            yield return null;

        yield return new WaitForSecondsRealtime(1.5f);
        Capture("06_menu_derrota");
        Log("Final: Estado=" + GameManager.Instance.State + " Puntaje=" + ScoreManager.Instance.GetScore());
        yield return new WaitForSecondsRealtime(1f);
        Application.Quit();
    }

    /*
     * Update()
     *
     * Decide qué hacer cada cuadro: dispara a los dinosaurios de su carril,
     * salta los vehículos cuando están cerca y esquiva si no alcanza a disparar.
     */
    void Update()
    {
        if (!isDriving || !GameManager.Instance.IsPlaying || player.IsDead)
            return;

        CaptureCloseUps();

        float distance;
        Hazard closest = FindClosestHazard(player.CurrentLane, 45f, out distance);
        if (closest == null)
        {
            GoTowardsNearestVehicle(); // Para probar el salto, busca vehículos
            return;
        }

        if (closest is Dinosaur)
        {
            if (shooter.TryShoot())
                shots++;
            else if (distance < 9f)
                DodgeToSafeLane();
        }
        else if (closest is Vehicle && distance < 3.2f && player.IsGrounded)
        {
            player.Jump();
            jumps++;
        }
    }

    private bool capturedDinosaur = false; // Ya se tomó la captura de un dinosaurio cercano
    private bool capturedVehicle = false;  // Ya se tomó la captura de un vehículo cercano
    private bool capturedJump = false;     // Ya se tomó la captura de un salto
    private float nextLaneChangeTime = 0f; // Evita cambiar de carril cada cuadro

    // Toma capturas cuando hay un dinosaurio o vehículo cerca, y durante un salto sobre un vehículo
    void CaptureCloseUps()
    {
        foreach (Hazard hazard in FindObjectsOfType<Hazard>())
        {
            float ahead = hazard.transform.position.z - player.transform.position.z;
            if (!capturedDinosaur && hazard is Dinosaur && ahead > 5f && ahead < 9f)
            {
                capturedDinosaur = true;
                Capture("07_dinosaurio_cerca");
            }
            else if (!capturedVehicle && hazard is Vehicle && ahead > 6f && ahead < 10f)
            {
                capturedVehicle = true;
                Capture("08_vehiculo_cerca");
            }
        }

        if (!capturedJump && jumps > 0 && !player.IsGrounded && player.transform.position.y > 1.6f)
        {
            capturedJump = true;
            Capture("09_salto_sobre_vehiculo");
        }
    }

    // Si hay un vehículo cerca en otro carril (y el carril vecino está libre), se mueve hacia él
    void GoTowardsNearestVehicle()
    {
        if (Time.time < nextLaneChangeTime)
            return;

        foreach (Vehicle vehicle in FindObjectsOfType<Vehicle>())
        {
            float ahead = vehicle.transform.position.z - player.transform.position.z;
            if (ahead < 15f || ahead > 35f)
                continue;

            int direction = vehicle.transform.position.x > player.GetLaneX(player.CurrentLane) + 1f ? 1
                          : vehicle.transform.position.x < player.GetLaneX(player.CurrentLane) - 1f ? -1 : 0;
            float unused;
            int nextLane = player.CurrentLane + direction;
            if (direction == 0 || FindClosestHazard(nextLane, 15f, out unused) != null)
                continue;

            if (direction < 0) player.MoveLeft();
            else player.MoveRight();
            nextLaneChangeTime = Time.time + 0.6f;
            return;
        }
    }

    // Se mueve a un carril vecino sin peligros cercanos
    void DodgeToSafeLane()
    {
        int lane = player.CurrentLane;
        int[] candidates = { lane - 1, lane + 1 };
        foreach (int candidate in candidates)
        {
            float unused;
            if (candidate < 0 || candidate >= PlayerController.LaneCount || FindClosestHazard(candidate, 12f, out unused) != null)
                continue;

            if (candidate < lane) player.MoveLeft();
            else player.MoveRight();
            dodges++;
            return;
        }
    }

    // Busca el peligro más cercano por delante del jugador en un carril
    Hazard FindClosestHazard(int lane, float maxDistance, out float closestDistance)
    {
        closestDistance = float.MaxValue;
        Hazard closest = null;
        float laneX = player.GetLaneX(lane);

        foreach (Hazard hazard in FindObjectsOfType<Hazard>())
        {
            Vector3 pos = hazard.transform.position;
            float ahead = pos.z - player.transform.position.z;
            if (ahead < -1f || ahead > maxDistance || Mathf.Abs(pos.x - laneX) > 1.2f)
                continue;

            float edgeDistance = ahead - 2f; // Distancia aproximada al borde delantero
            if (edgeDistance < closestDistance)
            {
                closestDistance = edgeDistance;
                closest = hazard;
            }
        }

        return closest;
    }

    // Guarda una captura de pantalla
    void Capture(string fileName)
    {
        string path = Path.Combine(outputFolder, fileName + ".png");
        ScreenCapture.CaptureScreenshot(path);
        Log("Captura: " + path);
    }

    void Log(string message)
    {
        Debug.Log("[AUTOTEST] " + message);
    }
}
