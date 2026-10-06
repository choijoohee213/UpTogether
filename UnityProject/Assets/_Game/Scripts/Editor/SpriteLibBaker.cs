using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UpTogether.EditorTools
{
    /// 임포트된 스프라이트 팩을 SpriteLib.asset 으로 모은다.
    public static class SpriteLibBaker
    {
        const string O = "Assets/_Game/Art/Obstacles";
        const string I = "Assets/_Game/Art/Identity";
        const string U = "Assets/_Game/Art/UI";
        const string Path = "Assets/_Game/SpriteLib.asset";

        [MenuItem("UpTogether/Bake Sprite Lib")]
        public static void Bake()
        {
            var lib = AssetDatabase.LoadAssetAtPath<SpriteLib>(Path);
            bool isNew = lib == null;
            if (isNew) lib = ScriptableObject.CreateInstance<SpriteLib>();

            lib.spikeFloor = One($"{O}/spike_floor.png");
            lib.thornVine = Frames($"{O}/thorn_vine_2f.png");
            lib.sawLog = One($"{O}/saw_log.png");
            lib.bounceMushroom = Frames($"{O}/bounce_mushroom_2f.png");
            lib.ringFlower = One($"{O}/ring_flower.png");
            lib.ringThorn = One($"{O}/ring_thorn.png");
            lib.wind = Frames($"{O}/wind_3f.png");

            lib.branchBar = One($"{O}/branch_bar.png");
            lib.pendulumWeight = One($"{O}/pendulum_weight.png");
            lib.pendulumChain = One($"{O}/pendulum_chain.png");
            lib.rockFall = Frames($"{O}/rock_fall_3f.png");
            lib.steam = Frames($"{O}/steam_4f.png");
            lib.wallGrip = One($"{O}/wall_grip.png");
            lib.updraft = Frames($"{O}/updraft_3f.png");
            lib.treatBone = Frames($"{O}/treat_bone_2f.png");
            lib.treatHeart = Frames($"{O}/treat_heart_2f.png");
            lib.treatStar = Frames($"{O}/treat_star_2f.png");
            lib.dustPuff = Frames($"{O}/dust_puff_5f.png");
            lib.warpSparkle = Frames($"{O}/warp_sparkle_4f.png");

            lib.leaf = Frames($"{I}/particle_leaf_4f.png");
            lib.firefly = Frames($"{I}/particle_firefly_3f.png");
            lib.doghouse = Frames($"{I}/goal_doghouse_2f.png");
            lib.emoteHeart = One($"{I}/emote_heart.png");
            lib.emoteExclaim = One($"{I}/emote_exclaim.png");
            lib.emoteQuestion = One($"{I}/emote_question.png");
            lib.emoteNote = One($"{I}/emote_note.png");

            lib.btnLeft = Frames($"{U}/btn_left_2f.png");
            lib.btnRight = Frames($"{U}/btn_right_2f.png");
            lib.btnUp = Frames($"{U}/btn_up_2f.png");
            lib.btnPause = Frames($"{U}/btn_pause_2f.png");
            lib.panelWood = One($"{U}/panel_wood.png");
            lib.gaugeFrame = One($"{U}/gauge_frame.png");
            lib.gaugeFill = One($"{U}/gauge_fill.png");
            lib.heartIcon = Frames($"{U}/heart_icon_3f.png");

            if (isNew) AssetDatabase.CreateAsset(lib, Path);
            else EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();

            int total = new object[] { lib.spikeFloor, lib.sawLog, lib.ringFlower, lib.ringThorn }
                .Count(x => x != null);
            int v3 = new object[] { lib.branchBar, lib.pendulumWeight, lib.pendulumChain, lib.wallGrip }
                .Count(x => x != null);
            Debug.Log($"SpriteLib 구움: 단일 확인 {total}/4, 먼지 {lib.dustPuff?.Length}, 강아지집 {lib.doghouse?.Length}, 버튼L {lib.btnLeft?.Length}\n" +
                      $"  v3 장애물: 단일 {v3}/4, 돌 {lib.rockFall?.Length}/3, 증기 {lib.steam?.Length}/4, 기류 {lib.updraft?.Length}/3");
        }

        static Sprite One(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning($"스프라이트 없음: {path}");
            return s;
        }

        static Sprite[] Frames(string path)
        {
            var list = new List<Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s) list.Add(s);
            if (list.Count == 0) { Debug.LogWarning($"프레임 없음: {path}"); return list.ToArray(); }
            return list.OrderBy(s => Idx(s.name)).ToArray();
        }

        static int Idx(string name)
        {
            int u = name.LastIndexOf('_');
            return u >= 0 && int.TryParse(name.Substring(u + 1), out int n) ? n : 0;
        }
    }
}
