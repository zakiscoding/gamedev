using System.Collections.Generic;
using UnityEngine;
using Cmis3150.Fps.Player;
using Cmis3150.Fps.Core;

namespace Cmis3150.Fps.Weapons
{
    /// <summary>
    /// Starts with no weapons. Pickups call CollectGun / CollectGrenade.
    /// Q/E cycles owned weapons; LMB fires the active one.
    /// Child meshes (gunVisual / grenadeVisual) start disabled and enable on pickup.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        [Header("Visuals (optional child meshes — drop Asset Store models here)")]
        [SerializeField] GameObject gunVisual;
        [SerializeField] GameObject grenadeVisual;

        [Header("Combat")]
        [SerializeField] Transform muzzle;
        [SerializeField] LayerMask gunHitMask = ~0;
        [SerializeField] float gunRange = 60f;
        [SerializeField] int gunDamage = 1;          // 3 hits to kill (GuardHealth max=3)
        [SerializeField] float grenadeThrowForce = 12f;
        [SerializeField] GameObject grenadePrefab;

        [Header("Ammo")]
        [SerializeField] int startingGunAmmoOnPickup = 6;

        PlayerInputReader _input;
        PlayerController _player;
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
            RefreshHud();
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            if (_input.NextWeaponPressed) Cycle(+1);
            if (_input.PrevWeaponPressed) Cycle(-1);
            if (_input.FirePressed) Fire();
        }

        public void CollectGun()
        {
            if (!_owned.Contains(WeaponType.Gun))
            {
                _owned.Add(WeaponType.Gun);
                if (gunVisual != null) gunVisual.SetActive(true);
            }

            _bullets += startingGunAmmoOnPickup;
            EnsureSelected(WeaponType.Gun);
            RefreshHud();
        }

        public void CollectAmmo(int amount)
        {
            if (!_owned.Contains(WeaponType.Gun))
            {
                // Ammo alone does not grant the gun — still useful if gun already owned.
            }

            _bullets += amount;
            RefreshHud();
        }

        public void CollectGrenade(int amount = 1)
        {
            if (!_owned.Contains(WeaponType.Grenade))
            {
                _owned.Add(WeaponType.Grenade);
                if (grenadeVisual != null) grenadeVisual.SetActive(true);
            }

            _grenades += amount;
            EnsureSelected(WeaponType.Grenade);
            RefreshHud();
        }

        void EnsureSelected(WeaponType type)
        {
            int i = _owned.IndexOf(type);
            if (i >= 0) _index = i;
            else if (_owned.Count > 0 && _index < 0) _index = 0;
            RefreshHud();
        }

        void Cycle(int dir)
        {
            if (_owned.Count == 0) return;
            _index = (_index + dir + _owned.Count) % _owned.Count;
            RefreshHud();
        }

        void Fire()
        {
            if (Current == WeaponType.Gun)
                FireGun();
            else if (Current == WeaponType.Grenade)
                ThrowGrenade();
        }

        void FireGun()
        {
            if (_bullets <= 0) return;
            _bullets--;
            GameEvents.RaiseGunFired();
            RefreshHud();

            Transform origin = muzzle != null
                ? muzzle
                : (_player != null && _player.LookCamera != null ? _player.LookCamera : transform);

            if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, gunRange, gunHitMask))
            {
                var guard = hit.collider.GetComponentInParent<AI.GuardHealth>();
                if (guard != null)
                    guard.TakeDamage(gunDamage, fromGrenade: false);
            }
        }

        void ThrowGrenade()
        {
            if (_grenades <= 0) return;
            _grenades--;
            GameEvents.RaiseGrenadeThrown();
            RefreshHud();

            Transform origin = muzzle != null
                ? muzzle
                : (_player != null && _player.LookCamera != null ? _player.LookCamera : transform);

            GameObject go;
            if (grenadePrefab != null)
                go = Instantiate(grenadePrefab, origin.position + origin.forward * 0.6f, Quaternion.identity);
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "GrenadeProjectile";
                go.transform.position = origin.position + origin.forward * 0.6f;
                go.transform.localScale = Vector3.one * 0.25f;
                Object.Destroy(go.GetComponent<Collider>());
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = false;
                col.radius = 0.5f;
            }

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.AddForce(origin.forward * grenadeThrowForce + Vector3.up * 2f, ForceMode.VelocityChange);

            var proj = go.GetComponent<GrenadeProjectile>();
            if (proj == null) proj = go.AddComponent<GrenadeProjectile>();
            proj.Arm();
        }

        void RefreshHud()
        {
            GameEvents.RaiseBullets(Current == WeaponType.Gun ? _bullets : _grenades);
            string summary = _owned.Count == 0
                ? "None"
                : string.Join(", ", _owned) + $"  |  Active: {Current}";
            if (_owned.Contains(WeaponType.Gun))
                summary += $"  |  Ammo:{_bullets}";
            if (_owned.Contains(WeaponType.Grenade))
                summary += $"  |  Grenades:{_grenades}";
            GameEvents.RaiseWeapons(summary);
        }

        public void SetVisuals(GameObject gun, GameObject grenade, Transform muzzlePoint)
        {
            gunVisual = gun;
            grenadeVisual = grenade;
            muzzle = muzzlePoint;
            if (gunVisual != null) gunVisual.SetActive(_owned.Contains(WeaponType.Gun));
            if (grenadeVisual != null) grenadeVisual.SetActive(_owned.Contains(WeaponType.Grenade));
        }
    }
}
