using System;
using UnityEngine;

namespace UpTogether
{
    [DefaultExecutionOrder(0)]
    [RequireComponent(typeof(CharacterBody))]
    public class PlayerController : MonoBehaviour
    {
        public Tuning tuning;
        public StageRunner stage;
        public Puffs puffs;

        /// 착지할 때 낙하 거리(m)를 흘려보낸다. 친밀도/연출이 여기에 붙는다.
        public event Action<float> Landed;
        /// 점프한 순간. 강아지가 같이 뛰려고 듣는다.
        public event Action Jumped;

        CharacterBody body;
        public CharacterBody Body => body != null ? body : (body = GetComponent<CharacterBody>());
        public float Squash { get; private set; }   // 점프 순간 1 → 0으로 감소
        public float ShakeAmount { get; private set; }

        float fallFromY = float.NaN;   // 0이 아니라 NaN이 센티넬이다 — Unity에서는 y=0이 실제 위치다

        void Awake()
        {
            if (stage == null || stage.Data == null) { enabled = false; return; }
            Body.tuning = tuning;
            Body.Bind(stage);
        }

        void FixedUpdate()
        {
            var input = GameInput.I;
            int dir = (input.Right ? 1 : 0) - (input.Left ? 1 : 0);
            Tick(Time.fixedDeltaTime, dir, input.JumpPressed, input.JumpHeld);
        }

        /// 한 물리 스텝. 입력을 인자로 받아서 검증 스크립트가 직접 돌릴 수 있다.
        /// (UpTogether ▸ Verify Jump Heights)
        public void Tick(float dt, int moveDir, bool jumpPressed, bool jumpHeld)
        {
            if (jumpPressed) TryJump();

            // 손을 떼거나 정점을 지나면 홀드 종료 → 그 뒤로는 기본 중력
            if (!jumpHeld || Body.vy <= 0f) Body.holding = false;
            if (Body.holding) Body.holdTime += dt;

            if (Squash > 0f) Squash -= dt * 5f;
            if (ShakeAmount > 0f) ShakeAmount *= 1f - Px.Smoothing(0.15f, dt);

            bool wasAirborne = !Body.grounded;
            Body.Step(dt, moveDir, tuning.WalkSpeedV);

            if (Body.grounded)
            {
                if (wasAirborne) OnLand();
            }
            else
            {
                // 올라가는 동안은 정점을 계속 갱신하고,
                // 점프 없이 발판에서 걸어 나간 경우엔 떨어지기 시작한 높이를 잡는다.
                if (Body.vy > 0f) fallFromY = Body.Y;
                else if (float.IsNaN(fallFromY)) fallFromY = Body.Y;
            }
        }

        void TryJump()
        {
            // 이중 점프 없음 — 땅에 있을 때만 뛴다.
            if (!Body.grounded) return;

            Body.vy = tuning.Jump1V;
            Body.grounded = false;
            puffs?.Burst(new Vector2(Body.X, Body.Y), 7, 18f, 3.5f, 2f);

            Squash = 1f;
            Body.holding = true;
            Body.holdTime = 0f;
            Jumped?.Invoke();
        }

        void OnLand()
        {
            float fellMeters = float.IsNaN(fallFromY)
                ? 0f
                : (fallFromY - Body.Y) * Px.PPU / Px.PxPerMeter;
            fallFromY = float.NaN;

            puffs?.Burst(new Vector2(Body.X, Body.Y), 8, 22f, 4.5f, 2.2f);

            if (fellMeters > 8f) ShakeAmount = Mathf.Min(Px.U(13f), fellMeters * Px.U(0.5f));
            Landed?.Invoke(fellMeters);
        }
    }
}
