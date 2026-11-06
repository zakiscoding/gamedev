using UnityEngine;
using Cmis3150.Fps.Core;

namespace Cmis3150.Fps.Audio
{
    /// <summary>
    /// Lives on the AudioManager object. Plays a sound for every required event by listening to GameEvents,
    /// and loops the background music.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioSource bgmSource;

        [Header("Clips (Assets/Audio/Clips)")]
        [SerializeField] AudioClip gunShot;
        [SerializeField] AudioClip grenadeThrow;
        [SerializeField] AudioClip grenadeExplosion;
        [SerializeField] AudioClip hitGuard;
        [SerializeField] AudioClip playerHit;
        [SerializeField] AudioClip treasure;
        [SerializeField] AudioClip bgm;
        [SerializeField] AudioClip footstep;

        void Awake()
        {
            Instance = this;
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.volume = 0.3f;
            }
        }

        void OnEnable()
        {
            GameEvents.GunFired += OnGun;
            GameEvents.GuardFired += OnGuardFired;
            GameEvents.GrenadeThrown += OnThrow;
            GameEvents.GrenadeExploded += OnExplode;
            GameEvents.GuardHit += OnGuardHit;
            GameEvents.PlayerHit += OnPlayerHit;
            GameEvents.TreasurePicked += OnTreasure;
            GameEvents.Footstep += OnFootstep;
        }

        void OnDisable()
        {
            GameEvents.GunFired -= OnGun;
            GameEvents.GuardFired -= OnGuardFired;
            GameEvents.GrenadeThrown -= OnThrow;
            GameEvents.GrenadeExploded -= OnExplode;
            GameEvents.GuardHit -= OnGuardHit;
            GameEvents.PlayerHit -= OnPlayerHit;
            GameEvents.TreasurePicked -= OnTreasure;
            GameEvents.Footstep -= OnFootstep;
        }

        void Start()
        {
            if (bgm != null && bgmSource != null)
            {
                bgmSource.clip = bgm;
                bgmSource.Play();
            }
        }

        void OnGun() => Play(gunShot, 0.8f);
        void OnGuardFired(Vector3 pos) { if (gunShot != null) AudioSource.PlayClipAtPoint(gunShot, pos, 0.5f); }
        void OnThrow() => Play(grenadeThrow, 0.8f);
        void OnExplode() => Play(grenadeExplosion, 1f);
        void OnGuardHit() => Play(hitGuard, 0.9f);
        void OnPlayerHit() => Play(playerHit, 1f);
        void OnTreasure() => Play(treasure, 1f);
        void OnFootstep(bool running) => Play(footstep, running ? 0.35f : 0.2f);

        void Play(AudioClip clip, float volume)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }

        // ---------- setup (called by LevelBootstrap in the Editor) ----------

        public void SetClips(AudioClip gun, AudioClip throwClip, AudioClip explode, AudioClip hitG, AudioClip hitP, AudioClip treasureClip, AudioClip music)
        {
            gunShot = gun;
            grenadeThrow = throwClip;
            grenadeExplosion = explode;
            hitGuard = hitG;
            playerHit = hitP;
            treasure = treasureClip;
            bgm = music;
        }

        public void SetFootsteps(AudioClip step) => footstep = step;
    }
}
