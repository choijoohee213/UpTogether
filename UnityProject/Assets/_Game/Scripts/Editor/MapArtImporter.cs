using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 숲 테마 배경(3겹)과 16px 타일셋을 올바른 설정으로 임포트한다.
    /// 배경: 단일 스프라이트, 타일링을 위해 FullRect + Repeat.
    /// 타일셋: 16x16 격자 32칸(위-왼쪽부터 0번), Point/무압축/PPU 16.
    public static class MapArtImporter
    {
        const string Dir = "Assets/_Game/Art/Map";

        [MenuItem("UpTogether/Import Map Art")]
        public static void Run()
        {
            // 배경 3겹 — 화면을 덮는 단일 스프라이트. 가로 타일링을 위해 FullRect.
            Background($"{Dir}/bg0_sky.png", 20f);   // 하늘(불투명, 전체)
            Background($"{Dir}/bg1_far.png", 40f);   // 먼 숲(위 투명)
            Background($"{Dir}/bg2_mid.png", 40f);   // 가까운 숲(위 투명)

            SliceTileset($"{Dir}/forest_tileset_16px.png");

            AssetDatabase.Refresh();
            Debug.Log("숲 배경·타일셋 임포트 완료.");
        }

        static void Background(string path, float ppu)
        {
            var im = (TextureImporter)AssetImporter.GetAtPath(path);
            im.textureType = TextureImporterType.Sprite;
            im.spriteImportMode = SpriteImportMode.Single;
            im.spritePixelsPerUnit = ppu;
            im.filterMode = FilterMode.Point;
            im.textureCompression = TextureImporterCompression.Uncompressed;
            im.mipmapEnabled = false;
            im.wrapMode = TextureWrapMode.Repeat;   // 가로로 이어붙일 때 이음매 없이

            var st = new TextureImporterSettings();
            im.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;   // Tiled drawMode 에 필요
            st.spriteAlignment = (int)SpriteAlignment.Center;
            im.SetTextureSettings(st);
            im.SaveAndReimport();
        }

        static void SliceTileset(string path)
        {
            const int cell = 16, cols = 8, rows = 4;
            var im = (TextureImporter)AssetImporter.GetAtPath(path);
            im.textureType = TextureImporterType.Sprite;
            im.spriteImportMode = SpriteImportMode.Multiple;
            im.spritePixelsPerUnit = cell;
            im.filterMode = FilterMode.Point;
            im.textureCompression = TextureImporterCompression.Uncompressed;
            im.mipmapEnabled = false;
            im.wrapMode = TextureWrapMode.Clamp;

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(im);
            provider.InitSpriteEditorDataProvider();

            var rects = new List<SpriteRect>();
            var pairs = new List<SpriteNameFileIdPair>();
            for (int i = 0; i < cols * rows; i++)
            {
                int row = i / cols, col = i % cols;
                // 번호표는 위-왼쪽부터. Unity rect 는 아래-왼쪽 원점이라 y 를 뒤집는다.
                int ry = (rows - 1 - row) * cell;
                var r = new SpriteRect
                {
                    name = $"forest_{i}",
                    spriteID = SheetSlicer.StableId("map/forest", i),
                    rect = new Rect(col * cell, ry, cell, cell),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
                rects.Add(r);
                pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
            }
            provider.SetSpriteRects(rects.ToArray());
            var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameIds?.SetNameFileIdPairs(pairs);
            provider.Apply();
            im.SaveAndReimport();
        }
    }
}
