using System.Text;
using UnityEditor;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 주인공 스프라이트 시트를 잘라서 캐릭터별 CharacterSpriteSet 을 만든다.
    /// 시트는 Art/Player/hero_sprites_v3/{character}64_sheet.png, 메타는 같은 폴더의 hero_sprites.json.
    public static class PlayerArtImporter
    {
        const string SheetDir = "Assets/_Game/Art/Player/hero_sprites_v3";
        const string OutDir = "Assets/_Game/Art/Player/Generated";

        /// ★ JSON 의 ppu(48)를 쓰지 않는다 ★
        /// 값과 근거는 SheetSlicer.CharacterPixelsPerUnit 참고.
        const float PixelsPerUnit = SheetSlicer.CharacterPixelsPerUnit;

        /// ★ JSON 의 pivot y(3/64)를 쓰지 않는다 ★
        /// 8캐릭터 전부 접지 프레임의 바닥 여백이 2px 이다 (강아지는 3px 이라 JSON 과 맞았다).
        /// 3 을 그대로 쓰면 피벗이 발바닥보다 1px 위라 발이 발판에 박힌다.
        /// 픽셀아트라 1px 이 화면 1px 이라 눈에 띈다.
        const float FeetPivotY = 2f / 64f;

        [System.Serializable]
        class Meta
        {
            public int cellWidth, cellHeight, columns;
            public float ppu;
            public float[] pivot;
            public string facing;
            public string fileNamePattern;
            public string[] characters;
        }

        /// 출처: hero_sprites.json. JSON 이 바뀌면 여기도 고쳐야 한다.
        static SpriteClip[] Clips() => new[]
        {
            new SpriteClip { name = "walk",  frames = new[] { 0, 1, 2, 3 }, durations = new[] { .13f, .13f, .13f, .13f }, loop = true },
            new SpriteClip { name = "idle",  frames = new[] { 4, 5 },       durations = new[] { 1.5f, .15f },             loop = true },
            new SpriteClip { name = "jump",  frames = new[] { 6 },          durations = new[] { 0f },                     loop = false },
            new SpriteClip { name = "fall",  frames = new[] { 7, 8 },       durations = new[] { .1f, .1f },               loop = true },
            new SpriteClip { name = "land",  frames = new[] { 9 },          durations = new[] { .12f },                   loop = false },
            new SpriteClip { name = "climb", frames = new[] { 10, 11 },     durations = new[] { .18f, .18f },             loop = true },
            new SpriteClip
            {
                name = "hold_idle", frames = new[] { 12 }, overlay = new[] { 15 },
                durations = new[] { 0f }, loop = false,
                dogFrame = 10, dogPivotOffsetPx = new Vector2(10.5f, -3f),
            },
            new SpriteClip
            {
                name = "hold_fall", frames = new[] { 13, 14 }, overlay = new[] { 16, 17 },
                durations = new[] { .1f, .1f }, loop = true,
                dogFrame = 10, dogPivotOffsetPx = new Vector2(10.5f, -1f),
            },
            // 포옹(친밀도 연출). 게임 조작에는 안 쓰고 타이틀 화면에서 쓴다.
            // 강아지는 happy(11,12)를 마주보게 뒤집어 붙인다 (TitleHug 가 직접 처리).
            new SpriteClip
            {
                name = "hug", frames = new[] { 18, 19 }, overlay = new[] { 20, 21 },
                durations = new[] { .42f, .42f }, loop = true,
                dogFrame = 11, dogPivotOffsetPx = new Vector2(28.5f, -7f),
            },
        };

        [MenuItem("UpTogether/Import Player Art")]
        public static void Run()
        {
            string jsonPath = $"{SheetDir}/hero_sprites.json";
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            if (json == null) { Debug.LogError($"{jsonPath} 를 찾을 수 없습니다."); return; }

            var meta = JsonUtility.FromJson<Meta>(json.text);
            if (meta.columns != 22 || meta.cellWidth != 64)
            {
                Debug.LogError($"시트 규격이 {meta.cellWidth}px x {meta.columns}칸 입니다 (코드는 64px x 22칸 기준). " +
                               "PlayerArtImporter.Clips() 의 프레임 표를 JSON 과 맞춰야 합니다.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutDir))
                AssetDatabase.CreateFolder("Assets/_Game/Art/Player", "Generated");

            var pivot = new Vector2(meta.pivot[0], FeetPivotY);
            var log = new StringBuilder(
                $"주인공 아트 임포트 (셀 {meta.cellWidth}x{meta.cellHeight}, {meta.columns}칸, PPU {PixelsPerUnit})\n");
            if (!Mathf.Approximately(meta.pivot[1], FeetPivotY))
                log.AppendLine($"  참고: 피벗 y 를 JSON 의 {meta.pivot[1]:F4} 대신 측정값 {FeetPivotY:F4} " +
                               "(바닥 여백 2px)로 씁니다. 그대로 쓰면 발이 1px 박힙니다.");

            var clips = Clips();
            int max = 0;
            foreach (var c in clips)
            {
                foreach (var f in c.frames) if (f > max) max = f;
                if (c.overlay != null) foreach (var f in c.overlay) if (f > max) max = f;
            }

            foreach (var id in meta.characters)
            {
                string sheet = $"{SheetDir}/{meta.fileNamePattern.Replace("{character}", id)}";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(sheet) == null)
                { log.AppendLine($"  [건너뜀] {id}: {sheet} 없음"); continue; }

                SheetSlicer.Slice(sheet, id, $"hero/{id}",
                                  meta.cellWidth, meta.cellHeight, meta.columns, pivot, PixelsPerUnit);

                var frames = SheetSlicer.Collect(sheet, id, max + 1, log);

                string outPath = $"{OutDir}/{id}.asset";
                var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(outPath);
                bool isNew = set == null;
                if (isNew) set = ScriptableObject.CreateInstance<CharacterSpriteSet>();

                set.displayName = id;
                set.frames = frames;
                set.clips = clips;

                if (isNew) AssetDatabase.CreateAsset(set, outPath);
                else EditorUtility.SetDirty(set);

                log.AppendLine($"  [완료] {id}: 프레임 {frames.Length}개");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(log.ToString());
        }
    }
}
