using UnityEngine;
using Cmis3150.Fps.Core;
using Cmis3150.Fps.AI;
using Cmis3150.Fps.Player;

namespace Cmis3150.Fps.Items
{
    /// <summary>
    /// Collectible treasure. On pickup: notify GameManager, tell zone guards to PursueStolen.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TreasureBox : MonoBehaviour
    {
        [SerializeField] ProtectionZone zone;
        [SerializeField] GuardAI[] guards;
        [SerializeField] float spin = 40f;

        bool _taken;

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void Update()
        {
            transform.Rotate(0f, spin * Time.deltaTime, 0f);
        }

        public void Configure(ProtectionZone z, params GuardAI[] g)
        {
            zone = z;
            guards = g;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_taken) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;

            _taken = true;
            var player = other.GetComponentInParent<Transform>();

            if (guards != null)
            {
                foreach (var g in guards)
                    g?.NotifyTreasureStolen(player);
            }

            if (GameManager.Instance != null)
                GameManager.Instance.NotifyTreasureCollected();

            Destroy(gameObject);
        }
    }
}
