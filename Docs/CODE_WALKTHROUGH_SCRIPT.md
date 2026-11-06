# Code walkthrough video: script (aim for 10–13 min, max 15)

Narrate in your own words. This is a map of what to open and point at, not a script to read aloud.
Open `Assets/Scenes/MainLevel.unity` first so you can click objects in the Hierarchy and show the Inspector.

---

## Part 1: Each script and the GameObject it's on (~5 min)

For each row: click the GameObject in the Hierarchy → show the component in the Inspector → open the script.

| Script | GameObject (Hierarchy) | One-line explanation |
|---|---|---|
| `PlayerInputReader.cs` | **Player** | New Input System. Builds an `InputActionMap` in code (WASD/arrows, Shift, Space, Mouse, LMB, Q/E). Other scripts read its properties (`Move`, `Look`, `FirePressed`…). No `UnityEngine.Input`. |
| `PlayerController.cs` | **Player** | `Look()` rotates the body (yaw) and `LookCamera` (pitch). `Move()` drives the `CharacterController`: walk/run speed, jump with `Mathf.Sqrt(h * -2g)`, gravity. Also raises footstep events. |
| `PlayerHealth.cs` | **Player** | `TakeDamage()`: called by guard bullets; short invulnerability, then forwards to `GameManager.DamagePlayer`. |
| `WeaponManager.cs` | **Player** | Starts with **no weapons**. `CollectGun/CollectGrenade/CollectAmmo` are called by pickups. Gun/grenade meshes are children of `LookCamera` and are hidden until picked up (`ShowActiveVisual`). Q/E → `Cycle()`, LMB → `Fire()` → `FireGun()` (raycast, 1 damage) or `ThrowGrenade()` (Rigidbody + `GrenadeProjectile`). |
| `WeaponPickup.cs` | **Pickup_Gun / Pickup_Ammo / Pickup_Grenade** | Trigger collider. `OnTriggerEnter` finds the `WeaponManager` on the player and calls the right Collect method, then destroys itself. |
| `GrenadeProjectile.cs` + `ExplosionFx.cs` | spawned at runtime | Explodes on impact (or fuse). `Physics.OverlapSphere` → `GuardHealth.TakeDamage(99)` = one-hit kill. Expanding sphere + light. |
| `GuardHealth.cs` | **Guard_*** | 3 HP. Gun = 1 damage → 3 hits. Grenade = 99 → 1 hit. `Die()` tells `GameManager` (kill count) and destroys the guard. |
| `GuardAI.cs` | **Guard_*** (with `NavMeshAgent`) | State machine: Patrol / Chase / PursueStolen / Flee / Dead. **The main interaction (Part 2).** |
| `ProtectionZone.cs` | **TreasureSite_*/ProtectionZone** (Sphere Collider, *Is Trigger*) | The invisible circle around each treasure box. Tells its guards when the player enters or leaves. |
| `TreasureBox.cs` | **TreasureSite_*/TreasureBox** | Trigger. On pickup: tells its guards `NotifyTreasureStolen`, tells `GameManager`. |
| `HealthKit.cs` | **HealthKit** | Trigger. `GameManager.HealPlayer(1)`. |
| `GameManager.cs` | **GameManager** | Owns lives (3), kills, treasures, guards alive. `CheckWin()` = 3 treasures and 0 guards. `Lose()` at 0 lives. |
| `GameEvents.cs` | (static class, not on an object) | Event hub. Gameplay raises events; HUD and audio listen. Keeps scripts decoupled. |
| `GameHUD.cs` | **HUD_Canvas** | uGUI `Text`s for lives, weapons, bullets, kills, treasures; subscribes to `GameEvents`. Win/lose panel, R to restart. |
| `AudioManager.cs` | **AudioManager** | Subscribes to events and plays the clips: gun, grenade throw, explosion, guard hit, player hit, treasure, footsteps; loops background music. |
| `LevelBootstrap.cs` + `Editor/ProjectSetupMenu.cs` | (editor tool) | Menu **CMIS3150 → Build Level In Scene** generated this scene (walls, house, guards, pickups) and baked the **NavMesh Surface** on `Ground`. Show `Ground` → *NavMesh Surface* component → **Bake** button. |
| `AutoTest.cs` | (only with `-autotest`) | Automated test that teleports the player through every mechanic and logs PASS/FAIL. Not active in normal play. |

Also show **Ground → NavMesh Surface** (the baked NavMesh is the blue overlay when you select it) and a **Guard → NavMeshAgent** component.

---

## Part 2: One complete interaction, step by step (~6 min) ⭐

**"What happens when I walk into a guard's protection zone, and back out."**
Keep `ProtectionZone.cs` and `GuardAI.cs` side by side and point at each line as you say it.

1. **Patrolling (before I arrive).**
   `GuardAI.Start()` → `PickNewPatrolPoint()`: picks a random point inside the zone's circle
   (`Random.insideUnitCircle * radius`), snaps it to the NavMesh with `NavMesh.SamplePosition`,
   and sends the agent there (`_agent.SetPath`). Every frame `Update()` → `TickPatrol()`: when
   `remainingDistance` is small, wait a moment and pick another point. *That's the random patrol.*

2. **I enter the zone → trigger callback.**
   The zone has a **Sphere Collider with Is Trigger on**. The player's `CharacterController` entering it makes Unity call
   `ProtectionZone.OnTriggerEnter(Collider other)`. It checks the collider is the player (tag/`PlayerController`)
   and loops over its `guards` array calling `guard.OnPlayerEnteredZone(player)`.

3. **Guard switches Patrol → Chase.**
   `GuardAI.OnPlayerEnteredZone` stores `_player` and calls `SetState(GuardState.Chase)`.
   `SetState` raises the agent speed (`chaseSpeed`) and sets `stoppingDistance = 4`.

4. **Chasing every frame.**
   `Update()` → `switch(_state)` → `TickChase()`: `_agent.SetDestination(_player.position)`
   (NavMesh pathfinding around walls), rotate to face the player, and if within `shootRange` and the cooldown has passed → `TryShoot()`.

5. **Shooting at the player.**
   `TryShoot()`: a raycast from the guard's `Muzzle` toward the player. Accuracy is `hitChance` (35%), so misses
   aim at a random offset. If the ray hits a collider with `PlayerHealth` → `PlayerHealth.TakeDamage(1)` →
   `GameManager.DamagePlayer` → `Lives--` → `GameEvents.RaisePlayerHit()` and `RaiseLives()`
   → **HUD** updates the lives text and flashes red, **AudioManager** plays the hit sound. At 0 lives → `Lose()`.
   The yellow line you see is the `LineRenderer` tracer (`Flash` coroutine).

6. **I leave the zone → back to patrol.**
   Unity calls `ProtectionZone.OnTriggerExit` → `guard.OnPlayerExitedZone()` → `_player = null`,
   `SetState(GuardState.Patrol)`, `PickNewPatrolPoint()`.

7. **Contrast: stealing the box.** `TreasureBox.OnTriggerEnter` → `guard.NotifyTreasureStolen(player)` sets `_boxStolen = true`
   and state `PursueStolen`. Now `OnPlayerExitedZone` sees `_boxStolen` and **keeps** pursuing, so the guard follows me anywhere.

8. **All three boxes taken → flee.** `GameManager.NotifyTreasureCollected` → `GameEvents.RaiseTreasure(3,3)` fires
   `AllTreasuresCollected`; every `GuardAI` subscribed in `OnEnable` → `OnAllTreasuresCollected()` → `Flee`.
   `TickFlee()` picks a NavMesh point in the direction away from me. Killing the last one → `GameManager.CheckWin()` → win panel.

---

## Part 3: Wrap-up (~30 s)
New Input System, NavMesh Surface + NavMeshAgent, uGUI HUD, event-driven audio. Tested with the AutoTest run and by playing.

---

# Gameplay video plan (record with Win+G or OBS)

**Run 1: Failure (~1 min).** Walk straight into the east yard with no weapon and stand there. Guards chase and shoot; lives drop 3 → 0 → GAME OVER. Press R.

**Run 2: Win (~4–5 min).**
1. Pick up the gun (start area) → HUD shows Gun, Bullets 6. Grab ammo (orange) + grenades (green) + note health kits (white/red).
2. West yard: shoot Guard_A from outside the zone (3 hits), walk in, take treasure 1.
3. East yard: throw a grenade at the two guards (1 hit each), take treasure 2.
4. House: kill the outside guard, go **through the door**, kill the inside guard, take treasure 3. If any guard is still alive, it now runs away. Hunt it → YOU WIN.
Show Q/E switching, Shift running, Space jumping, a health kit pickup after taking a hit.
