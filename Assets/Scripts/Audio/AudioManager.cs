using UnityEngine;
using Cmis3150.Fps.Core;

namespace Cmis3150.Fps.Audio
{
    /// <summary>
    /// SFX + BGM hooks for every required rubric sound event.
    /// Assign AudioClips in the Inspector, or let LevelBootstrap generate beep placeholders.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioSource bgmSource;

        [Header("Clips (placeholders OK)")]
        [SerializeField] AudioClip gunShot;
        [SerializeField] AudioClip grenadeThrow;
        [SerializeField] AudioClip grenadeExplosion;
        [SerializeField] AudioClip hitGuard;
        [SerializeField] AudioClip playerHit;
        [SerializeField] AudioClip treasure;
        [SerializeField] AudioClip bgm;

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
                bgmSource.volume = 0.35f;
            }
        }

        void OnEnable()
        {
            GameEvents.GunFired += OnGun;
            GameEvents.GrenadeThrown += OnThrow;
            GameEvents.GrenadeExploded += OnExplode;
            GameEvents.GuardHit += OnGuardHit;
            GameEvents.PlayerHit += OnPlayerHit;
            GameEvents.TreasurePicked += OnTreasure;
        }

        void OnDisable()
        {
            GameEvents.GunFired -= OnGun;
            GameEvents.GrenadeThrown -= OnThrow;
            GameEvents.GrenadeExploded -= OnExplode;
            GameEvents.GuardHit -= OnGuardHit;
            GameEvents.PlayerHit -= OnPlayerHit;
            GameEvents.TreasurePicked -= OnTreasure;
        }

        void Start()
        {
            if (bgm != null && bgmSource != null)
            {
                bgmSource.clip = bgm;
                bgmSource.Play();
            }
        }

        void OnGun() => Play(gunShot);
        void OnThrow() => Play(grenadeThrow);
        void OnExplode() => Play(grenadeExplosion);
        void OnGuardHit() => Play(hitGuard);
        void OnPlayerHit() => Play(playerHit);
        void OnTreasure() => Play(treasure);

        void Play(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }

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
    }
}
