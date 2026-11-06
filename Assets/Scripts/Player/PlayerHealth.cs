using UnityEngine;
using Cmis3150.Fps.Core;

namespace Cmis3150.Fps.Player
{
    /// <summary>
    /// Thin wrapper so enemy bullets / explosions can FindObjectOfType or GetComponent PlayerHealth.
    /// Lives are owned by GameManager (rubric: 3 lives).
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] float invulnSeconds = 0.6f;
        float _invulnUntil;

        public void TakeDamage(int amount = 1)
        {
            if (Time.time < _invulnUntil) return;
            _invulnUntil = Time.time + invulnSeconds;
            if (GameManager.Instance != null)
                GameManager.Instance.DamagePlayer(amount);
        }
    }
}
