# Rubric → implementation map

Course: CMIS 3150 / CSC4820 Project 2. Each graded line points at the script and runtime object that implements it.

## Player control & collectibles (15)

| Requirement | Implementation |
|-------------|----------------|
| Walk WASD / arrows | `PlayerInputReader` + `PlayerController.Move` on `Player` |
| Run Shift | `PlayerInputReader.SprintHeld` → `runSpeed` |
| Jump Space | `PlayerController` gravity / jump on `CharacterController` |
| Look mouse | `PlayerController.Look` on yaw + `LookCamera` pitch |
| Switch Q/E | `WeaponManager.Cycle` |
| Fire LMB | `WeaponManager.Fire` |
| New Input System | `PlayerInputReader` (code actions) + `Assets/Input/PlayerControls.inputactions` |
| 3 lives | `GameManager.startingLives = 3` |
| Start with no weapons | `WeaponManager` `_owned` empty; visuals disabled |
| Pick up by walking over | `WeaponPickup.OnTriggerEnter`, `HealthKit`, `TreasureBox` |
| Gun + 6 bullets + ammo pickups | `CollectGun` adds 6; `PickupKind.Ammo` |
| Multiple grenades | Several `Pickup_Grenade` + `CollectGrenade` |
| Health kits +1 life | `HealthKit` → `GameManager.HealPlayer` |

## Shoot / grenade kill (20)

| Requirement | Implementation |
|-------------|----------------|
| Gun 3 hits to kill | `WeaponManager` gunDamage=1; `GuardHealth.maxHealth=3` |
| Grenade 1 hit | `GrenadeProjectile.damageToGuards=99` |
| Gun fire | Raycast from muzzle/camera in `FireGun` |
| Grenade throw | Rigidbody impulse + fuse/impact explode |

## Guards patrol (10)

| Requirement | Implementation |
|-------------|----------------|
| Patrol in protection zone | `GuardAI` state `Patrol` → `PickNewPatrolPoint` samples NavMesh inside `ProtectionZone.Radius` |
| NavMeshAgent | `GuardAI` requires `NavMeshAgent`; surface baked in `LevelBootstrap` |

## Chase (10)

| Requirement | Implementation |
|-------------|----------------|
| Enter zone → chase | `ProtectionZone.OnTriggerEnter` → `GuardAI.OnPlayerEnteredZone` → `Chase` |
| Leave zone → patrol | `OnTriggerExit` → `OnPlayerExitedZone` → `Patrol` (if box not stolen) |
| Steal → pursue forever | `TreasureBox` → `NotifyTreasureStolen` → `PursueStolen` |

## Shoot player (5)

| Requirement | Implementation |
|-------------|----------------|
| Guards shoot in chase/pursue | `GuardAI.TryShoot` → `PlayerHealth.TakeDamage` → lose 1 life |

## Flee (5)

| Requirement | Implementation |
|-------------|----------------|
| After all 3 boxes, flee | `GameEvents.AllTreasuresCollected` → `GuardAI.OnAllTreasuresCollected` → `Flee` |

## Sound effects (5)

| Event | Hook |
|-------|------|
| Gun | `GameEvents.GunFired` → `AudioManager.OnGun` |
| Grenade throw | `GrenadeThrown` |
| Explosion | `GrenadeExploded` |
| Hit guard | `GuardHit` |
| Player hit | `PlayerHit` |
| Treasure | `TreasurePicked` |
| BGM | `AudioManager` looping `bgm` clip (procedural placeholder) |

## UI (5)

| Field | `GameHUD` label |
|-------|-----------------|
| Lives | `livesText` |
| Weapons collected | `weaponsText` |
| Bullets / charges | `bulletsText` |
| Kills | `killsText` |

(Also shows treasure count for clarity.)

## Other features (5)

| Feature | Where |
|---------|--------|
| 3 treasure boxes | `LevelBootstrap` sites A/B/C |
| ≥1 box in house with door | Site C interior; south wall doorway gap |
| ≥1 guard outside + ≥1 inside | `Guard_C_Outside`, `Guard_C_Inside` |
| Win = 3 boxes + all guards dead | `GameManager.CheckWin` |
| Lose at 0 lives | `GameManager.Lose` |
| One level | `MainLevel.unity` |
| Lighting | Directional sun + 3 point lights in `LevelBootstrap.CreateLighting` |
| Weapon child meshes visible on pickup | `WeaponManager` gun/grenade visuals |

## Code walkthrough video (20)

Use the zone → chase → shoot → leave → patrol path documented in `README.md`, pointing at:

1. `ProtectionZone.cs` on each `ProtectionZone` object  
2. `GuardAI.cs` on each `Guard_*`  
3. `PlayerHealth.cs` / `GameManager.cs` for damage  
4. `TreasureBox.cs` for the stolen-box pursue branch  

## Deliverables checklist (submission)

- [ ] Zip of **entire Unity project** (this repo; remove Library/ before zipping if present)
- [ ] Zip of **Windows standalone** folder (build on zakar — not produced here)
- [ ] Gameplay video (win + fail)
- [ ] Narrated code walkthrough ≤15 min
- [ ] Upload to iCollege → Assignments → Project 2
