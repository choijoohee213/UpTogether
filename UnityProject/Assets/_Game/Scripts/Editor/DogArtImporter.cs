using System.Text;
using UnityEditor;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 강아지 스프라이트 시트를 잘라서 견종별 CharacterSpriteSet 을 만든다.
    /// 시트는 Art/dog_platformer_sprites_v3/sprites/dogs/{breed}48_sheet.png, 메타는 같은 폴더의 dog_sprites.json.
    public static class DogArtImporter
    {
        const string SheetDir = "Assets/_Game/Art/dog_platformer_sprites_v3/sprites/dogs";
        const string OutDir = "Assets/_Game/Art/Dog/Generated";
        const int ExpectedVersion = 2;

        /// ★ JSON 의 ppu(48)를 쓰지 않는다 ★
        /// JSON 이 말하는 규칙("강아지와 주인공이 같은 PPU")은 지키되 절대값만 우리 월드에 맞춘다.
        /// 값과 근거는 SheetSlicer.CharacterPixelsPerUnit 참고.
        const float PixelsPerUnit = SheetSlicer.CharacterPixelsPerUnit;

        /// 출처: dog_sprites.json (v2). JSON 이 바뀌면 여기도 고쳐야 한다.
        /// 아래 Version 검사가 불일치를 잡아준다.
        static SpriteClip[] Clips() => new[]
        {
            new SpriteClip { name = "walk",  frames = new[] { 0, 1, 2, 3 }, durations = new[] { .13f, .13f, .13f, .13f }, loop = true },
            new SpriteClip { name = "idle",  frames = new[] { 4, 5 },       durations = new[] { 1.5f, .15f },             loop = true },
            new SpriteClip { name = "jump",  frames = new[] { 6 },          durations = new[] { 0f },                     loop = false },
            new SpriteClip { name = "fall",  frames = new[] { 7, 8 },       durations = new[] { .1f, .1f },               loop = true },
            new SpriteClip { name = "land",  frames = new[] { 9 },          durations = new[] { .12f },                   loop = false },
            new SpriteClip { name = "held",  frames = new[] { 10 },         durations = new[] { 0f },                     loop = false },
            new SpriteClip { name = "happy", frames = new[] { 11, 12 },     durations = new[] { .12f, .12f },             loop = true },
        };

        [System.Serializable]
        class Meta
        {
            public int version;
            public int cellWidth, cellHeight, columns, rows;
            public float ppu;
            public float[] pivot;
            public string facing;
            public string fileNamePattern;
            public string[] breeds;
        }

        [MenuItem("UpTogether/Import Dog Art")]
        public static void Run()
        {
            string jsonPath = $"{SheetDir}/dog_sprites.json";
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            if (json == null) { Debug.LogError($"{jsonPath} 를 찾을 수 없습니다."); return; }

            var meta = JsonUtility.FromJson<Meta>(json.text);
            if (meta.version != ExpectedVersion)
            {
                Debug.LogError($"시트 메타 버전이 {meta.version} 입니다 (코드는 v{ExpectedVersion} 기준). " +
                               "DogArtImporter.Clips() 의 프레임·지속시간 표를 JSON 과 맞춰야 합니다.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutDir))
                AssetDatabase.CreateFolder("Assets/_Game/Art/Dog", "Generated");

            var pivot = new Vector2(meta.pivot[0], meta.pivot[1]);
            var log = new StringBuilder($"강아지 아트 임포트 (셀 {meta.cellWidth}x{meta.cellHeight}, {meta.columns}칸, PPU {PixelsPerUnit})\n");

            foreach (var breed in meta.breeds)
            {
                string sheet = $"{SheetDir}/{meta.fileNamePattern.Replace("{breed}", breed)}";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(sheet) == null)
                { log.AppendLine($"  [건너뜀] {breed}: {sheet} 없음"); continue; }

                SheetSlicer.Slice(sheet, breed, $"dog/{breed}",
                                  meta.cellWidth, meta.cellHeight, meta.columns, pivot, PixelsPerUnit);
                int n = BuildSet(sheet, breed, log);
                log.AppendLine($"  [완료] {breed}: 프레임 {n}개");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(log.ToString());
        }

        /// 잘린 스프라이트를 프레임 순서대로 모아 CharacterSpriteSet 으로 굽는다.
        static int BuildSet(string sheet, string breed, StringBuilder log)
        {
            var clips = Clips();
            int max = 0;
            foreach (var c in clips) foreach (var f in c.frames) if (f > max) max = f;

            var frames = SheetSlicer.Collect(sheet, breed, max + 1, log);

            string outPath = $"{OutDir}/{breed}.asset";
            var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(outPath);
            bool isNew = set == null;
            if (isNew) set = ScriptableObject.CreateInstance<CharacterSpriteSet>();

            set.displayName = breed;
            set.frames = frames;
            set.clips = clips;

            if (isNew) AssetDatabase.CreateAsset(set, outPath);
            else EditorUtility.SetDirty(set);

            return frames.Length;
        }
    }
}
