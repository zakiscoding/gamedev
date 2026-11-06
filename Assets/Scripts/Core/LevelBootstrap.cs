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

namespace Cmis3150.Fps.Core
{
    /// <summary>
    /// Builds a complete playable level from primitives at runtime so the project
    /// works as soon as MainLevel is opened and Play is pressed (after packages restore).
    /// NavMesh is baked via NavMeshSurface.BuildNavMesh() — no manual Bake required for play mode.
    /// For the graded Windows standalone, still open in Unity Hub, confirm packages, Build Profiles → Windows.
    /// </summary>
    public class LevelBootstrap : MonoBehaviour
    {
        [Header("Bootstrap")]
        [SerializeField] bool buildOnAwake = true;

        bool _built;
        NavMeshSurface _surface;

        void Awake()
        {
            if (buildOnAwake)
                Build();
        }

        [ContextMenu("Rebuild Level")]
        public void Build()
        {
            if (_built) return;
            _built = true;

            EnsureTagsAndLayers();
            CreateLighting();
            CreateGround(out GameObject ground);
            CreateHouse(out Vector3 houseInterior, out Vector3 houseExteriorGuard);
            CreateGameSystems();
            CreatePlayer(new Vector3(0f, 1.1f, -18f));
            CreateHud();
            CreateAudio();

            // --- Treasure A: open yard (west) ---
            CreateTreasureSite(
                siteName: "TreasureSite_A_Yard",
                boxPos: new Vector3(-12f, 0.6f, 4f),
                zoneRadius: 9f,
                guardOffsets: new[] { new Vector3(-2f, 0f, 2f) },
                guardNames: new[] { "Guard_A_Outside" });

            // --- Treasure B: open yard (east) ---
            CreateTreasureSite(
                siteName: "TreasureSite_B_Yard",
                boxPos: new Vector3(14f, 0.6f, 2f),
                zoneRadius: 9f,
                guardOffsets: new[] { new Vector3(2f, 0f, -1f), new Vector3(-1f, 0f, 3f) },
                guardNames: new[] { "Guard_B1_Outside", "Guard_B2_Outside" });

            // --- Treasure C: inside house (door entry required) ---
            CreateTreasureSite(
                siteName: "TreasureSite_C_House",
                boxPos: houseInterior,
                zoneRadius: 7f,
                guardOffsets: new[] { Vector3.zero, houseExteriorGuard - houseInterior },
                guardNames: new[] { "Guard_C_Inside", "Guard_C_Outside" });

            // Pickups
            CreateWeaponPickup(PickupKind.Gun, new Vector3(-3f, 0.5f, -10f), 1);
            CreateWeaponPickup(PickupKind.Ammo, new Vector3(3f, 0.5f, -8f), 6);
            CreateWeaponPickup(PickupKind.Ammo, new Vector3(-8f, 0.5f, 8f), 6);
            CreateWeaponPickup(PickupKind.Grenade, new Vector3(5f, 0.5f, -12f), 1);
            CreateWeaponPickup(PickupKind.Grenade, new Vector3(-6f, 0.5f, -6f), 1);
            CreateWeaponPickup(PickupKind.Grenade, new Vector3(10f, 0.5f, 10f), 1);
            CreateHealthKit(new Vector3(0f, 0.5f, -6f));
            CreateHealthKit(new Vector3(-14f, 0.5f, -4f));
            CreateHealthKit(new Vector3(8f, 0.5f, -14f));

            // Bake NavMesh before GuardAI.Start so agents are on the mesh.
            _surface = ground.AddComponent<NavMeshSurface>();
            _surface.collectObjects = CollectObjects.All;
            _surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            _surface.BuildNavMesh();
        }

        void EnsureTagsAndLayers()
        {
            // Tag "Player" must exist in TagManager; bootstrap also sets tag if present.
        }

        void CreateLighting()
        {
            // Directional "sun"
            var sun = new GameObject("Directional Light (Sun)");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Point lights near house and yard boxes
            CreatePointLight("PointLight_House", new Vector3(0f, 3.5f, 12f), new Color(1f, 0.85f, 0.6f), 2.2f, 14f);
            CreatePointLight("PointLight_YardA", new Vector3(-12f, 3f, 4f), new Color(0.7f, 0.85f, 1f), 1.6f, 12f);
            CreatePointLight("PointLight_YardB", new Vector3(14f, 3f, 2f), new Color(0.7f, 1f, 0.8f), 1.6f, 12f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.42f);
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
            ground.transform.localScale = new Vector3(6f, 1f, 6f); // 60x60
            var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            mat.color = new Color(0.28f, 0.36f, 0.28f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Simple boundary walls so player/guards don't fall off
            CreateWall("Wall_N", new Vector3(0f, 1.5f, 30f), new Vector3(60f, 3f, 1f));
            CreateWall("Wall_S", new Vector3(0f, 1.5f, -30f), new Vector3(60f, 3f, 1f));
            CreateWall("Wall_E", new Vector3(30f, 1.5f, 0f), new Vector3(1f, 3f, 60f));
            CreateWall("Wall_W", new Vector3(-30f, 1.5f, 0f), new Vector3(1f, 3f, 60f));
        }

        static void CreateWall(string name, Vector3 pos, Vector3 scale)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            w.transform.position = pos;
            w.transform.localScale = scale;
            var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            mat.color = new Color(0.45f, 0.45f, 0.5f);
            w.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        void CreateHouse(out Vector3 interiorBoxPos, out Vector3 exteriorGuardPos)
        {
            // House footprint around z=12..18, x=-4..4, door facing south (player side)
            float y = 1.5f;
            float wallH = 3f;
            float thick = 0.4f;

            CreateWall("House_Wall_N", new Vector3(0f, y, 18f), new Vector3(8f, wallH, thick));
            CreateWall("House_Wall_E", new Vector3(4f, y, 14.5f), new Vector3(thick, wallH, 7f));
            CreateWall("House_Wall_W", new Vector3(-4f, y, 14.5f), new Vector3(thick, wallH, 7f));

            // South wall with door gap in the middle
            CreateWall("House_Wall_S_L", new Vector3(-2.5f, y, 11f), new Vector3(3f, wallH, thick));
            CreateWall("House_Wall_S_R", new Vector3(2.5f, y, 11f), new Vector3(3f, wallH, thick));

            // Roof (visual only)
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "House_Roof";
            roof.transform.position = new Vector3(0f, 3.2f, 14.5f);
            roof.transform.localScale = new Vector3(8.5f, 0.3f, 7.5f);
            var roofMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            roofMat.color = new Color(0.35f, 0.2f, 0.15f);
            roof.GetComponent<MeshRenderer>().sharedMaterial = roofMat;

            // Door frame marker (visual)
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "House_DoorwayMarker";
            door.transform.position = new Vector3(0f, 0.05f, 11f);
            door.transform.localScale = new Vector3(2f, 0.1f, 1.2f);
            var doorMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            doorMat.color = new Color(0.9f, 0.75f, 0.2f);
            door.GetComponent<MeshRenderer>().sharedMaterial = doorMat;
            Object.Destroy(door.GetComponent<Collider>());

            interiorBoxPos = new Vector3(0f, 0.6f, 16f);
            exteriorGuardPos = new Vector3(0f, 0f, 9f);
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

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Capsule visual
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "PlayerBody";
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            Object.Destroy(body.GetComponent<Collider>());
            var bodyMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            bodyMat.color = new Color(0.2f, 0.5f, 0.9f);
            body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

            var camGo = new GameObject("LookCamera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();

            // Weapon visuals (placeholders — replace with Asset Store gun/grenade meshes)
            var gunVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunVis.name = "GunVisual_PLACEHOLDER";
            gunVis.transform.SetParent(camGo.transform, false);
            gunVis.transform.localPosition = new Vector3(0.25f, -0.2f, 0.5f);
            gunVis.transform.localScale = new Vector3(0.12f, 0.12f, 0.5f);
            Object.Destroy(gunVis.GetComponent<Collider>());
            gunVis.SetActive(false);

            var grenVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            grenVis.name = "GrenadeVisual_PLACEHOLDER";
            grenVis.transform.SetParent(camGo.transform, false);
            grenVis.transform.localPosition = new Vector3(0.28f, -0.28f, 0.4f);
            grenVis.transform.localScale = Vector3.one * 0.12f;
            Object.Destroy(grenVis.GetComponent<Collider>());
            grenVis.SetActive(false);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(camGo.transform, false);
            muzzle.transform.localPosition = new Vector3(0.2f, -0.15f, 0.7f);

            player.AddComponent<PlayerInputReader>();
            var controller = player.AddComponent<PlayerController>();
            // look camera assigned via SerializeField defaults — set through reflection-free path:
            // PlayerController finds child Camera in Awake.

            player.AddComponent<PlayerHealth>();
            var weapons = player.AddComponent<WeaponManager>();
            weapons.SetVisuals(gunVis, grenVis, muzzle.transform);

            // Ensure PlayerController sees the camera (already FindInChildren)
            _ = controller;
        }

        void CreateHud()
        {
            var canvasGo = new GameObject("HUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Text MakeLabel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, string initial)
            {
                var go = new GameObject(name);
                go.transform.SetParent(canvasGo.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = pivot;
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(520f, 32f);
                var text = go.AddComponent<Text>();
                text.font = font;
                text.fontSize = 20;
                text.color = Color.white;
                text.text = initial;
                text.alignment = TextAnchor.UpperLeft;
                var outline = go.AddComponent<Outline>();
                outline.effectColor = Color.black;
                return text;
            }

            var lives = MakeLabel("Lives", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), "Lives: 3");
            var weapons = MakeLabel("Weapons", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -48), "Weapons: None");
            var bullets = MakeLabel("Bullets", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -80), "Ammo / Charges: 0");
            var kills = MakeLabel("Kills", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -112), "Kills: 0");
            var treasure = MakeLabel("Treasure", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -144), "Treasures: 0/3");

            var end = new GameObject("EndPanel");
            end.transform.SetParent(canvasGo.transform, false);
            var endRt = end.AddComponent<RectTransform>();
            endRt.anchorMin = Vector2.zero;
            endRt.anchorMax = Vector2.one;
            endRt.offsetMin = Vector2.zero;
            endRt.offsetMax = Vector2.zero;
            var img = end.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.65f);
            end.SetActive(false);

            var endMsgGo = new GameObject("EndMessage");
            endMsgGo.transform.SetParent(end.transform, false);
            var endMsgRt = endMsgGo.AddComponent<RectTransform>();
            endMsgRt.anchorMin = new Vector2(0.5f, 0.5f);
            endMsgRt.anchorMax = new Vector2(0.5f, 0.5f);
            endMsgRt.sizeDelta = new Vector2(700f, 160f);
            var endMsg = endMsgGo.AddComponent<Text>();
            endMsg.font = font;
            endMsg.fontSize = 28;
            endMsg.alignment = TextAnchor.MiddleCenter;
            endMsg.color = Color.white;

            var hud = canvasGo.AddComponent<GameHUD>();
            hud.Bind(lives, weapons, bullets, kills, treasure, end, endMsg);
        }

        void CreateAudio()
        {
            var go = new GameObject("AudioManager");
            var am = go.AddComponent<AudioManager>();
            am.SetClips(
                ProceduralClipFactory.Beep("gun", 880f, 0.08f, 0.5f),
                ProceduralClipFactory.Beep("throw", 420f, 0.12f, 0.4f),
                ProceduralClipFactory.NoiseBurst("explode", 0.35f, 0.55f),
                ProceduralClipFactory.Beep("hitGuard", 660f, 0.1f, 0.45f),
                ProceduralClipFactory.Beep("playerHit", 180f, 0.2f, 0.55f),
                ProceduralClipFactory.Beep("treasure", 990f, 0.25f, 0.5f),
                ProceduralClipFactory.Beep("bgm", 220f, 1.5f, 0.15f)
            );
        }

        GameObject CreateTreasureSite(string siteName, Vector3 boxPos, float zoneRadius, Vector3[] guardOffsets, string[] guardNames)
        {
            var site = new GameObject(siteName);
            site.transform.position = boxPos;

            var zoneGo = new GameObject("ProtectionZone");
            zoneGo.transform.SetParent(site.transform, false);
            zoneGo.transform.localPosition = Vector3.zero;
            var zoneCol = zoneGo.AddComponent<SphereCollider>();
            zoneCol.isTrigger = true;
            zoneCol.radius = zoneRadius;
            var zone = zoneGo.AddComponent<ProtectionZone>();

            // Visual ring (optional, non-colliding)
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ZoneRingVisual";
            ring.transform.SetParent(zoneGo.transform, false);
            ring.transform.localScale = new Vector3(zoneRadius * 2f, 0.02f, zoneRadius * 2f);
            Object.Destroy(ring.GetComponent<Collider>());
            var ringMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            ringMat.color = new Color(1f, 0.3f, 0.3f, 0.25f);
            ring.GetComponent<MeshRenderer>().sharedMaterial = ringMat;

            var guards = new GuardAI[guardOffsets.Length];
            for (int i = 0; i < guardOffsets.Length; i++)
            {
                Vector3 gpos = boxPos + guardOffsets[i];
                gpos.y = 0f;
                guards[i] = CreateGuard(guardNames[i], gpos, zone);
            }

            zone.SetGuards(guards);

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "TreasureBox";
            box.transform.SetParent(site.transform, false);
            box.transform.position = boxPos;
            box.transform.localScale = new Vector3(0.9f, 0.7f, 0.9f);
            var boxMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            boxMat.color = new Color(0.95f, 0.75f, 0.15f);
            box.GetComponent<MeshRenderer>().sharedMaterial = boxMat;
            var boxCol = box.GetComponent<Collider>();
            boxCol.isTrigger = true;
            var treasure = box.AddComponent<TreasureBox>();
            treasure.Configure(zone, guards);

            return site;
        }

        GuardAI CreateGuard(string name, Vector3 pos, ProtectionZone zone)
        {
            var go = new GameObject(name);
            go.transform.position = pos + Vector3.up * 0.05f;

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Mesh";
            capsule.transform.SetParent(go.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
            Object.Destroy(capsule.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            mat.color = new Color(0.75f, 0.15f, 0.15f);
            capsule.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var col = go.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.4f;
            col.center = new Vector3(0f, 1f, 0f);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 3.6f;
            agent.angularSpeed = 720f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 1.5f;
            agent.height = 2f;
            agent.radius = 0.4f;

            var ai = go.AddComponent<GuardAI>();
            ai.BindZone(zone);
            go.AddComponent<GuardHealth>();

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0.2f, 1.4f, 0.4f);

            return ai;
        }

        void CreateWeaponPickup(PickupKind kind, Vector3 pos, int amount)
        {
            PrimitiveType prim = kind == PickupKind.Grenade ? PrimitiveType.Sphere : PrimitiveType.Cube;
            var go = GameObject.CreatePrimitive(prim);
            go.name = $"Pickup_{kind}";
            go.transform.position = pos;
            go.transform.localScale = kind == PickupKind.Gun
                ? new Vector3(0.6f, 0.25f, 0.25f)
                : Vector3.one * 0.35f;
            var col = go.GetComponent<Collider>();
            col.isTrigger = true;
            var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            mat.color = kind switch
            {
                PickupKind.Gun => new Color(0.2f, 0.2f, 0.2f),
                PickupKind.Ammo => new Color(0.9f, 0.6f, 0.1f),
                _ => new Color(0.1f, 0.6f, 0.2f)
            };
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var pickup = go.AddComponent<WeaponPickup>();
            pickup.Configure(kind, amount);
        }

        void CreateHealthKit(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "HealthKit";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            go.GetComponent<Collider>().isTrigger = true;
            var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            mat.color = new Color(0.9f, 0.2f, 0.3f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<HealthKit>();
        }
    }
}
