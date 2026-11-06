# CMIS 3150 / CSC4820 — Project 2: 3D FPS

Unity **C#** first-person shooter that implements the full course rubric: New Input System movement/combat, weapon pickups, NavMesh guards (patrol / chase+shoot / pursue-after-steal / flee), three treasure boxes (one inside a house with a door), health kits, SFX, uGUI HUD, win/lose.

**Engine target:** Unity 6 (`6000.0.x`). Unity 2022 LTS+ can work if you retarget package versions.

> **Honesty note:** This repository was authored without a Unity Editor on the build agent. There is **no** prebuilt Windows standalone player in the repo. Open the project on a machine with Unity (e.g. zakar), press Play to verify, then build Windows from **File → Build Profiles**.

---

## Open in Unity Hub

1. Install **Unity 6** (or 2022.3 LTS+) with modules:
   - Windows Build Support (**Mono**)
   - (Optional) Mac Build Support if submitting from a Mac
2. Unity Hub → **Add** → select this project folder.
3. First open: let Package Manager restore:
   - `com.unity.inputsystem`
   - `com.unity.ai.navigation`
   - `com.unity.ugui`
4. **Edit → Project Settings → Player → Active Input Handling** = `Input System Package` (or `Both`). ProjectSettings already set `activeInputHandler: 1`.
5. Open scene `Assets/Scenes/MainLevel.unity`.
6. Press **Play**. `LevelBootstrap` builds the level from primitives, wires systems, and calls `NavMeshSurface.BuildNavMesh()` at runtime.

Menu helpers: **CMIS3150 → Open Main Level** / **Create Bootstrap Object** / **Print Windows Build Checklist**.

---

## Controls (New Input System)

| Action | Binding |
|--------|---------|
| Walk | WASD or Arrow keys |
| Run | Left/Right Shift |
| Jump | Space |
| Look | Mouse |
| Switch weapon | Q / E |
| Fire / throw | Left mouse button |

You start with **no weapons**. Walk over pickups.

---

## Scene wiring (what Play creates)

`LevelBootstrap` (only object in `MainLevel`) spawns:

| Object | Role |
|--------|------|
| `Player` | `CharacterController`, `PlayerInputReader`, `PlayerController`, `PlayerHealth`, `WeaponManager` + camera |
| `GameManager` | Lives, treasures, kills, win/lose |
| `HUD_Canvas` | Lives, weapons, bullets/charges, kills, treasures |
| `AudioManager` | Procedural beep clips for all required SFX + BGM loop |
| `TreasureSite_*` | Box + `ProtectionZone` trigger + `GuardAI` (`NavMeshAgent`) |
| House walls + doorway | Interior box + inside/outside guards |
| Pickups | 1 gun (6 rounds on collect), ammo, grenades, health kits |
| Lights | 1 directional sun + 3 point lights |

**Drop Asset Store gun/grenade models:** see `Assets/Prefabs/WHERE_TO_DROP_MODELS.md`.

---

## NavMesh

Runtime bake is already called from `LevelBootstrap.Build()` via `NavMeshSurface`.

Optional Editor bake (for inspection / builds that prefer baked data):

1. Select `Ground` (created at runtime — or pause Play and copy, or author a static ground).
2. Add **NavMesh Surface** → **Bake**.

For graded play, runtime bake is sufficient.

---

## Build Windows standalone

1. **File → Build Profiles → Windows** → Build into e.g. `Builds/Windows/`.
2. Scripting Backend: **Mono** if building Windows from Mac.
3. Zip the **entire** output folder (`Project2FPS.exe` + `Project2FPS_Data/`), not only the `.exe`.
4. Test the zip on a Windows machine before iCollege upload.

---

## Code walkthrough video map (≤15 min)

Narrate this **one complete interaction** (rubric recommendation):

1. Player walks into a red zone ring → `ProtectionZone.OnTriggerEnter`
2. → `GuardAI.OnPlayerEnteredZone` → state `Chase`
3. → `TickChase` → `NavMeshAgent.SetDestination(player)` + `TryShoot` → `PlayerHealth.TakeDamage`
4. Player leaves zone → `OnTriggerExit` → `OnPlayerExitedZone` → state `Patrol` → `PickNewPatrolPoint`
5. Contrast: steal box → `TreasureBox.OnTriggerEnter` → `NotifyTreasureStolen` → `PursueStolen` forever
6. Collect all 3 boxes → `GameEvents.AllTreasuresCollected` → remaining guards `Flee`
7. Kill last guard with all boxes held → `GameManager.CheckWin`

Also briefly open each script in the Inspector and name the GameObject it lives on (see `RUBRIC.md`).

Gameplay video: one **win** (3 boxes + all guards dead) and one **fail** (0 lives).

---

## Folder map

```
Assets/Scripts/
  Player/    PlayerController, PlayerInputReader, PlayerHealth
  Weapons/   WeaponManager, WeaponPickup, GrenadeProjectile, WeaponType
  AI/        GuardAI, GuardHealth, ProtectionZone
  Items/     TreasureBox, HealthKit
  UI/        GameHUD
  Audio/     AudioManager, ProceduralClipFactory
  Core/      GameManager, GameEvents, LevelBootstrap
  Editor/    ProjectSetupMenu
Assets/Input/PlayerControls.inputactions   # matches code bindings (for walkthrough)
Assets/Scenes/MainLevel.unity
```

---

## What works without Unity Editor vs Editor-only

| Without Editor (this cloud agent) | Needs Unity on zakar / local |
|-----------------------------------|------------------------------|
| Full C# gameplay source | Package resolve / compile |
| Scene + project settings scaffold | Press Play to verify |
| Runtime level design in code | Optional mesh art swap |
| README / RUBRIC / git history | Windows standalone build |
| Zip of Unity project sources | Gameplay + walkthrough videos |

---

## License / course note

Course assignment project for CMIS 3150 / CSC4820. You are responsible for every line you submit and must be able to explain it in the narrated walkthrough.
