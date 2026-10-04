using UnityEngine;

namespace UpTogether
{
    /// 구간 추락. 이 게임이 "떨어져도 잃는 게 없어" 단조롭던 것을 고치는 축이다.
    ///
    /// 스테이지를 일정 높이마다 구간으로 자르고, 구간 바닥선 아래로 떨어지면
    /// 그 구간 처음으로 돌아간다. 작은 실수(한 칸 아래 착지)는 그대로 값싸고,
    /// 정말 미끄러졌을 때만 크게 잃는다.
    ///
    /// ★ 강아지는 따라 내려오지 않는다 ★ — 떨어진 자리에 남아 기다린다.
    /// 다시 올라가 만나야 하고, 떨어져 있는 동안 친밀도가 천천히 깎인다.
    /// 되찾을 것이 있어야 다시 오를 마음이 든다.
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
        [Tooltip("강아지와 떨어져 있는 동안 초당 깎이는 친밀도")]
        public float lonelyDrain = 0.25f;
        [Tooltip("이 거리 안에 들어오면 다시 만난 것으로 친다(units)")]
        public float reunionRange = 0.7f;

        /// 지금까지 도달한 가장 높은 구간 (0부터)
        public int Zone { get; private set; }
        /// 강아지를 두고 내려온 상태
        public bool Separated => dog != null && dog.Waiting;

        StageData built;        // 어느 스테이지로 구간을 짰는지
        float drainPool;        // 외로움 감소를 모아 둔다 (아래 설명)
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

            if (Separated) Lonely();
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
            bond?.Add(-fallBond);
            Sfx.I?.Whine();

            // 강아지는 따라오지 않는다 — 있던 자리에서 기다린다
            if (dog != null && !dog.Waiting)
            {
                dog.WaitHere();
                narration?.Speak("미끄러졌다. 강아지는 위에서 기다리고 있다.");
            }
            else narration?.Speak("또 미끄러졌다.");
        }

        /// 떨어져 있는 동안 — 조금씩 깎이고, 가까워지면 다시 만난다
        void Lonely()
        {
            var b = player.Body;
            float d = Vector2.Distance(new Vector2(b.X, b.Y),
                                       new Vector2(dog.Body.X, dog.Body.Y));
            if (d < reunionRange)
            {
                drainPool = 0f;
                dog.Rejoin();
                bond?.Add(4f);
                Sfx.I?.BarkBig();
                narration?.Speak("다시 만났다. 강아지가 꼬리를 흔든다.");
                return;
            }
            // ★ 프레임마다 쪼개 넣으면 안 된다 ★
            // Bond.Add 는 Mathf.Approximately 로 미세 변화를 버린다. 초당 0.25 를
            // 프레임 수로 나누면 한 번에 0.0001 수준이라 통째로 삼켜져, 외로움
            // 감소가 아예 걸리지 않았다. 0.2 넘게 모이면 그때 넣는다.
            drainPool += lonelyDrain * Time.deltaTime;
            if (drainPool >= 0.2f) { bond?.Add(-drainPool); drainPool = 0f; }
        }

        /// 스테이지가 바뀌면 처음부터
        public void ResetForNewStage()
        {
            built = null;
            Zone = 0;
            dog?.Rejoin();
        }
    }
}
