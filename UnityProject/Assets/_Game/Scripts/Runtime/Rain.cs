using UnityEngine;

namespace UpTogether
{
    /// 프롤로그에 내리는 비. 그림에는 비가 없다 — 그리면 컷마다 다시 그려야 하고
    /// 멈춰 있는 비는 비처럼 안 보인다. 그래서 코드로 뿌린다.
    ///
    /// 카메라가 컷마다 줌을 크게 바꾸므로(0.7 ~ 1.9배) 뿜는 자리를 고정하면
    /// 넓은 컷에서는 화면을 다 못 덮고 좁은 컷에서는 한참 위에서 떨어진다.
    /// 매 프레임 지금 시야에 맞춰 띠를 다시 재운다.
    [RequireComponent(typeof(ParticleSystem))]
    public class Rain : MonoBehaviour
    {
        public Camera cam;
        /// 시야 위로 이만큼 띄워서 뿜는다
        public float above = 0.6f;

        /// 화면에 보이는 빗줄기 굵기·길이 (게임 px). 줌과 무관하게 이만큼 유지한다.
        const float WidthPx = 2f;
        const float LengthPx = 34f;
        /// 한 화면에 보일 빗줄기 수
        const float DropsOnScreen = 160f;
        /// 옆으로 밀리는 속도 (velocityOverLifetime.x 의 크기)
        const float SideSpeed = 2.1f;
        /// 시야 650px 일 때 1 유닛 = 100px 이므로 반높이 1 유닛당 325px 이다
        const float PxPerHalfUnit = 325f;

        /// 빗소리 크기. 글을 읽는 장면이라 깔리는 정도로만 둔다.
        public float volume = 0.33f;
        /// 그치고 잦아드는 데 걸리는 시간 (초)
        const float FadeSeconds = 1.6f;

        ParticleSystem ps;
        ParticleSystem.ShapeModule shape;
        ParticleSystemRenderer rend;
        AudioSource audioSource;
        float wantVolume;

        void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            shape = ps.shape;
            rend = GetComponent<ParticleSystemRenderer>();
            if (cam == null) cam = Camera.main;

            // 빗소리 파일이 없어서 만들어 쓴다. 비는 결국 걸러진 잡음이라
            // 녹음을 구하는 것보다 이게 확실하고, 그림의 비와 같은 방식이다.
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = MakeRainClip();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 0f;
            wantVolume = volume;
            if (!Application.isBatchMode) audioSource.Play();
        }

        void Update()
        {
            if (audioSource == null) return;
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, wantVolume,
                                                   volume * Time.unscaledDeltaTime / FadeSeconds);
        }

        /// 빗소리 한 토막. 흰 잡음을 두 번 걸러 쉿 소리를 눅눅하게 만들고,
        /// 아주 느린 흔들림을 얹어 "일정한 잡음"이 아니라 비처럼 들리게 한다.
        /// 잡음이라 이음새가 안 들려서 끝을 앞과 겹쳐 잇기만 하면 된다.
        static AudioClip MakeRainClip()
        {
            const int rate = 22050, seconds = 4;
            const int n = rate * seconds, blend = rate / 4;
            var data = new float[n];
            var rnd = new System.Random(20261111);
            float lp = 0f, lp2 = 0f;

            for (int i = 0; i < n; i++)
            {
                float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
                lp += (white - lp) * 0.5f;
                lp2 += (lp - lp2) * 0.5f;
                float t = i / (float)rate;
                float sway = 1f + 0.18f * Mathf.Sin(t * 0.9f) + 0.1f * Mathf.Sin(t * 2.3f);
                data[i] = lp2 * 2.4f * sway;
            }

            // 끝 토막을 앞 토막에 겹쳐 넣어 루프 이음새를 없앤다
            for (int i = 0; i < blend; i++)
            {
                float k = i / (float)blend;
                data[i] = Mathf.Lerp(data[n - blend + i], data[i], k);
            }

            var clip = AudioClip.Create("rain", n - blend, 1, rate, false);
            var loop = new float[n - blend];
            System.Array.Copy(data, loop, n - blend);
            clip.SetData(loop, 0);
            return clip;
        }

        void LateUpdate()
        {
            if (cam == null) return;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            // 카메라 바로 위에 가로로 긴 띠를 두고 거기서 떨어뜨린다
            transform.position = new Vector3(cam.transform.position.x,
                                             cam.transform.position.y + halfH + above,
                                             0f);

            // 굵기·길이는 월드 크기라서 그냥 두면 줌에 따라 2.7배씩 달라진다.
            // 당겨 본 컷에서 빗줄기가 손가락만 해지므로 시야에 맞춰 줄인다.
            var main = ps.main;
            float unitsPerPx = halfH / PxPerHalfUnit;
            float speed = Mathf.Max(1f, main.startSpeed.constant);
            main.startSize = WidthPx * unitsPerPx;
            if (rend != null) rend.velocityScale = LengthPx * unitsPerPx / speed;

            // 화면을 가로지를 만큼만 살려 둔다. 수명을 길게 두면 대부분이
            // 화면 한참 아래에서 떨어지고 있어서, 양을 올려도 화면은 안 찬다.
            float life = (halfH * 2f + above + 1f) / speed;
            main.startLifetime = life;

            // 비스듬히 내리므로 수명 동안 옆으로 밀린 만큼 넓게 뿜어야
            // 바람이 불어오는 쪽 가장자리가 비지 않는다
            float margin = SideSpeed * life + 0.3f;
            shape.scale = new Vector3((halfW + margin) * 2f, 0.1f, 1f);

            // 보이는 빗줄기 수를 일정하게 — 시야가 바뀌어도 같은 빗속이어야 한다.
            // 뿜는 띠가 화면보다 넓으니 그 비율만큼 더 뿜어야 화면이 찬다.
            var emission = ps.emission;
            emission.rateOverTime = DropsOnScreen / life * (halfW + margin) / Mathf.Max(0.1f, halfW);
        }

        /// 컷마다 켜고 끈다 (마지막 컷은 비가 그친 하늘이라 끈다).
        /// 소리는 뚝 끊지 않고 잦아들게 둔다 — 비는 그렇게 그친다.
        public void SetFalling(bool on)
        {
            if (ps == null) ps = GetComponent<ParticleSystem>();
            var emission = ps.emission;
            emission.enabled = on;
            wantVolume = on ? volume : 0f;
        }

        /// 지금 빗소리 크기 (테스트가 본다)
        public float Loudness => audioSource != null ? audioSource.volume : 0f;
    }
}
