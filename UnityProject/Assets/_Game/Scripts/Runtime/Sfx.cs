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
        public AudioClip barkSmall, barkBig;

        [Range(0f, 1f)] public float bgmVolume = 0.32f;

        AudioSource music, oneShot;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            // 배치모드(CLI 테스트·빌드)에서는 소리를 내지 않는다.
            // 테스트를 돌릴 때마다 BGM이 스피커로 울린다.
            if (Application.isBatchMode) AudioListener.volume = 0f;

            // 리스너가 없으면 아무 소리도 나오지 않는다 — 씬 카메라는 스크립트로 만들어 붙어 있지 않다
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();

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
        public void BarkSmall() => One(barkSmall, 0.7f);
        public void BarkBig() => One(barkBig, 0.85f);
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
