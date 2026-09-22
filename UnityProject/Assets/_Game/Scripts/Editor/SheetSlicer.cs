using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 가로 한 줄짜리 스프라이트 시트를 칸 단위로 자른다. 강아지와 주인공이 같이 쓴다.
    public static class SheetSlicer
    {
        /// 텍스처 임포트 설정 + 슬라이스. 스프라이트 이름은 "{namePrefix}_{i}".
        /// idKey 는 GUID 해시의 입력이라 이름과 분리해 둔다 —
        /// 이름 규칙을 바꿔도 이미 커밋된 .meta 의 spriteID 가 흔들리지 않게 하기 위해서다.
        public static void Slice(string path, string namePrefix, string idKey,
                                 int cellW, int cellH, int columns,
                                 Vector2 pivot, float pixelsPerUnit)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;          // 픽셀아트 — 뭉개지면 안 된다
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;

            // 구 API(importer.spritesheet)는 deprecated라 데이터 프로바이더를 쓴다
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var rects = new SpriteRect[columns];
            for (int i = 0; i < columns; i++)
            {
                rects[i] = new SpriteRect
                {
                    name = $"{namePrefix}_{i}",
                    spriteID = StableId(idKey, i),
                    rect = new Rect(i * cellW, 0, cellW, cellH),
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

        /// 잘린 스프라이트를 인덱스 순서대로 모은다.
        public static Sprite[] Collect(string sheetPath, string namePrefix, int count, StringBuilder log)
        {
            var byName = new Dictionary<string, Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
                if (o is Sprite s) byName[s.name] = s;

            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                if (byName.TryGetValue($"{namePrefix}_{i}", out var s)) frames[i] = s;
                else log?.AppendLine($"    경고: {namePrefix}_{i} 스프라이트가 없습니다");
            }
            return frames;
        }

        /// 이름에서 항상 같은 GUID 를 만든다.
        /// GUID.Generate() 를 쓰면 임포트할 때마다 spriteID 가 바뀌어 .meta 에 diff 가 생긴다
        /// (참조는 internalID 라 안 깨지지만 소음이 된다).
        public static GUID StableId(string idKey, int index)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"UpTogether/{idKey}/{index}"));
            var hex = new StringBuilder(32);
            foreach (var b in hash) hex.Append(b.ToString("x2"));
            return new GUID(hex.ToString());
        }
    }
}
