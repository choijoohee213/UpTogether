using UnityEngine;

namespace UpTogether
{
    /// 강아지. 뒤에서 따라오다가, 플레이어가 크게 떨어지면 달려와 품에 안긴다.
    /// 이 게임의 정서가 전부 여기에 달려 있어서 수치를 함부로 바꾸지 말 것.
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(CharacterBody))]
    public class DogController : MonoBehaviour
    {
        const float FollowDeadzonePx = 8f;   // 이 안에 들어오면 멈춘다. 없으면 덜덜 떤다.
        const float CatchUpDistancePx = 130f;
        const float CatchUpJumpChance = 0.06f; // 프레임당

        // ── 발판 타고 오르기 ──────────────────────────────
        /// 이보다 낮으면 그냥 걸어서 올라갈 수 있는 턱으로 본다
        const float MinRisePx = 20f;
        /// 목표 발판 가장자리에서 이만큼 안쪽을 노린다. 모서리를 노리면 자주 흘러내린다.
        /// 너무 크면 점프 사거리를 깎아 갈 수 있는 발판을 못 간다고 판단한다
        /// (14px 일 때 필요 86px vs 가능 86px 로 아슬아슬하게 막혔다).
        const float EdgeInsetPx = 6f;
        /// 이 안에 들어오면 뛴다
        const float AimTolerancePx = 10f;
        /// 목표 발판 위로 이만큼 여유를 두고 뛴다
        const float ClearancePx = 12f;
        /// 따라잡을 때 이만큼 아래에서 뛰어올라 들어온다.
        /// 옆에 그냥 생겨나면 "갑자기 팍 나타난" 느낌이 난다.
        const float CatchUpEntryDropPx = 90f;

        public Tuning tuning;
        public StageRunner stage;
        public PlayerController player;

        public bool IsClinging { get; private set; }

        /// 계측용. 실제로 순간이동한 횟수와, 화면 안이라 참은 횟수.
        public int TeleportCount { get; private set; }
        public int SuppressedTeleportCount { get; private set; }
        /// 플레이어를 따라 같이 뛴 횟수. 혼자 뛴 것과 구분해야 테스트가 흔들리지 않는다.
        public int SyncJumpCount { get; private set; }

        // 진단용 — 지금 어떤 발판을 노리고 있는지
        public bool HasStep => hasStep;
        public float StepY => stepY;
        public float StepAimX => stepStandX;

        CharacterBody body;

        // 한 번 정한 목표 발판은 착지할 때까지 붙든다.
        // 공중에서 매 프레임 다시 고르면 방향이 흔들려 착지를 놓친다.
        bool hasStep;
        float stepY;
        /// 뛰기 전에 서 있어야 할 x (지금 발판 위). 여기서 벗어나면 발판 밖으로 떨어진다.
        float stepStandX;
        /// 공중에서 향해야 할 x (목표 발판 위).
        float stepLandX;

        void Awake()
        {
            if (stage == null || stage.Data == null) { enabled = false; return; }
            body = GetComponent<CharacterBody>();
            body.tuning = tuning;
            body.Bind(stage);
            if (player != null) player.Jumped += OnPlayerJumped;
        }

        void OnDestroy()
        {
            if (player != null) player.Jumped -= OnPlayerJumped;
        }

        /// 플레이어가 뛰는 순간 같이 뛴다.
        /// 예전에는 플레이어가 34px 위로 올라간 뒤에야 반응해서 항상 한 박자 늦었다.
        void OnPlayerJumped()
        {
            if (IsClinging || !body.grounded) return;
            if (Mathf.Abs(player.Body.X - body.X) > tuning.DogSyncJumpRangeU) return;

            // 목표 발판을 먼저 잡는다. 이게 없으면 그냥 위로만 뛰었다가
            // 착지할 곳이 없어 떨어진다 — 플레이어를 따라 뛸 때 제일 자주 나던 문제.
            if (!hasStep) hasStep = TryPickStep(player.Body);

            body.vy = hasStep
                ? JumpSpeedFor(stepY - body.Y + Px.U(ClearancePx))
                : tuning.DogJumpV;
            SyncJumpCount++;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var p = player.Body;

            float dx = p.X - body.X;
            float dy = p.Y - body.Y;   // 양수 = 플레이어가 위에 있다

            // 빠르게 내려오는 것만으로는 부족하다. 높이 뛰었다 제자리로 내려오는 것도
            // 금방 이 속도를 넘긴다. 마지막으로 서 있던 높이보다 확실히 아래여야 진짜 낙하다.
            bool fallingBelowGround = p.Y < player.LastGroundedY - tuning.DogClingMinDropU;
            if (!p.grounded && p.vy < -tuning.DogClingFallV && fallingBelowGround && !IsClinging)
                IsClinging = true;

            if (IsClinging)
            {
                float k = Px.Smoothing(tuning.dogClingLerp, dt);
                body.X += ((p.X - tuning.DogClingOffXU * p.face) - body.X) * k;
                body.Y += ((p.Y + tuning.DogClingOffYU) - body.Y) * k;
                body.face = p.face;
                body.vx = 0f; body.vy = 0f;
                if (p.grounded) IsClinging = false;
                return;
            }

            // 플레이어가 위에 있으면 발판을 하나 골라 그쪽으로 간다.
            // 없으면(또는 같은 높이면) 예전처럼 플레이어 뒤를 따라간다.
            bool climbing = dy > tuning.DogJumpTrigU;

            // 목표는 땅을 딛을 때마다 새로 고른다.
            // ★ 공중에서는 지우지 않는다 ★ — 지우면 따라 뛰는 도중에 목표를 잃고
            //   "플레이어 뒤"로 방향을 틀어 발판을 놓치고 떨어진다.
            // ★ 땅에서는 반드시 다시 고른다 ★ — 예전엔 '목표에 도달했을 때만' 갱신해서,
            //   한 번 떨어져 목표가 사거리(최대 108px) 밖으로 벗어나면
            //   영원히 그 목표만 보고 헛뛰었다.
            if (body.grounded)
                hasStep = climbing && TryPickStep(p);

            if (hasStep)
            {
                // 땅에서는 '뛸 자리'로, 공중에서는 '내릴 자리'로 간다
                float aim = body.grounded ? stepStandX : stepLandX;
                float dead = Px.U(FollowDeadzonePx);
                int dir = body.X > aim + dead ? -1 : (body.X < aim - dead ? 1 : 0);
                // 공중에서는 더 빠르게 — 뛰어서 건너가는 동작이다
                body.Step(dt, dir, body.grounded ? tuning.DogSpeedV : tuning.DogAirSpeedV);

                if (body.grounded && Mathf.Abs(body.X - stepStandX) < Px.U(AimTolerancePx))
                    body.vy = JumpSpeedFor(stepY - body.Y + Px.U(ClearancePx));
            }
            else
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

                if (body.grounded)
                {
                    bool farBehind = Mathf.Abs(dx) > Px.U(CatchUpDistancePx)
                                     && Random.value < CatchUpJumpChance;
                    if (farBehind) body.vy = tuning.DogJumpV;
                }
            }

            // 뒤처지면 바로 따라붙는다.
            // 한 칸씩 밟고 올라오게 두면 느리고 기계적으로 보인다 —
            // 떨어졌을 때는 그냥 쫓아온 것으로 처리한다.
            if (Mathf.Abs(dx) > tuning.DogTeleportXU || dy > tuning.DogTeleportYU)
            {
                TeleportCount++;
                // 옆에 툭 생겨나면 튄다. 아래에서 솟아올라 착지하게 한다 —
                // 발판은 원웨이라 아래에서 통과해 올라간다.
                float drop = Px.U(CatchUpEntryDropPx);
                body.Teleport(p.X - Px.U(24f) * p.face, p.Y - drop);
                body.vy = JumpSpeedFor(drop + Px.U(ClearancePx));
                body.face = p.face;
                hasStep = false;
            }
        }

        bool SetStep(float y, float standX, float landX)
        {
            stepY = y; stepStandX = standX; stepLandX = landX; return true;
        }

        /// 높이 rise 를 뛰는 동안 가로로 움직일 수 있는 거리. 여유를 20% 둔다.
        /// 공중 속도로 계산한다 — 땅 속도로 재면 건널 수 있는 발판도 못 간다고 본다.
        float HorizontalReach(float rise)
        {
            float v = JumpSpeedFor(rise + Px.U(ClearancePx));
            return tuning.DogAirSpeedV * (v / tuning.GravityA) * 0.9f;
        }

        bool IsVisible()
        {
            var cam = Camera.main;
            if (cam == null) return false;
            var v = cam.WorldToViewportPoint(transform.position);
            return v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
        }

        /// 높이 h 만큼 오르는 데 필요한 초기 속도. 튜닝된 최대값을 넘지 않는다.
        float JumpSpeedFor(float rise)
            => Mathf.Min(tuning.DogJumpV, Mathf.Sqrt(2f * tuning.GravityA * Mathf.Max(rise, 0f)));

        /// 지금 딛고 있는 발판에서 한 번의 점프로 갈 수 있는 발판만 후보로 본다.
        /// 예전엔 높이만 보고 골라서, 가로로 안 겹치는 발판을 목표로 삼고
        /// 정렬하러 걸어가다 발판 밖으로 떨어졌다.
        bool TryPickStep(CharacterBody p)
        {
            if (body.groundIndex < 0) return false;
            var cur = stage.GetPlatform(body.groundIndex);

            float maxRise = tuning.DogJumpV * tuning.DogJumpV / (2f * tuning.GravityA)
                            - Px.U(ClearancePx);
            float minRise = Px.U(MinRisePx);
            float inset = Px.U(EdgeInsetPx);

            // 지금 발판에서 설 수 있는 구간
            float standLo = cur.left + inset, standHi = cur.right - inset;
            if (standLo > standHi) { standLo = standHi = (cur.left + cur.right) * 0.5f; }

            float bestScore = float.MaxValue;
            bool found = false;

            int n = stage.PlatformCount;
            for (int i = 0; i < n; i++)
            {
                if (i == body.groundIndex) continue;
                var c = stage.GetPlatform(i);
                float rise = c.y - body.Y;
                if (rise < minRise || rise > maxRise) continue;

                float landLo = c.left + inset, landHi = c.right - inset;
                if (landLo > landHi) { landLo = landHi = (c.left + c.right) * 0.5f; }

                // 뛰는 동안 가로로 움직일 수 있는 만큼만 벌어져 있어야 한다
                float reach = HorizontalReach(rise);
                float lo = Mathf.Max(standLo, landLo - reach);
                float hi = Mathf.Min(standHi, landHi + reach);
                if (lo > hi) continue;          // 여기서는 갈 수 없는 발판

                float standX = Mathf.Clamp(body.X, lo, hi);
                float landX = Mathf.Clamp(standX, landLo, landHi);

                // 플레이어 높이에 가까울수록 좋고, 옆으로 많이 걸어야 하면 감점
                float score = Mathf.Abs(p.Y - c.y) + Mathf.Abs(standX - body.X) * 0.5f;
                if (score < bestScore)
                {
                    bestScore = score;
                    SetStep(c.y, standX, landX);
                    found = true;
                }
            }
            return found;
        }
    }
}
