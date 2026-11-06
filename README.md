# CMIS 3150 / CSC4820: Project 2 (3D first-person shooter)

Unity **6000.6.2f1** · C# · New Input System · AI Navigation (NavMesh Surface + NavMeshAgent) · uGUI.

Steal three treasure boxes guarded by NavMesh guards, then hunt down the guards that are left.

## Play

`Builds/Windows/Project2FPS.exe`. Unzip the **whole** folder (exe + `Project2FPS_Data` + `MonoBleedingEdge` + DLLs) and run the exe.

| Action | Key |
|---|---|
| Walk | WASD / Arrow keys |
| Run | Shift + move |
| Jump | Space |
| Look | Mouse |
| Fire gun / throw grenade | Left mouse |
| Switch weapon | Q / E |
| Restart after win/lose | R |
| Quit | Esc |

## Gameplay (requirement → implementation)

| Requirement | Where |
|---|---|
| FP control: walk/run/jump/look, New Input System | `PlayerInputReader`, `PlayerController` |
| 3 lives, hit = −1 life, health kit = +1 | `GameManager`, `PlayerHealth`, `HealthKit` |
| No weapons at start; walk over to pick up; 1 gun (6 bullets), extra ammo, several grenades | `WeaponPickup`, `WeaponManager` |
| Weapons attached to the player but hidden until picked up | `WeaponManager.ShowActiveVisual` (children of `LookCamera`) |
| Gun: 3 hits to kill · Grenade: 1 hit | `GuardHealth` (3 HP), `WeaponManager.FireGun`, `GrenadeProjectile` |
| 3 treasure boxes, each with a protection zone and guards (1 / 2 / 2) | `TreasureSite_A/B/C`, `ProtectionZone`, `TreasureBox` |
| Guards patrol randomly inside the zone | `GuardAI.PickNewPatrolPoint` / `TickPatrol` |
| Player in zone → chase + shoot; leaves → patrol | `ProtectionZone.OnTriggerEnter/Exit` → `GuardAI.OnPlayerEnteredZone/ExitedZone`, `TickChase`, `TryShoot` |
| Box stolen → its guards pursue anywhere | `TreasureBox` → `GuardAI.NotifyTreasureStolen` → `PursueStolen` |
| House with one door; guard inside + outside; box inside | `House`, `Guard_C_Inside` (patrol limited to the room), `Guard_C_Outside` |
| All boxes taken → guards flee; win = 3 boxes + 0 guards | `GameEvents.AllTreasuresCollected` → `GuardAI` Flee; `GameManager.CheckWin` |
| AI Navigation package: NavMesh Surface baked, NavMeshAgent guards | `Ground` → NavMesh Surface (baked asset `Assets/Scenes/MainLevel_NavMesh.asset`) |
| Sounds: gun, grenade throw, explosion, guard hit, player hit, treasure, music, footsteps | `AudioManager` + `Assets/Audio/Clips` |
| GUI: lives, weapons, bullets, kills (+ treasures, status, crosshair) | `GameHUD` on `HUD_Canvas` |
| Lighting: directional sun + 4 point lights | scene |
| VFX (optional): tracers, explosion, hit flash, damage flash | `WeaponManager`, `GuardAI`, `GrenadeProjectile`/`ExplosionFx`, `GuardHealth`, `GameHUD` |

## Project layout

```
Assets/Scenes/MainLevel.unity        the level (all objects are real scene objects)
Assets/Scripts/Player|Weapons|AI|Items|UI|Audio|Core
Assets/Scripts/Editor/ProjectSetupMenu.cs   menu: CMIS3150 → Build Level In Scene / Build Windows Player
Assets/Materials, Assets/Audio/Clips
Docs/CODE_WALKTHROUGH_SCRIPT.md      outline for both videos
```

The level was generated once in the Editor by `LevelBootstrap` (menu **CMIS3150 → Build Level In Scene**), which also bakes the NavMesh Surface and saves the scene. You can edit the scene by hand afterward. Running the menu again regenerates it from code.

## Rebuild

Open in Unity Hub (6000.6.2f1) → **CMIS3150 → Build Windows Player**, or **File → Build Profiles → Windows → Build**.

Batch: `Unity.exe -batchmode -quit -projectPath . -executeMethod Cmis3150.Fps.EditorTools.ProjectSetupMenu.BatchBuildAll`

## Automated test

`Project2FPS.exe -autotest` runs `AutoTest.cs`: it teleports through the level, checks every mechanic above, logs `[AUTOTEST] PASS/FAIL` lines to the player log, and quits. Normal play is unaffected.
