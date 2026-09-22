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

        public CharacterSpriteSet spriteSet;
        public SpriteRenderer target;
        public DogController dog;
        public CharacterBody body;

        SpriteClip current;
        int frame;
        float elapsed;
        bool wasGrounded = true;
        float landLockUntil;

        void Awake()
        {
            if (spriteSet == null || target == null || body == null) { enabled = false; return; }
            Play("idle");
        }

        void Update()
        {
            string want = Choose();
            if (want != null && (current == null || current.name != want)) Play(want);
            Advance(Time.deltaTime);

            if (body.face != 0) target.flipX = body.face < 0;
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

            return Mathf.Abs(body.vx) > Px.V(WalkThresholdPx) ? "walk" : "idle";
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
