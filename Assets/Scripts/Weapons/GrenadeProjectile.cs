using UnityEngine;
using Cmis3150.Fps.Core;
using Cmis3150.Fps.AI;
using Cmis3150.Fps.Player;

namespace Cmis3150.Fps.Weapons
{
    /// <summary>
    /// Thrown grenade: explodes on impact or after fuse. One hit kills guards (damage >= max HP).
    /// </summary>
    public class GrenadeProjectile : MonoBehaviour
    {
        [SerializeField] float fuseSeconds = 2.2f;
        [SerializeField] float explodeRadius = 3.5f;
        [SerializeField] int damageToGuards = 99;
        [SerializeField] int damageToPlayer = 1;
        [SerializeField] bool harmPlayer = false; // optional friendly fire off by default

        bool _armed;
        bool _exploded;

        public void Arm()
        {
            _armed = true;
            Destroy(gameObject, fuseSeconds + 0.05f);
            Invoke(nameof(Explode), fuseSeconds);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!_armed || _exploded) return;
            // Explode shortly after first bounce for juicier feel
            CancelInvoke(nameof(Explode));
            Invoke(nameof(Explode), 0.05f);
        }

        void Explode()
        {
            if (_exploded) return;
            _exploded = true;
            GameEvents.RaiseGrenadeExploded();

            var hits = Physics.OverlapSphere(transform.position, explodeRadius);
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
    }
}
