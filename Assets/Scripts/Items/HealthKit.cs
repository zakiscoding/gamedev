using UnityEngine;
using Cmis3150.Fps.Core;
using Cmis3150.Fps.Player;

namespace Cmis3150.Fps.Items
{
    /// <summary>Walk-over health kit restores +1 life (clamped to max).</summary>
    [RequireComponent(typeof(Collider))]
    public class HealthKit : MonoBehaviour
    {
        [SerializeField] int restore = 1;
        [SerializeField] float bobSpeed = 2f;
        [SerializeField] float bobAmp = 0.25f;
        Vector3 _origin;

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void Start() => _origin = transform.position;

        void Update()
        {
            transform.position = _origin + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmp);
            transform.Rotate(0f, 60f * Time.deltaTime, 0f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            GameManager.Instance?.HealPlayer(restore);
            Destroy(gameObject);
        }
    }
}
