using UnityEngine;

namespace Cmis3150.Fps.AI
{
    /// <summary>
    /// Invisible circle around a treasure box. When the player enters, guards chase+shoot;
    /// when the player leaves (and the box is not stolen), guards return to patrol.
    /// Walkthrough interaction path starts here: OnTriggerEnter → GuardAI.OnPlayerEnteredZone.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class ProtectionZone : MonoBehaviour
    {
        [SerializeField] GuardAI[] guards;
        [SerializeField] string playerTag = "Player";

        public float Radius
        {
            get
            {
                var col = GetComponent<SphereCollider>();
                return col != null ? col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) : 8f;
            }
        }

        public Vector3 Center => transform.position;

        public void SetGuards(params GuardAI[] g) => guards = g;

        void Reset()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 8f;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag) && other.GetComponentInParent<Player.PlayerController>() == null)
                return;

            var player = other.GetComponentInParent<Transform>();
            foreach (var g in guards)
                g?.OnPlayerEnteredZone(player);
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag) && other.GetComponentInParent<Player.PlayerController>() == null)
                return;

            foreach (var g in guards)
                g?.OnPlayerExitedZone();
        }
    }
}
