using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 강아지 스프라이트 시트를 잘라서 견종별 CharacterSpriteSet 을 만든다.
    /// 시트는 Art/Dog/dog_sprites_v2/{breed}48_sheet.png, 메타는 같은 폴더의 dog_sprites.json.
    public static class DogArtImporter
    {
        const string SheetDir = "Assets/_Game/Art/Dog/dog_sprites_v2";
        const string OutDir = "Assets/_Game/Art/Dog/Generated";
        const int ExpectedVersion = 2;

        /// ★ JSON 의 ppu(48)를 쓰지 않는다 ★
        /// 48이면 48px 셀이 1유닛이라 강아지 키가 0.83유닛이 되는데,
        /// 발판 세로 간격이 0.68~1.02유닛이라 말이 안 된다.
        /// 135면 40px 몸통이 0.30유닛으로 프로토타입 강아지(~0.28)와 맞고,
        /// 세로 8유닛 화면이 1080px일 때 유닛당 135px이라 픽셀아트가 1:1로 찍힌다.
        const float PixelsPerUnit = 135f;

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

                Slice(sheet, breed, meta, pivot);
                int n = BuildSet(sheet, breed, log);
                log.AppendLine($"  [완료] {breed}: 프레임 {n}개");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(log.ToString());
        }

        /// 텍스처 임포트 설정 + 13칸 슬라이스.
        static void Slice(string path, string breed, Meta meta, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;          // 픽셀아트 — 뭉개지면 안 된다
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;

            // 구 API(importer.spritesheet)는 deprecated라 데이터 프로바이더를 쓴다
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var rects = new SpriteRect[meta.columns];
            for (int i = 0; i < meta.columns; i++)
            {
                rects[i] = new SpriteRect
                {
                    name = $"{breed}_{i}",
                    spriteID = StableId(breed, i),
                    rect = new Rect(i * meta.cellWidth, 0, meta.cellWidth, meta.cellHeight),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                };
            }
            provider.SetSpriteRects(rects);

            // 이름↔파일ID 표를 같이 넣어야 다시 임포트해도 참조가 유지된다
            var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameIds != null)
            {
                var pairs = new List<SpriteNameFileIdPair>();
                foreach (var r in rects) pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
                nameIds.SetNameFileIdPairs(pairs);
            }

            provider.Apply();
            importer.SaveAndReimport();
        }

        /// 이름에서 항상 같은 GUID 를 만든다.
        /// GUID.Generate() 를 쓰면 임포트할 때마다 spriteID 가 바뀌어
        /// 견종마다 .meta 에 diff 가 생긴다 (참조는 internalID 라 안 깨지지만 소음이 된다).
        static GUID StableId(string breed, int index)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"UpTogether/dog/{breed}/{index}"));
            var hex = new StringBuilder(32);
            foreach (var b in hash) hex.Append(b.ToString("x2"));
            return new GUID(hex.ToString());
        }

        /// 잘린 스프라이트를 프레임 순서대로 모아 CharacterSpriteSet 으로 굽는다.
        static int BuildSet(string sheet, string breed, StringBuilder log)
        {
            var byName = new Dictionary<string, Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(sheet))
                if (o is Sprite s) byName[s.name] = s;

            var clips = Clips();
            int max = 0;
            foreach (var c in clips) foreach (var f in c.frames) if (f > max) max = f;

            var frames = new Sprite[max + 1];
            for (int i = 0; i <= max; i++)
            {
                if (byName.TryGetValue($"{breed}_{i}", out var s)) frames[i] = s;
                else log.AppendLine($"    경고: {breed}_{i} 스프라이트가 없습니다");
            }

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
