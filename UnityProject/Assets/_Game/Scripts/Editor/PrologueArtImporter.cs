using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 프롤로그 아트 임포트. UpTogether ▸ Import Prologue Art.
    ///
    /// 플레이트는 **품종별로 한 벌씩** 있다 (Art/Prologue/&lt;품종&gt;/prologue_1..5.png).
    /// 고른 강아지로 프롤로그가 돌아가야 해서, 1~4번 컷은 품종마다 다시 그렸고
    /// 5번(언덕과 집)은 강아지가 없어 한 장을 공유한다.
    ///
    /// 한 장이 화면을 채우는 배경이라 자르지 않는다 — Single 스프라이트.
    /// PPU 100(Px.PPU)이면 750x1300 그림이 정확히 7.5x13 유닛이 되고,
    /// 그림 1픽셀 = 게임 1픽셀이 되어 본편과 밀도가 같아진다.
    public static class PrologueArtImporter
    {
        public const string Dir = "Assets/_Game/Art/Prologue";
        public const string SpriteDir = Dir + "/Sprites";
        public const int Count = 5;

        /// 본편 견종 id 와 같아야 한다 (Art/Dog/dog_sprites_v3/dog_sprites.json).
        public static readonly string[] Breeds =
        {
            "shiba", "retriever", "maltese", "bordercollie", "samoyed", "bernese", "poodle",
        };

        [MenuItem("UpTogether/Import Prologue Art")]
        public static void Import()
        {
            int plates = 0;
            foreach (string breed in Breeds)
                for (int i = 1; i <= Count; i++)
                    if (Plate($"{Dir}/{breed}/prologue_{i}.png")) plates++;

            // 품종 없는 공용 컷 (언덕과 집)
            if (Plate($"{Dir}/prologue_5_summit.png")) plates++;

            // 함께 받은 낱장 시트. 아직 쓰는 곳은 없지만 설정은 맞춰 둔다
            // (앞모습 48x48 6칸 / 도약 80x56 2칸 / 집 48x64 2칸, 전부 PPU 48).
            int sheets = 0;
            if (Directory.Exists(SpriteDir))
                foreach (string f in Directory.GetFiles(SpriteDir, "*.png"))
                    if (Sheet(f.Replace('\\', '/'))) sheets++;

            AssetDatabase.Refresh();
            if (plates == 0) Debug.LogWarning($"{Dir} 에 플레이트가 없습니다.");
            else Debug.Log($"프롤로그 플레이트 {plates}장 / 낱장 시트 {sheets}장을 임포트했습니다.");
        }

        static bool Plate(string path)
        {
            if (!File.Exists(path)) return false;
            var im = AssetImporter.GetAtPath(path) as TextureImporter;
            if (im == null) return false;

            im.textureType = TextureImporterType.Sprite;
            im.spriteImportMode = SpriteImportMode.Single;
            im.spritePixelsPerUnit = Px.PPU;
            im.filterMode = FilterMode.Point;          // 픽셀이 뭉개지면 안 된다
            im.textureCompression = TextureImporterCompression.Uncompressed;
            im.mipmapEnabled = false;
            im.alphaIsTransparency = false;            // 불투명한 배경판
            im.maxTextureSize = 2048;                  // 1300 보다 커야 원본이 유지된다
            im.SaveAndReimport();
            return true;
        }

        static bool Sheet(string path)
        {
            var im = AssetImporter.GetAtPath(path) as TextureImporter;
            if (im == null) return false;

            im.textureType = TextureImporterType.Sprite;
            im.spritePixelsPerUnit = 48f;              // 본편 강아지 시트와 같은 기준
            im.filterMode = FilterMode.Point;
            im.textureCompression = TextureImporterCompression.Uncompressed;
            im.mipmapEnabled = false;
            im.alphaIsTransparency = true;             // 낱장은 배경 투명
            im.SaveAndReimport();
            return true;
        }

        /// 한 품종의 플레이트 5장. 없는 칸은 null 로 둔다
        /// (PrologueScene 이 자리표시 판을 깐다).
        /// 5번은 품종 폴더에 없으면 공용 그림으로 떨어진다.
        public static Sprite[] Load(string breed)
        {
            var list = new Sprite[Count];
            for (int i = 0; i < Count; i++)
                list[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/{breed}/prologue_{i + 1}.png");
            if (list[Count - 1] == null)
                list[Count - 1] = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/prologue_5_summit.png");
            return list;
        }

        /// 품종별 플레이트를 전부. 씬에 한꺼번에 담아 두고 런타임에 고른 품종만 쓴다
        /// (프롤로그는 Select 다음이라 그때는 고른 견종을 이미 안다).
        public static List<PrologueScene.BreedPlates> LoadAll()
        {
            var all = new List<PrologueScene.BreedPlates>();
            foreach (string breed in Breeds)
            {
                var plates = Load(breed);
                bool any = false;
                foreach (var s in plates) if (s != null) { any = true; break; }
                if (any) all.Add(new PrologueScene.BreedPlates { breed = breed, plates = plates });
            }
            return all;
        }

        public static bool Any() => LoadAll().Count > 0;
    }
}
