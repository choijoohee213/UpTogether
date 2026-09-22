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

        public Tuning tuning;
        public StageRunner stage;
        public PlayerController player;

        public bool IsClinging { get; private set; }

        CharacterBody body;

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

            // 플레이어 뒤쪽을 목표로 잡는다
            float want = p.X - tuning.DogTrailU * p.face;
            float dead = Px.U(FollowDeadzonePx);
            int dir = body.X > want + dead ? -1 : (body.X < want - dead ? 1 : 0);

            body.Step(dt, dir, tuning.DogSpeedV);

            if (body.grounded)
            {
                bool playerAbove = dy > tuning.DogJumpTrigU;
                bool farBehind = Mathf.Abs(dx) > Px.U(CatchUpDistancePx)
                                 && Random.value < CatchUpJumpChance;
                if (playerAbove || farBehind) body.vy = tuning.DogJumpV;
            }

            // 너무 벌어지면 순간이동. 강아지를 찾으러 내려가는 건 재미가 없다.
            if (Mathf.Abs(dx) > tuning.DogTeleportXU || dy > tuning.DogTeleportYU)
            {
                body.Teleport(p.X - Px.U(24f) * p.face, p.Y);
                body.face = p.face;
            }
        }
    }
}
