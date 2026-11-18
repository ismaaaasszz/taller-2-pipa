using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
 * DinoCityRunBuilder.cs  (solo Editor)
 *
 * Arma automáticamente todo el proyecto a partir de los assets entregados:
 * configura la importación de modelos y animaciones, genera el audio, crea los
 * materiales, los prefabs (jugador, plataforma, dinosaurios, vehículos, moneda,
 * bala, efectos, decoración), la escena con su interfaz y la configuración de build.
 * Se ejecuta desde el menú superior "Dino City Run" de Unity. Así el proyecto es
 * reproducible: si algo se rompe, basta con volver a ejecutarlo.
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
public static class DinoCityRunBuilder
{
    // ---------- Rutas ----------
    const string Models = "Assets/AssetsDinoCityRun";
    const string AnimatedFolder = Models + "/PersonajesAnimados";
    const string KayKitFolder = Models + "/AutosyCiudad/KayKit";
    const string PropsFolder = Models + "/ArmasyPersonajes/PersonajesyObstaculos";
    const string WeaponsFolder = Models + "/ArmasyPersonajes/Armas";
    const string ApocalypticFolder = Models + "/AutosyCiudad/Apocalyptic";
    const string PlayerFbx = AnimatedFolder + "/Character_Hero@Walk With Rifle (1).fbx";
    const string ScenePath = "Assets/MainGame/DinoCityRun.unity";
    const string TmpFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    // Color del fondo y de la niebla (cielo polvoriento de ciudad devastada)
    static readonly Color SkyColor = new Color(0.80f, 0.64f, 0.47f);

    // Forma de ajustar el tamaño de un modelo
    enum Fit { Height, MaxHorizontal }

    static TMP_FontAsset font;
    static Material particleMaterial;

    // ==================================================================== MENÚ

    [MenuItem("Dino City Run/1. Importar TMP Essentials")]
    public static void ImportTmpEssentials()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
        {
            Debug.Log("[Builder] TMP Essentials ya está importado.");
            return;
        }

        var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.textmeshpro");
        string package = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
        AssetDatabase.ImportPackage(package, false);
        Debug.Log("[Builder] TMP Essentials importado desde " + package);
    }

    [MenuItem("Dino City Run/2. Construir proyecto completo")]
    public static void BuildAll()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
        if (font == null)
            throw new System.Exception("Primero ejecuta 'Dino City Run/1. Importar TMP Essentials'.");

        EnsureFolders();
        ConfigureImporters();

        AudioSynthesizer.GenerateAll("Assets/Audio/SFX", "Assets/Audio/Music");
        AssetDatabase.Refresh();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- Materiales ----------
        Texture2D cityTexture = Tex(KayKitFolder + "/citybits_texture.png");
        Material asphalt = Mat("M_Asfalto", new Color(0.24f, 0.24f, 0.26f), null, 0.15f);
        Material laneLine = Mat("M_LineaCarril", new Color(0.92f, 0.92f, 0.86f), null, 0.1f);
        Material sidewalk = Mat("M_Vereda", new Color(0.72f, 0.70f, 0.66f), Tex(ApocalypticFolder + "/Concrete026_1K_Color.jpg"), 0.05f);
        sidewalk.mainTextureScale = new Vector2(1f, 5f);
        Material dirt = Mat("M_Tierra", new Color(0.55f, 0.48f, 0.40f), Tex(ApocalypticFolder + "/Rocks007_1K_Color.jpg"), 0.05f);
        dirt.mainTextureScale = new Vector2(10f, 4f);
        Material ruinedBuilding = Mat("M_EdificioEnRuinas", new Color(0.72f, 0.66f, 0.60f), cityTexture, 0.1f);
        Material cityProps = Mat("M_ObjetosCiudad", new Color(0.85f, 0.80f, 0.75f), cityTexture, 0.1f);
        Material abandonedCar = Mat("M_VehiculoAbandonado", new Color(0.80f, 0.74f, 0.68f), cityTexture, 0.25f);
        Material playerMat = Mat("M_Jugador", Color.white, Tex(PropsFolder + "/VoxelApocalypse_Character.png"), 0.1f);
        Material weaponMat = Mat("M_Arma", Color.white, Tex(WeaponsFolder + "/T_Weapons.png"), 0.3f);
        Material coinMat = Mat("M_Moneda", new Color(1f, 0.78f, 0.15f), null, 0.75f, 0.85f, new Color(0.45f, 0.30f, 0.02f));
        Material bulletMat = Mat("M_Bala", new Color(1f, 0.85f, 0.3f), null, 0.5f, 0f, new Color(2.5f, 1.6f, 0.4f));
        particleMaterial = ParticleMat("M_Particula", "Legacy Shaders/Particles/Alpha Blended");
        Material trailMat = ParticleMat("M_EstelaBala", "Legacy Shaders/Particles/Additive");

        // ---------- Efectos ----------
        GameObject explosionFx = SavePrefab(Effect("FX_Explosion", new Color(1f, 0.6f, 0.1f), new Color(0.3f, 0.3f, 0.3f), 45, 3f, 9f, 0.3f, 0.9f, 0.9f, 0.4f, 0.8f), "Assets/Prefabs/Efectos/FX_Explosion.prefab");
        GameObject dinoDeathFx = SavePrefab(Effect("FX_MuerteDinosaurio", new Color(0.6f, 0.05f, 0.05f), new Color(0.5f, 0.4f, 0.3f), 40, 2f, 7f, 0.25f, 0.7f, 0.8f, 1f, 1f), "Assets/Prefabs/Efectos/FX_MuerteDinosaurio.prefab");
        GameObject coinFx = SavePrefab(Effect("FX_Moneda", new Color(1f, 0.95f, 0.4f), new Color(1f, 0.7f, 0.1f), 14, 1.5f, 3.5f, 0.1f, 0.25f, 0.4f, 0f, 0.3f), "Assets/Prefabs/Efectos/FX_Moneda.prefab");
        GameObject muzzleFx = Effect("FX_Fogonazo", new Color(1f, 0.95f, 0.5f), new Color(1f, 0.55f, 0.1f), 10, 1f, 3f, 0.15f, 0.35f, 0.1f, 0f, 0.05f);
        var muzzleMain = muzzleFx.GetComponent<ParticleSystem>().main;
        muzzleMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        muzzleFx = SavePrefab(muzzleFx, "Assets/Prefabs/Efectos/FX_Fogonazo.prefab");

        // ---------- Bala ----------
        Projectile bullet = BuildProjectile(bulletMat, trailMat);

        // ---------- Moneda ----------
        Coin coin = BuildCoin(coinMat, coinFx);

        // ---------- Dinosaurios (3 diseños) ----------
        var dinosaurs = new List<Dinosaur>
        {
            BuildDinosaur("Velociraptor", "_Run", 6.5f, 1.5f, new Color(0.62f, 0.42f, 0.26f), 1.3f, dinoDeathFx),
            BuildDinosaur("Triceratops", "_Walk", 3.5f, 2.0f, new Color(0.52f, 0.54f, 0.44f), 1.1f, dinoDeathFx),
            BuildDinosaur("Trex", "_Run", 4.5f, 2.3f, new Color(0.36f, 0.45f, 0.30f), 0.9f, dinoDeathFx),
        };

        // ---------- Vehículos abandonados (5 tipos) ----------
        var vehicles = new List<Vehicle>
        {
            BuildVehicle("car_police", "Policia", abandonedCar, explosionFx),
            BuildVehicle("car_taxi", "Taxi", abandonedCar, explosionFx),
            BuildVehicle("car_sedan", "Sedan", abandonedCar, explosionFx),
            BuildVehicle("car_hatchback", "Hatchback", abandonedCar, explosionFx),
            BuildVehicle("car_stationwagon", "StationWagon", abandonedCar, explosionFx),
        };

        // ---------- Decoración ----------
        var buildings = new List<GameObject>();
        foreach (string letter in new[] { "A", "B", "C", "D", "E", "F", "G", "H" })
        {
            GameObject root = new GameObject("Edificio_" + letter);
            Model(KayKitFolder + "/building_" + letter + "_withoutBase.fbx", Fit.MaxHorizontal, 9.5f, 0f, false, ruinedBuilding).transform.SetParent(root.transform, false);
            buildings.Add(SavePrefab(root, "Assets/Prefabs/Decoracion/Edificio_" + letter + ".prefab"));
        }

        var props = new List<GameObject>
        {
            BuildProp(KayKitFolder + "/dumpster.fbx", Fit.MaxHorizontal, 2.0f, cityProps),
            BuildProp(KayKitFolder + "/firehydrant.fbx", Fit.Height, 0.9f, cityProps),
            BuildProp(KayKitFolder + "/trash_A.fbx", Fit.MaxHorizontal, 1.0f, cityProps),
            BuildProp(KayKitFolder + "/trash_B.fbx", Fit.MaxHorizontal, 1.0f, cityProps),
            BuildProp(KayKitFolder + "/box_A.fbx", Fit.MaxHorizontal, 1.0f, cityProps),
            BuildProp(KayKitFolder + "/bench.fbx", Fit.MaxHorizontal, 2.0f, cityProps),
            BuildProp(KayKitFolder + "/streetlight.fbx", Fit.Height, 5f, cityProps),
            BuildProp(PropsFolder + "/Trash.fbx", Fit.MaxHorizontal, 1.6f, null),
            BuildProp(PropsFolder + "/Barricade_03.fbx", Fit.MaxHorizontal, 2.2f, null),
            BuildProp(PropsFolder + "/AttachedBoxes.fbx", Fit.MaxHorizontal, 1.6f, null),
            BuildProp(PropsFolder + "/BarbedWires.fbx", Fit.MaxHorizontal, 2.2f, null),
            BuildProp(PropsFolder + "/Bonefire.fbx", Fit.MaxHorizontal, 1.2f, null),
        };

        // ---------- Plataforma ----------
        Platform platform = BuildPlatform(asphalt, laneLine, sidewalk, dirt);

        // ---------- Jugador ----------
        GameObject playerPrefab = BuildPlayer(playerMat, weaponMat, bullet, muzzleFx);

        // ---------- Escena ----------
        BuildScene(playerPrefab, platform, dinosaurs.ToArray(), vehicles.ToArray(), coin, buildings.ToArray(), props.ToArray());

        ConfigurePlayerSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[Builder] Proyecto construido correctamente.");
    }

    // Construye el proyecto y compila el build de Windows en un solo paso (uso por línea de comandos)
    public static void BuildAllAndWindows()
    {
        BuildAll();
        BuildWindows();
    }

    [MenuItem("Dino City Run/3. Compilar build Windows")]
    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/DinoCityRun.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log("[Builder] Resultado del build: " + report.summary.result + " (" + report.summary.totalErrors + " errores)");
        if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    // ==================================================================== IMPORTACIÓN

    static void EnsureFolders()
    {
        foreach (string folder in new[] { "Assets/Prefabs/Efectos", "Assets/Prefabs/Dinosaurios", "Assets/Prefabs/Vehiculos",
                                          "Assets/Prefabs/Decoracion", "Assets/Materials", "Assets/Animations", "Assets/Data",
                                          "Assets/Audio/SFX", "Assets/Audio/Music", "Assets/MainGame" })
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }
        }
    }

    /*
     * ConfigureImporters()
     *
     * Los dinosaurios se importan con rig "Generic" y animaciones en bucle.
     * El jugador (Mixamo) con rig "Humanoid"; su animación de caminar se deja
     * en el lugar (sin root motion) para que el movimiento lo controle el código.
     * Las texturas pixeladas usan filtro "Point".
     */
    static void ConfigureImporters()
    {
        foreach (string dino in new[] { "Velociraptor", "Triceratops", "Trex" })
            SetupAnimatedModel(AnimatedFolder + "/" + dino + ".fbx", ModelImporterAnimationType.Generic, false);

        SetupAnimatedModel(PlayerFbx, ModelImporterAnimationType.Human, true);

        foreach (string texture in new[] { PropsFolder + "/VoxelApocalypse_Character.png", WeaponsFolder + "/T_Weapons.png" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(texture);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    static void SetupAnimatedModel(string path, ModelImporterAnimationType type, bool isPlayer)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        if (importer.animationType != type)
        {
            importer.animationType = type;
            if (type == ModelImporterAnimationType.Human)
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
        }

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.loopTime = true;
            if (isPlayer)
            {
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = false; // Se descarta como root motion: el personaje camina en el lugar
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
            }
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    // ==================================================================== PREFABS

    static Projectile BuildProjectile(Material bulletMat, Material trailMat)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Bala";
        root.transform.localScale = Vector3.one * 0.3f;
        root.GetComponent<Renderer>().sharedMaterial = bulletMat;
        root.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        SphereCollider collider = root.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 1.5f; // ~0.45 m reales: más fácil acertar

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.sharedMaterial = trailMat;
        trail.time = 0.12f;
        trail.startWidth = 0.22f;
        trail.endWidth = 0f;
        trail.startColor = new Color(1f, 0.8f, 0.3f, 1f);
        trail.endColor = new Color(1f, 0.4f, 0f, 0f);
        trail.shadowCastingMode = ShadowCastingMode.Off;

        root.AddComponent<Projectile>();
        return SavePrefab(root, "Assets/Prefabs/Bala.prefab").GetComponent<Projectile>();
    }

    static Coin BuildCoin(Material coinMat, GameObject coinFx)
    {
        GameObject root = new GameObject("Moneda");
        SphereCollider collider = root.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.75f;
        collider.center = new Vector3(0f, 1f, 0f);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, 1f, 0f);
        visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        visual.transform.localScale = new Vector3(0.9f, 0.06f, 0.9f);
        visual.GetComponent<Renderer>().sharedMaterial = coinMat;

        Coin coin = root.AddComponent<Coin>();
        coin.collectEffectPrefab = coinFx;
        return SavePrefab(root, "Assets/Prefabs/Moneda.prefab").GetComponent<Coin>();
    }

    /*
     * BuildDinosaur()
     *
     * Crea el diseño (ScriptableObject), el material con su color, el Animator
     * con la animación en bucle y el prefab con collider alto (no se puede saltar).
     */
    static Dinosaur BuildDinosaur(string name, string clipSuffix, float speed, float size, Color color, float animSpeed, GameObject deathFx)
    {
        string fbx = AnimatedFolder + "/" + name + ".fbx";

        string designPath = "Assets/Data/Diseno_" + name + ".asset";
        DinosaurDesign design = AssetDatabase.LoadAssetAtPath<DinosaurDesign>(designPath);
        if (design == null)
        {
            design = ScriptableObject.CreateInstance<DinosaurDesign>();
            AssetDatabase.CreateAsset(design, designPath);
        }
        design.designName = name;
        design.moveSpeed = speed;
        design.sizeMultiplier = size;
        design.color = color;
        design.animationSpeed = animSpeed;
        EditorUtility.SetDirty(design);

        Material material = Mat("M_Dino_" + name, color, null, 0.15f);

        GameObject root = new GameObject("Dino_" + name);
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Modelo con largo base de 3 m (el tamaño final lo da design.sizeMultiplier)
        AnimationClip clip = FindClip(fbx, clipSuffix);
        GameObject model = Model(fbx, Fit.MaxHorizontal, 3f, 180f, true, material, 0f, clip);
        model.transform.SetParent(root.transform, false);
        Bounds bounds = CalculateBounds(model);

        BoxCollider collider = model.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(Mathf.Min(bounds.size.x, 2.4f / size), Mathf.Max(bounds.size.y, 6f / size), bounds.size.z * 0.8f);
        collider.center = new Vector3(0f, collider.size.y * 0.5f, 0f);

        Animator animator = model.GetComponentInChildren<Animator>();
        if (animator == null)
            animator = model.transform.GetChild(0).gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = CreateController("AC_" + name, clip);
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        Dinosaur dinosaur = root.AddComponent<Dinosaur>();
        dinosaur.design = design;
        dinosaur.modelRoot = model.transform;
        dinosaur.modelAnimator = animator;
        dinosaur.destructionEffectPrefab = deathFx;

        Debug.Log("[Builder] Dinosaurio " + name + " tamaño base " + bounds.size);
        return SavePrefab(root, "Assets/Prefabs/Dinosaurios/Dino_" + name + ".prefab").GetComponent<Dinosaur>();
    }

    // Vehículo: modelo de ~4.4 m de largo y como máximo 1.6 m de alto (se puede saltar)
    static Vehicle BuildVehicle(string fbxName, string typeName, Material material, GameObject explosionFx)
    {
        GameObject root = new GameObject("Vehiculo_" + typeName);
        GameObject model = Model(KayKitFolder + "/" + fbxName + ".fbx", Fit.MaxHorizontal, 4.4f, 180f, true, material, 1.6f);
        model.transform.SetParent(root.transform, false);
        Bounds bounds = CalculateBounds(model);

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(Mathf.Min(bounds.size.x, 2.6f) * 0.9f, bounds.size.y * 0.85f, bounds.size.z * 0.9f);
        collider.center = new Vector3(0f, collider.size.y * 0.5f, 0f);

        Vehicle vehicle = root.AddComponent<Vehicle>();
        vehicle.vehicleType = typeName;
        vehicle.modelRoot = model.transform;
        vehicle.destructionEffectPrefab = explosionFx;

        Debug.Log("[Builder] Vehículo " + typeName + " tamaño " + bounds.size);
        return SavePrefab(root, "Assets/Prefabs/Vehiculos/Vehiculo_" + typeName + ".prefab").GetComponent<Vehicle>();
    }

    static GameObject BuildProp(string fbx, Fit fit, float size, Material material)
    {
        string name = "Escombro_" + Path.GetFileNameWithoutExtension(fbx);
        GameObject root = new GameObject(name);
        Model(fbx, fit, size, 0f, false, material).transform.SetParent(root.transform, false);
        return SavePrefab(root, "Assets/Prefabs/Decoracion/" + name + ".prefab");
    }

    /*
     * BuildPlatform()
     *
     * Plataforma de 20 m de largo: calle de 3 carriles (con Material y Color),
     * líneas de carril, veredas, suelo exterior, límites invisibles a los lados,
     * 3 puntos de aparición (uno por carril) y espacios para decoración.
     */
    static Platform BuildPlatform(Material asphalt, Material laneLine, Material sidewalk, Material dirt)
    {
        const float length = 20f;
        GameObject root = new GameObject("Plataforma");

        GameObject road = Cube("Calle", root.transform, new Vector3(0f, -0.25f, 0f), new Vector3(12f, 0.5f, length), asphalt, true);
        road.GetComponent<Renderer>().receiveShadows = true;

        foreach (float x in new[] { -1.75f, 1.75f })
            Cube("LineaCarril", root.transform, new Vector3(x, 0.01f, 0f), new Vector3(0.15f, 0.02f, length), laneLine, false);
        foreach (float x in new[] { -5.7f, 5.7f })
            Cube("LineaBorde", root.transform, new Vector3(x, 0.01f, 0f), new Vector3(0.2f, 0.02f, length), laneLine, false);

        Cube("Vereda_Izquierda", root.transform, new Vector3(-8f, 0.05f, 0f), new Vector3(4f, 0.3f, length), sidewalk, false);
        Cube("Vereda_Derecha", root.transform, new Vector3(8f, 0.05f, 0f), new Vector3(4f, 0.3f, length), sidewalk, false);
        Cube("Suelo_Exterior", root.transform, new Vector3(0f, -0.2f, 0f), new Vector3(70f, 0.2f, length), dirt, false);

        // Límites del mapa: muros invisibles que impiden salir por los costados
        foreach (float x in new[] { -6.3f, 6.3f })
        {
            GameObject wall = new GameObject(x < 0 ? "Limite_Izquierdo" : "Limite_Derecho");
            wall.transform.SetParent(root.transform, false);
            wall.transform.localPosition = new Vector3(x, 5f, 0f);
            BoxCollider wallCollider = wall.AddComponent<BoxCollider>();
            wallCollider.size = new Vector3(0.6f, 12f, length);
        }

        Transform[] lanes = new Transform[3];
        for (int i = 0; i < 3; i++)
            lanes[i] = Empty("Carril_" + i, root.transform, new Vector3((i - 1) * 3.5f, 0f, 0f), 0f);

        var buildingSlots = new List<Transform>();
        var propSlots = new List<Transform>();
        foreach (float z in new[] { -5f, 5f })
        {
            buildingSlots.Add(Empty("Edificio_Izq", root.transform, new Vector3(-15.2f, 0f, z), 90f));
            buildingSlots.Add(Empty("Edificio_Der", root.transform, new Vector3(15.2f, 0f, z), -90f));
            propSlots.Add(Empty("Escombro_Izq", root.transform, new Vector3(-8.3f, 0.2f, z), 0f));
            propSlots.Add(Empty("Escombro_Der", root.transform, new Vector3(8.3f, 0.2f, z), 0f));
        }

        Platform platform = root.AddComponent<Platform>();
        platform.length = length;
        platform.laneSpawnPoints = lanes;
        platform.buildingSlots = buildingSlots.ToArray();
        platform.propSlots = propSlots.ToArray();
        return SavePrefab(root, "Assets/Prefabs/Plataforma.prefab").GetComponent<Platform>();
    }

    /*
     * BuildPlayer()
     *
     * Jugador con CharacterController, modelo animado (Mixamo), rifle,
     * punto de disparo y los scripts PlayerController y PlayerShooter.
     */
    static GameObject BuildPlayer(Material playerMat, Material weaponMat, Projectile bullet, GameObject muzzleFx)
    {
        GameObject root = new GameObject("Jugador");
        root.tag = "Player";

        CharacterController controller = root.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.4f;
        controller.center = new Vector3(0f, 0.92f, 0f);
        controller.stepOffset = 0.3f;
        controller.skinWidth = 0.05f;

        GameObject model = Model(PlayerFbx, Fit.Height, 1.8f, 0f, false, playerMat);
        model.transform.SetParent(root.transform, false);

        Animator animator = model.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = CreateController("AC_Jugador", FindClip(PlayerFbx, ""));
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        GameObject weapon = Model(WeaponsFolder + "/AssaultRiffle_A.fbx", Fit.MaxHorizontal, 0.95f, 0f, true, weaponMat);
        weapon.name = "Arma";
        weapon.transform.SetParent(root.transform, false);
        weapon.transform.localPosition = new Vector3(0.2f, 1.05f, 0.25f);

        Transform muzzle = Empty("PuntoDeDisparo", root.transform, new Vector3(0.2f, 1.2f, 1.0f), 0f);

        PlayerController player = root.AddComponent<PlayerController>();
        player.modelAnimator = animator;

        PlayerShooter shooter = root.AddComponent<PlayerShooter>();
        shooter.projectilePrefab = bullet;
        shooter.muzzlePoint = muzzle;
        shooter.muzzleFlashPrefab = muzzleFx;

        return SavePrefab(root, "Assets/Prefabs/Jugador.prefab");
    }

    // ==================================================================== ESCENA

    static void BuildScene(GameObject playerPrefab, Platform platform, Dinosaur[] dinosaurs, Vehicle[] vehicles, Coin coin, GameObject[] buildings, GameObject[] props)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Luz y ambiente (fondo de color + niebla que oculta la generación de plataformas)
        GameObject lightObject = new GameObject("Luz Direccional");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.88f, 0.72f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.58f, 0.53f, 0.49f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = SkyColor;
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 230f;
        QualitySettings.shadowDistance = 90f;

        // Jugador
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = Vector3.zero;

        // Cámara
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = SkyColor;
        camera.fieldOfView = 60f;
        camera.farClipPlane = 260f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 5.5f, -8.5f);
        cameraObject.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
        cameraObject.AddComponent<CameraFollow>().target = player.transform;

        // Managers
        GameObject managers = new GameObject("GameManager");
        GameManager gameManager = managers.AddComponent<GameManager>();
        ScoreManager scoreManager = managers.AddComponent<ScoreManager>();
        scoreManager.player = player.transform;
        managers.AddComponent<CoinManager>();

        GameObject audioObject = new GameObject("AudioManager");
        AudioManager audio = audioObject.AddComponent<AudioManager>();
        audio.sfxSource = audioObject.AddComponent<AudioSource>();
        audio.sfxSource.playOnAwake = false;
        audio.musicSource = audioObject.AddComponent<AudioSource>();
        audio.musicSource.playOnAwake = false;
        audio.musicSource.volume = 0.45f;
        audio.menuMusic = Clip("Music/musica_menu");
        audio.gameplayMusic = Clip("Music/musica_juego");
        audio.gameOverMusic = Clip("Music/musica_derrota");
        audio.shootSound = Clip("SFX/disparo");
        audio.dinosaurDeathSound = Clip("SFX/dinosaurio_muere");
        audio.explosionSound = Clip("SFX/explosion");
        audio.coinSound = Clip("SFX/moneda");
        audio.playerDeathSound = Clip("SFX/jugador_muere");
        audio.buttonSound = Clip("SFX/click");

        GameObject generatorObject = new GameObject("PlatformGenerator");
        PlatformGenerator generator = generatorObject.AddComponent<PlatformGenerator>();
        generator.player = player.transform;
        generator.platformPrefab = platform;
        generator.dinosaurPrefabs = dinosaurs;
        generator.vehiclePrefabs = vehicles;
        generator.coinPrefab = coin;
        generator.buildingPrefabs = buildings;
        generator.propPrefabs = props;

        BuildUI(gameManager);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    /*
     * BuildUI()
     *
     * Canvas escalado para 1920x1080 con HUD (anclado arriba), menú principal,
     * menú de pausa y menú de derrota. Los botones se conectan al GameManager.
     */
    static void BuildUI(GameManager game)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform canvas = canvasObject.transform;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        Color orange = new Color(1f, 0.55f, 0.12f);
        Color white = Color.white;

        // ---------- HUD ----------
        RectTransform hud = Panel(canvas, "HUD", Color.clear);
        hud.GetComponent<Image>().raycastTarget = false;

        RectTransform counters = Panel(hud, "Contadores", new Color(0f, 0f, 0f, 0.45f));
        Anchor(counters, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -25f), new Vector2(640f, 150f));
        TMP_Text score = Text(counters, "TextoPuntaje", "PUNTUACIÓN: 00000000", 50, white, TextAlignmentOptions.Left);
        Anchor(score.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -10f), new Vector2(600f, 65f));
        Image coinIcon = Img(counters, "IconoMoneda", new Color(1f, 0.8f, 0.15f), "UI/Skin/Knob.psd");
        Anchor(coinIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -88f), new Vector2(46f, 46f));
        TMP_Text coins = Text(counters, "TextoMonedas", "MONEDAS: 00", 50, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Left);
        Anchor(coins.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(85f, -78f), new Vector2(520f, 65f));

        TMP_Text record = Text(hud, "TextoRecord", "RÉCORD: 00000000", 36, white, TextAlignmentOptions.Right);
        Anchor(record.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-35f, -30f), new Vector2(600f, 50f));

        TMP_Text hint = Text(hud, "TextoControles", "A / D: moverse     |     ESPACIO: saltar     |     F: disparar     |     ESC: pausa", 28, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.Center);
        Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1400f, 45f));

        // ---------- Menú principal ----------
        RectTransform mainMenu = Panel(canvas, "MenuPrincipal", new Color(0.06f, 0.03f, 0.02f, 0.72f));
        CenterText(mainMenu, "Titulo", "DINO CITY RUN", 130, orange, 300f, 1400f);
        CenterText(mainMenu, "Subtitulo", "¡Sobrevive a la ciudad invadida por dinosaurios!", 38, white, 190f, 1400f);
        MakeButton(mainMenu, "JUGAR", 50f, game.StartGame);
        MakeButton(mainMenu, "SALIR", -70f, game.QuitGame);
        CenterText(mainMenu, "Controles", "A / D: cambiar de carril     ESPACIO: saltar     F: disparar     ESC: pausa", 30, new Color(1f, 0.9f, 0.75f), -230f, 1600f);
        TMP_Text menuRecord = CenterText(mainMenu, "Record", "RÉCORD: 00000000", 36, new Color(1f, 0.85f, 0.3f), -320f, 900f);

        // ---------- Menú de pausa ----------
        RectTransform pause = Panel(canvas, "MenuPausa", new Color(0f, 0f, 0f, 0.6f));
        CenterText(pause, "Titulo", "PAUSA", 110, orange, 260f, 900f);
        MakeButton(pause, "CONTINUAR", 80f, game.ResumeGame);
        MakeButton(pause, "REINICIAR", -40f, game.RestartGame);
        MakeButton(pause, "MENÚ PRINCIPAL", -160f, game.GoToMainMenu);

        // ---------- Menú de derrota ----------
        RectTransform gameOver = Panel(canvas, "MenuDerrota", new Color(0.25f, 0.02f, 0.0f, 0.68f));
        CenterText(gameOver, "Titulo", "¡TE ATRAPARON!", 110, new Color(1f, 0.35f, 0.2f), 340f, 1400f);
        TMP_Text newRecord = CenterText(gameOver, "NuevoRecord", "¡NUEVO RÉCORD!", 46, new Color(1f, 0.9f, 0.2f), 245f, 900f);
        TMP_Text summary = CenterText(gameOver, "Resumen", "", 40, white, 90f, 1000f);
        summary.rectTransform.sizeDelta = new Vector2(1000f, 230f);
        MakeButton(gameOver, "REINICIAR", -130f, game.RestartGame);
        MakeButton(gameOver, "MENÚ PRINCIPAL", -250f, game.GoToMainMenu);

        UIManager ui = canvasObject.AddComponent<UIManager>();
        ui.hudPanel = hud.gameObject;
        ui.scoreText = score;
        ui.coinText = coins;
        ui.highScoreText = record;
        ui.mainMenuPanel = mainMenu.gameObject;
        ui.pausePanel = pause.gameObject;
        ui.gameOverPanel = gameOver.gameObject;
        ui.menuHighScoreText = menuRecord;
        ui.finalSummaryText = summary;
        ui.newHighScoreLabel = newRecord.gameObject;

        pause.gameObject.SetActive(false);
        gameOver.gameObject.SetActive(false);
    }

    // ==================================================================== AYUDANTES UI

    static RectTransform Panel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static TMP_Text Text(Transform parent, string name, string content, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    static TMP_Text CenterText(Transform parent, string name, string content, float size, Color color, float y, float width)
    {
        TMP_Text text = Text(parent, name, content, size, color, TextAlignmentOptions.Center);
        Anchor(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(width, size * 1.6f));
        return text;
    }

    static Image Img(Transform parent, string name, Color color, string builtinSprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(builtinSprite);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    /*
     * MakeButton()
     *
     * Botón con cambio de color (ColorTint), animación de escala e indicador
     * (ButtonFeedback) y su acción conectada como evento persistente.
     */
    static void MakeButton(Transform parent, string label, float y, UnityAction action)
    {
        GameObject go = new GameObject("Boton_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        Anchor(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(460f, 92f));

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.85f, 0.33f, 0.08f);
        colors.highlightedColor = new Color(1f, 0.6f, 0.15f);
        colors.pressedColor = new Color(0.55f, 0.18f, 0.05f);
        colors.selectedColor = new Color(0.85f, 0.33f, 0.08f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        TMP_Text text = Text(go.transform, "Texto", label, 42, Color.white, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Image indicator = Img(go.transform, "Indicador", new Color(1f, 0.9f, 0.25f), "UI/Skin/Knob.psd");
        Anchor(indicator.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40f, 0f), new Vector2(36f, 36f));
        indicator.gameObject.SetActive(false);

        ButtonFeedback feedback = go.AddComponent<ButtonFeedback>();
        feedback.hoverIndicator = indicator.gameObject;

        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    // ==================================================================== AYUDANTES 3D

    /*
     * Model()
     *
     * Instancia un FBX dentro de un objeto "Modelo", lo escala para que mida "size"
     * (alto o largo), opcionalmente gira su eje largo hacia Z, lo apoya en el suelo
     * (y = 0) y lo centra. Si se entrega un material, se lo asigna a todo el modelo.
     */
    static GameObject Model(string fbx, Fit fit, float size, float yaw, bool alignLongAxisToZ, Material material, float maxHeight = 0f, AnimationClip poseClip = null)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if (asset == null)
            throw new System.Exception("No se encontró el modelo " + fbx);

        GameObject wrapper = new GameObject("Modelo");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.transform.SetParent(wrapper.transform, false);

        // Se mide el modelo en la pose de su animación (la animación puede cambiar la escala del esqueleto)
        if (poseClip != null)
        {
            Vector3 originalPosition = instance.transform.localPosition;
            Quaternion originalRotation = instance.transform.localRotation;
            Vector3 originalScale = instance.transform.localScale;
            poseClip.SampleAnimation(instance, 0f);
            instance.transform.localPosition = originalPosition;
            instance.transform.localRotation = originalRotation;
            instance.transform.localScale = originalScale;
            foreach (SkinnedMeshRenderer skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                skinned.updateWhenOffscreen = true; // Bounds siempre correctos aunque se anime
        }

        if (material != null)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
        }

        Bounds raw = CalculateBounds(wrapper);
        if (alignLongAxisToZ && raw.size.x > raw.size.z)
            yaw += 90f;
        instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * instance.transform.localRotation;

        Bounds bounds = CalculateBounds(wrapper);
        float current = fit == Fit.Height ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.z);
        float scale = size / Mathf.Max(current, 0.0001f);
        if (maxHeight > 0f)
            scale = Mathf.Min(scale, maxHeight / Mathf.Max(bounds.size.y, 0.0001f));
        instance.transform.localScale *= scale;

        bounds = CalculateBounds(wrapper);
        instance.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        return wrapper;
    }

    static Bounds CalculateBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        bounds.center -= go.transform.position;
        return bounds;
    }

    static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        if (!keepCollider)
            Object.DestroyImmediate(cube.GetComponent<Collider>());
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    static Transform Empty(string name, Transform parent, Vector3 position, float yaw)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return go.transform;
    }

    static GameObject Effect(string name, Color colorA, Color colorB, int count, float speedMin, float speedMax,
                             float sizeMin, float sizeMax, float lifetime, float gravity, float radius)
    {
        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
        return go;
    }

    // ==================================================================== AYUDANTES ASSETS

    static Material Mat(string name, Color color, Texture texture, float smoothness, float metallic = 0f, Color? emission = null)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        material.mainTexture = texture;
        material.SetFloat("_Glossiness", smoothness);
        material.SetFloat("_Metallic", metallic);
        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material ParticleMat(string name, string shader)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = Shader.Find(shader);
        material.mainTexture = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        EditorUtility.SetDirty(material);
        return material;
    }

    static Texture2D Tex(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            Debug.LogWarning("[Builder] No se encontró la textura " + path);
        return texture;
    }

    static AudioClip Clip(string name)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + name + ".wav");
    }

    static AnimationClip FindClip(string fbx, string suffix)
    {
        List<AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview")).ToList();
        Debug.Log("[Builder] Clips en " + Path.GetFileName(fbx) + ": " + string.Join(", ", clips.Select(c => c.name)));
        AnimationClip chosen = clips.FirstOrDefault(c => suffix.Length > 0 && c.name.EndsWith(suffix)) ?? clips.First();
        Debug.Log("[Builder] Clip elegido: " + chosen.name);
        return chosen;
    }

    static AnimatorController CreateController(string name, AnimationClip clip)
    {
        string path = "Assets/Animations/" + name + ".controller";
        AssetDatabase.DeleteAsset(path);
        return AnimatorController.CreateAnimatorControllerAtPathWithClip(path, clip);
    }

    static GameObject SavePrefab(GameObject go, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = "UCN";
        PlayerSettings.productName = "Dino City Run";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.runInBackground = true;
    }
}
