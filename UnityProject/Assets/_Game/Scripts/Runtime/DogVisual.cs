using UnityEngine;

namespace UpTogether
{
    /// 강아지 상태를 보고 스프라이트를 갈아끼운다.
    /// 아트는 오른쪽을 보고 있어서 왼쪽은 flipX로 뒤집는다.
    [DefaultExecutionOrder(50)]   // 강아지(10)가 움직인 뒤에 그림을 고른다
    public class DogVisual : MonoBehaviour
    {
        /// 이보다 느리면 서 있는 것으로 본다 (px/frame 0.4 = 원본 걷기 판정과 같음)
        const float WalkThresholdPx = 0.4f;

        // ── 친밀도에 따른 몸짓 ────────────────────────────────
        // 거리와 타이밍만 바꾸면 "AI 가 못 따라온다"로 읽힌다. 겁먹은 프레임이
        // 시트에 없으므로(walk/idle/jump/fall/land/held/happy 13장이 전부),
        // 있는 그림 그대로 떨림·웅크림·시선으로 상태를 보이게 만든다.
        /// 떨림 폭(px). 48px 강아지라 1px 도 눈에 보인다.
        const float TremblePx = 1.2f;
        /// 떨리는 속도(Hz)
        const float TrembleHz = 11f;
        /// 친밀도 0 에서 세로로 눌리는 비율. 피벗이 발밑이라 세로만 줄이면 웅크려 보인다.
        const float CowerScaleY = 0.9f;
        /// 이 아래면 가만히 있을 때 플레이어를 보지 않는다
        const float LookAwayTrust = 0.45f;
        /// 이 위로는 서 있을 때 꼬리를 흔든다 (Bond 의 '한 몸' 82 와 같은 지점)
        const float HappyTrust = 0.82f;

        public CharacterSpriteSet spriteSet;
        public SpriteRenderer target;
        public DogController dog;
        public CharacterBody body;

        SpriteClip current;
        int frame;

        /// 지금 재생 중인 클립 이름 (PlayerVisual 과 같은 창구)
        public string CurrentClip => current?.name;

        // 워프로 나타날 때 살짝 부풀었다 돌아오는 연출
        const float PopDuration = 0.16f;
        float popUntil = -1f;
        float elapsed;
        bool wasGrounded = true;
        float landLockUntil;
        Vector3 basePos;

        void Awake()
        {
            if (spriteSet == null || target == null || body == null) { enabled = false; return; }
            basePos = target.transform.localPosition;
            Play("idle");
        }

        /// 워프 직후에 부른다. 작게 나타났다 제 크기로 돌아온다.
        public void PopIn() => popUntil = Time.time + PopDuration;

        void Update()
        {
            string want = Choose();
            if (want != null && (current == null || current.name != want)) Play(want);
            Advance(Time.deltaTime);

            if (body.face != 0) target.flipX = body.face < 0;

            // 팝: 0.6배에서 1배로. 끝나면 건드리지 않는다.
            if (popUntil > 0f)
            {
                float left = popUntil - Time.time;
                if (left <= 0f) { target.transform.localScale = Vector3.one; popUntil = -1f; }
                else
                {
                    float t = 1f - left / PopDuration;
                    float k = Mathf.Lerp(0.6f, 1f, Mathf.Sin(t * Mathf.PI * 0.5f));
                    target.transform.localScale = new Vector3(k, k, 1f);
                    return;                      // 팝이 스케일을 쓰는 동안은 몸짓을 겹치지 않는다
                }
            }

            ApplyMood();
        }

        /// 떨림·웅크림·시선 회피. 전부 스프라이트 자식 트랜스폼에만 건다 —
        /// 몸통(루트)을 움직이면 안긴 위치와 발판 판정이 어긋난다.
        void ApplyMood()
        {
            if (dog == null) return;

            // 안겨 있는 동안은 PlayerVisual 이 자리를 정확히 잡는다. 비켜 준다.
            if (dog.IsClinging)
            {
                target.transform.localPosition = basePos;
                target.transform.localScale = Vector3.one;
                return;
            }

            float trust = dog.Trust;
            // 움츠러든 동안은 가장 겁먹은 상태로 보인다 (친밀도가 높아도)
            float fear = dog.InFlashback ? 1f : 1f - trust;
            bool still = body.grounded && Mathf.Abs(body.vx) <= Px.V(WalkThresholdPx);

            // 떨림 — 서 있을 때만. 걸으면 묻혀서 안 보이고 지저분해진다.
            float amp = Px.U(TremblePx) * fear;
            float dx = still && amp > 0f
                     ? Mathf.Sin(Time.time * TrembleHz * Mathf.PI * 2f) * amp
                     : 0f;
            target.transform.localPosition = basePos + new Vector3(dx, 0f, 0f);

            target.transform.localScale = new Vector3(1f, Mathf.Lerp(CowerScaleY, 1f, trust), 1f);

            // 눈을 피한다 — 가만히 있을 때 플레이어 반대쪽을 본다.
            // 아트가 오른쪽을 보고 있으니, 플레이어가 오른쪽이면 뒤집어 왼쪽을 보게 한다.
            if (still && trust < LookAwayTrust && dog.player != null)
            {
                float toPlayer = dog.player.Body.X - body.X;
                if (Mathf.Abs(toPlayer) > 0.01f) target.flipX = toPlayer > 0f;
            }
        }

        string Choose()
        {
            // 착지 연출은 짧게 끊기지 않도록 시간 동안 붙잡는다
            if (Time.time < landLockUntil) return null;

            if (dog != null && dog.IsClinging) { wasGrounded = false; return "held"; }

            if (!body.grounded)
            {
                wasGrounded = false;
                return body.vy > 0f ? "jump" : "fall";
            }

            // 공중에서 방금 내려왔으면 착지 한 장
            if (!wasGrounded)
            {
                wasGrounded = true;
                var land = spriteSet.Find("land");
                landLockUntil = Time.time + (land != null ? land.TotalDuration : 0.12f);
                return "land";
            }

            if (Mathf.Abs(body.vx) > Px.V(WalkThresholdPx)) return "walk";

            // 많이 친해졌으면 서 있을 때 꼬리를 흔든다.
            // happy(11-12번)는 시트에 그려져 있는데 지금까지 아무 데서도 안 쓰였다.
            if (dog != null && dog.Trust >= HappyTrust && spriteSet.Find("happy") != null)
                return "happy";

            return "idle";
        }

        void Play(string clipName)
        {
            var c = spriteSet.Find(clipName);
            if (c == null || c.Length == 0) return;
            current = c;
            frame = 0;
            elapsed = 0f;
            Show();
        }

        void Advance(float dt)
        {
            if (current == null || current.Length <= 1) return;

            float d = current.Duration(frame);
            if (d <= 0f) return;            // 고정 프레임

            elapsed += dt;
            while (elapsed >= d)
            {
                elapsed -= d;
                frame++;
                if (frame >= current.Length)
                {
                    if (!current.loop) { frame = current.Length - 1; elapsed = 0f; break; }
                    frame = 0;
                }
                d = current.Duration(frame);
                if (d <= 0f) break;
            }
            Show();
        }

        void Show()
        {
            var s = spriteSet.Frame(current.frames[frame]);
            if (s != null) target.sprite = s;
        }
    }
}
