using UnityEngine;
using UnityEngine.UI;
using Cmis3150.Fps.Core;

namespace Cmis3150.Fps.UI
{
    /// <summary>
    /// uGUI HUD on HUD_Canvas: lives, weapons collected, bullets left, kills, treasures,
    /// a status line, a red damage flash and the win/lose panel. Updated only through GameEvents.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] Text livesText;
        [SerializeField] Text weaponsText;
        [SerializeField] Text bulletsText;
        [SerializeField] Text killsText;
        [SerializeField] Text treasureText;
        [SerializeField] Text statusText;
        [SerializeField] Image damageFlash;
        [SerializeField] GameObject endPanel;
        [SerializeField] Text endMessage;
        [SerializeField] float statusSeconds = 4f;

        float _statusUntil;
        float _flashAlpha;

        void OnEnable()
        {
            GameEvents.LivesChanged += OnLives;
            GameEvents.WeaponsChanged += OnWeapons;
            GameEvents.AmmoChanged += OnAmmo;
            GameEvents.KillsChanged += OnKills;
            GameEvents.TreasureCollected += OnTreasure;
            GameEvents.PlayerWon += OnWin;
            GameEvents.PlayerLost += OnLose;
            GameEvents.PlayerHit += OnPlayerHit;
            GameEvents.StatusMessage += OnStatus;
        }

        void OnDisable()
        {
            GameEvents.LivesChanged -= OnLives;
            GameEvents.WeaponsChanged -= OnWeapons;
            GameEvents.AmmoChanged -= OnAmmo;
            GameEvents.KillsChanged -= OnKills;
            GameEvents.TreasureCollected -= OnTreasure;
            GameEvents.PlayerWon -= OnWin;
            GameEvents.PlayerLost -= OnLose;
            GameEvents.PlayerHit -= OnPlayerHit;
            GameEvents.StatusMessage -= OnStatus;
        }

        void Start()
        {
            if (endPanel) endPanel.SetActive(false);
            _statusUntil = Time.time + 8f;
        }

        void OnLives(int v)
        {
            if (livesText) livesText.text = $"Lives: {v}";
        }

        void OnWeapons(string s)
        {
            if (weaponsText) weaponsText.text = $"Weapons: {s}";
        }

        void OnAmmo(int bullets, int grenades)
        {
            if (bulletsText) bulletsText.text = $"Bullets: {bullets}   Grenades: {grenades}";
        }

        void OnKills(int v)
        {
            if (killsText) killsText.text = $"Kills: {v}";
        }

        void OnTreasure(int c, int t)
        {
            if (treasureText) treasureText.text = $"Treasures: {c}/{t}";
        }

        void OnStatus(string msg)
        {
            if (statusText == null) return;
            statusText.text = msg;
            statusText.enabled = true;
            _statusUntil = Time.unscaledTime + statusSeconds;
        }

        void OnPlayerHit()
        {
            _flashAlpha = 0.45f;
            OnStatus("You were hit!");
        }

        void OnWin() => ShowEnd("YOU WIN!\nAll treasures collected and all guards eliminated.\n\nPress R to play again");
        void OnLose() => ShowEnd("GAME OVER\nYou ran out of lives.\n\nPress R to try again");

        void ShowEnd(string msg)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (endPanel) endPanel.SetActive(true);
            if (endMessage) endMessage.text = msg;
        }

        void Update()
        {
            if (statusText != null && statusText.enabled && Time.unscaledTime > _statusUntil)
                statusText.enabled = false;

            if (damageFlash != null)
            {
                _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, Time.unscaledDeltaTime * 1.2f);
                damageFlash.color = new Color(1f, 0f, 0f, _flashAlpha);
            }

            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (endPanel != null && endPanel.activeSelf && kb.rKey.wasPressedThisFrame)
                GameManager.Instance?.RestartLevel();
            if (kb.escapeKey.wasPressedThisFrame)
                Application.Quit();
        }

        // ---------- setup (called by LevelBootstrap in the Editor) ----------

        public void Bind(Text lives, Text weapons, Text bullets, Text kills, Text treasure, GameObject end, Text endMsg)
        {
            livesText = lives;
            weaponsText = weapons;
            bulletsText = bullets;
            killsText = kills;
            treasureText = treasure;
            endPanel = end;
            endMessage = endMsg;
        }

        public void BindExtras(Text status, Image flash)
        {
            statusText = status;
            damageFlash = flash;
        }
    }
}
