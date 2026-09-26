using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// Stage1 에 장애물을 손으로 얹는다. 공중에 뜬금없이 두지 않고,
    /// 발판 순서(경로)를 따라 착지면·발판 밑·건너는 틈에 붙여 배치한다.
    /// 항상 새로 베이크한 뒤 얹으므로 여러 번 실행해도 겹치지 않는다.
    public static class StageObstacles
    {
        const string Path = "Assets/_Game/Stages/Stage1.asset";

        [MenuItem("UpTogether/Author Stage1 Obstacles")]
        public static void Author()
        {
            var s = AssetDatabase.LoadAssetAtPath<StageData>(Path);
            if (s == null) { Debug.LogError($"{Path} 없음. Bake Stages 먼저."); return; }

            // 발판 배열은 생성 순서 = 올라가는 경로다. P[0] 은 바닥.
            var P = new List<StageData.Platform>(s.platforms);
            int n = P.Count;
            float C(StageData.Platform p) => p.x + p.width * 0.5f;
            int Idx(float f) => Mathf.Clamp(Mathf.RoundToInt(f), 1, n - 1);

            var vanishers = new List<StageData.Vanisher>();
            var movers = new List<StageData.Mover>();
            var spikes = new List<StageData.Spike>();
            var winds = new List<StageData.Wind>();
            var bouncers = new List<StageData.Bouncer>();
            var saws = new List<StageData.Saw>();
            var rings = new List<StageData.Ring>();
            var treats = new List<StageData.Treat>();

            // ── 발판 자체를 바꾸는 것(경로를 유지하도록 제자리에서) ──
            int cVanA = Idx(n * 0.40f), cVanB = Idx(n * 0.78f);   // 사라지는 다리
            int cMover = Idx(n * 0.62f);                          // 위아래 이동
            var convert = new HashSet<int> { cVanA, cVanB, cMover };

            var keep = new List<StageData.Platform>();
            for (int i = 0; i < n; i++)
            {
                if (i == cVanA || i == cVanB)
                    vanishers.Add(new StageData.Vanisher { x = P[i].x, y = P[i].y, width = P[i].width });
                else if (i == cMover)
                    movers.Add(new StageData.Mover {
                        x = P[i].x, y = P[i].y, width = P[i].width,
                        range = 0.5f, speed = 1.5f, phase = 0f, vertical = true });
                else
                    keep.Add(P[i]);
            }

            // ── 착지면 위 가시 (가운데 40%, 양 끝으로 피해 밟게) ──
            foreach (int i in new[] { Idx(n * 0.18f), Idx(n * 0.52f), Idx(n * 0.88f) })
                if (!convert.Contains(i))
                    spikes.Add(new StageData.Spike {
                        x = P[i].x + P[i].width * 0.30f, y = P[i].y, width = P[i].width * 0.40f });

            // ── 발판 밑에 매달린 가시 (반쪽만 — 빈 쪽으로 올라타게) ──
            foreach (int i in new[] { Idx(n * 0.30f), Idx(n * 0.70f) })
                if (!convert.Contains(i))
                    spikes.Add(new StageData.Spike {
                        x = P[i].x, y = P[i].y, width = P[i].width * 0.5f, down = true });

            // ── 착지 발판을 감싸는 링 (위로 통과해 올라선다) ──
            int iRingP = Idx(n * 0.24f);   // 평범한 링(+친밀도) — 가운데 간식도 함께
            rings.Add(new StageData.Ring { x = C(P[iRingP]), y = P[iRingP].y + 0.62f, radius = 0.6f, thorny = false });
            treats.Add(new StageData.Treat { x = C(P[iRingP]), y = P[iRingP].y + 0.62f });

            int iRingT = Idx(n * 0.46f);   // 가시 링 — 가운데로 정확히 통과
            rings.Add(new StageData.Ring { x = C(P[iRingT]), y = P[iRingT].y + 0.66f, radius = 0.6f, thorny = true });

            // ── 톱니: 두 발판 사이 틈을 한쪽으로 치우쳐 순찰 (반대쪽으로 지나가게) ──
            int iSaw = Idx(n * 0.58f);
            int j = Mathf.Min(iSaw + 1, n - 1);
            float mx = (C(P[iSaw]) + C(P[j])) * 0.5f;
            float my = Mathf.Max(P[iSaw].y, P[j].y) + 0.25f;
            saws.Add(new StageData.Saw { x = mx + 0.35f, y = my, blade = 0.2f, orbit = 0.35f, speed = 2.2f });

            // ── 바람: 높은 세로 틈에서 안쪽으로 민다 ──
            int iWind = Idx(n * 0.84f);
            float dir = C(P[iWind]) > 4.5f ? -1f : 1f;
            winds.Add(new StageData.Wind {
                x = P[iWind].x - 0.2f, y = P[iWind].y + 0.15f,
                width = P[iWind].width + 0.4f, height = 1.5f, force = dir * 4f });

            // ── 간식: 착지면 위에 두 개 더 (경로에서 자연히 줍게) ──
            foreach (int i in new[] { Idx(n * 0.12f), Idx(n * 0.66f) })
                if (!convert.Contains(i))
                    treats.Add(new StageData.Treat { x = C(P[i]), y = P[i].y + 0.32f });

            // ── 튕김판: 바닥 근처 '장난감' 하나 (주 경로 밖) ──
            bouncers.Add(new StageData.Bouncer { x = 6.4f, y = P[0].y + 0.55f, width = 0.7f });

            s.platforms = keep.ToArray();
            s.movers = movers.ToArray();
            s.vanishers = vanishers.ToArray();
            s.spikes = spikes.ToArray();
            s.winds = winds.ToArray();
            s.bouncers = bouncers.ToArray();
            s.saws = saws.ToArray();
            s.rings = rings.ToArray();
            s.treats = treats.ToArray();

            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            Debug.Log($"Stage1 장애물(경로 기준): 사라짐 {vanishers.Count} / 세로이동 {movers.Count} / " +
                      $"가시 {spikes.Count} / 링 {rings.Count} / 톱니 {saws.Count} / 바람 {winds.Count} / " +
                      $"간식 {treats.Count} / 튕김판 {bouncers.Count}");
        }
    }
}
