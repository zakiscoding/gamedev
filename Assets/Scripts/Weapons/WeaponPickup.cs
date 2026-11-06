using UnityEngine;

namespace Cmis3150.Fps.Weapons
{
    public enum PickupKind
    {
        Gun,
        Grenade,
        Ammo
    }

    /// <summary>
    /// Walk-over pickup. Place in world; player CharacterController trigger or OnTriggerEnter collects.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponPickup : MonoBehaviour
    {
        [SerializeField] PickupKind kind = PickupKind.Gun;
        [SerializeField] int amount = 1;
        [SerializeField] float spinSpeed = 90f;

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
        }

        void OnTriggerEnter(Collider other)
        {
            var weapons = other.GetComponentInParent<WeaponManager>();
            if (weapons == null) return;

            switch (kind)
            {
                case PickupKind.Gun:
                    weapons.CollectGun();
                    break;
                case PickupKind.Grenade:
                    weapons.CollectGrenade(amount);
                    break;
                case PickupKind.Ammo:
                    weapons.CollectAmmo(amount);
                    break;
            }

            Destroy(gameObject);
        }

        public void Configure(PickupKind k, int amt)
        {
            kind = k;
            amount = amt;
        }
    }
}
