using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 숲 스프라이트 팩 v2(장애물·UI·정체성·변주)를 임포트한다.
    /// _Nf 이름은 가로 N프레임 시트로 슬라이스한다.
    public static class SpritePackImporter
    {
        const string Art = "Assets/_Game/Art";
        static readonly float PPU = SheetSlicer.CharacterPixelsPerUnit;   // 타일·캐릭터와 같은 스케일

        [MenuItem("UpTogether/Import Sprite Pack")]
        public static void Run()
        {
            foreach (var p in Pngs($"{Art}/Obstacles")) ImportSprite(p, PPU);
            foreach (var p in Pngs($"{Art}/Identity")) ImportSprite(p, PPU);
            foreach (var p in Pngs($"{Art}/UI")) ImportSprite(p, PPU);

            // 변주 배경 3테마
            foreach (var theme in new[] { "bg_night", "bg_snow", "bg_sunset" })
            {
                MapArtImporter.Background($"{Art}/Variants/{theme}/bg0_sky.png", 20f);
                MapArtImporter.Background($"{Art}/Variants/{theme}/bg1_far.png", 40f);
                MapArtImporter.Background($"{Art}/Variants/{theme}/bg2_mid.png", 40f);
            }
            // 변주 타일셋 (숲과 같은 배열)
            MapArtImporter.SliceTileset($"{Art}/Variants/tileset_snow_16px.png", "map/snow");
            MapArtImporter.SliceTileset($"{Art}/Variants/tileset_stone_16px.png", "map/stone");
            // 변주 장식
            ImportSprite($"{Art}/Variants/deco_big_tree.png", PPU);
            ImportSprite($"{Art}/Variants/deco_sign.png", PPU);
            ImportSprite($"{Art}/Variants/deco_mushroom_cluster.png", PPU);

            AssetDatabase.Refresh();
            Debug.Log("스프라이트 팩 v2 임포트 완료.");
        }

        static IEnumerable<string> Pngs(string dir)
        {
            if (!Directory.Exists(dir)) yield break;
            foreach (var f in Directory.GetFiles(dir, "*.png"))
                yield return f.Replace('\\', '/');
        }

        static int FrameCount(string path)
        {
            var m = Regex.Match(Path.GetFileName(path), @"_(\d+)f\.png$");
            return m.Success ? int.Parse(m.Groups[1].Value) : 1;
        }

        /// 파일별 피벗: 땅에 앉는 것은 하단, 매달린 것은 상단, 나머지는 중앙.
        static Vector2 Pivot(string name)
        {
            name = Path.GetFileNameWithoutExtension(name);
            if (name.StartsWith("thorn_vine")) return new Vector2(0.5f, 1f);   // 위에 매달림
            if (name.StartsWith("spike_floor") || name.StartsWith("bounce_mushroom") ||
                name.StartsWith("dust_puff") || name.StartsWith("goal_doghouse") ||
                name.StartsWith("deco_")) return new Vector2(0.5f, 0f);        // 바닥에 앉음
            return new Vector2(0.5f, 0.5f);
        }

        static void ImportSprite(string path, float ppu)
        {
            var im = (TextureImporter)AssetImporter.GetAtPath(path);
            if (im == null) { Debug.LogWarning($"임포터 없음: {path}"); return; }
            im.textureType = TextureImporterType.Sprite;
            im.spritePixelsPerUnit = ppu;
            im.filterMode = FilterMode.Point;
            im.textureCompression = TextureImporterCompression.Uncompressed;
            im.mipmapEnabled = false;
            im.wrapMode = TextureWrapMode.Clamp;

            var pivot = Pivot(path);
            var st = new TextureImporterSettings();
            im.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = pivot;
            // 나무판은 9-slice 테두리 12px
            if (Path.GetFileNameWithoutExtension(path) == "panel_wood")
                st.spriteBorder = new Vector4(12, 12, 12, 12);
            im.SetTextureSettings(st);

            int frames = FrameCount(path);
            if (frames <= 1)
            {
                im.spriteImportMode = SpriteImportMode.Single;
                im.SaveAndReimport();
                return;
            }

            // 가로 N프레임 시트 슬라이스
            im.spriteImportMode = SpriteImportMode.Multiple;
            im.GetSourceTextureWidthAndHeight(out int w, out int h);
            int cw = w / frames;
            string baseName = Path.GetFileNameWithoutExtension(path);

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(im);
            provider.InitSpriteEditorDataProvider();

            var rects = new List<SpriteRect>();
            var pairs = new List<SpriteNameFileIdPair>();
            for (int i = 0; i < frames; i++)
            {
                var r = new SpriteRect
                {
                    name = $"{baseName}_{i}",
                    spriteID = SheetSlicer.StableId($"pack/{baseName}", i),
                    rect = new Rect(i * cw, 0, cw, h),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                };
                rects.Add(r);
                pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(pairs);
            provider.Apply();
            im.SaveAndReimport();
        }
    }
}
