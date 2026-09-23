using UnityEngine;

namespace UpTogether
{
    /// 강아지. 메이플스토리 펫 방식이다 (레퍼런스 영상 분석).
    ///
    /// ★ 중력도 발판 충돌도 없다 ★
    /// 영상에서 펫은 발판이 없는 허공에 떠서 플레이어 쪽으로 가로질러 온다.
    /// 그래서 떨어질 수도, 막힐 수도, 앞서 나갈 수도 없다.
    /// 발판을 밟고 따라오게 만들었을 때 끝없이 나던 문제들이 구조적으로 사라진다.
    ///
    /// 목표는 '플레이어의 발밑'이라, 평지에서는 걸어다니는 것처럼 보이고
    /// 플레이어가 뛰어오를 때만 떠서 따라온다.
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(CharacterBody))]
    public class DogController : MonoBehaviour
    {
        /// 이 안에 들어오면 멈춘다. 없으면 제자리에서 덜덜 떤다.
        const float SettleDistancePx = 6f;
        /// 멀수록 빨리 따라온다. 이 거리에서 최고 속도.
        const float FullSpeedDistancePx = 220f;
        /// 걷는 것처럼 보이게 하는 최저 속도
        const float MinSpeedPx = 2.0f;

        public Tuning tuning;
        public StageRunner stage;
        public PlayerController player;

        public bool IsClinging { get; private set; }
        /// 계측용. 너무 멀어 즉시 붙은 횟수.
        public int TeleportCount { get; private set; }

        CharacterBody body;
        float bobPhase;

        void Awake()
        {
            if (stage == null || stage.Data == null) { enabled = false; return; }
            body = GetComponent<CharacterBody>();
            body.tuning = tuning;
            body.Bind(stage);
            // 물리를 쓰지 않는다. 애니메이션이 걷기/대기를 고르도록 접지 상태만 유지한다.
            body.grounded = true;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var p = player.Body;

            if (UpdateCling(dt, p)) return;
            Follow(dt, p);
        }

        /// 크게 떨어질 때 달려와 품에 안긴다. 이 게임만의 동작이라 그대로 둔다.
        bool UpdateCling(float dt, CharacterBody p)
        {
            // 빠르게 내려오는 것만으로는 부족하다 — 높이 뛰었다 내려오는 것도 그 속도를 넘긴다.
            // 마지막으로 발을 붙였던 높이보다 확실히 아래여야 진짜 낙하다.
            bool reallyFalling = !p.grounded && p.vy < -tuning.DogClingFallV
                                 && p.Y < player.LastGroundedY - tuning.DogClingMinDropU;
            if (reallyFalling) IsClinging = true;
            if (!IsClinging) return false;

            float k = Px.Smoothing(tuning.dogClingLerp, dt);
            body.X += ((p.X - tuning.DogClingOffXU * p.face) - body.X) * k;
            body.Y += ((p.Y + tuning.DogClingOffYU) - body.Y) * k;
            body.face = p.face;
            body.vx = 0f; body.vy = 0f;
            body.grounded = true;
            if (p.grounded) IsClinging = false;
            return true;
        }

        /// 플레이어 발밑을 목표로 곧장 간다. 발판은 무시한다.
        void Follow(float dt, CharacterBody p)
        {
            Vector2 target = new Vector2(p.X - tuning.DogTrailU * p.face, p.Y);
            Vector2 here = new Vector2(body.X, body.Y);
            Vector2 to = target - here;
            float dist = to.magnitude;

            if (dist > tuning.DogWarpDistanceU)
            {
                // 화면 밖으로 벗어날 만큼 멀면 그냥 옆에 놓는다
                body.Teleport(target.x, target.y);
                TeleportCount++;
                body.grounded = true;
                return;
            }

            float settle = Px.U(SettleDistancePx);
            if (dist < settle)
            {
                body.vx = 0f;
                Bob(dt, false);
                return;
            }

            // 멀수록 빠르게. 가까우면 살살 붙어서 덜덜 떨지 않는다.
            float t = Mathf.Clamp01(dist / Px.U(FullSpeedDistancePx));
            float speed = Mathf.Lerp(Px.V(MinSpeedPx), tuning.DogSpeedV, t);
            Vector2 next = here + to / dist * Mathf.Min(speed * dt, dist);

            body.vx = (next.x - here.x) / dt;   // 걷기 애니메이션 판정에 쓰인다
            if (Mathf.Abs(to.x) > settle) body.face = to.x > 0f ? 1 : -1;

            body.X = next.x;
            body.Y = next.y;
            body.vy = 0f;
            body.grounded = true;
            Bob(dt, Mathf.Abs(body.vx) > Px.V(0.4f));
        }

        /// 떠 있을 때 살짝 위아래로 흔들어 붕 떠 보이게 한다.
        void Bob(float dt, bool moving)
        {
            bobPhase += dt * (moving ? 9f : 4f);
            float amount = Px.U(moving ? 1.5f : 1.0f);
            transform.position = new Vector3(
                body.X, body.Y + Mathf.Sin(bobPhase) * amount, transform.position.z);
        }
    }
}
