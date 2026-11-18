using System.Collections.Generic;
using UnityEngine;

/*
 * PlatformGenerator.cs
 *
 * Sistema de "suelos infinitos".
 *  - Al comenzar genera entre 10 y 30 plataformas seguidas.
 *  - Cuando el jugador deja atrás una plataforma, la destruye (con todos sus hijos)
 *    y genera 1 plataforma nueva al final del camino.
 *  - Decide qué aparece en cada plataforma: un dinosaurio o un vehículo en un
 *    carril al azar, y monedas en otro carril. Siempre deja un carril libre
 *    y cada plataforma tiene como mínimo 1 hijo.
 *  - La cantidad de peligros aumenta con la dificultad.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public class PlatformGenerator : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;          // Jugador, para saber qué plataformas quedaron atrás
    public Platform platformPrefab;   // Prefab de la plataforma (suelo)

    [Header("Contenido de las plataformas")]
    public Dinosaur[] dinosaurPrefabs; // Prefabs de los diseños de dinosaurio
    public Vehicle[] vehiclePrefabs;   // Prefabs de los vehículos abandonados
    public Coin coinPrefab;            // Prefab de la moneda
    public GameObject[] buildingPrefabs; // Edificios para decorar los costados
    public GameObject[] propPrefabs;     // Escombros y objetos para las veredas

    [Header("Generación")]
    [Range(10, 30)]
    public int platformCount = 15;          // Plataformas activas a la vez (entre 10 y 30)
    public int safeStartPlatforms = 3;      // Plataformas iniciales sin peligros
    public float destroyDistanceBehind = 15f; // Cuánto debe pasar el jugador el borde para destruirla

    [Header("Probabilidades")]
    public float minHazardChance = 0.6f;        // Probabilidad de peligro al inicio
    public float maxHazardChance = 0.95f;       // Probabilidad de peligro con dificultad máxima
    public float maxSecondHazardChance = 0.45f; // Probabilidad de un 2° peligro con dificultad máxima
    [Range(0f, 1f)] public float dinosaurChance = 0.5f; // Probabilidad de que el peligro sea dinosaurio
    [Range(0f, 1f)] public float coinRowChance = 0.7f;  // Probabilidad de una fila de monedas
    [Range(0f, 1f)] public float propChance = 0.5f;     // Probabilidad de escombros en cada vereda

    private Queue<Platform> activePlatforms = new Queue<Platform>(); // De la más cercana a la más lejana
    private List<int> dinosaurBag = new List<int>(); // "Bolsa" para que salgan todos los diseños
    private float nextPlatformStartZ;                // Dónde empieza la próxima plataforma
    private int platformsSpawned = 0;                // Total de plataformas generadas

    public int ActivePlatformCount { get { return activePlatforms.Count; } }

    /*
     * Start()
     *
     * Genera el camino inicial. La primera plataforma empieza detrás del
     * jugador para que la cámara siempre vea suelo.
     */
    void Start()
    {
        platformCount = Mathf.Clamp(platformCount, 10, 30);
        nextPlatformStartZ = player.position.z - platformPrefab.length * 1.5f;

        for (int i = 0; i < platformCount; i++)
        {
            SpawnPlatform();
        }
    }

    /*
     * Update()
     *
     * Si el jugador ya dejó atrás la primera plataforma de la fila,
     * se destruye y se genera una nueva al final (1 destruida = 1 nueva).
     */
    void Update()
    {
        Platform oldest = activePlatforms.Peek();
        if (player.position.z > oldest.EndZ + destroyDistanceBehind)
        {
            activePlatforms.Dequeue();
            Destroy(oldest.gameObject);
            SpawnPlatform();
        }
    }

    // Crea una plataforma al final del camino, la decora y la llena de contenido
    void SpawnPlatform()
    {
        Vector3 position = new Vector3(0f, 0f, nextPlatformStartZ + platformPrefab.length * 0.5f);
        Platform platform = Instantiate(platformPrefab, position, Quaternion.identity, transform);
        platform.name = "Plataforma_" + platformsSpawned;

        platform.Decorate(buildingPrefabs, propPrefabs, propChance);

        bool isSafe = platformsSpawned <= safeStartPlatforms;
        FillPlatform(platform, isSafe);

        activePlatforms.Enqueue(platform);
        nextPlatformStartZ += platformPrefab.length;
        platformsSpawned++;
    }

    /*
     * FillPlatform()
     *
     * Mezcla los 3 carriles y va tomando carriles libres: primero para los
     * peligros (si corresponde) y después para las monedas. Nunca se ocupan
     * los 3 carriles con peligros, así siempre hay salida.
     */
    void FillPlatform(Platform platform, bool isSafe)
    {
        List<int> freeLanes = new List<int> { 0, 1, 2 };
        Shuffle(freeLanes);

        int children = 0;
        float difficulty = GameManager.Instance.Difficulty01;

        if (!isSafe && Random.value < Mathf.Lerp(minHazardChance, maxHazardChance, difficulty))
        {
            SpawnHazard(platform, TakeLane(freeLanes));
            children++;

            if (Random.value < Mathf.Lerp(0f, maxSecondHazardChance, difficulty))
            {
                SpawnHazard(platform, TakeLane(freeLanes));
                children++;
            }
        }

        // Si no salió ningún peligro, las monedas son obligatorias (mínimo 1 hijo)
        if (children == 0 || Random.value < coinRowChance)
        {
            int lane = TakeLane(freeLanes);
            for (int i = -1; i <= 1; i++)
            {
                platform.SpawnInLane(coinPrefab, lane, i * 3f); // Fila de 3 monedas
            }
        }
    }

    // Genera un dinosaurio o un vehículo en el carril indicado
    void SpawnHazard(Platform platform, int lane)
    {
        if (Random.value < dinosaurChance)
        {
            platform.SpawnInLane(dinosaurPrefabs[NextDinosaurIndex()], lane, 0f);
        }
        else
        {
            Vehicle vehicle = vehiclePrefabs[Random.Range(0, vehiclePrefabs.Length)];
            platform.SpawnInLane(vehicle, lane, 0f);
        }
    }

    /*
     * NextDinosaurIndex()
     *
     * Usa una "bolsa" mezclada de diseños: todos los diseños salen una vez
     * antes de repetirse, garantizando que aparezcan todos en la partida.
     */
    int NextDinosaurIndex()
    {
        if (dinosaurBag.Count == 0)
        {
            for (int i = 0; i < dinosaurPrefabs.Length; i++)
                dinosaurBag.Add(i);
            Shuffle(dinosaurBag);
        }

        int index = dinosaurBag[dinosaurBag.Count - 1];
        dinosaurBag.RemoveAt(dinosaurBag.Count - 1);
        return index;
    }

    // Saca el primer carril libre de la lista (la lista ya está mezclada)
    int TakeLane(List<int> freeLanes)
    {
        int lane = freeLanes[0];
        freeLanes.RemoveAt(0);
        return lane;
    }

    // Mezcla una lista al azar (algoritmo Fisher-Yates)
    void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
