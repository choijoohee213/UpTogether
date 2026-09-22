using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// StageGenerator(원본 JS 이식본)를 돌려 StageData 에셋으로 굽는다.
    /// 구운 뒤에는 인스펙터에서 손으로 고쳐도 된다 — 다시 구우면 덮어쓰니 주의.
    public static class StageBaker
    {
        const string Dir = "Assets/_Game/Stages";

        [MenuItem("UpTogether/Bake Stages")]
        public static void BakeAll()
        {
            var log = new StringBuilder("스테이지 베이크\n");
            int totalBad = 0;

            for (int i = 0; i < StageGenerator.Stages.Length; i++)
            {
                var cfg = StageGenerator.Stages[i];
                var boxes = StageGenerator.Generate(cfg);

                var bad = StageGenerator.Validate(boxes, out double tightest);
                totalBad += bad.Count;
                log.AppendLine($"[{cfg.Name}] 발판 {boxes.Count - 1}개 / 위반 {bad.Count}건 / 최대 간격 {tightest:F1}px");
                foreach (var v in bad) log.AppendLine($"    #{v.Index}: {v.Message}");

                Write($"{Dir}/Stage{i + 1}.asset", cfg, boxes);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (totalBad > 0) Debug.LogError(log.ToString());
            else Debug.Log(log.ToString());
        }

        static void Write(string path, StageGenerator.Config cfg, List<StageGenerator.Box> boxes)
        {
            var so = AssetDatabase.LoadAssetAtPath<StageData>(path);
            bool isNew = so == null;
            if (isNew) so = ScriptableObject.CreateInstance<StageData>();

            double H = StageGenerator.StageHeight(cfg.Rows);
            // px(아래쪽 양수) → units(위쪽 양수)
            float U(double px) => (float)(px / Px.PPU);
            float UY(double py) => (float)((H - py) / Px.PPU);

            var plats = new List<StageData.Platform>();
            var movers = new List<StageData.Mover>();
            foreach (var b in boxes)
            {
                if (b.IsMover)
                    movers.Add(new StageData.Mover {
                        x = U(b.X), y = UY(b.Y), width = U(b.W),
                        range = U(b.Range), speed = (float)b.Speed, phase = (float)b.Phase });
                else
                    plats.Add(new StageData.Platform { x = U(b.X), y = UY(b.Y), width = U(b.W) });
            }

            var top = StageGenerator.Highest(boxes);

            so.displayName = cfg.Name;
            so.seed = cfg.Seed;
            so.groundY = UY(H - 40);
            so.height = UY(0);
            so.platforms = plats.ToArray();
            so.movers = movers.ToArray();
            so.goal = new Vector2(U(top.X + top.W / 2), UY(top.Y - 30));

            if (isNew) AssetDatabase.CreateAsset(so, path);
            else EditorUtility.SetDirty(so);
        }
    }
}
