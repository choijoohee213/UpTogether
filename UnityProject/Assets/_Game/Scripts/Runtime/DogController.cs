using UnityEngine;

namespace UpTogether
{
    /// 강아지. 메이플스토리 펫 방식이다 (레퍼런스 영상 분석).
    ///
    /// 걷고 뛴다. 다만 ★경로를 계산하지 않는다★.
    /// 어느 발판을 밟고 어떻게 올라갈지 따지지 않고, 그냥 플레이어 쪽으로 걷다가
    /// 위에 있으면 폴짝 뛴다. 성공하든 못 하든 상관없다 —
    /// 못 따라가면 워프가 덮어준다. 그래서 갇히거나 앞서 나갈 일이 없다.
    ///
    /// 예전에 발판을 골라 경로를 짜게 만들었더니 헛뛰고 떨어지거나,
    /// 한 칸씩 기계적으로 기어오르거나, 플레이어보다 먼저 도착하는 문제가 끝없이 나왔다.
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(CharacterBody))]
    public class DogController : MonoBehaviour
    {
        /// 이 안에 들어오면 멈춘다. 없으면 제자리에서 덜덜 떤다.
        const float FollowDeadzonePx = 10f;
        /// 발판 가장자리에서 이만큼 안쪽까지만 걸어간다
        const float EdgeInsetPx = 6f;
        /// 연달아 뛰지 않도록 두는 간격
        const float HopInterval = 0.45f;
        /// 워프 직후 잠깐은 다시 워프하지 않는다
        const float WarpCooldown = 0.3f;

        public Tuning tuning;
        public StageRunner stage;
        public PlayerController player;

        [Header("워프 연출")]
        public Puffs puffs;
        public DogVisual visual;

        public bool IsClinging { get; private set; }
        /// 계측용
        public int TeleportCount { get; private set; }

        CharacterBody body;
        float farSince = -1f;
        float lastWarp = -99f;
        float lastHop = -99f;

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

        /// 플레이어 뒤를 따라 걷고, 위에 있으면 폴짝 뛴다. 어디에 착지할지는 따지지 않는다.
        void Follow(float dt, CharacterBody p)
        {
            float want = p.X - tuning.DogTrailU * p.face;

            // 딛고 선 발판 밖으로는 걸어 나가지 않는다.
            // 목표가 발판 끝을 넘어가면 그대로 걸어 나가 떨어진다.
            if (body.groundIndex >= 0)
            {
                var cur = stage.GetPlatform(body.groundIndex);
                float inset = Px.U(EdgeInsetPx);
                float lo = cur.left + inset, hi = cur.right - inset;
                if (lo <= hi) want = Mathf.Clamp(want, lo, hi);
            }

            float dead = Px.U(FollowDeadzonePx);
            int dir = body.X > want + dead ? -1 : (body.X < want - dead ? 1 : 0);
            body.Step(dt, dir, tuning.DogSpeedV);

            // 플레이어가 위에 있으면 뛴다. 착지 지점은 따지지 않는다 —
            // 못 올라가면 워프가 덮어준다.
            bool playerAbove = player.LastGroundedY - body.Y > tuning.DogJumpTrigU;
            if (body.grounded && playerAbove && Time.time - lastHop > HopInterval)
            {
                body.vy = tuning.DogJumpV;
                lastHop = Time.time;
            }
        }

        /// 같은 높이로 못 오는 상태가 잠깐 이어지면 플레이어 옆으로 옮겨간다.
        void WarpIfLeftBehind(CharacterBody p)
        {
            // 플레이어 점프 정점으로 판단하면 안 된다. 마지막으로 디딘 발판을 기준으로 본다.
            float dx = Mathf.Abs(p.X - body.X);
            float dy = Mathf.Abs(player.LastGroundedY - body.Y);
            bool far = dy > tuning.DogWarpHeightU || dx > tuning.DogWarpDistanceU;

            if (!far) { farSince = -1f; return; }
            if (farSince < 0f) farSince = Time.time;
            if (Time.time - farSince < tuning.dogWarpDelay) return;
            if (Time.time - lastWarp < WarpCooldown) return;

            // 사라진 자리와 나타난 자리 양쪽에 먼지를 남긴다.
            // 아무 연출 없이 옮기면 툭 하고 생겨난 것처럼 보인다.
            var from = new Vector2(body.X, body.Y);
            var to = new Vector2(p.X - Px.U(24f) * p.face, p.Y);
            puffs?.Burst(from, 8, 20f, 4f, 2f);

            body.Teleport(to.x, to.y);
            body.face = p.face;

            puffs?.Burst(to, 10, 24f, 4.5f, 2.4f);
            visual?.PopIn();
            TeleportCount++;
            farSince = -1f;
            lastWarp = Time.time;
        }
    }
}
