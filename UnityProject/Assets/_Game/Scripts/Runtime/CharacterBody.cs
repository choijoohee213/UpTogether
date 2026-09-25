using System;
using UnityEngine;

namespace UpTogether
{
    /// climb-dog2.html 의 stepBody() 이식본. 플레이어와 강아지가 같이 쓴다.
    ///
    /// ★ 부호 주의 ★
    /// 프로토타입은 y가 아래쪽 양수, vy>0 = 낙하였다. Unity는 위쪽 양수다.
    /// 여기서는 전부 Unity 기준(vy>0 = 상승)으로 뒤집혀 있다.
    /// 원본과 대조할 때 이 점을 먼저 떠올릴 것.
    ///
    /// Rigidbody2D를 쓰지 않는다. 검증된 수치를 그대로 살리려면 직접 적분해야 한다.
    /// (PROJECT.md "물리 구현 방식 권장")
    public class CharacterBody : MonoBehaviour
    {
        const float SnapEpsilon = 0.01f;   // 원본의 1px
        const float WalkAnimSpeed = 0.22f; // 프레임당. 원본과 동일

        public Tuning tuning;

        [Header("상태 (읽기용)")]
        public float vx, vy;
        public bool grounded;
        /// 지금 딛고 있는 발판 번호. 공중이면 -1.
        /// 강아지가 "여기서 갈 수 있는 발판"을 고를 때 쓴다.
        [HideInInspector] public int groundIndex = -1;
        public int face = 1;
        public float walkPhase;

        // 홀드 점프 — 플레이어만 쓴다. 강아지는 항상 기본 중력.
        [HideInInspector] public bool holding;
        [HideInInspector] public float holdTime;

        /// 튕김판을 밟은 순간. 소리가 여기에 붙는다.
        public event Action Bounced;

        StageRunner stage;

        public float X { get => transform.position.x; set { var p = transform.position; p.x = value; transform.position = p; } }
        public float Y { get => transform.position.y; set { var p = transform.position; p.y = value; transform.position = p; } }

        public void Bind(StageRunner s) => stage = s;

        /// moveDir: -1, 0, +1 / speed: units/s
        public void Step(float dt, int moveDir, float speed)
        {
            float target = moveDir * speed;
            vx += (target - vx) * Px.Smoothing(tuning.walkAccel, dt);
            if (moveDir != 0) face = moveDir;

            X += vx * dt;
            float minX = tuning.WallMarginU;
            float maxX = tuning.MapWidthU - tuning.WallMarginU;
            if (X < minX) { X = minX; vx = 0f; }
            if (X > maxX) { X = maxX; vx = 0f; }

            float prevY = Y;

            // 홀드 중이고 아직 올라가는 중이면 약한 중력 → 길게 누를수록 높이 뛴다
            bool soft = holding && vy > 0f && holdTime < tuning.holdMaxSeconds;
            vy -= (soft ? tuning.HoldGravityA : tuning.GravityA) * dt;
            if (vy < -tuning.MaxFallV) vy = -tuning.MaxFallV;

            Y += vy * dt;
            grounded = false;
            groundIndex = -1;

            // 내려가는 중에만 발판을 잡는다 = 아래에서 통과 가능(원웨이)
            if (vy <= 0f)
            {
                float grab = tuning.GrabMarginU;
                int n = stage.PlatformCount;
                for (int i = 0; i < n; i++)
                {
                    var p = stage.GetPlatform(i);
                    if (!p.active) continue;                 // 사라진 발판은 건너뛴다
                    // 세로로 움직이는 발판은 이미 움직인 뒤라, 이번에 오른 만큼(deltaY)을
                    // 빼서 '움직이기 전 높이'와 비교해야 위로 밀어올려도 발을 붙잡는다.
                    float py0 = p.y - p.deltaY;
                    if (X > p.left - grab && X < p.right + grab &&
                        prevY >= py0 - SnapEpsilon && Y <= p.y + SnapEpsilon)
                    {
                        if (p.kind == StageRunner.Kind.Bounce)
                        {
                            Y = p.y;
                            vy = tuning.Jump1V * 1.7f;        // 튕김판 — 점프보다 훨씬 높이
                            Bounced?.Invoke();
                        }
                        else
                        {
                            Y = p.y;
                            vy = 0f;
                            grounded = true;
                            groundIndex = i;
                            X += p.deltaX;                   // 움직이는 발판에 실려 간다
                            if (p.kind == StageRunner.Kind.Vanish) stage.NotifyStand(i);
                        }
                        break;
                    }
                }
            }

            // 맵 아래로 빠지면 바닥으로 되돌린다 (원본: y > H+300)
            if (Y < stage.FallResetY)
            {
                Y = stage.Data.groundY;
                vy = 0f;
            }

            float walkThreshold = Px.V(0.4f);
            if (Mathf.Abs(vx) > walkThreshold && grounded) walkPhase += WalkAnimSpeed * dt * Px.FPS;
            else walkPhase *= 1f - Px.Smoothing(0.1f, dt);
        }

        public void Teleport(float x, float y)
        {
            transform.position = new Vector3(x, y, transform.position.z);
            vx = 0f; vy = 0f;
        }
    }
}
