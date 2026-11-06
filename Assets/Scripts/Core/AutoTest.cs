using System.Collections;
using System.Linq;
using UnityEngine;
using Cmis3150.Fps.AI;
using Cmis3150.Fps.Player;
using Cmis3150.Fps.Weapons;

namespace Cmis3150.Fps.Core
{
    /// <summary>
    /// Automated smoke test. Does nothing in normal play. Only runs when the game is started with
    /// "-autotest" on the command line: it teleports the player through the level, exercises every
    /// mechanic and writes "[AUTOTEST]" PASS/FAIL lines to the player log, then quits.
    /// </summary>
    public class AutoTest : MonoBehaviour
    {
        PlayerController _player;
        CharacterController _cc;
        WeaponManager _weapons;
        int _fails;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (!System.Environment.GetCommandLineArgs().Contains("-autotest")) return;
            new GameObject("AutoTest").AddComponent<AutoTest>();
        }

        void Log(string m) => Debug.Log("[AUTOTEST] " + m);

        void Check(bool ok, string what)
        {
            if (!ok) _fails++;
            Log((ok ? "PASS " : "FAIL ") + what);
        }

        void Teleport(Vector3 p)
        {
            _cc.enabled = false;
            _player.transform.position = p;
            _cc.enabled = true;
        }

        void LookAt(Vector3 target)
        {
            Vector3 flat = target - _player.transform.position;
            flat.y = 0f;
            _player.transform.rotation = Quaternion.LookRotation(flat);
            var cam = _player.LookCamera;
            cam.rotation = Quaternion.LookRotation(target - cam.position);
        }

        GuardAI Guard(string name) => FindObjectsByType<GuardAI>(FindObjectsInactive.Exclude)
            .FirstOrDefault(g => g.name == name);

        IEnumerator Start()
        {
            yield return new WaitForSeconds(1.5f);
            _player = FindAnyObjectByType<PlayerController>();
            _cc = _player.GetComponent<CharacterController>();
            _weapons = _player.GetComponent<WeaponManager>();
            var gm = GameManager.Instance;

            var guards = FindObjectsByType<GuardAI>(FindObjectsInactive.Exclude);
            Check(guards.Length == 5, $"5 guards in level (found {guards.Length})");
            foreach (var g in guards)
                Check(g.GetComponent<UnityEngine.AI.NavMeshAgent>().isOnNavMesh, $"{g.name} is on NavMesh");
            Check(gm.Lives == 3, "player starts with 3 lives");
            Check(_weapons.Current == WeaponType.None, "player starts with no weapon");

            var gA = Guard("Guard_A");
            Vector3 startA = gA.transform.position;
            yield return new WaitForSeconds(2f);
            Check(gA.State == GuardState.Patrol && Vector3.Distance(startA, gA.transform.position) > 0.3f, "Guard_A patrols (moved while in Patrol)");

            // --- Pickups ---
            Teleport(new Vector3(-3f, 0.1f, -16f));
            yield return new WaitForSeconds(0.5f);
            Check(_weapons.Current == WeaponType.Gun && _weapons.Bullets == 6, $"gun picked up with 6 bullets (bullets={_weapons.Bullets})");
            Teleport(new Vector3(4f, 0.1f, -12f));
            yield return new WaitForSeconds(0.4f);
            Check(_weapons.Bullets == 12, $"ammo pickup adds bullets (bullets={_weapons.Bullets})");
            Teleport(new Vector3(3f, 0.1f, -17f));
            yield return new WaitForSeconds(0.4f);
            Teleport(new Vector3(-10f, 0.1f, -14f));
            yield return new WaitForSeconds(0.4f);
            Check(_weapons.Grenades == 2, $"grenade pickups (grenades={_weapons.Grenades})");

            // --- Zone enter â†’ chase + shoot ---
            Vector3 boxA = new Vector3(-16f, 0f, 2f);
            Teleport(boxA + new Vector3(0f, 0.1f, -7f));
            yield return new WaitForSeconds(0.6f);
            Check(gA.State == GuardState.Chase, $"Guard_A chases when player enters zone (state={gA.State})");
            int livesBefore = gm.Lives;
            float t0 = Time.time;
            while (Time.time - t0 < 12f && gm.Lives == livesBefore) yield return null;
            Check(gm.Lives < livesBefore, $"guard shot the player (lives {livesBefore} â†’ {gm.Lives})");

            // --- Zone exit â†’ back to patrol ---
            Teleport(new Vector3(-3f, 0.1f, -20f));
            yield return new WaitForSeconds(0.6f);
            Check(gA.State == GuardState.Patrol, $"Guard_A returns to patrol when player leaves (state={gA.State})");

            // --- Health kit ---
            int l0 = gm.Lives;
            Teleport(new Vector3(0f, 0.1f, -8f));
            yield return new WaitForSeconds(0.4f);
            Check(gm.Lives == Mathf.Min(3, l0 + 1), $"health kit restores a life ({l0} â†’ {gm.Lives})");

            // --- Gun: 3 hits kill ---
            var gB1 = Guard("Guard_B1");
            var hp = gB1.GetComponent<GuardHealth>();
            int kills0 = gm.Kills;
            _weapons.TestSelect(WeaponType.Gun);
            for (int i = 0; i < 3; i++)
            {
                if (gB1 == null) break;
                Teleport(gB1.transform.position + new Vector3(0f, 0.1f, -26f) + Vector3.left * 0.01f);
                yield return null;
                LookAt(gB1.transform.position + Vector3.up * 1.0f);
                yield return null;
                _weapons.TestFire();
                yield return new WaitForSeconds(0.2f);
                if (i < 2) Check(gB1 != null, $"Guard_B1 still alive after {i + 1} gun hit(s)");
            }
            yield return null;
            Check(gB1 == null && gm.Kills == kills0 + 1, $"Guard_B1 dies on the 3rd gun hit (kills={gm.Kills})");

            // --- Grenade: 1 hit kills ---
            var gB2 = Guard("Guard_B2");
            _weapons.TestSelect(WeaponType.Grenade);
            Teleport(new Vector3(16f, 0.1f, -14f));
            yield return null;
            if (gB2 != null)
            {
                gB2.GetComponent<UnityEngine.AI.NavMeshAgent>().isStopped = true;
                Teleport(gB2.transform.position + new Vector3(0f, 0.1f, -5f));
                yield return null;
                LookAt(gB2.transform.position + Vector3.up * 0.3f);
                _weapons.TestFire();
                yield return new WaitForSeconds(2.8f);
            }
            Check(gB2 == null, "Guard_B2 killed by one grenade");

            // --- Steal treasure A â†’ guard pursues outside the zone ---
            gA = Guard("Guard_A");
            Teleport(new Vector3(-16f, 0.1f, 2f));
            yield return new WaitForSeconds(0.4f);
            Check(gm.TreasuresCollected == 1, $"treasure A collected ({gm.TreasuresCollected}/3)");
            Check(gA != null && gA.State == GuardState.PursueStolen, $"Guard_A pursues after theft (state={gA?.State})");
            Teleport(new Vector3(-16f, 0.1f, -22f)); // far outside zone A
            yield return new WaitForSeconds(1.5f);
            Check(gA != null && gA.State == GuardState.PursueStolen, $"Guard_A keeps pursuing outside its zone (state={gA?.State})");

            // --- House: enter through the door ---
            var gIn = Guard("Guard_C_Inside");
            Check(gIn != null && gIn.transform.position.z > 11f && Mathf.Abs(gIn.transform.position.x) < 4f, "inside guard is inside the house");
            Teleport(new Vector3(16f, 0.1f, 0f));      // treasure B
            yield return new WaitForSeconds(0.4f);
            Teleport(new Vector3(0f, 0.1f, 7f));       // in front of door
            yield return new WaitForSeconds(0.2f);
            Teleport(new Vector3(0f, 0.1f, 16.5f));    // treasure C (inside)
            yield return new WaitForSeconds(0.5f);
            Check(gm.TreasuresCollected == 3, $"all 3 treasures collected ({gm.TreasuresCollected}/3)");

            // --- Remaining guards flee ---
            yield return new WaitForSeconds(0.5f);
            var remaining = FindObjectsByType<GuardAI>(FindObjectsInactive.Exclude);
            Check(remaining.Length > 0 && remaining.All(g => g.State == GuardState.Flee), $"remaining guards flee ({string.Join(",", remaining.Select(g => g.name + ":" + g.State))})");
            if (gA != null)
            {
                Vector3 before = gA.transform.position;
                float d0 = Vector3.Distance(before, _player.transform.position);
                yield return new WaitForSeconds(2f);
                if (gA != null)
                    Check(Vector3.Distance(gA.transform.position, _player.transform.position) > d0, "fleeing guard moves away from the player");
            }

            // --- Hunt the rest â†’ win ---
            foreach (var g in FindObjectsByType<GuardHealth>(FindObjectsInactive.Exclude))
                g.TakeDamage(99, true);
            yield return new WaitForSecondsRealtime(0.5f);
            Check(gm.IsGameOver && gm.Kills == 5, $"win when all treasure + all guards dead (kills={gm.Kills}, over={gm.IsGameOver})");
            yield return new WaitForSecondsRealtime(0.5f);

            Log(_fails == 0 ? "ALL PASSED" : $"{_fails} FAILED");
            Application.Quit(_fails == 0 ? 0 : 1);
        }
    }
}
