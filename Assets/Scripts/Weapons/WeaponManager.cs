using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cmis3150.Fps.Player;
using Cmis3150.Fps.Core;

namespace Cmis3150.Fps.Weapons
{
    /// <summary>
    /// Lives on the Player. Starts with no weapons; WeaponPickup calls CollectGun / CollectGrenade / CollectAmmo.
    /// Q/E cycles owned weapons; Left mouse fires the active one.
    /// The gun and grenade meshes are attached to the camera from the start but hidden until picked up.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        [Header("Visuals (child objects of LookCamera)")]
        [SerializeField] GameObject gunVisual;
        [SerializeField] GameObject grenadeVisual;

        [Header("Combat")]
        [SerializeField] Transform muzzle;
        [SerializeField] LayerMask gunHitMask = ~0;
        [SerializeField] float gunRange = 60f;
        [SerializeField] int gunDamage = 1;          // guard has 3 HP → 3 gun hits
        [SerializeField] float grenadeThrowForce = 14f;
        [SerializeField] GameObject grenadePrefab;

        [Header("Ammo")]
        [SerializeField] int startingGunAmmoOnPickup = 6; // the gun holds six bullets

        [Header("Effects")]
        [SerializeField] Material tracerMaterial;
        [SerializeField] Material explosionMaterial;
        [SerializeField] Material grenadeMaterial;

        PlayerInputReader _input;
        PlayerController _player;
        LineRenderer _tracer;
        readonly List<WeaponType> _owned = new();
        int _index = -1;
        int _bullets;
        int _grenades;

        public int Bullets => _bullets;
        public int Grenades => _grenades;
        public WeaponType Current => _index >= 0 && _index < _owned.Count ? _owned[_index] : WeaponType.None;

        void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            _player = GetComponent<PlayerController>();
            if (gunVisual != null) gunVisual.SetActive(false);
            if (grenadeVisual != null) grenadeVisual.SetActive(false);

            if (tracerMaterial != null)
            {
                var t = new GameObject("GunTracer");
                _tracer = t.AddComponent<LineRenderer>();
                _tracer.sharedMaterial = tracerMaterial;
                _tracer.positionCount = 2;
                _tracer.startWidth = 0.03f;
                _tracer.endWidth = 0.01f;
                _tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _tracer.enabled = false;
            }
        }

        void Start() => RefreshHud();

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            if (_input.NextWeaponPressed) Cycle(+1);
            if (_input.PrevWeaponPressed) Cycle(-1);
            if (_input.FirePressed) Fire();
        }

        // ---------- pickups ----------

        public void CollectGun()
        {
            if (!_owned.Contains(WeaponType.Gun))
                _owned.Add(WeaponType.Gun);
            _bullets += startingGunAmmoOnPickup;
            Select(WeaponType.Gun);
            GameEvents.RaiseStatus("Picked up the GUN (6 bullets).");
        }

        public void CollectAmmo(int amount)
        {
            _bullets += amount;
            RefreshHud();
            GameEvents.RaiseStatus($"+{amount} bullets");
        }

        public void CollectGrenade(int amount = 1)
        {
            bool first = !_owned.Contains(WeaponType.Grenade);
            if (first)
                _owned.Add(WeaponType.Grenade);
            _grenades += amount;
            if (first || Current == WeaponType.None) Select(WeaponType.Grenade);
            else { ShowActiveVisual(); RefreshHud(); }
            GameEvents.RaiseStatus($"+{amount} grenade");
        }

        void Select(WeaponType type)
        {
            int i = _owned.IndexOf(type);
            if (i >= 0) _index = i;
            ShowActiveVisual();
            RefreshHud();
        }

        void Cycle(int dir)
        {
            if (_owned.Count == 0) return;
            _index = (_index + dir + _owned.Count) % _owned.Count;
            ShowActiveVisual();
            RefreshHud();
        }

        // Only the weapon currently held is shown in the player's hand.
        void ShowActiveVisual()
        {
            if (gunVisual != null) gunVisual.SetActive(Current == WeaponType.Gun);
            if (grenadeVisual != null) grenadeVisual.SetActive(Current == WeaponType.Grenade && _grenades > 0);
        }

        // ---------- firing ----------

        void Fire()
        {
            if (Current == WeaponType.Gun) FireGun();
            else if (Current == WeaponType.Grenade) ThrowGrenade();
        }

        Transform AimTransform => _player != null && _player.LookCamera != null ? _player.LookCamera : transform;

        void FireGun()
        {
            if (_bullets <= 0)
            {
                GameEvents.RaiseStatus("Out of bullets! Find ammo boxes (orange).");
                return;
            }
            _bullets--;
            GameEvents.RaiseGunFired();
            RefreshHud();

            // Hitscan from the camera centre (crosshair). Tracer is drawn from the gun muzzle.
            Transform aim = AimTransform;
            Vector3 end = aim.position + aim.forward * gunRange;
            if (Physics.Raycast(aim.position, aim.forward, out RaycastHit hit, gunRange, gunHitMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var guard = hit.collider.GetComponentInParent<AI.GuardHealth>();
                if (guard != null)
                    guard.TakeDamage(gunDamage, fromGrenade: false);
            }

            if (_tracer != null)
            {
                StopAllCoroutines();
                StartCoroutine(TracerFlash(muzzle != null ? muzzle.position : aim.position, end));
            }
        }

        IEnumerator TracerFlash(Vector3 a, Vector3 b)
        {
            _tracer.SetPosition(0, a);
            _tracer.SetPosition(1, b);
            _tracer.enabled = true;
            yield return new WaitForSeconds(0.05f);
            _tracer.enabled = false;
        }

        void ThrowGrenade()
        {
            if (_grenades <= 0)
            {
                GameEvents.RaiseStatus("No grenades left.");
                return;
            }
            _grenades--;
            GameEvents.RaiseGrenadeThrown();
            RefreshHud();

            Transform origin = AimTransform;
            GameObject go;
            if (grenadePrefab != null)
                go = Instantiate(grenadePrefab, origin.position + origin.forward * 0.8f, Quaternion.identity);
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "GrenadeProjectile";
                go.transform.position = origin.position + origin.forward * 0.8f;
                go.transform.localScale = Vector3.one * 0.22f;
                if (grenadeMaterial != null) go.GetComponent<MeshRenderer>().sharedMaterial = grenadeMaterial;
            }

            // Never collide with the thrower.
            var gcol = go.GetComponent<Collider>();
            var cc = GetComponent<CharacterController>();
            if (gcol != null && cc != null) Physics.IgnoreCollision(gcol, cc);

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.AddForce(origin.forward * grenadeThrowForce + Vector3.up * 3f, ForceMode.VelocityChange);

            var proj = go.GetComponent<GrenadeProjectile>();
            if (proj == null) proj = go.AddComponent<GrenadeProjectile>();
            proj.SetExplosionMaterial(explosionMaterial);
            proj.Arm();
            ShowActiveVisual();
        }

        void RefreshHud()
        {
            GameEvents.RaiseAmmo(_bullets, _grenades);
            string summary = _owned.Count == 0
                ? "None"
                : string.Join(", ", _owned) + $"   (holding: {Current})";
            GameEvents.RaiseWeapons(summary);
        }

        // ---------- used by the automated test (AutoTest.cs) ----------
        public void TestSelect(WeaponType t) => Select(t);
        public void TestFire() => Fire();

        // ---------- setup (called by LevelBootstrap in the Editor) ----------

        public void SetVisuals(GameObject gun, GameObject grenade, Transform muzzlePoint)
        {
            gunVisual = gun;
            grenadeVisual = grenade;
            muzzle = muzzlePoint;
        }

        public void SetEffectMaterials(Material tracer, Material explosion, Material grenade)
        {
            tracerMaterial = tracer;
            explosionMaterial = explosion;
            grenadeMaterial = grenade;
        }
    }
}
