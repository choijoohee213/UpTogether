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
        const float EdgeInsetPx = 14f;
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

        CharacterBody body;

        // 한 번 정한 목표 발판은 착지할 때까지 붙든다.
        // 공중에서 매 프레임 다시 고르면 방향이 흔들려 착지를 놓친다.
        bool hasStep;
        float stepY, stepAimX;

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

            // 올라갈 발판을 이미 정해뒀으면 거기에 맞춰, 아니면 평소 점프로.
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

            if (!p.grounded && p.vy < -tuning.DogClingFallV && !IsClinging)
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

            if (climbing)
            {
                // 땅에 있고, 목표가 없거나 이미 그 높이에 올라섰으면 새로 고른다
                if (body.grounded && (!hasStep || body.Y >= stepY - Px.U(2f)))
                {
                    hasStep = TryPickStep(p, out var step, out float ax);
                    if (hasStep) { stepY = step.y; stepAimX = ax; }
                }
            }
            else hasStep = false;

            if (climbing && hasStep)
            {
                float dead = Px.U(FollowDeadzonePx);
                int dir = body.X > stepAimX + dead ? -1 : (body.X < stepAimX - dead ? 1 : 0);
                body.Step(dt, dir, tuning.DogSpeedV);

                // 목표 발판 x 에 들어왔을 때만 뛴다. 아무 데서나 뛰면 헛뛰고 떨어진다.
                if (body.grounded && Mathf.Abs(body.X - stepAimX) < Px.U(AimTolerancePx))
                    body.vy = JumpSpeedFor(stepY - body.Y + Px.U(ClearancePx));
            }
            else
            {
                float want = p.X - tuning.DogTrailU * p.face;
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

            // 너무 벌어지면 순간이동. 강아지를 찾으러 내려가는 건 재미가 없다.
            // 단, 보이는 데서 사라졌다 나타나면 튄다 — 화면 밖일 때만 옮긴다.
            if (Mathf.Abs(dx) > tuning.DogTeleportXU || dy > tuning.DogTeleportYU)
            {
                if (IsVisible()) SuppressedTeleportCount++;
                else
                {
                    TeleportCount++;
                    // 플레이어 옆에 바로 놓지 않는다. 아래에서 솟아올라 착지하게 한다 —
                    // 발판은 원웨이라 아래에서 통과해 올라간다.
                    float drop = Px.U(CatchUpEntryDropPx);
                    body.Teleport(p.X - Px.U(24f) * p.face, p.Y - drop);
                    body.vy = JumpSpeedFor(drop + Px.U(ClearancePx));
                    body.face = p.face;
                    hasStep = false;
                }
            }
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

        /// 지금 발밑에서 한 번에 닿을 수 있는 발판 중 플레이어에 가장 가까워지는 것을 고른다.
        /// 스테이지가 한 줄짜리 사슬이라 이 정도로 충분하다 — 경로 탐색까지 갈 필요가 없다.
        bool TryPickStep(CharacterBody p, out StageRunner.RuntimePlatform best, out float aimX)
        {
            best = default; aimX = 0f;

            float maxRise = tuning.DogJumpV * tuning.DogJumpV / (2f * tuning.GravityA)
                            - Px.U(ClearancePx);
            float minRise = Px.U(MinRisePx);
            float inset = Px.U(EdgeInsetPx);
            float bestScore = float.MaxValue;
            bool found = false;

            int n = stage.PlatformCount;
            for (int i = 0; i < n; i++)
            {
                var c = stage.GetPlatform(i);
                float rise = c.y - body.Y;
                if (rise < minRise || rise > maxRise) continue;

                // 발판 폭 안에서 플레이어 쪽에 가장 가까운 지점을 노린다
                float lo = c.left + inset, hi = c.right - inset;
                if (lo > hi) { lo = hi = (c.left + c.right) * 0.5f; }
                float target = Mathf.Clamp(p.X, lo, hi);

                // 플레이어에 가까워질수록, 지금 위치에서 멀지 않을수록 좋다
                float score = Mathf.Abs(target - p.X) + Mathf.Abs(target - body.X) * 0.5f
                              + Mathf.Abs(p.Y - c.y) * 0.25f;
                if (score < bestScore) { bestScore = score; best = c; aimX = target; found = true; }
            }
            return found;
        }
    }
}
