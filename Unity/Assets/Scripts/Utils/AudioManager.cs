using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.Utils
{
    /// <summary>
    /// 音频管理器 - BGM + 音效 + 3D空间音效
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("音量")]
        [Range(0, 1)] public float MasterVolume = 1f;
        [Range(0, 1)] public float BGMVolume = 0.6f;
        [Range(0, 1)] public float SFXVolume = 0.8f;

        private AudioSource bgmSource;
        private List<AudioSource> sfxSources = new List<AudioSource>();
        private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        private int maxSfxChannels = 16;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // BGM通道
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;

            // SFX通道池
            for (int i = 0; i < maxSfxChannels; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                sfxSources.Add(src);
            }
        }

        void Update()
        {
            bgmSource.volume = BGMVolume * MasterVolume;
        }

        // ===== BGM =====

        public void PlayBGM(string clipName, bool fade = true)
        {
            var clip = GetClip(clipName);
            if (clip == null) return;

            if (fade && bgmSource.isPlaying)
            {
                StartCoroutine(FadeBGM(clip));
            }
            else
            {
                bgmSource.clip = clip;
                bgmSource.volume = BGMVolume * MasterVolume;
                bgmSource.Play();
            }
        }

        public void StopBGM(bool fade = true)
        {
            if (fade) StartCoroutine(FadeOutBGM());
            else bgmSource.Stop();
        }

        private System.Collections.IEnumerator FadeBGM(AudioClip newClip)
        {
            float timer = 0;
            float duration = 0.5f;
            float startVol = bgmSource.volume;

            while (timer < duration)
            {
                bgmSource.volume = Mathf.Lerp(startVol, 0, timer / duration);
                timer += Time.deltaTime;
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.clip = newClip;
            bgmSource.Play();

            timer = 0;
            while (timer < duration)
            {
                bgmSource.volume = Mathf.Lerp(0, BGMVolume * MasterVolume, timer / duration);
                timer += Time.deltaTime;
                yield return null;
            }
        }

        private System.Collections.IEnumerator FadeOutBGM()
        {
            float timer = 0;
            float duration = 0.5f;
            float startVol = bgmSource.volume;

            while (timer < duration)
            {
                bgmSource.volume = Mathf.Lerp(startVol, 0, timer / duration);
                timer += Time.deltaTime;
                yield return null;
            }
            bgmSource.Stop();
        }

        // ===== 2D音效 =====

        public void PlaySFX(string clipName)
        {
            var clip = GetClip(clipName);
            if (clip == null) return;

            var src = GetFreeSFXSource();
            if (src == null) return;

            src.clip = clip;
            src.volume = SFXVolume * MasterVolume;
            src.spatialBlend = 0;
            src.Play();
        }

        // ===== 3D音效 =====

        public void PlaySFX3D(string clipName, Vector3 position, float maxDistance = 20f)
        {
            var clip = GetClip(clipName);
            if (clip == null) return;

            var src = GetFreeSFXSource();
            if (src == null) return;

            src.clip = clip;
            src.volume = SFXVolume * MasterVolume;
            src.spatialBlend = 1;
            src.minDistance = 1f;
            src.maxDistance = maxDistance;
            src.transform.position = position;
            src.Play();
        }

        // ===== 游戏音效快捷方法 =====

        public void PlayHit() => PlaySFX("sfx_hit");
        public void PlayParry() => PlaySFX("sfx_parry");
        public void PlayDodge() => PlaySFX("sfx_dodge");
        public void PlayBlock() => PlaySFX("sfx_block");
        public void PlayKill() => PlaySFX("sfx_kill");
        public void PlaySkill(int index) => PlaySFX($"sfx_skill_{index}");
        public void PlayWeaponSwitch() => PlaySFX("sfx_weapon_switch");
        public void PlayCountdown(int num) => PlaySFX($"sfx_countdown_{num}");
        public void PlayVictory() => PlaySFX("sfx_victory");
        public void PlayDefeat() => PlaySFX("sfx_defeat");

        // ===== 工具 =====

        private AudioSource GetFreeSFXSource()
        {
            foreach (var src in sfxSources)
            {
                if (!src.isPlaying) return src;
            }
            // 没有空闲通道，找最早播放完的
            AudioSource oldest = null;
            float oldestTime = float.MaxValue;
            foreach (var src in sfxSources)
            {
                if (src.time < oldestTime)
                {
                    oldestTime = src.time;
                    oldest = src;
                }
            }
            oldest?.Stop();
            return oldest;
        }

        private AudioClip GetClip(string clipName)
        {
            if (clipCache.TryGetValue(clipName, out var clip)) return clip;
            // 从Resources加载
            clip = Resources.Load<AudioClip>($"Audio/{clipName}");
            if (clip != null) clipCache[clipName] = clip;
            return clip;
        }

        public void SetMasterVolume(float vol) => MasterVolume = Mathf.Clamp01(vol);
        public void SetBGMVolume(float vol) => BGMVolume = Mathf.Clamp01(vol);
        public void SetSFXVolume(float vol) => SFXVolume = Mathf.Clamp01(vol);
    }
}
