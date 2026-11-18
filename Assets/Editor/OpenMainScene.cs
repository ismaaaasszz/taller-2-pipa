using UnityEditor;
using UnityEditor.SceneManagement;

/*
 * OpenMainScene.cs  (solo Editor)
 *
 * Al abrir el proyecto en Unity, si no hay ninguna escena abierta
 * (escena "Untitled"), abre automáticamente la escena del juego
 * Assets/MainGame/DinoCityRun.unity para poder presionar Play directamente.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
[InitializeOnLoad]
public static class OpenMainScene
{
    const string ScenePath = "Assets/MainGame/DinoCityRun.unity"; // Escena principal del juego

    static OpenMainScene()
    {
        EditorApplication.delayCall += OpenIfEmpty;
    }

    // Abre la escena del juego solo si la escena activa no está guardada (Untitled)
    static void OpenIfEmpty()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
            EditorSceneManager.OpenScene(ScenePath);
    }
}
