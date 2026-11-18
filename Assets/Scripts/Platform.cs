using UnityEngine;

/*
 * Platform.cs
 *
 * Plataforma (suelo plano) que forma parte del camino infinito.
 * Tiene 3 posiciones posibles (una por carril) donde pueden aparecer
 * un dinosaurio, un obstáculo o monedas, y espacios a los costados para
 * decorar con edificios y escombros de la ciudad devastada.
 * Todo lo que aparece en ella queda como "hijo" en su jerarquía, por lo
 * que se destruye junto con la plataforma.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public class Platform : MonoBehaviour
{
    [Header("Dimensiones")]
    public float length = 20f; // Largo de la plataforma en Z (dirección de avance)

    [Header("Posiciones de aparición")]
    public Transform[] laneSpawnPoints = new Transform[3]; // Izquierda, centro y derecha

    [Header("Decoración")]
    public Transform[] buildingSlots; // Espacios a los costados para edificios
    public Transform[] propSlots;     // Espacios en las veredas para escombros

    // Coordenada Z del borde final (lejano) de la plataforma
    public float EndZ { get { return transform.position.z + length * 0.5f; } }

    /*
     * SpawnInLane()
     *
     * Crea un objeto (dinosaurio, vehículo o moneda) como HIJO de la plataforma
     * en la posición del carril indicado. zOffset permite correrlo en Z
     * (se usa para hacer filas de monedas).
     */
    public T SpawnInLane<T>(T prefab, int lane, float zOffset) where T : Component
    {
        Transform spawnPoint = laneSpawnPoints[lane];
        Vector3 position = spawnPoint.position + Vector3.forward * zOffset;
        return Instantiate(prefab, position, prefab.transform.rotation, transform);
    }

    /*
     * Decorate()
     *
     * Llena los costados con edificios elegidos al azar y, con cierta
     * probabilidad, pone escombros en las veredas.
     */
    public void Decorate(GameObject[] buildingPrefabs, GameObject[] propPrefabs, float propChance)
    {
        foreach (Transform slot in buildingSlots)
        {
            GameObject building = buildingPrefabs[Random.Range(0, buildingPrefabs.Length)];
            Instantiate(building, slot.position, slot.rotation, slot);
        }

        foreach (Transform slot in propSlots)
        {
            if (Random.value > propChance)
                continue;

            GameObject prop = propPrefabs[Random.Range(0, propPrefabs.Length)];
            Quaternion rotation = slot.rotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Instantiate(prop, slot.position, rotation, slot);
        }
    }
}
