using System;
using UnityEngine;

namespace Cmis3150.Fps.Core
{
    /// <summary>
    /// Central static events so UI, audio, and AI can react without hard scene wiring.
    /// Gameplay code calls Raise*; listeners subscribe in OnEnable and unsubscribe in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int> LivesChanged;
        public static event Action<int, int> AmmoChanged;        // bullets, grenades
        public static event Action<int> KillsChanged;
        public static event Action<string> WeaponsChanged;
        public static event Action<int, int> TreasureCollected;  // collected, total
        public static event Action AllTreasuresCollected;
        public static event Action PlayerWon;
        public static event Action PlayerLost;
        public static event Action GuardDied;
        public static event Action PlayerHit;
        public static event Action GuardHit;
        public static event Action GunFired;
        public static event Action<Vector3> GuardFired;
        public static event Action GrenadeThrown;
        public static event Action GrenadeExploded;
        public static event Action TreasurePicked;
        public static event Action<bool> Footstep;              // true = running
        public static event Action<string> StatusMessage;

        public static void RaiseLives(int v) => LivesChanged?.Invoke(v);
        public static void RaiseAmmo(int bullets, int grenades) => AmmoChanged?.Invoke(bullets, grenades);
        public static void RaiseKills(int v) => KillsChanged?.Invoke(v);
        public static void RaiseWeapons(string summary) => WeaponsChanged?.Invoke(summary);

        // HUD counter only (used at game start).
        public static void RaiseTreasureCount(int collected, int total) => TreasureCollected?.Invoke(collected, total);

        // A treasure box was actually picked up.
        public static void RaiseTreasure(int collected, int total)
        {
            TreasureCollected?.Invoke(collected, total);
            TreasurePicked?.Invoke();
            if (collected >= total)
                AllTreasuresCollected?.Invoke();
        }

        public static void RaiseWin() => PlayerWon?.Invoke();
        public static void RaiseLose() => PlayerLost?.Invoke();
        public static void RaiseGuardDied() => GuardDied?.Invoke();
        public static void RaisePlayerHit() => PlayerHit?.Invoke();
        public static void RaiseGuardHit() => GuardHit?.Invoke();
        public static void RaiseGunFired() => GunFired?.Invoke();
        public static void RaiseGuardFired(Vector3 pos) => GuardFired?.Invoke(pos);
        public static void RaiseGrenadeThrown() => GrenadeThrown?.Invoke();
        public static void RaiseGrenadeExploded() => GrenadeExploded?.Invoke();
        public static void RaiseFootstep(bool running) => Footstep?.Invoke(running);
        public static void RaiseStatus(string msg) => StatusMessage?.Invoke(msg);
    }
}
