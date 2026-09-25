using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// Stage1 에 장애물을 손으로 얹는다 (자동 생성 위에 배치).
    /// 도달 가능성을 지키려고, 이미 검증된 발판 몇 개를 장애물 발판으로 바꾼다.
    /// 다시 구워도 시드가 같으면 발판 위치가 같아 그대로 맞는다.
    public static class StageObstacles
    {
        const string Path = "Assets/_Game/Stages/Stage1.asset";

        [MenuItem("UpTogether/Author Stage1 Obstacles")]
        public static void Author()
        {
            var s = AssetDatabase.LoadAssetAtPath<StageData>(Path);
            if (s == null) { Debug.LogError($"{Path} 없음. Bake Stages 먼저."); return; }

            // 바닥(0번)을 뺀 발판을 높이순으로 — 아래에서 위로 분포시켜 배치한다
            var body = new List<StageData.Platform>(s.platforms);
            var ground = body[0];
            body.RemoveAt(0);
            var up = body.OrderBy(p => p.y).ToList();   // 낮은 것부터

            // 인덱스를 리스트 크기에 맞춰 고른다
            int n = up.Count;
            int iVanA = Mathf.Clamp(1, 0, n - 1);
            int iSpikeA = Mathf.Clamp(2, 0, n - 1);
            int iMover = Mathf.Clamp(n / 3, 0, n - 1);
            int iVanB = Mathf.Clamp(n / 2, 0, n - 1);
            int iSpikeB = Mathf.Clamp(n * 2 / 3, 0, n - 1);
            int iWind = Mathf.Clamp(n * 3 / 4, 0, n - 1);

            var convert = new HashSet<int> { iVanA, iMover, iVanB };
            var vanishers = new List<StageData.Vanisher>();
            var movers = new List<StageData.Mover>(s.movers ?? new StageData.Mover[0]);
            var spikes = new List<StageData.Spike>();
            var winds = new List<StageData.Wind>();
            var bouncers = new List<StageData.Bouncer>();

            var keep = new List<StageData.Platform> { ground };
            for (int i = 0; i < n; i++)
            {
                var p = up[i];
                if (i == iVanA || i == iVanB)
                {
                    vanishers.Add(new StageData.Vanisher { x = p.x, y = p.y, width = p.width });
                    continue;   // 발판 목록에서 빼고 사라지는 발판으로
                }
                if (i == iMover)
                {
                    // 위아래로 왕복하는 발판으로 변환
                    movers.Add(new StageData.Mover {
                        x = p.x, y = p.y, width = p.width,
                        range = 0.45f, speed = 1.6f, phase = 0f, vertical = true });
                    continue;
                }
                keep.Add(p);

                // 가시: 발판 가운데 45% 에 얹는다 (양 끝으로 피해 밟게)
                if (i == iSpikeA || i == iSpikeB)
                    spikes.Add(new StageData.Spike {
                        x = p.x + p.width * 0.28f, y = p.y, width = p.width * 0.45f });
            }

            // 바람: 한 발판 위 세로 구간에서 가운데로 민다
            {
                var p = up[iWind];
                float dir = p.x > 4.5f ? -1f : 1f;      // 화면 밖으로 안 밀도록 안쪽으로
                winds.Add(new StageData.Wind {
                    x = p.x - 0.2f, y = p.y + 0.2f, width = p.width + 0.4f, height = 1.6f,
                    force = dir * 4f });   // units/s^2 — 걷기로 버틸 수 있는 세기
            }

            // 튕김판: 바닥 근처에 두는 '장난감' 하나 (주 경로 밖, 눌러보게)
            bouncers.Add(new StageData.Bouncer { x = 6.4f, y = ground.y + 0.55f, width = 0.7f });

            s.platforms = keep.ToArray();
            s.movers = movers.ToArray();
            s.vanishers = vanishers.ToArray();
            s.spikes = spikes.ToArray();
            s.winds = winds.ToArray();
            s.bouncers = bouncers.ToArray();

            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            Debug.Log($"Stage1 장애물 배치: 사라짐 {vanishers.Count} / 세로이동 1 / 가시 {spikes.Count} / 바람 {winds.Count} / 튕김판 {bouncers.Count}");
        }
    }
}
