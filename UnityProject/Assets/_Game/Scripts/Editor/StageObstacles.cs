using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 세 스테이지에 장애물을 손으로 얹는다. 공중에 뜬금없이 두지 않고,
    /// 발판 순서(경로)를 따라 착지면·발판 밑·건너는 틈에 붙여 배치한다.
    /// 항상 새로 베이크한 뒤 얹으므로 여러 번 실행해도 겹치지 않는다.
    ///
    /// 스테이지마다 밀도를 달리해 난이도 곡선을 만든다 —
    /// 1은 하나씩 소개하고, 2는 겹쳐 쓰고, 3은 쉴 틈을 줄인다.
    public static class StageObstacles
    {
        /// 스테이지 하나의 장애물 분량. 개수는 발판 수와 무관하게 절대값으로 둔다
        /// (발판이 늘어도 밀도가 아니라 배치 간격이 넓어지도록).
        struct Recipe
        {
            public string path;
            public int vanishers;     // 사라지는 발판으로 바꿀 칸
            public int verticalMovers;// 위아래로 움직이게 바꿀 칸
            public int spikes;        // 착지면 위 가시
            public int hangingSpikes; // 발판 밑에 매달린 가시
            public int vines;         // 타고 오르는 밧줄
            public int plainRings;    // 통과하면 친밀도 ↑
            public int thornyRings;   // 테두리에 닿으면 아픔
            public int saws;          // 틈을 순찰하는 톱니
            public int winds;         // 옆으로 미는 바람
            public int treats;        // 간식
            public int bouncers;      // 튕김판
        }

        static readonly Recipe[] Recipes =
        {
            new Recipe { path = "Assets/_Game/Stages/Stage1.asset",
                vanishers = 2, verticalMovers = 1, spikes = 3, hangingSpikes = 1, vines = 3,
                plainRings = 2, thornyRings = 1, saws = 1, winds = 1, treats = 4, bouncers = 1 },
            new Recipe { path = "Assets/_Game/Stages/Stage2.asset",
                vanishers = 3, verticalMovers = 2, spikes = 5, hangingSpikes = 2, vines = 3,
                plainRings = 2, thornyRings = 2, saws = 2, winds = 2, treats = 5, bouncers = 1 },
            new Recipe { path = "Assets/_Game/Stages/Stage3.asset",
                vanishers = 4, verticalMovers = 3, spikes = 7, hangingSpikes = 3, vines = 4,
                plainRings = 2, thornyRings = 3, saws = 3, winds = 3, treats = 6, bouncers = 2 },
        };

        /// 바닥(0번)과 골 직전은 건드리지 않는다 — 시작과 도착은 늘 안전하게.
        const float FirstFraction = 0.10f;
        const float LastFraction = 0.94f;

        [MenuItem("UpTogether/Author Stage Obstacles")]
        public static void Author()
        {
            foreach (var r in Recipes) AuthorOne(r);
            AssetDatabase.SaveAssets();
        }

        static void AuthorOne(Recipe r)
        {
            var s = AssetDatabase.LoadAssetAtPath<StageData>(r.path);
            if (s == null) { Debug.LogError($"{r.path} 없음. Bake Stages 먼저."); return; }

            // 발판 배열은 생성 순서 = 올라가는 경로다. P[0] 은 바닥.
            var P = new List<StageData.Platform>(s.platforms);
            int n = P.Count;
            if (n < 6) { Debug.LogError($"{r.path}: 발판이 너무 적다 ({n})."); return; }

            float C(StageData.Platform p) => p.x + p.width * 0.5f;

            // 베이커가 만든 가로 이동 발판은 그대로 두고 여기에 세로 이동만 더한다
            var movers = new List<StageData.Mover>(s.movers);
            var vanishers = new List<StageData.Vanisher>();
            var spikes = new List<StageData.Spike>();
            var winds = new List<StageData.Wind>();
            var bouncers = new List<StageData.Bouncer>();
            var saws = new List<StageData.Saw>();
            var rings = new List<StageData.Ring>();
            var treats = new List<StageData.Treat>();
            var vines = new List<StageData.Vine>();

            // 경로를 따라 고르게 흩되, 한 발판이 두 역할을 맡지 않게 한 번 쓰면 뺀다.
            // 풀이 둘이다 — 발판 표면을 차지하는 것(가시·링·발판 변형)과
            // 그 위나 옆 틈에 놓이는 것(밧줄·톱니·바람·간식). 둘은 서로 겹쳐도 된다.
            var surface = new Slots(n);
            var extras = new Slots(n);

            // ── 발판 자체를 바꾸는 것 (경로를 유지하도록 제자리에서) ──
            var toVanish = surface.Take(r.vanishers);
            var toMove = surface.Take(r.verticalMovers);

            var keep = new List<StageData.Platform>();
            for (int i = 0; i < n; i++)
            {
                if (toVanish.Contains(i))
                    vanishers.Add(new StageData.Vanisher { x = P[i].x, y = P[i].y, width = P[i].width });
                else if (toMove.Contains(i))
                    movers.Add(new StageData.Mover {
                        x = P[i].x, y = P[i].y, width = P[i].width,
                        range = 0.5f, speed = 1.5f, phase = 0f, vertical = true });
                else
                    keep.Add(P[i]);
            }

            // ── 착지면 위 가시 (가운데 40%, 양 끝으로 피해 밟게) ──
            foreach (int i in surface.Take(r.spikes))
                spikes.Add(new StageData.Spike {
                    x = P[i].x + P[i].width * 0.30f, y = P[i].y, width = P[i].width * 0.40f });

            // ── 발판 밑에 매달린 가시 (밑을 스치며 지나갈 때 아프다) ──
            foreach (int i in surface.Take(r.hangingSpikes))
                spikes.Add(new StageData.Spike {
                    x = P[i].x + P[i].width * 0.25f, y = P[i].y, width = P[i].width * 0.50f, down = true });

            // ── 타고 오르는 가시덩굴(밧줄): 두 칸 위 발판으로 오르는 지름길 ──
            foreach (int i in extras.Take(r.vines))
            {
                // 너무 짧으면 의미가 없고, 너무 길면 (사이에 이동 발판이 빠진 자리) 허공을 탄다.
                // 두 칸 위가 기본이되 높이가 안 맞으면 그 앞뒤에서 맞는 발판을 찾는다.
                foreach (int step in new[] { 2, 3, 1 })
                {
                    int j = i + step;
                    if (j > n - 1) continue;
                    float h = P[j].y - P[i].y;
                    if (h < 0.45f || h > 2.4f) continue;
                    vines.Add(new StageData.Vine { x = C(P[i]), y = P[i].y, height = h });
                    break;
                }
            }

            // ── 착지 발판을 감싸는 링 (위로 통과해 올라선다) ──
            foreach (int i in surface.Take(r.plainRings))
            {
                rings.Add(new StageData.Ring { x = C(P[i]), y = P[i].y + 0.55f, radius = 0.5f, thorny = false });
                treats.Add(new StageData.Treat { x = C(P[i]), y = P[i].y + 0.62f });   // 가운데 간식
            }
            foreach (int i in surface.Take(r.thornyRings))
                rings.Add(new StageData.Ring { x = C(P[i]), y = P[i].y + 0.55f, radius = 0.42f, thorny = true });

            // ── 톱니: 두 발판 사이 틈을 한쪽으로 치우쳐 순찰 (반대쪽으로 지나가게) ──
            foreach (int i in extras.Take(r.saws))
            {
                int j = Mathf.Min(i + 1, n - 1);
                float mx = (C(P[i]) + C(P[j])) * 0.5f;
                float my = Mathf.Max(P[i].y, P[j].y) + 0.25f;
                saws.Add(new StageData.Saw { x = mx + 0.35f, y = my, blade = 0.2f, orbit = 0.35f, speed = 2.2f });
            }

            // ── 바람: 세로 틈에서 안쪽으로 민다 ──
            foreach (int i in extras.Take(r.winds))
            {
                float dir = C(P[i]) > 4.5f ? -1f : 1f;
                winds.Add(new StageData.Wind {
                    x = P[i].x - 0.2f, y = P[i].y + 0.15f,
                    width = P[i].width + 0.4f, height = 1.5f, force = dir * 4f });
            }

            // ── 간식: 착지면 위에 (경로에서 자연히 줍게) ──
            foreach (int i in extras.Take(r.treats))
                treats.Add(new StageData.Treat { x = C(P[i]), y = P[i].y + 0.32f });

            // ── 튕김판: 바닥 근처 '장난감' (주 경로 밖) ──
            for (int k = 0; k < r.bouncers; k++)
                bouncers.Add(new StageData.Bouncer { x = 6.4f - k * 1.4f, y = P[0].y + 0.55f, width = 0.7f });

            s.platforms = keep.ToArray();
            s.movers = movers.ToArray();
            s.vanishers = vanishers.ToArray();
            s.spikes = spikes.ToArray();
            s.winds = winds.ToArray();
            s.bouncers = bouncers.ToArray();
            s.saws = saws.ToArray();
            s.rings = rings.ToArray();
            s.treats = treats.ToArray();
            s.vines = vines.ToArray();

            EditorUtility.SetDirty(s);
            Debug.Log($"{s.displayName} 장애물: 발판 {keep.Count} / 사라짐 {vanishers.Count} / 이동 {movers.Count} / " +
                      $"가시 {spikes.Count} / 링 {rings.Count} / 톱니 {saws.Count} / 바람 {winds.Count} / " +
                      $"간식 {treats.Count} / 튕김판 {bouncers.Count} / 밧줄 {vines.Count}");
        }

        /// 경로 위에 아직 안 쓴 발판을 고르게 나눠 준다.
        /// 요청한 만큼 구간을 잘라 그 구간의 가운데부터 가까운 순으로 빈 칸을 찾는다.
        sealed class Slots
        {
            readonly int n;
            readonly HashSet<int> used = new HashSet<int>();

            public Slots(int n) { this.n = n; }

            public HashSet<int> Take(int count)
            {
                var got = new HashSet<int>();
                if (count <= 0) return got;

                int lo = Mathf.Max(1, Mathf.RoundToInt(n * FirstFraction));
                int hi = Mathf.Min(n - 1, Mathf.RoundToInt(n * LastFraction));
                float span = Mathf.Max(1, hi - lo);

                for (int k = 0; k < count; k++)
                {
                    int want = lo + Mathf.RoundToInt(span * (k + 0.5f) / count);
                    int pick = Nearest(want, lo, hi);
                    if (pick < 0) break;    // 빈 칸이 동났다
                    used.Add(pick); got.Add(pick);
                }
                return got;
            }

            /// want 에서 좌우로 번갈아 벌어지며 빈 칸을 찾는다
            int Nearest(int want, int lo, int hi)
            {
                for (int d = 0; d <= hi - lo; d++)
                {
                    int a = want - d, b = want + d;
                    if (a >= lo && a <= hi && !used.Contains(a)) return a;
                    if (b >= lo && b <= hi && !used.Contains(b)) return b;
                }
                return -1;
            }
        }
    }
}
