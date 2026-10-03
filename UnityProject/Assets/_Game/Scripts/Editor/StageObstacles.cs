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
        /// 스테이지 하나의 장애물 분량. 발판 수에 대한 비율이다 —
        /// 맵 길이를 바꿔도 밀도(만나는 빈도)가 그대로 유지되도록.
        /// 0.1 이면 발판 10칸에 하나꼴.
        struct Recipe
        {
            public string path;
            public float vanishers;     // 사라지는 발판으로 바꿀 칸
            public float verticalMovers;// 위아래로 움직이게 바꿀 칸
            public float spikes;        // 착지면 위 가시
            public float hangingSpikes; // 발판 밑에 매달린 가시
            public float vines;         // 타고 오르는 밧줄
            public float plainRings;    // 통과하면 친밀도 ↑
            public float thornyRings;   // 테두리에 닿으면 아픔
            public float saws;          // 틈을 순찰하는 톱니
            public float winds;         // 옆으로 미는 바람
            public float treats;        // 간식
            public int bouncers;        // 튕김판 — 바닥 장난감이라 개수 그대로
        }

        static readonly Recipe[] Recipes =
        {
            new Recipe { path = "Assets/_Game/Stages/Stage1.asset",
                vanishers = 0.07f, verticalMovers = 0.04f, spikes = 0.11f, hangingSpikes = 0.04f, vines = 0.11f,
                plainRings = 0.07f, thornyRings = 0.04f, saws = 0.04f, winds = 0.04f, treats = 0.15f, bouncers = 1 },
            new Recipe { path = "Assets/_Game/Stages/Stage2.asset",
                vanishers = 0.13f, verticalMovers = 0.09f, spikes = 0.21f, hangingSpikes = 0.09f, vines = 0.13f,
                plainRings = 0.09f, thornyRings = 0.09f, saws = 0.09f, winds = 0.09f, treats = 0.21f, bouncers = 1 },
            new Recipe { path = "Assets/_Game/Stages/Stage3.asset",
                vanishers = 0.15f, verticalMovers = 0.11f, spikes = 0.23f, hangingSpikes = 0.11f, vines = 0.15f,
                plainRings = 0.07f, thornyRings = 0.09f, saws = 0.11f, winds = 0.11f, treats = 0.22f, bouncers = 2 },
        };

        /// 맵 폭 (StageGenerator.MapWidth 900px → units)
        const float MapWidthU = 9f;

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
            int Count(float density) => Mathf.RoundToInt(density * n);

            // ── 발판 자체를 바꾸는 것 (경로를 유지하도록 제자리에서) ──
            var toVanish = surface.Take(Count(r.vanishers));
            var toMove = surface.Take(Count(r.verticalMovers));
            // 발판이 무엇이 되었는지. 밧줄은 여기 걸면 안 된다 —
            // 사라지거나 움직이면 줄만 공중에 남는다.
            bool Static(int i) => !toVanish.Contains(i) && !toMove.Contains(i);

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
            var spikedAt = surface.Take(Count(r.spikes));
            foreach (int i in spikedAt)
                spikes.Add(new StageData.Spike {
                    x = P[i].x + P[i].width * 0.30f, y = P[i].y, width = P[i].width * 0.40f });

            // ── 발판 밑에 매달린 가시 (밑을 스치며 지나갈 때 아프다) ──
            var hungAt = surface.Take(Count(r.hangingSpikes));
            foreach (int i in hungAt)
                spikes.Add(new StageData.Spike {
                    x = P[i].x + P[i].width * 0.25f, y = P[i].y, width = P[i].width * 0.50f, down = true });

            // ── 타고 오르는 가시덩굴(밧줄): 두 칸 위 발판으로 오르는 지름길 ──
            // ★ 위 발판에 매달린다 ★ — x 를 위 발판 폭 안에서 고른다.
            // 예전엔 아래 발판 중심에 세워서, 발판이 어긋나면 다 올라가 허공에 내렸다.
            // 성립하는 (바닥, 꼭대기) 쌍을 먼저 전부 구한다. 한 칸에서 출발해 훑으면
            // 경로가 이동 발판으로 끊긴 스테이지에서는 거의 못 찾는다.
            var vineCand = new List<(int lo, float x, float y, float h, float reach)>();
            for (int lo = 1; lo < n; lo++)
            {
                if (!Static(lo)) continue;
                for (int hi = lo + 1; hi < n; hi++)
                {
                    float h = P[hi].y - P[lo].y;
                    if (h < 0.45f) continue;
                    if (h > 3.6f) break;                        // 위로 갈수록 더 높아지기만 한다
                    if (!Static(hi)) continue;

                    // 밧줄은 위 발판에 매달린다 — x 는 위 발판 폭 안에서 고른다
                    float inset = Mathf.Min(0.25f, P[hi].width * 0.3f);
                    float x = Mathf.Clamp(C(P[lo]), P[hi].x + inset, P[hi].Right - inset);

                    // 아래 발판에서 손이 닿아야 오를 수 있다. 생성기가 보장하는
                    // 점프 간격이 0.68u 이므로 그 안쪽만 받는다.
                    float reach = Mathf.Max(0f, Mathf.Max(P[lo].x - x, x - P[lo].Right));
                    if (reach > 0.68f) continue;

                    // 꼭대기 발판 밑에 가시가 매달려 있으면 타고 오르다 찔린다
                    if (hungAt.Contains(hi) &&
                        x > P[hi].x + P[hi].width * 0.20f && x < P[hi].x + P[hi].width * 0.80f) continue;

                    vineCand.Add((lo, x, P[lo].y, h, reach));
                    break;                                       // 한 바닥에 하나면 충분
                }
            }

            // 경로를 따라 고르게 고른다
            int wantVines = Count(r.vines);
            // 바닥이 발판 바로 위인 자리(reach≈0)를 먼저 쓰고, 모자랄 때만 떨어진 자리를 쓴다
            var tight = vineCand.FindAll(c => c.reach <= 0.15f);
            foreach (var pool in new[] { tight, vineCand })
            {
                for (int k = 0; k < wantVines && vines.Count < wantVines; k++)
                {
                    if (pool.Count == 0) break;
                    var c = pool[Mathf.RoundToInt((pool.Count - 1) * (k + 0.5f) / wantVines)];
                    bool dup = false;
                    foreach (var v in vines)
                        if (Mathf.Abs(v.y - c.y) < 0.05f && Mathf.Abs(v.x - c.x) < 0.05f) dup = true;
                    if (!dup) vines.Add(new StageData.Vine { x = c.x, y = c.y, height = c.h });
                }
                if (vines.Count >= wantVines) break;
            }

            // ── 착지 발판을 감싸는 링 (위로 통과해 올라선다) ──
            foreach (int i in surface.Take(Count(r.plainRings)))
            {
                rings.Add(new StageData.Ring { x = C(P[i]), y = P[i].y + 0.55f, radius = 0.5f, thorny = false });
                treats.Add(new StageData.Treat { x = C(P[i]), y = P[i].y + 0.62f });   // 가운데 간식
            }
            foreach (int i in surface.Take(Count(r.thornyRings)))
                rings.Add(new StageData.Ring { x = C(P[i]), y = P[i].y + 0.55f, radius = 0.42f, thorny = true });

            // ── 톱니: 두 발판 사이 틈을 순찰. 어느 면에도 닿지 않는 자리에만 둔다 ──
            // 정적 발판뿐 아니라 이동·사라짐 발판까지 봐야 한다 (박히는 사고가 여기서 났다).
            var solids = new List<(float l, float r, float y)>();
            foreach (var p in keep) solids.Add((p.x, p.Right, p.y));
            foreach (var m in movers)
                solids.Add(m.vertical ? (m.x, m.Right, m.y) : (m.x - m.range, m.Right + m.range, m.y));
            foreach (var v in vanishers) solids.Add((v.x, v.Right, v.y));

            const float SawBlade = 0.2f, SawOrbit = 0.35f;
            const float SawReach = SawBlade + SawOrbit + 0.1f;
            int sawSkipped = 0;
            foreach (int i in extras.Take(Count(r.saws)))
            {
                int j = Mathf.Min(i + 1, n - 1);
                float wantX = (C(P[i]) + C(P[j])) * 0.5f;
                float wantY = Mathf.Lerp(Mathf.Min(P[i].y, P[j].y), Mathf.Max(P[i].y, P[j].y), 0.5f);

                // 발판이 세로로 0.7~1.0u 간격이라, 발판과 가로로 겹치는 x 에서는
                // 어느 높이든 여유 반경에 걸린다. 가로로 빈 자리를 훑어 가장 가까운 곳에 둔다.
                float bestX = 0f, bestY = 0f, bestD = float.MaxValue;
                foreach (float sy in new[] { wantY, wantY + 0.3f, wantY - 0.3f })
                {
                    for (float sx = 0.6f; sx <= MapWidthU - 0.6f; sx += 0.2f)
                    {
                        if (!Clear(solids, sx, sy, SawReach)) continue;
                        float d = Mathf.Abs(sx - wantX) + Mathf.Abs(sy - wantY) * 2f;
                        if (d < bestD) { bestD = d; bestX = sx; bestY = sy; }
                    }
                }
                if (bestD < 3.5f)   // 경로에서 너무 먼 곳에 두면 만날 일이 없다
                    saws.Add(new StageData.Saw { x = bestX, y = bestY, blade = SawBlade, orbit = SawOrbit, speed = 2.2f });
                else sawSkipped++;
            }

            // ── 바람: 세로 틈에서 안쪽으로 민다 ──
            foreach (int i in extras.Take(Count(r.winds)))
            {
                float dir = C(P[i]) > 4.5f ? -1f : 1f;
                winds.Add(new StageData.Wind {
                    x = P[i].x - 0.2f, y = P[i].y + 0.15f,
                    width = P[i].width + 0.4f, height = 1.5f, force = dir * 4f });
            }

            // ── 간식: 착지면 위에. 가시가 난 발판이면 가시를 피해 끝쪽에 둔다 ──
            foreach (int i in extras.Take(Count(r.treats)))
            {
                float tx = spikedAt.Contains(i) ? P[i].x + P[i].width * 0.12f : C(P[i]);
                treats.Add(new StageData.Treat { x = tx, y = P[i].y + 0.32f });
            }

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

            if (vines.Count < Count(r.vines) || sawSkipped > 0)
                Debug.Log($"{s.displayName} 배치 부족 — 밧줄 {vines.Count}/{Count(r.vines)} " +
                          $"(성립 가능한 자리 {vineCand.Count}곳) / 톱니 못 놓음 {sawSkipped}");
            Verify(s);
            EditorUtility.SetDirty(s);
            Debug.Log($"{s.displayName} 장애물: 발판 {keep.Count} / 사라짐 {vanishers.Count} / 이동 {movers.Count} / " +
                      $"가시 {spikes.Count} / 링 {rings.Count} / 톱니 {saws.Count} / 바람 {winds.Count} / " +
                      $"간식 {treats.Count} / 튕김판 {bouncers.Count} / 밧줄 {vines.Count}");
        }



        /// 구워 놓은 결과를 되짚어 본다. 배치 규칙을 고칠 때 조용히 새는 걸 막는다 —
        /// 밧줄이 허공에 뜨거나 톱니가 발판에 박히는 일이 실제로 있었다.
        static void Verify(StageData s)
        {
            // 올라설 수 있는 면 전부 (왼쪽, 오른쪽, y)
            var surf = new List<(float l, float r, float y, string kind)>();
            foreach (var p in s.platforms) surf.Add((p.x, p.Right, p.y, "발판"));
            foreach (var m in s.movers)
                surf.Add(m.vertical ? (m.x, m.Right, m.y, "세로이동")
                                    : (m.x - m.range, m.Right + m.range, m.y, "가로이동"));
            foreach (var v in s.vanishers) surf.Add((v.x, v.Right, v.y, "사라짐"));
            foreach (var b in s.bouncers) surf.Add((b.x, b.Right, b.y, "튕김판"));

            string At(float x, float y)
            {
                foreach (var f in surf)
                    if (Mathf.Abs(f.y - y) < 0.12f && x >= f.l - 0.05f && x <= f.r + 0.05f) return f.kind;
                return null;
            }

            // 그 높이의 면에서 x 까지의 가로 거리 (0이면 바로 위)
            float Near(float x, float y)
            {
                float best = float.MaxValue;
                foreach (var f in surf)
                    if (Mathf.Abs(f.y - y) < 0.12f)
                        best = Mathf.Min(best, Mathf.Max(0f, Mathf.Max(f.l - x, x - f.r)));
                return best;
            }

            var bad = new List<string>();

            foreach (var v in s.vines)
            {
                string topK = At(v.x, v.y + v.height);
                if (topK != "발판") bad.Add($"밧줄 x={v.x:F2} 꼭대기가 {topK ?? "허공"} (매달릴 발판이 없다)");
                if (Near(v.x, v.y) > 0.68f) bad.Add($"밧줄 x={v.x:F2} 아래가 비어 손이 안 닿는다");
            }

            foreach (var w in s.saws)
            {
                float reach = w.blade + w.orbit;
                foreach (var f in surf)
                    if (w.x > f.l - reach && w.x < f.r + reach && Mathf.Abs(w.y - f.y) < reach)
                    { bad.Add($"톱니 ({w.x:F2},{w.y:F2})가 {f.kind}에 박힘"); break; }
            }

            foreach (var t in s.treats)
                foreach (var sp in s.spikes)
                    if (!sp.down && t.x >= sp.x && t.x <= sp.Right && Mathf.Abs(sp.y - (t.y - 0.32f)) < 0.25f)
                    { bad.Add($"간식 ({t.x:F2},{t.y:F2})가 가시 위"); break; }

            // 사라지는 발판이 연달아 있으면 둘 다 없어진 순간 못 건넌다
            var vy = new List<float>();
            foreach (var v in s.vanishers) vy.Add(v.y);
            vy.Sort();
            for (int i = 1; i < vy.Count; i++)
                if (vy[i] - vy[i - 1] < 1.15f)
                    bad.Add($"사라지는 발판이 연달아 있음 (y={vy[i - 1]:F2}, {vy[i]:F2})");

            if (bad.Count > 0)
                Debug.LogWarning($"{s.displayName} 배치 문제 {bad.Count}건\n  " + string.Join("\n  ", bad));
        }

        /// (x,y)가 어느 면에서도 margin 만큼 떨어져 있나. 톱니가 발판에 박히지 않게 쓴다.
        static bool Clear(List<(float l, float r, float y)> solids, float x, float y, float margin)
        {
            foreach (var f in solids)
                if (x > f.l - margin && x < f.r + margin && Mathf.Abs(y - f.y) < margin) return false;
            return true;
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
