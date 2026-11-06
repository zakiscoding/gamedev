# Where to drop Asset Store weapon models

Placeholder primitives are created at runtime by `LevelBootstrap`:

| Slot | Runtime object | Replace with |
|------|----------------|--------------|
| Gun mesh | `Player/LookCamera/GunVisual_PLACEHOLDER` | Your gun FBX/prefab as a child of `LookCamera`; assign to `WeaponManager.gunVisual` |
| Grenade mesh | `Player/LookCamera/GrenadeVisual_PLACEHOLDER` | Grenade mesh; assign to `WeaponManager.grenadeVisual` |
| Muzzle | `Player/LookCamera/Muzzle` | Keep empty transform at the barrel tip for raycasts / throws |

World pickups (`Pickup_Gun`, `Pickup_Grenade`, `Pickup_Ammo`) are also primitives — swap their MeshFilter/MeshRenderer after baking a static scene if you prefer authored prefabs over the bootstrap.

Guards and treasure boxes are primitives; recolor or replace meshes without changing scripts.
