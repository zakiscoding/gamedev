using UnityEngine;
using Cmis3150.Fps.Core;

namespace Cmis3150.Fps.AI
{
    /// <summary>
    /// Gun needs 3 hits (maxHealth=3, gunDamage=1). Grenade needs 1 hit (damage=99).
    /// </summary>
    public class GuardHealth : MonoBehaviour
    {
        [SerializeField] int maxHealth = 3;
        int _hp;
        GuardAI _ai;
        bool _dead;

        void Awake()
        {
            _hp = maxHealth;
            _ai = GetComponent<GuardAI>();
        }

        public void TakeDamage(int amount, bool fromGrenade)
        {
            if (_dead) return;
            _hp -= amount;
            GameEvents.RaiseGuardHit();
            if (_hp <= 0)
                Die();
        }

        void Die()
        {
            if (_dead) return;
            _dead = true;
            _ai?.NotifyDied();
            if (GameManager.Instance != null)
                GameManager.Instance.NotifyGuardDied();
            Destroy(gameObject);
        }
    }
}
