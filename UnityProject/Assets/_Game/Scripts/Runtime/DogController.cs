using UnityEngine;

namespace UpTogether
{
    /// 강아지. 메이플스토리 펫 방식이다.
    ///
    /// ★ 스스로 경로를 판단하지 않는다 ★
    /// 발판을 고르고 점프해서 따라오게 만들었더니 문제가 끝없이 나왔다 —
    /// 헛뛰고 떨어지거나, 한 칸씩 기계적으로 기어오르거나, 플레이어보다 먼저 도착했다.
    /// 펫은 판단하지 않는다. 그냥 옆에서 따라 걷고, 못 따라가면 워프한다.
    /// 그래서 앞서 나갈 수도, 실패할 수도 없다.
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(CharacterBody))]
    public class DogController : MonoBehaviour
    {
        /// 이 안에 들어오면 멈춘다. 없으면 제자리에서 덜덜 떤다.
        const float FollowDeadzonePx = 10f;
        /// 워프한 직후 잠깐은 다시 워프하지 않는다
        const float WarpCooldown = 0.25f;

        public Tuning tuning;
        public StageRunner stage;
        public PlayerController player;

        public bool IsClinging { get; private set; }
        /// 계측용
        public int TeleportCount { get; private set; }

        CharacterBody body;
        float farSince = -1f;      // 멀어진 채로 유지된 시작 시각
        float lastWarp = -99f;

        void Awake()
        {
            if (stage == null || stage.Data == null) { enabled = false; return; }
            body = GetComponent<CharacterBody>();
            body.tuning = tuning;
            body.Bind(stage);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var p = player.Body;

            if (UpdateCling(dt, p)) return;

            Follow(dt, p);
            WarpIfLeftBehind(p);
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
            if (p.grounded) IsClinging = false;
            return true;
        }

        /// 플레이어 뒤를 따라 걷는다. 점프는 하지 않는다.
        void Follow(float dt, CharacterBody p)
        {
            float want = p.X - tuning.DogTrailU * p.face;

            // 딛고 선 발판 밖으로는 걸어 나가지 않는다.
            // 목표가 발판 끝을 넘어가면 그대로 걸어 나가 떨어진다.
            if (body.groundIndex >= 0)
            {
                var cur = stage.GetPlatform(body.groundIndex);
                float inset = Px.U(6f);
                float lo = cur.left + inset, hi = cur.right - inset;
                if (lo <= hi) want = Mathf.Clamp(want, lo, hi);
            }

            float dead = Px.U(FollowDeadzonePx);
            int dir = body.X > want + dead ? -1 : (body.X < want - dead ? 1 : 0);
            body.Step(dt, dir, tuning.DogSpeedV);
        }

        /// 같은 자리로 못 가는 상태가 잠깐 이어지면 플레이어 옆으로 옮겨간다.
        void WarpIfLeftBehind(CharacterBody p)
        {
            // 플레이어가 점프 중일 때의 순간적인 높이 차이로 판단하면 안 된다.
            // 마지막으로 디딘 발판을 기준으로 본다.
            float dx = Mathf.Abs(p.X - body.X);
            float dy = Mathf.Abs(player.LastGroundedY - body.Y);
            bool far = dy > tuning.DogWarpHeightU || dx > tuning.DogWarpDistanceU;

            if (!far) { farSince = -1f; return; }

            if (farSince < 0f) farSince = Time.time;
            if (Time.time - farSince < tuning.dogWarpDelay) return;
            if (Time.time - lastWarp < WarpCooldown) return;

            body.Teleport(p.X - Px.U(24f) * p.face, p.Y);
            body.face = p.face;
            TeleportCount++;
            farSince = -1f;
            lastWarp = Time.time;
        }
    }
}
