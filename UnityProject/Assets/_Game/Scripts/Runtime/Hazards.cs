using System;
using UnityEngine;

namespace UpTogether
{
    /// 발판이 아닌 장애물 — 가시(위/아래), 바람, 돌아가는 톱니, 가시 링.
    /// 플레이어만 건드린다 (강아지는 알아서 순간이동하니 제외).
    [DefaultExecutionOrder(15)]   // 플레이어(0)가 움직인 뒤, 세션(20)이 판정하기 전
    public class Hazards : MonoBehaviour
    {
        public CharacterBody player;
        public StageRunner stage;
        public Bond bond;

        /// 무언가에 찔린 순간. 소리가 붙는다.
        public event Action Hurt;

        const float SpikeBond = 2f;      // 찔리면 깎이는 친밀도
        const float HitCooldown = 0.8f;
        const float PlayerR = 0.14f;     // 몸통 반지름 어림
        const float HeadUp = 0.5f;       // 발 기준 머리 높이 어림
        float cooldown;

        void FixedUpdate()
        {
            if (stage == null || stage.Data == null || player == null) return;
            float dt = Time.fixedDeltaTime;
            ApplyWind(dt);

            if (cooldown > 0f) { cooldown -= dt; return; }
            if (HitSpikes() || HitSaws() || HitThornRings()) DoHurt();
        }

        void ApplyWind(float dt)
        {
            var winds = stage.Data.winds;
            if (winds == null) return;
            foreach (var w in winds)
                if (player.X > w.x && player.X < w.x + w.width &&
                    player.Y > w.y && player.Y < w.y + w.height)
                    player.vx += w.force * dt;
        }

        bool HitSpikes()
        {
            var spikes = stage.Data.spikes;
            if (spikes == null) return false;
            float grab = player.tuning.GrabMarginU;
            foreach (var s in spikes)
            {
                if (player.X <= s.x - grab || player.X >= s.Right + grab) continue;
                if (!s.down)
                {
                    if (player.Y > s.y - 0.05f && player.Y < s.y + 0.30f) return true;
                }
                else
                {
                    float head = player.Y + HeadUp;   // 매달린 가시는 머리에 닿는다
                    if (head > s.y - 0.18f && head < s.y + 0.04f) return true;
                }
            }
            return false;
        }

        bool HitSaws()
        {
            float cx = player.X, cy = player.Y + 0.28f;
            for (int i = 0; i < stage.SawCount; i++)
            {
                var p = stage.SawPos(i);
                float d = Vector2.Distance(new Vector2(cx, cy), p);
                if (d < stage.SawBlade(i) + PlayerR) return true;
            }
            return false;
        }

        bool HitThornRings()
        {
            var rings = stage.Data.rings;
            if (rings == null) return false;
            float cx = player.X, cy = player.Y + 0.28f;
            foreach (var r in rings)
            {
                if (!r.thorny) continue;
                float d = Vector2.Distance(new Vector2(cx, cy), new Vector2(r.x, r.y));
                if (d > r.radius - 0.14f && d < r.radius + 0.16f) return true;  // 테두리에 닿음
            }
            return false;
        }

        void DoHurt()
        {
            player.vy = player.tuning.Jump1V * 0.55f;
            player.vx = -player.face * Px.V(5f);
            player.grounded = false;
            bond?.Add(-SpikeBond);
            cooldown = HitCooldown;
            Hurt?.Invoke();
        }
    }
}
