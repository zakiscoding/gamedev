using UnityEngine;
using Cmis3150.Fps.Core;
using Cmis3150.Fps.AI;
using Cmis3150.Fps.Player;

namespace Cmis3150.Fps.Weapons
{
    /// <summary>
    /// Thrown grenade: explodes shortly after it first hits something (or when the fuse runs out).
    /// One explosion kills any guard in the radius (damage 99 &gt; guard HP 3).
    /// </summary>
    public class GrenadeProjectile : MonoBehaviour
    {
        [SerializeField] float fuseSeconds = 2.5f;
        [SerializeField] float explodeRadius = 3.5f;
        [SerializeField] int damageToGuards = 99;
        [SerializeField] int damageToPlayer = 1;
        [SerializeField] bool harmPlayer = false;

        bool _armed;
        bool _exploded;
        Material _explosionMat;

        public void SetExplosionMaterial(Material m) => _explosionMat = m;

        public void Arm()
        {
            _armed = true;
            Invoke(nameof(Explode), fuseSeconds);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!_armed || _exploded) return;
            // Direct hit on a guard → explode immediately, otherwise after a short bounce.
            CancelInvoke(nameof(Explode));
            bool hitGuard = collision.collider.GetComponentInParent<GuardHealth>() != null;
            Invoke(nameof(Explode), hitGuard ? 0f : 0.4f);
        }

        void Explode()
        {
            if (_exploded) return;
            _exploded = true;
            GameEvents.RaiseGrenadeExploded();
            SpawnExplosionVisual();

            var hits = Physics.OverlapSphere(transform.position, explodeRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var col in hits)
            {
                var guard = col.GetComponentInParent<GuardHealth>();
                if (guard != null)
                {
                    guard.TakeDamage(damageToGuards, fromGrenade: true);
                    continue;
                }

                if (harmPlayer)
                {
                    var player = col.GetComponentInParent<PlayerHealth>();
                    player?.TakeDamage(damageToPlayer);
                }
            }

            Destroy(gameObject);
        }

        void SpawnExplosionVisual()
        {
            var fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.name = "ExplosionFX";
            Destroy(fx.GetComponent<Collider>());
            fx.transform.position = transform.position;
            if (_explosionMat != null) fx.GetComponent<MeshRenderer>().sharedMaterial = _explosionMat;
            var l = fx.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.6f, 0.2f);
            l.range = explodeRadius * 3f;
            l.intensity = 6f;
            fx.AddComponent<ExplosionFx>().Init(explodeRadius * 2f, 0.35f);
        }
    }
}
