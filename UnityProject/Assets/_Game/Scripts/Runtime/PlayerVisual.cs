using UnityEngine;

namespace UpTogether
{
    /// 주인공 상태를 보고 스프라이트를 갈아끼운다.
    ///
    /// 안기(hold)는 3겹이다: 주인공 본체 → 강아지 → 앞팔 오버레이.
    /// 왼쪽을 볼 때는 셋 다 flipX 하고 강아지의 x 오프셋도 부호를 뒤집어야 한다.
    /// 하나라도 빠뜨리면 팔이 엉뚱한 데 붙는다.
    [DefaultExecutionOrder(60)]   // 강아지(10)·DogVisual(50) 다음에 위치를 확정한다
    public class PlayerVisual : MonoBehaviour
    {
        const float WalkThresholdPx = 0.4f;
        /// 강아지를 평소엔 주인공 뒤에, 안았을 때는 앞에 둔다
        const int DogOrderBehind = 9;
        const int DogOrderHeld = 11;

        public CharacterSpriteSet spriteSet;
        public SpriteRenderer target;
        public SpriteRenderer overlay;      // 앞팔. 안았을 때만 켠다
        public PlayerController player;
        public CharacterBody body;

        [Header("강아지 — 안았을 때 위치를 여기서 잡는다")]
        public DogController dog;
        public SpriteRenderer dogRenderer;
        public CharacterSpriteSet dogSpriteSet;
        public DogVisual dogVisual;

        SpriteClip current;
        int frame;

        /// 테스트·디버그용. 지금 재생 중인 클립 이름.
        public string CurrentClip => current?.name;
        float elapsed;
        bool wasGrounded = true;
        float landLockUntil;

        void Awake()
        {
            if (spriteSet == null || target == null || body == null) { enabled = false; return; }
            if (overlay != null) overlay.enabled = false;
            Play("idle");
        }

        void LateUpdate()
        {
            string want = Choose();
            if (want != null && (current == null || current.name != want)) Play(want);
            Advance(Time.deltaTime);

            bool flip = body.face < 0;
            target.flipX = flip;

            bool holding = current != null && current.dogFrame >= 0;
            ApplyHold(holding, flip);
        }

        string Choose()
        {
            if (Time.time < landLockUntil) return null;

            bool holding = dog != null && dog.IsClinging;

            if (!body.grounded)
            {
                wasGrounded = false;
                if (holding) return "hold_fall";
                return body.vy > 0f ? "jump" : "fall";
            }

            if (!wasGrounded)
            {
                wasGrounded = true;
                if (!holding)
                {
                    var land = spriteSet.Find("land");
                    landLockUntil = Time.time + (land != null ? land.TotalDuration : 0.12f);
                    return "land";
                }
            }

            if (holding) return "hold_idle";
            return Mathf.Abs(body.vx) > Px.V(WalkThresholdPx) ? "walk" : "idle";
        }

        /// 안았을 때만 강아지를 주인공에 고정하고 앞팔을 켠다.
        void ApplyHold(bool holding, bool flip)
        {
            if (overlay != null)
            {
                overlay.enabled = holding;
                if (holding)
                {
                    var s = spriteSet.Frame(current.OverlayFrame(frame));
                    if (s != null) overlay.sprite = s;
                    overlay.flipX = flip;
                }
            }

            if (dogRenderer != null)
                dogRenderer.sortingOrder = holding ? DogOrderHeld : DogOrderBehind;

            if (!holding)
            {
                if (dogVisual != null) dogVisual.enabled = true;
                return;
            }

            // 안고 있는 동안은 DogVisual 이 프레임을 고르지 않게 하고 직접 붙인다
            if (dogVisual != null) dogVisual.enabled = false;
            if (dogSpriteSet != null && dogRenderer != null)
            {
                var ds = dogSpriteSet.Frame(current.dogFrame);
                if (ds != null) dogRenderer.sprite = ds;
                dogRenderer.flipX = flip;
            }

            if (dog != null)
            {
                var off = current.dogPivotOffsetPx;
                if (flip) off.x = -off.x;     // ★ 왼쪽을 보면 x 도 뒤집는다
                dog.transform.position = new Vector3(
                    body.X + Px.U(off.x),
                    body.Y + Px.U(off.y),
                    dog.transform.position.z);
            }
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
            if (d <= 0f) return;

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
