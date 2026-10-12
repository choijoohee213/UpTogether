using UnityEngine;

namespace UpTogether
{
    /// 고른 특기를 실제 능력으로 만든다. 판정 주인은 건드리지 않고
    /// (Hazards·StageSession·Collectibles) 그쪽에서 이 컴포넌트에 물어보게 한다.
    [DefaultExecutionOrder(18)]   // 장애물(15)·수집(16) 뒤, 세션(20) 앞
    public class DogAbilities : MonoBehaviour
    {
        public PlayerController player;
        public DogController dog;
        public StageRunner stage;
        public Collectibles collectibles;
        public DogEmote emote;
        public Puffs puffs;
        public Narration narration;

        [Header("슈퍼맨")]
        public float rescueCooldown = 20f;
        [Header("먹보")]
        public float fetchRadius = 2.4f;
        public float fetchInterval = 1.2f;
        [Header("똑똑이")]
        public float warnRadius = 1.1f;
        public float warnCooldown = 3.5f;
        [Header("포근이")]
        public float shieldCooldown = 12f;

        float rescueReady, warnReady, shieldReady, fetchTimer;

        bool Has(DogPerks.Kind k) => DogPerks.Has(k);

        void Update()
        {
            if (Has(DogPerks.Kind.Glutton)) Fetch();
            if (Has(DogPerks.Kind.Clever)) Warn();
        }

        // ── 슈퍼맨: 크게 떨어질 때 물어 올려 구해준다 ──
        // StageSession 이 친밀도를 깎기 전에 묻는다. true 면 손실 없이 되돌린다.
        public bool TryRescue(float fellMeters)
        {
            if (!Has(DogPerks.Kind.Super) || Time.time < rescueReady) return false;
            rescueReady = Time.time + rescueCooldown;

            // 떨어진 높이의 절반쯤까지 끌어올린다 — 완전 무효는 너무 세다
            float up = Px.U(fellMeters * Px.PxPerMeter * 0.5f);
            player.Body.Teleport(player.Body.X, player.Body.Y + up);
            player.Body.vy = 0f;

            puffs?.Burst(new Vector2(player.Body.X, player.Body.Y), 9, 24f, 4.5f, 2f,
                         sizePx: 7f, sizeVarPx: 5f, life: 0.5f);
            emote?.Show(ExclaimSprite);
            Sfx.I?.BarkBig();
            narration?.Speak("강아지가 옷깃을 물어 끌어올렸다.");
            return true;
        }

        // ── 포근이: 찔릴 때 한 번 막아준다 ──
        // Hazards 가 친밀도를 깎기 전에 묻는다.
        public bool TryShield()
        {
            if (!Has(DogPerks.Kind.Cozy) || Time.time < shieldReady) return false;
            shieldReady = Time.time + shieldCooldown;
            emote?.Show(HeartSprite);
            Sfx.I?.Whine();
            return true;
        }

        // ── 먹보: 손이 닿지 않는 간식을 가져온다 ──
        void Fetch()
        {
            if (collectibles == null) return;
            fetchTimer -= Time.deltaTime;
            if (fetchTimer > 0f) return;
            fetchTimer = fetchInterval;

            if (collectibles.FetchNear(player.Body.X, player.Body.Y, fetchRadius, out float tx, out float ty))
            {
                puffs?.Burst(new Vector2(tx, ty), 7, 18f, 3.5f, 2f);
                emote?.Show(NoteSprite);
                Sfx.I?.BarkSmall();
            }
        }

        // ── 똑똑이: 위험한 것이 가까우면 짖어 알려준다 ──
        void Warn()
        {
            if (stage == null || stage.Data == null || Time.time < warnReady) return;
            float px = player.Body.X, py = player.Body.Y;

            bool near = false;
            var spikes = stage.Data.spikes;
            if (spikes != null)
                foreach (var s in spikes)
                {
                    if (px < s.x - warnRadius || px > s.Right + warnRadius) continue;
                    if (Mathf.Abs(py - s.y) < warnRadius) { near = true; break; }
                }
            if (!near)
                for (int i = 0; i < stage.SawCount; i++)
                    if (Vector2.Distance(new Vector2(px, py + 0.28f), stage.SawPos(i)) < warnRadius + 0.3f)
                    { near = true; break; }

            if (!near) return;
            warnReady = Time.time + warnCooldown;
            emote?.Show(ExclaimSprite);
            Sfx.I?.BarkSmall();
        }

        Sprite ExclaimSprite => emote != null && emote.lib != null ? emote.lib.emoteExclaim : null;
        Sprite HeartSprite => emote != null && emote.lib != null ? emote.lib.emoteHeart : null;
        Sprite NoteSprite => emote != null && emote.lib != null ? emote.lib.emoteNote : null;
    }
}
