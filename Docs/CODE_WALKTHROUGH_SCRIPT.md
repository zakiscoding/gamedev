# Suggested narration outline (≤15 minutes)

1. Show Play Mode level (house, three gold boxes, red zone rings, pickups).
2. Open `PlayerInputReader.cs` — New Input System, not `UnityEngine.Input`.
3. Open `PlayerController.cs` on Player — walk/run/jump/look.
4. Open `WeaponManager.cs` — empty inventory, pickup enables child mesh, Q/E, LMB.
5. **Main interaction (required depth):**
   - Breakpoint or Pause: walk into zone.
   - `ProtectionZone.OnTriggerEnter` → `GuardAI.OnPlayerEnteredZone` → Chase.
   - `TickChase` / `TryShoot` → `PlayerHealth.TakeDamage`.
   - Leave zone → patrol resumes.
6. Steal a box → pursue forever branch.
7. All boxes → flee; kill last guard → win UI.
8. Point at HUD + AudioManager event subscriptions.
