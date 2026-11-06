using UnityEngine;
using UnityEngine.UI;
using Cmis3150.Fps.Core;

namespace Cmis3150.Fps.UI
{
    /// <summary>
    /// uGUI HUD: lives, weapons collected, bullets, kills (+ optional treasure count / end panel).
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] Text livesText;
        [SerializeField] Text weaponsText;
        [SerializeField] Text bulletsText;
        [SerializeField] Text killsText;
        [SerializeField] Text treasureText;
        [SerializeField] GameObject endPanel;
        [SerializeField] Text endMessage;

        void OnEnable()
        {
            GameEvents.LivesChanged += OnLives;
            GameEvents.WeaponsChanged += OnWeapons;
            GameEvents.BulletsChanged += OnBullets;
            GameEvents.KillsChanged += OnKills;
            GameEvents.TreasureCollected += OnTreasure;
            GameEvents.PlayerWon += OnWin;
            GameEvents.PlayerLost += OnLose;
        }

        void OnDisable()
        {
            GameEvents.LivesChanged -= OnLives;
            GameEvents.WeaponsChanged -= OnWeapons;
            GameEvents.BulletsChanged -= OnBullets;
            GameEvents.KillsChanged -= OnKills;
            GameEvents.TreasureCollected -= OnTreasure;
            GameEvents.PlayerWon -= OnWin;
            GameEvents.PlayerLost -= OnLose;
        }

        void OnLives(int v)
        {
            if (livesText) livesText.text = $"Lives: {v}";
        }

        void OnWeapons(string s)
        {
            if (weaponsText) weaponsText.text = $"Weapons: {s}";
        }

        void OnBullets(int v)
        {
            if (bulletsText) bulletsText.text = $"Ammo / Charges: {v}";
        }

        void OnKills(int v)
        {
            if (killsText) killsText.text = $"Kills: {v}";
        }

        void OnTreasure(int c, int t)
        {
            if (treasureText) treasureText.text = $"Treasures: {c}/{t}";
        }

        void OnWin()
        {
            ShowEnd("YOU WIN — all treasures collected and all guards eliminated.\nPress R to restart.");
        }

        void OnLose()
        {
            ShowEnd("YOU LOSE — out of lives.\nPress R to restart.");
        }

        void ShowEnd(string msg)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (endPanel) endPanel.SetActive(true);
            if (endMessage) endMessage.text = msg;
        }

        void Update()
        {
            if (endPanel == null || !endPanel.activeSelf) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
                GameManager.Instance?.RestartLevel();
        }

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
    }
}
