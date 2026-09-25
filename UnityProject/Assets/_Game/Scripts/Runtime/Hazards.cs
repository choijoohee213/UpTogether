using System;
using UnityEngine;

namespace UpTogether
{
    /// 발판이 아닌 장애물 — 가시와 바람. 플레이어만 건드린다
    /// (강아지는 알아서 순간이동하니 제외).
    [DefaultExecutionOrder(15)]   // 플레이어(0)가 움직인 뒤, 세션(20)이 판정하기 전
    public class Hazards : MonoBehaviour
    {
        public CharacterBody player;
        public StageRunner stage;
        public Bond bond;

        /// 가시에 찔린 순간. 소리가 붙는다.
        public event Action Hurt;

        const float SpikeBond = 2f;      // 찔리면 깎이는 친밀도
        const float HitCooldown = 0.8f;
        float cooldown;

        void FixedUpdate()
        {
            if (stage == null || stage.Data == null || player == null) return;
            float dt = Time.fixedDeltaTime;
            ApplyWind(dt);
            ApplySpikes(dt);
        }

        void ApplyWind(float dt)
        {
            var winds = stage.Data.winds;
            if (winds == null) return;
            foreach (var w in winds)
            {
                if (player.X > w.x && player.X < w.x + w.width &&
                    player.Y > w.y && player.Y < w.y + w.height)
                    player.vx += w.force * dt;
            }
        }

        void ApplySpikes(float dt)
        {
            if (cooldown > 0f) { cooldown -= dt; return; }
            var spikes = stage.Data.spikes;
            if (spikes == null) return;

            float grab = player.tuning.GrabMarginU;
            foreach (var s in spikes)
            {
                if (player.X > s.x - grab && player.X < s.Right + grab &&
                    player.Y > s.y - 0.05f && player.Y < s.y + 0.30f)
                {
                    // 반동으로 튕겨나가고 친밀도가 깎인다
                    player.vy = player.tuning.Jump1V * 0.55f;
                    player.vx = -player.face * Px.V(5f);
                    player.grounded = false;
                    bond?.Add(-SpikeBond);
                    cooldown = HitCooldown;
                    Hurt?.Invoke();
                    break;
                }
            }
        }
    }
}
