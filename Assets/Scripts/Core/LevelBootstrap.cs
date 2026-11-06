using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Unity.AI.Navigation;
using Cmis3150.Fps.Player;
using Cmis3150.Fps.Weapons;
using Cmis3150.Fps.AI;
using Cmis3150.Fps.Items;
using Cmis3150.Fps.UI;
using Cmis3150.Fps.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Cmis3150.Fps.Core
{
    /// <summary>
    /// Level generator. Normally run ONCE in the Editor (menu CMIS3150 → Build Level In Scene):
    /// it creates every GameObject of the level as a real scene object, saves materials as assets,
    /// assigns the WAV clips, bakes the NavMeshSurface and saves MainLevel.unity.
    /// After that the scene contains ordinary objects (Player, Guards, TreasureSites, HUD ...)
    /// and this component is no longer in the scene.
    /// It can still run at runtime (Play with buildOnAwake) as a fallback.
    /// </summary>
    public class LevelBootstrap : MonoBehaviour
    {
        [Header("Bootstrap")]
        [SerializeField] bool buildOnAwake = true;

        public const string MaterialFolder = "Assets/Materials";
        public const string NavMeshAssetPath = "Assets/Scenes/MainLevel_NavMesh.asset";

        bool _built;
        NavMeshSurface _surface;
        readonly Dictionary<string, Material> _mats = new();

        Material _tracerMat;
        Material _explosionMat;

        void Awake()
        {
            if (buildOnAwake && Application.isPlaying)
                Build();
        }

        // ---------- helpers that work in both Edit mode and Play mode ----------

        static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        Material Mat(string name, Color color, bool emissive = false)
        {
            if (_mats.TryGetValue(name, out var cached)) return cached;
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader) { name = name, color = color };
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 2f);
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (!AssetDatabase.IsValidFolder(MaterialFolder))
                    AssetDatabase.CreateFolder("Assets", "Materials");
                string path = $"{MaterialFolder}/{name}.mat";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(m, path);
            }
#endif
            _mats[name] = m;
            return m;
        }

        GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!keepCollider) Kill(go.GetComponent<Collider>());
            return go;
        }

        static void IgnoreForNavMesh(GameObject go)
        {
            var mod = go.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;
        }

        [ContextMenu("Rebuild Level")]
        public void Build()
        {
            if (_built) return;
            _built = true;

            _tracerMat = Mat("M_Tracer", new Color(1f, 0.85f, 0.3f), emissive: true);
            _explosionMat = Mat("M_Explosion", new Color(1f, 0.45f, 0.1f), emissive: true);

            CreateLighting();
            CreateGround(out GameObject ground);
            CreateHouse(out Vector3 houseInterior);
            CreateGameSystems();
            CreatePlayer(new Vector3(0f, 0.1f, -22f));
            CreateHud();
            CreateAudio();

            // --- Treasure A: open yard (west), 1 guard ---
            CreateTreasureSite(
                siteName: "TreasureSite_A_Yard",
                boxPos: new Vector3(-16f, 0.6f, 2f),
                zoneRadius: 9f,
                guards: new[] { new GuardSpec("Guard_A", new Vector3(-14f, 0f, 4f)) });

            // --- Treasure B: open yard (east), 2 guards ---
            CreateTreasureSite(
                siteName: "TreasureSite_B_Yard",
                boxPos: new Vector3(16f, 0.6f, 0f),
                zoneRadius: 9f,
                guards: new[]
                {
                    new GuardSpec("Guard_B1", new Vector3(18f, 0f, -1f)),
                    new GuardSpec("Guard_B2", new Vector3(15f, 0f, 3f))
                });

            // --- Treasure C: inside the house (door only), 1 guard inside + 1 outside ---
            CreateTreasureSite(
                siteName: "TreasureSite_C_House",
                boxPos: houseInterior,
                zoneRadius: 9f,
                guards: new[]
                {
                    // Inside guard patrols only the room.
                    new GuardSpec("Guard_C_Inside", new Vector3(1.5f, 0f, 15f), new Vector3(0f, 0f, 14.5f), 2.3f),
                    // Outside guard patrols the front yard by the door.
                    new GuardSpec("Guard_C_Outside", new Vector3(0f, 0f, 8f), new Vector3(0f, 0f, 7.5f), 4f)
                });

            // Pickups — exactly one gun in the world.
            CreateWeaponPickup(PickupKind.Gun, new Vector3(-3f, 0.6f, -16f), 1);
            CreateWeaponPickup(PickupKind.Ammo, new Vector3(4f, 0.5f, -12f), 6);
            CreateWeaponPickup(PickupKind.Ammo, new Vector3(-8f, 0.5f, -6f), 6);
            CreateWeaponPickup(PickupKind.Ammo, new Vector3(8f, 0.5f, -6f), 6);
            CreateWeaponPickup(PickupKind.Ammo, new Vector3(-22f, 0.5f, 14f), 6);
            CreateWeaponPickup(PickupKind.Grenade, new Vector3(3f, 0.5f, -17f), 1);
            CreateWeaponPickup(PickupKind.Grenade, new Vector3(-10f, 0.5f, -14f), 1);
            CreateWeaponPickup(PickupKind.Grenade, new Vector3(10f, 0.5f, -14f), 1);
            CreateWeaponPickup(PickupKind.Grenade, new Vector3(22f, 0.5f, 14f), 1);
            CreateHealthKit(new Vector3(0f, 0.6f, -8f));
            CreateHealthKit(new Vector3(-24f, 0.6f, -10f));
            CreateHealthKit(new Vector3(24f, 0.6f, -10f));
            CreateHealthKit(new Vector3(-10f, 0.6f, 22f));

            // NavMesh Surface on the ground: bake walkable area for the guards' NavMeshAgents.
            _surface = ground.AddComponent<NavMeshSurface>();
            _surface.collectObjects = CollectObjects.All;
            _surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            _surface.BuildNavMesh();
#if UNITY_EDITOR
            if (!Application.isPlaying && _surface.navMeshData != null)
            {
                AssetDatabase.DeleteAsset(NavMeshAssetPath);
                AssetDatabase.CreateAsset(_surface.navMeshData, NavMeshAssetPath);
            }
#endif
        }

        // ------------------------------------------------------------------

        void CreateLighting()
        {
            var sun = new GameObject("Directional Light (Sun)");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.93f, 0.82f);
            light.intensity = 1.0f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            CreatePointLight("PointLight_HouseInside", new Vector3(0f, 2.6f, 14.5f), new Color(1f, 0.8f, 0.5f), 2.5f, 9f);
            CreatePointLight("PointLight_HouseDoor", new Vector3(0f, 3.2f, 9.5f), new Color(1f, 0.7f, 0.4f), 1.8f, 8f);
            CreatePointLight("PointLight_YardA", new Vector3(-16f, 3f, 2f), new Color(0.5f, 0.7f, 1f), 2f, 12f);
            CreatePointLight("PointLight_YardB", new Vector3(16f, 3f, 0f), new Color(0.6f, 1f, 0.6f), 2f, 12f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.33f, 0.36f, 0.42f);
        }

        static void CreatePointLight(string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
        }

        void CreateGround(out GameObject ground)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(6f, 1f, 6f); // 60 x 60 m
            ground.GetComponent<MeshRenderer>().sharedMaterial = Mat("M_Ground", new Color(0.28f, 0.38f, 0.26f));

            var wallMat = Mat("M_BoundaryWall", new Color(0.45f, 0.45f, 0.5f));
            CreateWall("Wall_N", new Vector3(0f, 1.5f, 30f), new Vector3(60f, 3f, 1f), wallMat);
            CreateWall("Wall_S", new Vector3(0f, 1.5f, -30f), new Vector3(60f, 3f, 1f), wallMat);
            CreateWall("Wall_E", new Vector3(30f, 1.5f, 0f), new Vector3(1f, 3f, 60f), wallMat);
            CreateWall("Wall_W", new Vector3(-30f, 1.5f, 0f), new Vector3(1f, 3f, 60f), wallMat);

            // A few crates for cover.
            var crate = Mat("M_Crate", new Color(0.55f, 0.38f, 0.2f));
            CreateWall("Cover_1", new Vector3(-8f, 0.6f, -2f), new Vector3(1.2f, 1.2f, 1.2f), crate);
            CreateWall("Cover_2", new Vector3(8f, 0.6f, -3f), new Vector3(2.5f, 1.2f, 1.2f), crate);
            CreateWall("Cover_3", new Vector3(-5f, 0.6f, 5f), new Vector3(1.2f, 1.2f, 2.5f), crate);
            CreateWall("Cover_4", new Vector3(6f, 0.6f, 6f), new Vector3(1.2f, 1.2f, 1.2f), crate);
            CreateWall("Cover_5", new Vector3(-20f, 0.6f, -6f), new Vector3(2f, 1.2f, 1.2f), crate);
            CreateWall("Cover_6", new Vector3(20f, 0.6f, 8f), new Vector3(1.2f, 1.2f, 2f), crate);
        }

        void CreateWall(string name, Vector3 pos, Vector3 scale, Material mat)
        {
            Prim(PrimitiveType.Cube, name, null, pos, scale, mat, keepCollider: true);
        }

        void CreateHouse(out Vector3 interiorBoxPos)
        {
            // House footprint x=-4..4, z=11..18, single door gap (2 m) on the south wall.
            var house = new GameObject("House");
            float y = 1.5f, wallH = 3f, thick = 0.4f;
            var wallMat = Mat("M_HouseWall", new Color(0.78f, 0.7f, 0.58f));

            Prim(PrimitiveType.Cube, "House_Wall_N", house.transform, new Vector3(0f, y, 18f), new Vector3(8.4f, wallH, thick), wallMat, true);
            Prim(PrimitiveType.Cube, "House_Wall_E", house.transform, new Vector3(4f, y, 14.5f), new Vector3(thick, wallH, 7f), wallMat, true);
            Prim(PrimitiveType.Cube, "House_Wall_W", house.transform, new Vector3(-4f, y, 14.5f), new Vector3(thick, wallH, 7f), wallMat, true);
            Prim(PrimitiveType.Cube, "House_Wall_S_L", house.transform, new Vector3(-2.5f, y, 11f), new Vector3(3.4f, wallH, thick), wallMat, true);
            Prim(PrimitiveType.Cube, "House_Wall_S_R", house.transform, new Vector3(2.5f, y, 11f), new Vector3(3.4f, wallH, thick), wallMat, true);
            // Lintel above the door
            Prim(PrimitiveType.Cube, "House_DoorLintel", house.transform, new Vector3(0f, 2.75f, 11f), new Vector3(1.6f, 0.5f, thick), wallMat, true);

            var roof = Prim(PrimitiveType.Cube, "House_Roof", house.transform, new Vector3(0f, 3.15f, 14.5f), new Vector3(8.8f, 0.3f, 7.8f),
                Mat("M_Roof", new Color(0.4f, 0.18f, 0.12f)), true);
            IgnoreForNavMesh(roof);

            // Door frame posts (visual) + threshold marker
            var doorMat = Mat("M_Door", new Color(0.45f, 0.28f, 0.12f));
            Prim(PrimitiveType.Cube, "House_DoorPost_L", house.transform, new Vector3(-0.85f, 1.25f, 10.8f), new Vector3(0.15f, 2.5f, 0.15f), doorMat, false);
            Prim(PrimitiveType.Cube, "House_DoorPost_R", house.transform, new Vector3(0.85f, 1.25f, 10.8f), new Vector3(0.15f, 2.5f, 0.15f), doorMat, false);
            Prim(PrimitiveType.Cube, "House_Doorstep", house.transform, new Vector3(0f, 0.02f, 10.6f), new Vector3(1.6f, 0.04f, 0.8f),
                Mat("M_Doorstep", new Color(0.9f, 0.75f, 0.2f)), false);

            interiorBoxPos = new Vector3(0f, 0.6f, 16.5f);
        }

        void CreateGameSystems()
        {
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        void CreatePlayer(Vector3 pos)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = pos;
            IgnoreForNavMesh(player);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var camGo = new GameObject("LookCamera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 70f;
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            // Weapons are attached to the player from the start but hidden until picked up.
            var gunMat = Mat("M_Gun", new Color(0.15f, 0.15f, 0.17f));
            var gunVis = new GameObject("GunVisual");
            gunVis.transform.SetParent(camGo.transform, false);
            gunVis.transform.localPosition = new Vector3(0.25f, -0.22f, 0.45f);
            Prim(PrimitiveType.Cube, "Slide", gunVis.transform, new Vector3(0f, 0f, 0.05f), new Vector3(0.07f, 0.09f, 0.32f), gunMat, false);
            Prim(PrimitiveType.Cube, "Grip", gunVis.transform, new Vector3(0f, -0.1f, -0.06f), new Vector3(0.06f, 0.16f, 0.08f), gunMat, false);
            Prim(PrimitiveType.Cylinder, "Barrel", gunVis.transform, new Vector3(0f, 0.01f, 0.24f), new Vector3(0.04f, 0.06f, 0.04f), gunMat, false)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            gunVis.SetActive(false);

            var grenVis = new GameObject("GrenadeVisual");
            grenVis.transform.SetParent(camGo.transform, false);
            grenVis.transform.localPosition = new Vector3(-0.28f, -0.25f, 0.45f);
            Prim(PrimitiveType.Sphere, "Body", grenVis.transform, Vector3.zero, Vector3.one * 0.11f, Mat("M_Grenade", new Color(0.2f, 0.35f, 0.15f)), false);
            Prim(PrimitiveType.Cube, "Pin", grenVis.transform, new Vector3(0f, 0.065f, 0f), new Vector3(0.03f, 0.03f, 0.03f), gunMat, false);
            grenVis.SetActive(false);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(camGo.transform, false);
            muzzle.transform.localPosition = new Vector3(0.25f, -0.2f, 0.75f);

            player.AddComponent<PlayerInputReader>();
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerHealth>();
            var weapons = player.AddComponent<WeaponManager>();
            weapons.SetVisuals(gunVis, grenVis, muzzle.transform);
            weapons.SetEffectMaterials(_tracerMat, _explosionMat, Mat("M_Grenade", Color.green));
        }

        void CreateHud()
        {
            var canvasGo = new GameObject("HUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Text MakeLabel(string name, Vector2 anchor, Vector2 pos, string initial, TextAnchor align, int size = 28)
            {
                var go = new GameObject(name);
                go.transform.SetParent(canvasGo.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = anchor;
                rt.anchorMax = anchor;
                rt.pivot = anchor;
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(900f, 40f);
                var text = go.AddComponent<Text>();
                text.font = font;
                text.fontSize = size;
                text.color = Color.white;
                text.text = initial;
                text.alignment = align;
                var outline = go.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(2f, -2f);
                return text;
            }

            var tl = new Vector2(0f, 1f);
            var lives = MakeLabel("Lives", tl, new Vector2(24, -20), "Lives: 3", TextAnchor.UpperLeft);
            var weapons = MakeLabel("Weapons", tl, new Vector2(24, -62), "Weapons: None", TextAnchor.UpperLeft);
            var bullets = MakeLabel("Bullets", tl, new Vector2(24, -104), "Bullets: 0   Grenades: 0", TextAnchor.UpperLeft);
            var kills = MakeLabel("Kills", tl, new Vector2(24, -146), "Kills: 0", TextAnchor.UpperLeft);
            var treasure = MakeLabel("Treasure", tl, new Vector2(24, -188), "Treasures: 0/3", TextAnchor.UpperLeft);
            var status = MakeLabel("Status", new Vector2(0.5f, 1f), new Vector2(0, -20), "Find a weapon. Steal the 3 treasure boxes.", TextAnchor.UpperCenter, 26);
            MakeLabel("Help", new Vector2(0f, 0f), new Vector2(24, 20),
                "WASD/Arrows move · Shift run · Space jump · Mouse look · LMB fire · Q/E switch weapon", TextAnchor.LowerLeft, 20);

            // Crosshair
            var cross = MakeLabel("Crosshair", new Vector2(0.5f, 0.5f), Vector2.zero, "+", TextAnchor.MiddleCenter, 40);
            cross.rectTransform.sizeDelta = new Vector2(60f, 60f);

            // Red flash when the player is hit
            var flashGo = new GameObject("DamageFlash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            var flashRt = flashGo.AddComponent<RectTransform>();
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.offsetMin = Vector2.zero;
            flashRt.offsetMax = Vector2.zero;
            var flash = flashGo.AddComponent<Image>();
            flash.color = new Color(1f, 0f, 0f, 0f);
            flash.raycastTarget = false;

            var end = new GameObject("EndPanel");
            end.transform.SetParent(canvasGo.transform, false);
            var endRt = end.AddComponent<RectTransform>();
            endRt.anchorMin = Vector2.zero;
            endRt.anchorMax = Vector2.one;
            endRt.offsetMin = Vector2.zero;
            endRt.offsetMax = Vector2.zero;
            var img = end.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.7f);

            var endMsgGo = new GameObject("EndMessage");
            endMsgGo.transform.SetParent(end.transform, false);
            var endMsgRt = endMsgGo.AddComponent<RectTransform>();
            endMsgRt.anchorMin = new Vector2(0.5f, 0.5f);
            endMsgRt.anchorMax = new Vector2(0.5f, 0.5f);
            endMsgRt.sizeDelta = new Vector2(1400f, 300f);
            var endMsg = endMsgGo.AddComponent<Text>();
            endMsg.font = font;
            endMsg.fontSize = 48;
            endMsg.alignment = TextAnchor.MiddleCenter;
            endMsg.color = Color.white;
            end.SetActive(false);

            var hud = canvasGo.AddComponent<GameHUD>();
            hud.Bind(lives, weapons, bullets, kills, treasure, end, endMsg);
            hud.BindExtras(status, flash);

            // EventSystem is not needed (no clickable UI), keyboard R restarts.
        }

        void CreateAudio()
        {
            var go = new GameObject("AudioManager");
            var am = go.AddComponent<AudioManager>();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                AudioClip L(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Clips/{n}.wav");
                am.SetClips(L("sfx_gun"), L("sfx_grenade_throw"), L("sfx_explosion"), L("sfx_hit_guard"),
                    L("sfx_player_hit"), L("sfx_treasure"), L("bgm_loop"));
                am.SetFootsteps(L("sfx_step"));
                return;
            }
#endif
            am.SetClips(
                ProceduralClipFactory.Beep("gun", 880f, 0.08f, 0.5f),
                ProceduralClipFactory.Beep("throw", 420f, 0.12f, 0.4f),
                ProceduralClipFactory.NoiseBurst("explode", 0.35f, 0.55f),
                ProceduralClipFactory.Beep("hitGuard", 660f, 0.1f, 0.45f),
                ProceduralClipFactory.Beep("playerHit", 180f, 0.2f, 0.55f),
                ProceduralClipFactory.Beep("treasure", 990f, 0.25f, 0.5f),
                ProceduralClipFactory.Beep("bgm", 220f, 1.5f, 0.15f));
        }

        readonly struct GuardSpec
        {
            public readonly string Name;
            public readonly Vector3 Pos;
            public readonly bool HasPatrolOverride;
            public readonly Vector3 PatrolCenter;
            public readonly float PatrolRadius;

            public GuardSpec(string name, Vector3 pos)
            {
                Name = name; Pos = pos; HasPatrolOverride = false; PatrolCenter = default; PatrolRadius = 0f;
            }

            public GuardSpec(string name, Vector3 pos, Vector3 patrolCenter, float patrolRadius)
            {
                Name = name; Pos = pos; HasPatrolOverride = true; PatrolCenter = patrolCenter; PatrolRadius = patrolRadius;
            }
        }

        void CreateTreasureSite(string siteName, Vector3 boxPos, float zoneRadius, GuardSpec[] guards)
        {
            var site = new GameObject(siteName);
            site.transform.position = new Vector3(boxPos.x, 0f, boxPos.z);

            // Invisible protection zone (trigger sphere). A faint floor ring is drawn only as a visual aid.
            var zoneGo = new GameObject("ProtectionZone");
            zoneGo.transform.SetParent(site.transform, false);
            zoneGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            var zoneCol = zoneGo.AddComponent<SphereCollider>();
            zoneCol.isTrigger = true;
            zoneCol.radius = zoneRadius;
            var zone = zoneGo.AddComponent<ProtectionZone>();
            IgnoreForNavMesh(zoneGo);

            var ring = Prim(PrimitiveType.Cylinder, "ZoneRingVisual", site.transform, new Vector3(0f, 0.01f, 0f),
                new Vector3(zoneRadius * 2f, 0.005f, zoneRadius * 2f), Mat("M_ZoneRing", new Color(0.42f, 0.3f, 0.26f)), false);
            IgnoreForNavMesh(ring);

            var guardAIs = new GuardAI[guards.Length];
            for (int i = 0; i < guards.Length; i++)
            {
                var g = guards[i];
                guardAIs[i] = CreateGuard(g.Name, g.Pos, zone);
                if (g.HasPatrolOverride)
                    guardAIs[i].SetPatrolArea(g.PatrolCenter, g.PatrolRadius);
            }
            zone.SetGuards(guardAIs);

            var box = new GameObject("TreasureBox");
            box.transform.SetParent(site.transform, false);
            box.transform.position = boxPos;
            var boxMat = Mat("M_Treasure", new Color(0.95f, 0.75f, 0.15f), emissive: true);
            var trimMat = Mat("M_TreasureTrim", new Color(0.35f, 0.2f, 0.08f));
            Prim(PrimitiveType.Cube, "Chest", box.transform, Vector3.zero, new Vector3(0.9f, 0.6f, 0.6f), boxMat, false);
            Prim(PrimitiveType.Cube, "Lid", box.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.95f, 0.12f, 0.65f), trimMat, false);
            var boxCol = box.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(1.4f, 1.6f, 1.2f);
            var treasure = box.AddComponent<TreasureBox>();
            treasure.Configure(zone, guardAIs);
            IgnoreForNavMesh(box);
        }

        GuardAI CreateGuard(string name, Vector3 pos, ProtectionZone zone)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            IgnoreForNavMesh(go);

            var mat = Mat("M_Guard", new Color(0.75f, 0.15f, 0.15f));
            Prim(PrimitiveType.Capsule, "Mesh", go.transform, new Vector3(0f, 1f, 0f), Vector3.one, mat, false);
            Prim(PrimitiveType.Cube, "Visor", go.transform, new Vector3(0f, 1.55f, 0.35f), new Vector3(0.6f, 0.15f, 0.2f),
                Mat("M_Visor", new Color(0.1f, 0.1f, 0.1f)), false);
            Prim(PrimitiveType.Cube, "Rifle", go.transform, new Vector3(0.35f, 1.2f, 0.45f), new Vector3(0.08f, 0.1f, 0.6f),
                Mat("M_Gun", Color.black), false);

            var col = go.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.45f;
            col.center = new Vector3(0f, 1f, 0f);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 3.2f;
            agent.angularSpeed = 540f;
            agent.acceleration = 10f;
            agent.stoppingDistance = 0.5f;
            agent.height = 2f;
            agent.radius = 0.45f;
            agent.enabled = false; // GuardAI.Start enables it once the NavMesh data is loaded

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0.35f, 1.25f, 0.8f);

            var ai = go.AddComponent<GuardAI>();
            ai.BindZone(zone);
            ai.SetMuzzle(muzzle.transform, _tracerMat);
            go.AddComponent<GuardHealth>();
            return ai;
        }

        void CreateWeaponPickup(PickupKind kind, Vector3 pos, int amount)
        {
            var go = new GameObject($"Pickup_{kind}");
            go.transform.position = pos;
            IgnoreForNavMesh(go);
            switch (kind)
            {
                case PickupKind.Gun:
                {
                    var m = Mat("M_Gun", Color.black);
                    Prim(PrimitiveType.Cube, "Slide", go.transform, Vector3.zero, new Vector3(0.6f, 0.18f, 0.14f), m, false);
                    Prim(PrimitiveType.Cube, "Grip", go.transform, new Vector3(-0.18f, -0.18f, 0f), new Vector3(0.12f, 0.3f, 0.12f), m, false);
                    break;
                }
                case PickupKind.Ammo:
                    Prim(PrimitiveType.Cube, "AmmoBox", go.transform, Vector3.zero, new Vector3(0.45f, 0.3f, 0.3f), Mat("M_Ammo", new Color(0.95f, 0.6f, 0.1f), true), false);
                    break;
                default:
                    Prim(PrimitiveType.Sphere, "Grenade", go.transform, Vector3.zero, Vector3.one * 0.35f, Mat("M_GrenadePickup", new Color(0.2f, 0.7f, 0.2f), true), false);
                    break;
            }

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.8f;
            var pickup = go.AddComponent<WeaponPickup>();
            pickup.Configure(kind, amount);
        }

        void CreateHealthKit(Vector3 pos)
        {
            var go = new GameObject("HealthKit");
            go.transform.position = pos;
            IgnoreForNavMesh(go);
            Prim(PrimitiveType.Cube, "Box", go.transform, Vector3.zero, new Vector3(0.5f, 0.35f, 0.5f), Mat("M_HealthKit", new Color(0.95f, 0.95f, 0.95f)), false);
            var red = Mat("M_HealthCross", new Color(0.9f, 0.1f, 0.15f), true);
            Prim(PrimitiveType.Cube, "CrossA", go.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.36f, 0.02f, 0.1f), red, false);
            Prim(PrimitiveType.Cube, "CrossB", go.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.1f, 0.02f, 0.36f), red, false);
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.8f;
            go.AddComponent<HealthKit>();
        }
    }
}
