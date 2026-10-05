using UnityEngine;

namespace UpTogether
{
    /// 구간 추락. 이 게임이 "떨어져도 잃는 게 없어" 단조롭던 것을 고치는 축이다.
    ///
    /// 스테이지를 일정 높이마다 구간으로 자르고, 구간 바닥선 아래로 떨어지면
    /// 그 구간 처음으로 돌아간다. 작은 실수(한 칸 아래 착지)는 그대로 값싸고,
    /// 정말 미끄러졌을 때만 크게 잃는다.
    ///
    /// 강아지도 같이 떨어진다 — 혼자 남겨두지 않는다.
    /// 잃는 것은 높이지 동반자가 아니다.
    [DefaultExecutionOrder(22)]   // 세션(20) 뒤
    public class Zones : MonoBehaviour
    {
        public PlayerController player;
        public DogController dog;
        public StageRunner stage;
        public Bond bond;
        public Narration narration;
        public Puffs puffs;

        [Tooltip("구간 하나의 높이(m). 이만큼 올라갈 때마다 되돌아갈 지점이 생긴다.")]
        public float zoneMeters = 30f;
        [Tooltip("구간 바닥선보다 이만큼 더 내려가야 추락으로 친다(m).")]
        public float slackMeters = 4f;
        [Tooltip("구간 추락으로 잃는 친밀도")]
        public float fallBond = 6f;

        /// 지금까지 도달한 가장 높은 구간 (0부터)
        public int Zone { get; private set; }

        StageData built;        // 어느 스테이지로 구간을 짰는지
        float[] zoneY;          // 구간 시작 발판의 y
        float[] zoneX;          // 그 발판의 가운데 x

        void Update()
        {
            if (stage == null || stage.Data == null || player == null) return;
            if (built != stage.Data) Rebuild();
            if (zoneY == null || zoneY.Length == 0) return;

            float y = player.Body.Y;

            // 더 높은 구간에 올라섰나
            while (Zone + 1 < zoneY.Length && y >= zoneY[Zone + 1]) Zone++;

            // 구간 바닥선 아래로 떨어졌나
            float floor = zoneY[Zone] - Px.U(slackMeters * Px.PxPerMeter);
            if (y < floor) FallBack();
        }

        /// 스테이지 높이를 구간으로 자른다. 구간 시작은 그 높이에 가장 가까운 발판이다.
        void Rebuild()
        {
            built = stage.Data;
            Zone = 0;

            float step = Px.U(zoneMeters * Px.PxPerMeter);
            int count = Mathf.Max(1, Mathf.FloorToInt((built.height - built.groundY) / step) + 1);
            zoneY = new float[count];
            zoneX = new float[count];

            for (int i = 0; i < count; i++)
            {
                float want = built.groundY + step * i;
                if (i == 0) { zoneY[0] = built.groundY; zoneX[0] = 0.9f; continue; }

                // 그 높이 바로 아래의 발판을 구간 시작으로 (딛고 설 자리여야 한다)
                float bestY = built.groundY, bestX = 0.9f;
                foreach (var p in built.platforms)
                    if (p.y <= want && p.y > bestY) { bestY = p.y; bestX = p.x + p.width * 0.5f; }
                zoneY[i] = bestY;
                zoneX[i] = bestX;
            }
        }

        void FallBack()
        {
            var b = player.Body;
            b.Teleport(zoneX[Zone], zoneY[Zone] + 0.05f);
            b.vx = 0f; b.vy = 0f;
            puffs?.Burst(new Vector2(b.X, b.Y), 11, 26f, 5f, 2.4f, sizePx: 8f, sizeVarPx: 6f, life: 0.5f);

            // 강아지도 같이 내려온다 — 혼자 두지 않는다
            if (dog != null && dog.Body != null)
            {
                var db = dog.Body;
                db.Teleport(zoneX[Zone] - 0.35f, zoneY[Zone] + 0.05f);
                db.vx = 0f; db.vy = 0f;
                puffs?.Burst(new Vector2(db.X, db.Y), 9, 24f, 4.5f, 2f,
                             sizePx: 7f, sizeVarPx: 5f, life: 0.5f);
            }

            bond?.Add(-fallBond);
            Sfx.I?.Whine();
            narration?.Speak("미끄러졌다. 둘 다 아래로 굴러떨어졌다.");
        }

        /// 스테이지가 바뀌면 처음부터
        public void ResetForNewStage()
        {
            built = null;
            Zone = 0;
        }
    }
}
