using UnityEngine;

namespace Cmis3150.Fps.Core
{
    /// <summary>
    /// Tracks lives, treasures, kills, and win/lose. Attach to a persistent GameManager object.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Rubric: 3 lives")]
        [SerializeField] int startingLives = 3;
        [SerializeField] int maxLives = 3;
        [SerializeField] int totalTreasures = 3;

        public int Lives { get; private set; }
        public int Kills { get; private set; }
        public int TreasuresCollected { get; private set; }
        public int GuardsAlive { get; private set; }
        public bool AllTreasuresTaken { get; private set; }
        public bool IsGameOver { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Lives = startingLives;
        }

        void Start()
        {
            GameEvents.RaiseLives(Lives);
            GameEvents.RaiseKills(Kills);
            GameEvents.RaiseTreasure(0, totalTreasures);
        }

        public void RegisterGuard()
        {
            GuardsAlive++;
        }

        public void NotifyGuardDied()
        {
            GuardsAlive = Mathf.Max(0, GuardsAlive - 1);
            Kills++;
            GameEvents.RaiseKills(Kills);
            GameEvents.RaiseGuardDied();
            CheckWin();
        }

        public void NotifyTreasureCollected()
        {
            if (IsGameOver) return;
            TreasuresCollected = Mathf.Min(totalTreasures, TreasuresCollected + 1);
            GameEvents.RaiseTreasure(TreasuresCollected, totalTreasures);
            if (TreasuresCollected >= totalTreasures && !AllTreasuresTaken)
            {
                AllTreasuresTaken = true;
                // Remaining guards flee — handled via AllTreasuresCollected event in GuardAI.
            }

            CheckWin();
        }

        public void DamagePlayer(int amount = 1)
        {
            if (IsGameOver) return;
            Lives = Mathf.Max(0, Lives - amount);
            GameEvents.RaiseLives(Lives);
            GameEvents.RaisePlayerHit();
            if (Lives <= 0)
                Lose();
        }

        public void HealPlayer(int amount = 1)
        {
            if (IsGameOver) return;
            Lives = Mathf.Min(maxLives, Lives + amount);
            GameEvents.RaiseLives(Lives);
        }

        void CheckWin()
        {
            if (IsGameOver) return;
            if (TreasuresCollected >= totalTreasures && GuardsAlive <= 0)
                Win();
        }

        void Win()
        {
            IsGameOver = true;
            GameEvents.RaiseWin();
            Time.timeScale = 0f;
        }

        void Lose()
        {
            IsGameOver = true;
            GameEvents.RaiseLose();
            Time.timeScale = 0f;
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
