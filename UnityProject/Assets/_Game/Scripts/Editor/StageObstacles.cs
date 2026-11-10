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
            public float updrafts;      // 상승 기류 — 발판 없이 오르는 길
            public float bars;          // 매달려 건너는 가지
            public float rollers;       // 구르는 통나무
            public float rocks;         // 떨어지는 돌 (돌 언덕의 성격)
            public float pendulums;     // 흔들리는 추 (구름 언덕에서 데뷔)
            public float steams;        // 뿜는 증기 (눈 언덕에서 데뷔)
            public float icy;           // 얼음 발판 비율 (눈 언덕은 거의 전부)
            public int bouncers;        // 튕김판 — 바닥 장난감이라 개수 그대로
        }

        static readonly Recipe[] Recipes =
        {
            new Recipe { path = "Assets/_Game/Stages/Stage1.asset",
                vanishers = 0.07f, verticalMovers = 0.04f, spikes = 0.11f, hangingSpikes = 0.04f, vines = 0.11f,
                plainRings = 0.07f, thornyRings = 0.04f, saws = 0.04f, winds = 0.04f, treats = 0.15f,
                updrafts = 0.06f, bars = 0.06f, rollers = 0.04f, bouncers = 1 },
            new Recipe { path = "Assets/_Game/Stages/Stage2.asset",
                vanishers = 0.13f, verticalMovers = 0.09f, spikes = 0.21f, hangingSpikes = 0.09f, vines = 0.13f,
                plainRings = 0.09f, thornyRings = 0.09f, saws = 0.09f, winds = 0.09f, treats = 0.21f,
                updrafts = 0.09f, bars = 0.09f, rollers = 0.07f, pendulums = 0.10f, bouncers = 1 },
            new Recipe { path = "Assets/_Game/Stages/Stage3.asset",
                vanishers = 0.15f, verticalMovers = 0.11f, spikes = 0.23f, hangingSpikes = 0.11f, vines = 0.15f,
                plainRings = 0.07f, thornyRings = 0.09f, saws = 0.11f, winds = 0.11f, treats = 0.22f,
                updrafts = 0.11f, bars = 0.11f, rollers = 0.09f, pendulums = 0.08f, bouncers = 2 },
            new Recipe { path = "Assets/_Game/Stages/Stage4.asset",
                vanishers = 0.15f, verticalMovers = 0.12f, spikes = 0.22f, hangingSpikes = 0.11f, vines = 0.15f,
                plainRings = 0.08f, thornyRings = 0.09f, saws = 0.11f, winds = 0.12f, treats = 0.22f,
                updrafts = 0.12f, bars = 0.12f, rollers = 0.09f, icy = 0.92f,
                pendulums = 0.08f, steams = 0.09f, bouncers = 2 },
            new Recipe { path = "Assets/_Game/Stages/Stage5.asset",
                vanishers = 0.16f, verticalMovers = 0.13f, spikes = 0.24f, hangingSpikes = 0.12f, vines = 0.15f,
                plainRings = 0.07f, thornyRings = 0.10f, saws = 0.12f, winds = 0.12f, treats = 0.22f,
                updrafts = 0.12f, bars = 0.13f, rollers = 0.10f, rocks = 0.40f,
                pendulums = 0.12f, steams = 0.10f, bouncers = 2 },
        };

        /// 맵 폭 (StageGenerator.MapWidth 900px → units)
        const float MapWidthU = 9f;

        /// 바닥(0번)과 골 직전은 건드리지 않는다 — 시작과 도착은 늘 안전하게.
        const float FirstFraction = 0.10f;
        const float LastFraction = 0.94f;

        [MenuItem("UpTogether/Author Stage Obstacles")]
        public static void Author()
        {
            // ★ 항상 먼저 굽는다 ★ 이 배치기는 '갓 구운 발판'을 전제로 한다.
            // 이미 얹힌 에셋에 또 얹으면 변환된 발판을 다시 변환하면서
            // 사라짐·이동 목록을 새로 덮어쓴다 — 그만큼 발판이 증발한다.
            // (실제로 맵 중간에 발판이 비는 사고가 났다.)
            StageBaker.BakeAll();
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
            var updrafts = new List<StageData.Updraft>();
            var bars = new List<StageData.Bar>();
            var rollers = new List<StageData.Roller>();
            var rocks = new List<StageData.Rock>();
            var pendulums = new List<StageData.Pendulum>();
            var steams = new List<StageData.Steam>();

            // 경로를 따라 고르게 흩되, 한 발판이 두 역할을 맡지 않게 한 번 쓰면 뺀다.
            // 풀이 둘이다 — 발판 표면을 차지하는 것(가시·링·발판 변형)과
            // 그 위나 옆 틈에 놓이는 것(밧줄·톱니·간식). 칸은 겹칠 수 있다.
            // ★ 두 풀에 위상차를 준다 ★ 같은 간격 공식을 쓰면 글자 그대로 같은 칸을
            // 고른다. 그래서 표면 풀이 사라짐/이동으로 바꾼 칸을 부가 풀이 또 골랐고,
            // 가지가 전부 '변형 발판 위'로 떨어져 하나도 안 놓였다.
            var surface = new Slots(n, 0.5f);
            var extras = new Slots(n, 0.15f);
            // 간식은 작고 발판 위에 앉기만 한다. 겹침은 어차피 자리 점유가 막으므로
            // 칸까지 다투게 하면 다른 장애물이 늘 때마다 보상이 사라진다
            // (돌을 늘렸더니 돌 언덕 간식이 9개 → 2개가 됐다).
            var treatSlots = new Slots(n, 0.8f);

            // ★ 자리 점유 ★ 칸(슬롯)이 달라도 실제 공간은 겹칠 수 있다.
            // 가지는 발판 위 1.15u 인데 링은 0.55u 에 반지름 0.5 라 서로 파고들고,
            // 기류·톱니는 빈 통로를 훑어 놓느라 다른 장애물을 아예 안 봤다.
            // 놓기 전에 전부 여기에 물어본다.
            var taken = new List<Rect>();
            bool Free(Rect box)
            {
                foreach (var t in taken) if (Bites(t, box)) return false;
                return true;
            }
            bool Claim(Rect box) { if (!Free(box)) return false; taken.Add(box); return true; }
            // 바람은 넓은 영역을 덮는 '지대'라 다른 것과 겹쳐도 된다 — 점유에서 뺀다.
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

            // 톱니·기류가 피해야 할 '딛는 면' 목록. 정적 발판뿐 아니라
            // 이동·사라짐 발판까지 봐야 한다 (박히는 사고가 여기서 났다).
            var solids = new List<(float l, float r, float y)>();
            foreach (var p in keep) solids.Add((p.x, p.Right, p.y));
            foreach (var m in movers)
                solids.Add(m.vertical ? (m.x, m.Right, m.y) : (m.x - m.range, m.Right + m.range, m.y));
            foreach (var v in vanishers) solids.Add((v.x, v.Right, v.y));

            // ── 발판 밑에 매달린 가시 (밑을 스치며 지나갈 때 아프다) ──
            var hungAt = new HashSet<int>();
            foreach (int i in surface.Take(Count(r.hangingSpikes)))
            {
                float sx = P[i].x + P[i].width * 0.25f, sw = P[i].width * 0.50f;
                if (!Claim(new Rect(sx, P[i].y - 0.35f, sw, 0.35f))) continue;
                spikes.Add(new StageData.Spike { x = sx, y = P[i].y, width = sw, down = true });
                hungAt.Add(i);
            }

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
                    if (dup) continue;
                    if (!Claim(new Rect(c.x - 0.15f, c.y, 0.30f, c.h))) continue;
                    vines.Add(new StageData.Vine { x = c.x, y = c.y, height = c.h });
                }
                if (vines.Count >= wantVines) break;
            }

            // ── 매달려 건너는 가지: 발판 위 손 닿는 높이에 ──
            foreach (int i in extras.Take(Count(r.bars)))
            {
                // 사라지거나 움직이는 발판 위에 두면 매달리느라 그 발판을 못 밟는다 —
                // 발판이 하는 일이 통째로 사라진다.
                // 사라지거나 움직이는 발판 위에 두면 매달리느라 그 발판을 못 밟는다 —
                // 발판이 하는 일이 통째로 사라진다. 옆의 고정 발판으로 비킨다.
                int bi = -1;
                for (int dd = 0; dd <= 3 && bi < 0; dd++)
                {
                    if (i - dd >= 1 && Static(i - dd)) bi = i - dd;
                    else if (i + dd <= n - 1 && Static(i + dd)) bi = i + dd;
                }
                if (bi < 0) continue;
                float w = Mathf.Max(0.9f, P[bi].width * 0.9f);
                float bx = C(P[bi]) - w * 0.5f, by = P[bi].y + 1.15f;
                // 매달리면 몸이 아래로 늘어지므로 가지 아래쪽까지 비워야 한다
                if (!Claim(new Rect(bx, by - 0.75f, w, 0.95f))) continue;
                bars.Add(new StageData.Bar { x = bx, y = by, width = w });
            }

            // ── 상승 기류: 아래 발판에서 위 발판으로 이어주는 '길' ──
            // ★ 올라간 끝에 설 자리가 있어야 한다 ★ 빈 통로만 보고 세웠더니
            // 타고 올라가도 위에 아무것도 없어 그냥 떨어졌다 (뜬금없어 보이던 이유).
            const float DraftW = 1.0f;
            foreach (int i in extras.Take(Count(r.updrafts)))
            {
                float y0 = P[i].y + 0.1f;
                float wantX = C(P[i]);
                bool placed = false;

                // 기둥을 세울 x 후보를 경로에서 가까운 순으로 본다
                var xs = new List<float>();
                for (float x = 0.6f; x <= MapWidthU - 0.6f; x += 0.2f) xs.Add(x);
                xs.Sort((a, b) => Mathf.Abs(a - wantX).CompareTo(Mathf.Abs(b - wantX)));

                foreach (float cx in xs)
                {
                    if (Mathf.Abs(cx - wantX) > 2.5f) break;     // 경로에서 멀면 만날 일이 없다

                    // 이 기둥 바로 위에 올라설 발판이 있나
                    int top = -1;
                    for (int j = i + 1; j < n; j++)
                    {
                        float rise = P[j].y - P[i].y;
                        if (rise < 1.0f) continue;
                        if (rise > 3.2f) break;
                        if (cx > P[j].x + 0.15f && cx < P[j].Right - 0.15f) { top = j; break; }
                    }
                    if (top < 0) continue;

                    // ★ 착지 발판 표면에서 딱 끊는다 ★ 조금이라도 더 띄우면 그 발판에
                    // 올라선 뒤에도 계속 떠올라 조작을 뺏는다.
                    float h = P[top].y - y0;
                    // 중간에 걸리는 발판이 없어야 한다
                    bool clear = true;
                    foreach (var f in solids)
                        if (cx + DraftW * 0.5f > f.l && cx - DraftW * 0.5f < f.r &&
                            f.y > y0 + 0.2f && f.y < P[top].y - 0.1f)
                        { clear = false; break; }
                    if (!clear) continue;
                    if (!Claim(new Rect(cx - DraftW * 0.5f, y0, DraftW, h))) continue;

                    updrafts.Add(new StageData.Updraft {
                        x = cx - DraftW * 0.5f, y = y0, width = DraftW, height = h, lift = Px.V(4.2f) });
                    placed = true; break;
                }
            }

            // ── 떨어지는 돌: 발판 아랫면에 매달아 둔다 ──
            // 밑을 지나면 흔들리다 떨어진다. 떨어질 공간이 있어야 보이고,
            // 매달릴 천장(발판)도 있어야 한다.
            foreach (int i in extras.Take(Count(r.rocks)))
            {
                for (int dd = 0; dd <= 5; dd++)
                {
                    int j = PickNear(i, dd, n);
                    if (j < 1 || !Static(j)) continue;
                    float cx = C(P[j]), cy = P[j].y - 0.18f;   // 발판 아랫면

                    // 아래가 바로 막혀 있으면 떨어지는 게 안 보인다
                    bool blocked = false;
                    foreach (var f in solids)
                        if (cx > f.l && cx < f.r && f.y < cy && f.y > cy - 1.0f) { blocked = true; break; }
                    if (blocked) continue;

                    if (!Claim(new Rect(cx - 0.22f, cy - 0.3f, 0.44f, 0.4f))) continue;
                    rocks.Add(new StageData.Rock { x = cx, y = cy });
                    break;
                }
            }

            // ── 흔들리는 추: 천장에 매달아 좌우로 크게 흔든다 ──
            // 흔들리는 호(弧) 전체가 비어 있어야 한다. 발판에 박히면 가려서 안 보인다.
            const float PendRad = 0.26f, PendSwing = 0.6f;   // 라디안
            const float PendLen = 0.85f;
            float pendReach = Mathf.Sin(PendSwing) * PendLen + PendRad;
            foreach (int i in extras.Take(Count(r.pendulums)))
            {
                // 매달 천장을 고르고, 거기서 좌우로 비켜 가며 '흔들릴 자리' 를 찾는다.
                // 발판 한가운데만 보면 빽빽한 맵에서는 거의 못 건다.
                bool placed = false;
                for (int dd = 0; dd <= 7 && !placed; dd++)
                {
                    int j = PickNear(i, dd, n);
                    if (j < 1 || !Static(j)) continue;
                    float ay = P[j].y - 0.1f;
                    float low = ay - PendLen - PendRad;

                    foreach (float off in new[] { 0f, 0.4f, -0.4f, 0.8f, -0.8f })
                    {
                        float ax = C(P[j]) + off;
                        if (ax < P[j].x - 0.1f || ax > P[j].Right + 0.1f) continue;   // 천장 밖
                        if (ax - pendReach < 0.4f || ax + pendReach > MapWidthU - 0.4f) continue;

                        bool blocked = false;
                        foreach (var f in solids)
                            if (ax + pendReach > f.l && ax - pendReach < f.r &&
                                f.y < ay - 0.15f && f.y > low + 0.05f)
                            { blocked = true; break; }
                        if (blocked) continue;
                        if (!Claim(new Rect(ax - pendReach, low, pendReach * 2f, ay - low))) continue;

                        pendulums.Add(new StageData.Pendulum {
                            x = ax, y = ay, length = PendLen, swing = PendSwing,
                            speed = 1.6f, phase = j * 0.9f });
                        placed = true; break;
                    }
                }
            }

            // ── 뿜는 증기: 발판 위에서 주기적으로 솟는다 ──
            // 머리 위로 솟으므로 위쪽이 비어 있어야 하고, 설 자리는 남겨야 한다.
            foreach (int i in surface.Take(Count(r.steams)))
            {
                for (int dd = 0; dd <= 4; dd++)
                {
                    int j = PickNear(i, dd, n);
                    if (j < 1) continue;
                    float w = Mathf.Min(0.5f, P[j].width * 0.4f);
                    if (P[j].width - w < 0.42f) continue;        // 옆에 설 자리가 없다
                    bool left = (j % 2) == 0;
                    float sx = left ? P[j].x : P[j].Right - w;
                    float h = 1.3f;

                    bool blocked = false;
                    foreach (var f in solids)
                        if (sx + w > f.l && sx < f.r && f.y > P[j].y + 0.2f && f.y < P[j].y + h)
                        { blocked = true; break; }
                    if (blocked) continue;

                    if (!Claim(new Rect(sx, P[j].y, w, h))) continue;
                    steams.Add(new StageData.Steam {
                        x = sx, y = P[j].y, width = w, height = h,
                        period = 2.6f, phase = j * 0.7f });
                    break;
                }
            }

            // ── 착지면 위 가시 (가운데 40%, 양 끝으로 피해 밟게) ──
            // ★ 가운데에 두면 안 된다 ★ 발판이 좁아지면서 양옆이 0.22u 밖에 안 남아
            // 어느 쪽으로 내려도 찔렸다. 한쪽 끝에 붙이고 반대쪽을 설 자리로 비운다.
            const float SafeLanding = 0.42f;   // 몸 반지름(0.14)의 세 배쯤
            var spikedAt = new HashSet<int>();
            foreach (int i in surface.Take(Count(r.spikes)))
            {
                float w = P[i].width;
                float sw = Mathf.Min(w * 0.38f, w - SafeLanding);
                if (sw < 0.18f) continue;                  // 너무 좁은 발판엔 두지 않는다
                // ★ 건너갈 쪽은 비워둔다 ★ 다음 발판 방향 끝에 가시를 두면
                // 도움닫기와 도약 지점이 막혀 점프 자체가 안 된다.
                // 방향은 '가장 가까운 윗면' 으로 본다 — 플레이어는 이동 발판도 밟으므로
                // 정적 발판만 보면 검사와 어긋난다.
                float bestUp = float.MaxValue, upC = C(P[Mathf.Min(i + 1, n - 1)]);
                foreach (var f in solids)
                {
                    if (f.y <= P[i].y + 0.05f) continue;
                    if (f.y - P[i].y < bestUp) { bestUp = f.y - P[i].y; upC = (f.l + f.r) * 0.5f; }
                }
                bool goesRight = upC >= C(P[i]);
                float sx = goesRight ? P[i].x : P[i].Right - sw;   // 진행 방향 반대쪽에
                if (!Claim(new Rect(sx, P[i].y, sw, 0.30f))) continue;
                spikes.Add(new StageData.Spike { x = sx, y = P[i].y, width = sw });
                spikedAt.Add(i);
            }

            // ── 톱니: 두 발판 사이 틈을 순찰. 어느 면에도 닿지 않는 자리에만 둔다 ──

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
                        if (!Free(new Rect(sx - SawReach, sy - SawReach, SawReach * 2f, SawReach * 2f))) continue;
                        float d = Mathf.Abs(sx - wantX) + Mathf.Abs(sy - wantY) * 2f;
                        if (d < bestD) { bestD = d; bestX = sx; bestY = sy; }
                    }
                }
                if (bestD < 3.5f &&   // 경로에서 너무 먼 곳에 두면 만날 일이 없다
                    Claim(new Rect(bestX - SawReach, bestY - SawReach, SawReach * 2f, SawReach * 2f)))
                    saws.Add(new StageData.Saw { x = bestX, y = bestY, blade = SawBlade, orbit = SawOrbit, speed = 2.2f });
                else sawSkipped++;
            }

            // ── 구르는 통나무: 발판 위를 왕복 ──
            foreach (int i in surface.Take(Count(r.rollers)))
            {
                // 가운데를 왕복시키면 폭 1.14u 이상이 필요한데 2·3 스테이지 발판은
                // 0.52~0.96u 라 하나도 못 둔다. 가시처럼 한쪽으로 몰고
                // 반대쪽을 설 자리(0.32u)로 비운다.
                const float RollerRad = 0.13f, RollerFree = 0.32f, Margin = 0.05f;
                int ri = -1; float range = 0f;
                for (int dd = 0; dd <= 4 && ri < 0; dd++)
                    foreach (int cand in new[] { i - dd, i + dd })
                    {
                        if (cand < 1 || cand > n - 1) continue;
                        float rr = (P[cand].width - RollerFree - Margin - RollerRad * 2f) * 0.5f;
                        if (rr < 0.12f) continue;
                        ri = cand; range = rr; break;
                    }
                if (ri < 0) continue;

                bool left = (ri % 2) == 0;    // 한쪽으로 몰리지 않게 번갈아
                float half = range + RollerRad;
                float cx = left ? P[ri].x + Margin + half : P[ri].Right - Margin - half;
                if (!Claim(new Rect(cx - half, P[ri].y, half * 2f, RollerRad * 2f))) continue;
                rollers.Add(new StageData.Roller {
                    x = cx, y = P[ri].y, radius = RollerRad, range = range,
                    speed = 1.1f, phase = ri * 0.7f });
            }

            // ── 바람: 세로 틈에서 안쪽으로 민다 ──
            foreach (int i in extras.Take(Count(r.winds)))
            {
                float dir = C(P[i]) > 4.5f ? -1f : 1f;
                winds.Add(new StageData.Wind {
                    x = P[i].x - 0.2f, y = P[i].y + 0.15f,
                    width = P[i].width + 0.4f, height = 1.5f, force = dir * 4f });
            }

            // ── 착지 발판을 감싸는 링 (위로 통과해 올라선다) ──
            foreach (int i in surface.Take(Count(r.plainRings)))
            {
                // 자리가 막히면 근처 발판을 찾는다 — 그냥 포기하면 링이 통째로 사라진다
                for (int dd = 0; dd <= 3; dd++)
                {
                    int j = PickNear(i, dd, n);
                    if (j < 0) continue;
                    float cx = C(P[j]), cy = P[j].y + 0.55f;
                    if (!Claim(new Rect(cx - 0.5f, cy - 0.5f, 1.0f, 1.0f))) continue;
                    rings.Add(new StageData.Ring { x = cx, y = cy, radius = 0.5f, thorny = false });
                    treats.Add(new StageData.Treat { x = cx, y = P[j].y + 0.62f });   // 링과 한 쌍이라 따로 잡지 않는다
                    break;
                }
            }

            // ★ 가시 링은 '가는 길'에서 비켜 둔다 ★
            // 발판 가운데 위에 두면 다음 발판으로 뛰는 궤적이 반드시 테두리를 지난다.
            // (틈에 거는 것도 해봤지만 발판 간격이 최대 0.68u 라 지름 0.84u 가 안 들어간다.)
            // 진행 방향 반대쪽 끝에 걸어, 평범하게 오르면 안 닿고
            // 굳이 반대쪽으로 가야 만나게 한다.
            // 어떤 발판에서 다음 면으로 가는 통로 안인가 (Verify 와 같은 기준)
            bool InTravelCorridor(float cx, float cy)
            {
                foreach (var f in solids)
                {
                    if (f.y < cy - 1.7f || f.y > cy + 0.2f) continue;
                    float fc = (f.l + f.r) * 0.5f;
                    float best = float.MaxValue, nxC = float.NaN;
                    foreach (var t in solids)
                    {
                        if (t.y <= f.y + 0.05f) continue;
                        if (t.y - f.y < best) { best = t.y - f.y; nxC = (t.l + t.r) * 0.5f; }
                    }
                    if (float.IsNaN(nxC)) continue;
                    if (cx > Mathf.Min(fc, nxC) - 0.3f && cx < Mathf.Max(fc, nxC) + 0.3f) return true;
                }
                return false;
            }

            foreach (int i in surface.Take(Count(r.thornyRings)))
            {
                for (int dd = 0; dd <= 4; dd++)
                {
                    int j = PickNear(i, dd, n);
                    if (j < 1) continue;
                    int nx = Mathf.Min(j + 1, n - 1);
                    bool goesRight = C(P[nx]) >= C(P[j]);
                    // 가는 쪽 반대편 가장자리
                    float cx = goesRight ? P[j].x - 0.1f : P[j].Right + 0.1f;
                    float cy = P[j].y + 1.05f;
                    if (cx < 0.5f || cx > MapWidthU - 0.5f) continue;
                    if (InTravelCorridor(cx, cy)) continue;

                    if (!Claim(new Rect(cx - 0.42f, cy - 0.42f, 0.84f, 0.84f))) continue;
                    rings.Add(new StageData.Ring { x = cx, y = cy, radius = 0.42f, thorny = true });
                    break;
                }
            }

            // ── 간식: 착지면 위에. 가시가 난 발판이면 가시를 피해 끝쪽에 둔다 ──
            foreach (int i in treatSlots.Take(Count(r.treats)))
            {
                float ty = P[i].y + 0.32f;
                // 가운데가 기본, 가시가 났으면 끝쪽, 그래도 막히면 반대쪽 끝
                foreach (float f in new[] { 0.5f, 0.15f, 0.85f, 0.3f, 0.7f })
                {
                    float tx = spikedAt.Contains(i) && f == 0.5f ? P[i].x + P[i].width * 0.12f
                                                                 : P[i].x + P[i].width * f;
                    if (!Claim(new Rect(tx - 0.18f, ty - 0.18f, 0.36f, 0.36f))) continue;
                    treats.Add(new StageData.Treat { x = tx, y = ty });
                    break;
                }
            }

            // ── 튕김판: 바닥 근처 '장난감' (주 경로 밖) ──
            for (int k = 0; k < r.bouncers; k++)
                bouncers.Add(new StageData.Bouncer { x = 6.4f - k * 1.4f, y = P[0].y + 0.55f, width = 0.7f });

            // ── 얼음 발판 (눈 언덕의 성격) ──
            // 장애물이 아니라 '땅의 성질' 이라 자리 점유와 무관하다. 고르게 섞는다.
            // 바닥(0번)은 제외 — 시작부터 미끄러지면 조작을 배울 데가 없다.
            int icyWanted = Mathf.RoundToInt(r.icy * keep.Count);
            if (icyWanted > 0)
            {
                var arr = keep.ToArray();
                for (int k = 0; k < icyWanted; k++)
                {
                    int idx = Mathf.RoundToInt((arr.Length - 1) * (k + 0.5f) / icyWanted);
                    if (idx <= 0 || arr[idx].icy) continue;
                    arr[idx].icy = true;
                }
                keep = new List<StageData.Platform>(arr);
            }

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
            s.updrafts = updrafts.ToArray();
            s.bars = bars.ToArray();
            s.rollers = rollers.ToArray();
            s.rocks = rocks.ToArray();
            s.pendulums = pendulums.ToArray();
            s.steams = steams.ToArray();

            if (vines.Count < Count(r.vines) || sawSkipped > 0)
                Debug.Log($"{s.displayName} 배치 부족 — 밧줄 {vines.Count}/{Count(r.vines)} " +
                          $"(성립 가능한 자리 {vineCand.Count}곳) / 톱니 못 놓음 {sawSkipped}");
            Verify(s);
            EditorUtility.SetDirty(s);
            Debug.Log($"{s.displayName} 장애물: 발판 {keep.Count} / 사라짐 {vanishers.Count} / 이동 {movers.Count} / " +
                      $"가시 {spikes.Count} / 링 {rings.Count} / 톱니 {saws.Count} / 바람 {winds.Count} / " +
                      $"간식 {treats.Count} / 튕김판 {bouncers.Count} / 밧줄 {vines.Count} / " +
                      $"기류 {updrafts.Count} / 가지 {bars.Count} / 통나무 {rollers.Count} / " +
                      $"얼음 {keep.FindAll(p => p.icy).Count} / 돌 {rocks.Count} / " +
                      $"추 {pendulums.Count} / 증기 {steams.Count}");
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

            // ── 서로 겹치는 것이 없는지 전수 확인 ──
            // 칸(슬롯)이 달라도 공간은 겹칠 수 있다. 실제로 기류가 가지·링을 뚫고,
            // 간식이 가시 위에 놓이는 일이 있었다. 바람은 넓은 '지대'라 뺀다.
            var boxes = new List<(string name, Rect r)>();
            foreach (var u in s.updrafts) boxes.Add(("기류", new Rect(u.x, u.y, u.width, u.height)));
            foreach (var b in s.bars) boxes.Add(("가지", new Rect(b.x, b.y - 0.1f, b.width, 0.2f)));
            foreach (var v in s.vines) boxes.Add(("밧줄", new Rect(v.x - 0.08f, v.y, 0.16f, v.height)));
            foreach (var w in s.saws)
            { float rr = w.blade + w.orbit; boxes.Add(("톱니", new Rect(w.x - rr, w.y - rr, rr * 2f, rr * 2f))); }
            foreach (var o in s.rollers)
                boxes.Add(("통나무", new Rect(o.x - o.range - o.radius, o.y,
                                            (o.range + o.radius) * 2f, o.radius * 2f)));
            foreach (var g in s.rings) boxes.Add(("링", new Rect(g.x - g.radius, g.y - g.radius, g.radius * 2f, g.radius * 2f)));
            foreach (var sp in s.spikes)
                boxes.Add((sp.down ? "매달린가시" : "가시",
                           sp.down ? new Rect(sp.x, sp.y - 0.3f, sp.width, 0.3f)
                                   : new Rect(sp.x, sp.y, sp.width, 0.3f)));
            foreach (var t in s.treats) boxes.Add(("간식", new Rect(t.x - 0.16f, t.y - 0.16f, 0.32f, 0.32f)));

            for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    // 링 가운데 간식은 의도된 한 쌍이다
                    if (boxes[i].name == "링" && boxes[j].name == "간식") continue;
                    if (boxes[j].name == "링" && boxes[i].name == "간식") continue;
                    if (!Bites(boxes[i].r, boxes[j].r)) continue;
                    bad.Add($"{boxes[i].name}({boxes[i].r.x:F2},{boxes[i].r.y:F2})와 " +
                            $"{boxes[j].name}({boxes[j].r.x:F2},{boxes[j].r.y:F2})가 겹침");
                }

            // ── 말이 되는 배치인가 ──
            // 겹치지만 않으면 되는 게 아니다. 올라설 자리가 없거나, 올라서는 것만으로
            // 찔리거나, 타고 올라가도 갈 데가 없으면 플레이가 안 된다.
            const float Body = 0.28f, Head = 0.5f;
            (float l, float r, float y, string kind)? Host(float x, float y)
            {
                foreach (var f in surf)
                    if (Mathf.Abs(f.y - y) < 0.03f && x > f.l && x < f.r) return f;
                return null;
            }

            foreach (var sp in s.spikes)
            {
                if (sp.down) continue;
                var h = Host(sp.x + sp.width * 0.5f, sp.y);
                if (h == null) continue;
                float safe = Mathf.Max(sp.x - h.Value.l, h.Value.r - (sp.x + sp.width));
                if (safe < Body)
                    bad.Add($"가시({sp.x:F2},{sp.y:F2}) — 설 자리 {safe:F2}u 뿐이라 무조건 찔린다");
            }

            // 가시 링이 '발판 → 다음 발판' 통로 안에 있으면 뛸 때마다 스친다.
            // 점프가 1.3u 쯤 뜨므로 그 높이 구간을 본다.
            foreach (var g in s.rings)
            {
                if (!g.thorny) continue;
                foreach (var f in surf)
                {
                    if (f.y < g.y - 1.7f || f.y > g.y + 0.2f) continue;
                    float fc = (f.l + f.r) * 0.5f;
                    // 이 발판에서 올라갈 다음 면
                    float best = float.MaxValue, nxC = float.NaN;
                    foreach (var t in surf)
                    {
                        if (t.y <= f.y + 0.05f) continue;
                        if (t.y - f.y < best) { best = t.y - f.y; nxC = (t.l + t.r) * 0.5f; }
                    }
                    if (float.IsNaN(nxC)) continue;
                    float lo = Mathf.Min(fc, nxC) - 0.3f, hi = Mathf.Max(fc, nxC) + 0.3f;
                    if (g.x > lo && g.x < hi)
                    { bad.Add($"가시 링({g.x:F2},{g.y:F2}) — 가는 길 통로 안이라 뛰면 스친다"); break; }
                }
            }

            // 가시가 다음 발판으로 건너갈 쪽 끝을 막으면 도약을 못 한다
            foreach (var sp in s.spikes)
            {
                if (sp.down) continue;
                // 가시를 통째로 품는 면이어야 한다. 가운데 한 점으로 찾으면 같은 높이에
                // 걸친 이동 발판이 잡혀 엉뚱한 여유를 재게 된다.
                (float l, float r, float y, string kind)? h = null;
                foreach (var f in surf)
                    if (Mathf.Abs(f.y - sp.y) < 0.03f &&
                        f.l <= sp.x + 0.01f && f.r >= sp.Right - 0.01f) { h = f; break; }
                if (h == null) continue;
                // 더 높은 이웃 발판이 어느 쪽인지
                float best = float.MaxValue; float nxC = float.NaN;
                foreach (var f in surf)
                {
                    if (f.y <= h.Value.y + 0.05f) continue;
                    float d = f.y - h.Value.y;
                    if (d < best) { best = d; nxC = (f.l + f.r) * 0.5f; }
                }
                if (float.IsNaN(nxC)) continue;
                bool goesRight = nxC >= (h.Value.l + h.Value.r) * 0.5f;
                float exitGap = goesRight ? h.Value.r - (sp.x + sp.width) : sp.x - h.Value.l;
                if (exitGap < Body)
                    bad.Add($"가시({sp.x:F2},{sp.y:F2}) — 건너갈 쪽 끝을 막아 도약할 자리가 {exitGap:F2}u 뿐이다");
            }

            foreach (var b in s.bars)
            {
                var h = Host(b.x + b.width * 0.5f, b.y - 1.15f);
                if (h != null && h.Value.kind != "발판")
                    bad.Add($"가지({b.x:F2},{b.y:F2}) — {h.Value.kind} 위라 그 발판을 못 밟는다");
            }

            foreach (var u in s.updrafts)
            {
                float cx = u.x + u.width * 0.5f;
                bool top = false;
                foreach (var f in surf)
                    if (cx > f.l - 0.3f && cx < f.r + 0.3f &&
                        f.y > u.y + u.height - 1.0f && f.y < u.y + u.height + 1.2f) { top = true; break; }
                if (!top) bad.Add($"기류({cx:F2},{u.y:F2}) — 타고 올라가도 설 자리가 없다");
            }

            foreach (var o in s.rollers)
            {
                var h = Host(o.x, o.y);
                if (h == null) continue;
                float free = Mathf.Max((o.x - o.range - o.radius) - h.Value.l,
                                       h.Value.r - (o.x + o.range + o.radius));
                if (free < Body)
                    bad.Add($"통나무({o.x:F2},{o.y:F2}) — 피할 폭 {free:F2}u 뿐이다");
            }

            foreach (var pd in s.pendulums)
            {
                float reach = Mathf.Sin(pd.swing) * pd.length + 0.26f;
                float low = pd.y - pd.length - 0.26f;
                foreach (var f in surf)
                    if (pd.x + reach > f.l && pd.x - reach < f.r && f.y < pd.y - 0.15f && f.y > low + 0.05f)
                    { bad.Add($"추({pd.x:F2},{pd.y:F2}) — 흔들리는 길에 {f.kind} 이 있다"); break; }
            }

            foreach (var st in s.steams)
            {
                var host = Host(st.x + st.width * 0.5f, st.y);
                if (host == null) { bad.Add($"증기({st.x:F2},{st.y:F2}) — 솟을 바닥이 없다"); continue; }
                float free = Mathf.Max(st.x - host.Value.l, host.Value.r - (st.x + st.width));
                if (free < Body)
                    bad.Add($"증기({st.x:F2},{st.y:F2}) — 피해 설 자리가 {free:F2}u 뿐이다");
            }

            foreach (var rk in s.rocks)
            {
                var ceil = Host(rk.x, rk.y + 0.18f);
                if (ceil == null) bad.Add($"돌({rk.x:F2},{rk.y:F2}) — 매달릴 천장이 없다");
                foreach (var f in surf)
                    if (rk.x > f.l && rk.x < f.r && f.y < rk.y && f.y > rk.y - 1.0f)
                    { bad.Add($"돌({rk.x:F2},{rk.y:F2}) — 바로 아래가 막혀 떨어질 데가 없다"); break; }
            }

            // 발판이 빠진 자리 — 정상 상승폭은 0.68~1.02u 다
            var ys = new List<float>();
            foreach (var f in surf) ys.Add(f.y);
            ys.Sort();
            for (int i = 1; i < ys.Count; i++)
                if (ys[i] - ys[i - 1] > 1.15f)
                    bad.Add($"y {ys[i - 1]:F2} → {ys[i]:F2} 사이에 딛을 면이 없다 ({ys[i] - ys[i - 1]:F2}u)");

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

        /// 경계만 닿는 것과 실제로 파고드는 것을 구분한다.
        /// 발판 위로 뻗는 밧줄과 그 발판 아래로 매달린 가시처럼, 선 하나를 공유하는
        /// 경우가 정상인데 부동소수점 오차로 겹침 판정이 나곤 했다.
        const float Bite = 0.03f;
        static bool Bites(Rect a, Rect b)
        {
            return b.xMax - Bite > a.xMin + Bite && b.xMin + Bite < a.xMax - Bite &&
                   b.yMax - Bite > a.yMin + Bite && b.yMin + Bite < a.yMax - Bite;
        }

        /// i 에서 dd 만큼 떨어진 칸 (왼쪽 먼저, 범위를 벗어나면 -1)
        static int PickNear(int i, int dd, int n)
        {
            if (dd == 0) return i >= 1 && i <= n - 1 ? i : -1;
            if (i - dd >= 1) return i - dd;
            if (i + dd <= n - 1) return i + dd;
            return -1;
        }

        /// 경로 위에 아직 안 쓴 발판을 고르게 나눠 준다.
        /// 요청한 만큼 구간을 잘라 그 구간의 가운데부터 가까운 순으로 빈 칸을 찾는다.
        sealed class Slots
        {
            readonly int n;
            readonly float bias;          // 구간 안에서 어디를 집을지 (풀마다 다르게)
            readonly HashSet<int> used = new HashSet<int>();

            public Slots(int n, float bias) { this.n = n; this.bias = bias; }

            public HashSet<int> Take(int count)
            {
                var got = new HashSet<int>();
                if (count <= 0) return got;

                int lo = Mathf.Max(1, Mathf.RoundToInt(n * FirstFraction));
                int hi = Mathf.Min(n - 1, Mathf.RoundToInt(n * LastFraction));
                float span = Mathf.Max(1, hi - lo);

                for (int k = 0; k < count; k++)
                {
                    int want = lo + Mathf.RoundToInt(span * (k + bias) / count);
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
