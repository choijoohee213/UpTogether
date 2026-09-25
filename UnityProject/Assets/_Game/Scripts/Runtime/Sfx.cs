using UnityEngine;

namespace UpTogether
{
    /// 효과음·배경음 재생기. 씬을 넘어가도 하나만 살아남아 BGM 이 끊기지 않게 한다.
    /// 클립은 빌드 때 AudioSetup 이 채워 넣는다 (모든 씬에 붙지만 첫 번째만 남는다).
    [DefaultExecutionOrder(-200)]
    public class Sfx : MonoBehaviour
    {
        public static Sfx I;

        [Header("BGM")] public AudioClip bgm;
        [Header("SFX")]
        public AudioClip jump, land, whine, catchDog;
        public AudioClip bondUp, bondDown, reward, clear;
        public AudioClip uiClick, uiPopup;
        public AudioClip step1, step2, step3;

        [Range(0f, 1f)] public float bgmVolume = 0.32f;

        AudioSource music, oneShot;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            music = gameObject.AddComponent<AudioSource>();
            music.loop = true; music.playOnAwake = false; music.volume = bgmVolume;
            oneShot = gameObject.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;

            if (bgm != null) { music.clip = bgm; music.Play(); }
        }

        void One(AudioClip c, float v)
        {
            if (c != null && oneShot != null) oneShot.PlayOneShot(c, v);
        }

        public void Jump() => One(jump, 0.6f);
        public void Land() => One(land, 0.6f);
        public void Whine() => One(whine, 0.8f);
        public void Catch() => One(catchDog, 0.9f);
        public void BondUp() => One(bondUp, 0.7f);
        public void BondDown() => One(bondDown, 0.7f);
        public void Reward() => One(reward, 0.8f);
        public void Clear() => One(clear, 0.9f);
        public void UiClick() => One(uiClick, 0.7f);
        public void UiPopup() => One(uiPopup, 0.7f);

        int stepToggle;
        public void Step()
        {
            // 세 발소리를 번갈아 — 같은 소리 반복보다 자연스럽다
            var c = stepToggle == 0 ? step1 : stepToggle == 1 ? step2 : step3;
            stepToggle = (stepToggle + 1) % 3;
            One(c, 0.35f);
        }
    }
}
