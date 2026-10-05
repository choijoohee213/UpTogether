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

        /// 가지에 매달려 있다
        public bool Hanging { get; private set; }
        /// 발 기준 머리 높이 (가지 판정용)
        const float HeadU = 0.5f;
        /// 점프한 순간. 강아지가 같이 뛰려고 듣는다.
        public event Action Jumped;

        CharacterBody body;
        public CharacterBody Body => body != null ? body : (body = GetComponent<CharacterBody>());
        public float Squash { get; private set; }   // 점프 순간 1 → 0으로 감소
        /// 마지막으로 발을 붙였던 높이. "높이 뛰었다 내려오는 중"과
        /// "실제로 아래로 떨어지는 중"을 구분하는 데 쓴다.
        public float LastGroundedY { get; private set; }
        public float ShakeAmount { get; private set; }
        /// 지금 가시덩굴(밧줄)을 타고 있는가. PlayerVisual 이 climb 그림을 쓰려고 읽는다.
        public bool Climbing { get; private set; }

        float fallFromY = float.NaN;   // 0이 아니라 NaN이 센티넬이다 — Unity에서는 y=0이 실제 위치다

        void Awake()
        {
            if (stage == null || stage.Data == null) { enabled = false; return; }
            LastGroundedY = transform.position.y;
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
            // ── 가지에 매달리기 ──
            // 머리가 가지에 닿으면 매달린다. 좌우로 건너고, 점프로 놓는다.
            float barY = 0f, barL = 0f, barR = 0f;
            bool atBar = stage != null && stage.TryGetBar(Body.X, Body.Y + HeadU, out barY, out barL, out barR);
            if (Hanging && (!atBar || jumpPressed)) 
            {
                Hanging = false;
                if (jumpPressed) { Body.vy = tuning.Jump1V * 0.8f; Body.holding = true; }  // 놓으며 띄우기
            }
            else if (atBar && !Body.grounded && Body.vy <= 0f) Hanging = true;

            if (Hanging)
            {
                Body.vy = 0f;
                Body.Y = barY - HeadU;                     // 가지에 머리를 건다
                Body.vx = moveDir * tuning.WalkSpeedV * 0.75f;   // 매달린 채로는 조금 느리게
                Body.X = Mathf.Clamp(Body.X + Body.vx * dt, barL, barR);
                if (moveDir != 0) Body.face = moveDir;
                Body.grounded = false; Body.groundIndex = -1;
                LastGroundedY = Body.Y;    // 매달린 건 추락이 아니다
                Body.holding = false; Squash = 0f;
                fallFromY = float.NaN;
                return;
            }

            // ── 밧줄 타기 ── 겹친 채 점프를 누르면 붙잡고 오른다. 좌우로 떼면 놓는다.
            float vineX = 0f, vineTop = 0f;
            bool onVine = stage != null && stage.TryGetVine(Body.X, Body.Y, out vineX, out vineTop);
            if (onVine && jumpHeld && moveDir == 0) Climbing = true;
            if (Climbing && (!onVine || moveDir != 0)) Climbing = false;
            if (Climbing)
            {
                Body.vx = 0f;
                Body.X += (vineX - Body.X) * Mathf.Min(1f, 14f * dt);   // 밧줄에 정렬
                Body.vy = jumpHeld ? tuning.WalkSpeedV * 0.9f : 0f;     // 누르는 동안만 오름
                Body.Y += Body.vy * dt;
                Body.grounded = false; Body.groundIndex = -1;
                if (Body.Y >= vineTop) { Body.Y = vineTop; Climbing = false; }  // 꼭대기 도착
                LastGroundedY = Body.Y;   // 밧줄에선 추락으로 치지 않는다
                Body.holding = false; Squash = 0f;
                fallFromY = float.NaN;
                return;
            }

            if (jumpPressed) TryJump();

            // ── 상승 기류 ── 안에 있으면 떠오른다. 발판 없이 높이를 버는 길.
            if (stage != null && stage.InUpdraft(Body.X, Body.Y, out float lift))
            {
                // 매 스텝 lift 로 다시 세운다. 조금씩 더하는 식으로는 같은 스텝의
                // 중력(Body.Step)에 먹혀 그대로 떨어진다.
                Body.vy = Mathf.Max(Body.vy, lift);
                Body.holding = false;
                fallFromY = Body.Y;        // 기류에 실린 건 추락이 아니다
                LastGroundedY = Body.Y;
            }

            // 손을 떼거나 정점을 지나면 홀드 종료 → 그 뒤로는 기본 중력
            if (!jumpHeld || Body.vy <= 0f) Body.holding = false;
            if (Body.holding) Body.holdTime += dt;

            if (Squash > 0f) Squash -= dt * 5f;
            if (ShakeAmount > 0f) ShakeAmount *= 1f - Px.Smoothing(0.15f, dt);

            bool wasAirborne = !Body.grounded;
            Body.Step(dt, moveDir, tuning.WalkSpeedV);

            if (Body.grounded)
            {
                LastGroundedY = Body.Y;
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
